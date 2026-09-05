using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Oqtane.Models;

namespace GIBS.Module.TextMe.Models
{
    [Table("GIBSTextMe_Messages")]
    public class TextMessage : ModelBase
    {
        [Key]
        public int MessageId { get; set; }

        public int ModuleId { get; set; }

        [MaxLength(64)]
        public string TwilioMessageSid { get; set; }

        [MaxLength(64)]
        public string ConversationId { get; set; }

        [Required]
        [MaxLength(10)]
        public string Direction { get; set; }

        [Required]
        [MaxLength(20)]
        public string SenderNumber { get; set; }

        [Required]
        [MaxLength(20)]
        public string RecipientNumber { get; set; }

        public string Body { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; }

        [MaxLength(10)]
        public string ErrorCode { get; set; }

        public List<TextMedia> MediaItems { get; set; } = new();
    }
}
