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
