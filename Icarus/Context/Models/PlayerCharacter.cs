using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Icarus.Context.Models
{
    public class PlayerCharacter
    {
        [Key]
        public string CharacterId { get; set; } = Guid.NewGuid().ToString();
        public string CharacterName { get; set; }

        public int YearOfBirth { get; set; }
        public int YearOfDeath { get; set; } = -1;

        public List<CharacterToken> Tokens { get; set; }
    }
}
