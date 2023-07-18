using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Context.Models
{
    public class CharacterCurrency
    {
        public string PlayerCharacterId { get; set; }
        public virtual PlayerCharacter PlayerCharacter { get; set; }
        public int CurrencyId { get; set; }
        public virtual Currency Currency { get; set; }
        public float Amount { get; set; }
    }
}
