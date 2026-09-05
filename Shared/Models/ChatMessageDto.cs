using System;

namespace GIBS.Module.TextMe.Models
{
    public class ChatMessageDto
    {
        public int MessageId { get; set; }
        public string Direction { get; set; }
        public string Body { get; set; }
        public string SenderNumber { get; set; }
        public string RecipientNumber { get; set; }
        public string Status { get; set; }
        public string ConversationId { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
