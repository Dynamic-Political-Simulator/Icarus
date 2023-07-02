using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Exceptions
{
    public class GroupOfInterestNotFoundException : Exception
    {
        public GroupOfInterestNotFoundException() { }

        public GroupOfInterestNotFoundException(string goi)
            : base(String.Format("Could not find Group of Interest: {0}.", goi)) { }
    }
}
