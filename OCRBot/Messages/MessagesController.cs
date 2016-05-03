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

                if( attachCount == 0)
                {
                    return message.CreateReplyMessage($"*Send me some image files to begin recognition*");
                }

                System.Threading.Timer timer = null;
                timer = new System.Threading.Timer((obj) =>
                {
#if EMULATOR
                    var connector = new ConnectorClient(new Uri("http://localhost:9000"), new ConnectorClientCredentials());
#else
                    var connector = new ConnectorClient();
#endif
                    var messageSender = new ReplyMessageSender(connector, message);
                    var recognizer = new Recognizer(messageSender);
                    recognizer.RecognizeAttachments(message.Attachments);
                    timer.Dispose();
                }, null, 1, System.Threading.Timeout.Infinite);

                return message.CreateReplyMessage($"Your files will be processed shortly");
            }
            else
            {
                return HandleSystemMessage(message);
            }
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
                return message.CreateReplyMessage($"Hello! I am OCR Bot, send me some document images.");
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
                return message.CreateReplyMessage($"Bye!");
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