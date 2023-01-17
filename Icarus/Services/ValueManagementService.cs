using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Icarus.Context;
using Icarus.Context.Models;

namespace Icarus.Services
{
    public class ValueManagementService
    {
        private readonly IcarusContext _icarusContext;
        public List<Value> Values;

        public ValueManagementService(IcarusContext context)
        {
            _icarusContext = context;
            Values = _icarusContext.Values.ToList();

            Values.First().Id = 0;

        }

    }
}
