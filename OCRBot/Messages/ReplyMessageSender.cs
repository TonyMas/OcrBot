using Microsoft.Bot.Connector;
using System;

namespace OCRBot
{
    public class ReplyMessageSender
    {
        public ReplyMessageSender(Message _replyTo)
        {
#if EMULATOR
            connector = new ConnectorClient(new Uri("http://localhost:9000"), new ConnectorClientCredentials());
#else
            connector = new ConnectorClient();
#endif
            replyTo = _replyTo;
        }

        public Message CreateMessage(string text = null)
        {
            return replyTo.CreateReplyMessage(text);
        }

        public void SendMessage(Message message)
        {
            connector.Messages.SendMessage(message);
        }

        public void SendMessage(string text)
        {
            connector.Messages.SendMessage(replyTo.CreateReplyMessage(text));
        }

        private ConnectorClient connector { get; set; }
        private Message replyTo { get; set; }
    }
}