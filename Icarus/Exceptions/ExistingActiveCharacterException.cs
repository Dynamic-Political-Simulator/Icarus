using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Exceptions
{
    public class ExistingActiveCharacterException : Exception
    {
        public ExistingActiveCharacterException() { }

        public ExistingActiveCharacterException(string discordId)
            : base(String.Format("{0} already has an active character.", discordId)) { }
    }
}
