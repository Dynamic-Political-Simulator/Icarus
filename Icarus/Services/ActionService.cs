using Icarus.Context;
using Icarus.Context.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icarus.Services
{
    public class ActionService
    {
        private readonly IConfiguration Configuration;
        private readonly IcarusContext _dbcontext;

        public ActionService(IConfiguration configuration, IcarusContext dbcontext)
        {
            Configuration = configuration;
            _dbcontext = dbcontext;
        }

        public string ExampleAction(PlayerCharacter character)
        {
            return "Example aciton.";
        }
    }

    public enum ActionTokenType
    {
        TestToken = 0,
        OtherTestToken = 1
    }
}
