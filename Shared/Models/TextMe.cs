using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Oqtane.Models;

namespace GIBS.Module.TextMe.Models
{
    [Table("GIBSTextMe")]
    public class TextMe : ModelBase
    {
        [Key]
        public int TextMeId { get; set; }
        public int ModuleId { get; set; }
        public string Name { get; set; }
    }
}
