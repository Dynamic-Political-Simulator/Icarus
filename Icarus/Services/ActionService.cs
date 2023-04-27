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
        private readonly IcarusConfig Configuration;

        public ActionService(IcarusConfig config)
        {
            Configuration = config;
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
