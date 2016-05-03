using Microsoft.Bot.Connector;

namespace OCRBot
{
    public class ReplyMessageSender
    {
        public ReplyMessageSender(ConnectorClient _connector, Message _replyTo)
        {
            connector = _connector;
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