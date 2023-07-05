using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Dto
{
    public class CharacterWithDebtDto
    {
        public string CharacterName { get; set; }
        public string DiscordId { get; set; }
        public List<FavourDebtLine> FavourDebtLines { get; set; }
    }

    public class FavourDebtLine
    {
        public string FavourName { get; set; }
        public int Amount { get; set; }
    }
}
