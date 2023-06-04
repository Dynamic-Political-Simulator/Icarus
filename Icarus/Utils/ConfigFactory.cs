using Castle.Core.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Utils
{
    public class ConfigFactory
    {
        public static IcarusConfig GetConfig()
        {
            var envVariables = Environment.GetEnvironmentVariables();

            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.{envVariables["environment"]}.json", optional: false, reloadOnChange: true);

            var Configuration = configBuilder.Build();

            var icarusConfig = new IcarusConfig();
            Configuration.GetSection("IcarusConfig").Bind(icarusConfig);

            return icarusConfig;
        }
    }
}
