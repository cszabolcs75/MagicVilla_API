﻿using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;

namespace MagicVilla_VillaAPI.Repository
{
    public class VillaRepository : Repository<Villa>, IVillaRepository
    {
        private ApplicationDbContext _dbContext;

        public VillaRepository(ApplicationDbContext dbContext): base(dbContext)
        {
            _dbContext = dbContext;
        }

     

        public async Task<Villa> UpdateAsync(Villa entity)
        {
            entity.UpdatedDate = DateTime.Now;
            _dbContext.Villas.Update(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }
    }
}
