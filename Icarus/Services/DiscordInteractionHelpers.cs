using Discord;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class DiscordInteractionHelpers
    {
        public SelectMenuBuilder CreateSelectMenu(string ModifierName, string Stage, List<string> Options, string Placeholder = "Select Option")
        {
            Debug.WriteLine("Creating Dropdown!");
            SelectMenuBuilder builder = new SelectMenuBuilder()
                .WithPlaceholder(Placeholder)
                .WithCustomId(GenerateCompoundId(ModifierName, Stage));

            foreach (string option in Options)
            {
                builder.AddOption(option, option);
            }
            Debug.WriteLine("Returning Select Menu");
            return builder;
        }

        public Dictionary<string, string> ReadCompoundId(string Id)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            List<string> data = Id.Split('_').ToList();
            result.Add("modifier", data[0]);
            if (data.Count < 2)
            {
                return result;
            }
            result.Add("stage", data[1]);
            //If the stage is "amount" the compound key will also contain info about the type of value
            //if (data[1] == "Amount")
            //{
            //    result.Add("value", data[2]);
            //}

            return result;
        }

        public string GenerateCompoundId(string modifier, string stage, string value = "")
        {
            string id = modifier + "_" + stage;
            return id;
        }
    }
}
