using Icarus.Context;
using Icarus.Context.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class GoIService
    {
        public async Task<string> AddGoI(string name)
        {
            using var db = new IcarusContext();

            var alreadyExists = db.GroupOfInterests.SingleOrDefault(g => g.Name.ToLower() == name.ToLower());

            if (alreadyExists != null)
            {
                return "GoI already exists.";
            }

            var newGoi = new GroupOfInterest { Name = name };

            db.GroupOfInterests.Add(newGoi);
            await db.SaveChangesAsync();

            return $"New GoI added with name {name}.";
        }

        public async Task<List<GroupOfInterest>> GetCharacterGoIList()
        {
            using var db = new IcarusContext();

            var gois = await db.GroupOfInterests.Include(goi => goi.Characters).ToListAsync();

            return gois;
        }

        public async Task<List<GroupOfInterest>> GetAllGroups()
        {
            using var db = new IcarusContext();

            return await db.GroupOfInterests.ToListAsync();
        }
    }
}
