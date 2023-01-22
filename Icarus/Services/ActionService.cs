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

        public bool ExampleAction(PlayerCharacter character)
        {
            if (character.Tokens.Single(t => t.TokenType == TokenType.TestToken).Amount >= 5)
            {
                return true;
            }

            return false;
        }
    }

    public enum TokenType
    {
        TestToken = 0,
        OtherTestToken = 1
    }
}
