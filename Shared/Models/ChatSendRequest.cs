using System.ComponentModel.DataAnnotations;

namespace GIBS.Module.TextMe.Models
{
    public class ChatSendRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(20)]
        public string VisitorPhoneNumber { get; set; }

        [Required]
        [MaxLength(1600)]
        public string Message { get; set; }

        // TODO: Re-add [Required] when client-side conversation id creation is guaranteed for all anonymous/new sessions.
        [MaxLength(64)]
        public string ConversationId { get; set; }
    }
}
