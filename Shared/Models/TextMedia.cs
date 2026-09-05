using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GIBS.Module.TextMe.Models
{
    [Table("GIBSTextMe_Media")]
    public class TextMedia
    {
        [Key]
        public int MediaId { get; set; }

        public int MessageId { get; set; }

        [ForeignKey(nameof(MessageId))]
        public TextMessage Message { get; set; }

        [Required]
        [MaxLength(500)]
        public string MediaUrl { get; set; }

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
