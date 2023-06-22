using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class EconVisualsService
    {
        private readonly TickService _tickService;
        public EconVisualsService(TickService tickService) 
        {
            _tickService = tickService;
        }


        public async Task UpdateProvinceView()
        {

        }
    }
}
