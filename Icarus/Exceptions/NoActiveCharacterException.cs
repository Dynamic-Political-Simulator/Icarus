using Icarus.Context.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Exceptions
{
    public class NoActiveCharacterException : Exception
    {
        public NoActiveCharacterException() { }


        public NoActiveCharacterException(string discordId) 
            : base(String.Format("Could not find active character for: {0}", discordId)) { }
    }
}
