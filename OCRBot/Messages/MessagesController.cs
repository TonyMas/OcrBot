using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Http;

namespace OCRBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<Message> Post([FromBody]Message message)
        {
            if (message.Type == "Message")
            {
                var userData = new UserData(message.From);

                int attachCount = message.Attachments.Count;
                if (attachCount != 0)
                {
                    ProcessAttachaments(message, userData);
                }

                if (message.Text.Equals( "Settings", StringComparison.InvariantCultureIgnoreCase ) )
                {
                    return talkAboutSettings(message, userData);
                }
                else if( message.Text.Equals("Info", StringComparison.InvariantCultureIgnoreCase) )
                {
                    return showUserInfo(message, userData);
                }
                else if (message.Text.Equals("Help", StringComparison.InvariantCultureIgnoreCase))
                {
                    return showHelpMessage(message,false);
                }
                else
                {
                    Message replyMessage = message.CreateReplyMessage();

                    if (tryProcessSerial(message, userData, replyMessage) ||
                        tryProcessPromo(message, userData, replyMessage) ||
                        tryProcessMaster(message, userData, replyMessage) )
                    {
                        return replyMessage;
                    }
                    return showHelpMessage(message, true);
                }
            }
            else
            {
                return HandleSystemMessage(message);
            }
        }

        private bool tryProcessMaster(Message message, UserData userData, Message replyMessage)
        {
            if(message.Text.Equals(ConfigurationManager.AppSettings["MasterKey"], StringComparison.CurrentCulture))
            {
                if( userData.MasterMode )
                {
                    replyMessage.Text = $"You are already in master mode. What can I do for you.";
                } else
                {
                    replyMessage.Text = $"Congrats! You are now in master mode! What can I do for you.";
                }
                return true;
            }
            return false;
        }

        private bool tryProcessPromo(Message message, UserData userData, Message replyMessage)
        {
            return false;
        }

        private bool tryProcessSerial(Message message, UserData userData, Message replyMessage)
        {
            return false;
        }

        private Message showUserInfo(Message message, UserData userData)
        {
            return message.CreateReplyMessage(userData.UserInfoString());
        }

        private Message talkAboutSettings(Message message, UserData userData)
        {
            return message.CreateReplyMessage($"Recognition settings will be here");
        }

        private void ProcessAttachaments(Message message, UserData userData)
        {
            System.Threading.Timer timer = null;
            timer = new System.Threading.Timer((obj) =>
            {
                var messageSender = new ReplyMessageSender(message);
                var recognizer = new Recognizer(messageSender,userData);
                recognizer.RecognizeAttachments(message.Attachments);
                timer.Dispose();
            }, null, 1, System.Threading.Timeout.Infinite);
        }

        private Message showHelpMessage(Message message, bool replyToUnknown)
        {
            string unknownText = $"Sorry, I cannot understand you.";
            string helpText = $"I am simple OCR bot. Your can send me image files and I send back recognition results. Type **Settings** to view and change current recognition settings. Type **Info** to see how many pages for recognition your have. Type **Help** to see this message again.";

            if(replyToUnknown)
            {
                return message.CreateReplyMessage(unknownText + $"\r\n" + helpText);
            }
            return message.CreateReplyMessage(helpText);
        }
        
        private Message HandleSystemMessage(Message message)
        {
            if (message.Type == "Ping")
            {
                Message reply = message.CreateReplyMessage();
                reply.Type = "Ping";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == "BotAddedToConversation")
            {
                return showHelpMessage(message, false);
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
            }
            else if (message.Type == "UserAddedToConversation")
            {
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
            }

            return null;
        }
    }
}