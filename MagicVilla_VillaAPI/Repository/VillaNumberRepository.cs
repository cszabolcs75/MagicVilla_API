﻿using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;

namespace MagicVilla_VillaAPI.Repository
{
    public class VillaNumberRepository : Repository<VillaNumber>, IVillaNumberRepository
    {
        private ApplicationDbContext _dbContext;

        public VillaNumberRepository(ApplicationDbContext dbContext): base(dbContext)
        {
            _dbContext = dbContext;
        }

     

        public async Task<VillaNumber> UpdateAsync(VillaNumber entity)
        {
            entity.UpdatedDated = DateTime.Now;
            _dbContext.VillaNumbers.Update(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }
    }
}
