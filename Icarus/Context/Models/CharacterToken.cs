
using System.ComponentModel.DataAnnotations;

namespace Icarus.Context.Models
{
    public class CharacterToken
    {
        public string PlayerCharacterId { get; set; }
        public virtual PlayerCharacter Character { get; set; }
        public string TokenTypeId { get; set; }
        public CharacterTokenType TokenType { get; set; }
        [Required]
        public int Amount { get; set; } = 0;

    }
}
