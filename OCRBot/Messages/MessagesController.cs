using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;
using System;
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
                int attachCount = message.Attachments.Count;
                if (attachCount != 0)
                {
                    ProcessAttachaments(message);
                }

                if (message.Text.Equals( "Settings", StringComparison.InvariantCultureIgnoreCase ) )
                {
                    return talkAboutSettings(message);
                }
                else if( message.Text.Equals("Info", StringComparison.InvariantCultureIgnoreCase) )
                {
                    return showUserInfo(message);
                }
                else if (message.Text.Equals("Help", StringComparison.InvariantCultureIgnoreCase))
                {
                    return showHelpMessage(message,false);
                }
                else
                {
                    Message promoReply = tryProcessPromo(message);
                    if( promoReply.Text.Length > 0 )
                    {
                        return promoReply;
                    }
                    if( message.Text.Length > 0)
                    {
                        return showHelpMessage(message, true);
                    }
                    return null;
                }
            }
            else
            {
                return HandleSystemMessage(message);
            }
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

        private Message tryProcessPromo(Message message)
        {
            return message.CreateReplyMessage(); ;
        }

        private Message showUserInfo(Message message)
        {
            return message.CreateReplyMessage($"User info will be here");
        }

        private Message talkAboutSettings(Message message)
        {
            return message.CreateReplyMessage($"Recognition settings will be here");
        }

        private void ProcessAttachaments(Message message)
        {
            System.Threading.Timer timer = null;
            timer = new System.Threading.Timer((obj) =>
            {
                var messageSender = new ReplyMessageSender(message);
                var recognizer = new Recognizer(messageSender);
                recognizer.RecognizeAttachments(message.Attachments);
                timer.Dispose();
            }, null, 1, System.Threading.Timeout.Infinite);
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