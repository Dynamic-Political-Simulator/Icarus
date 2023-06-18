using System.ComponentModel.DataAnnotations;

namespace Icarus.Context.Models
{
    public class CharacterTokenType
    {
        [Key]
        public string TokenTypeName { get; set; }
    }
}