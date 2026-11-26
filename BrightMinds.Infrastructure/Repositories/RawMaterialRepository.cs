using Blendd.Infrastructure.DataAccess;
using BrightMinds.Core.Interfaces;
using BrightMinds.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightMinds.Infrastructure.Repositories
{
    public class RawMaterialRepository : GenericRepository<RawMaterial>, IRawMaterialRepository
    {
        private readonly AppDbContext _context;

        public RawMaterialRepository(AppDbContext context) : base(context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<bool> Exists(int id)
        {
            return await _context.RawMaterials.AnyAsync(rawMaterial => rawMaterial.Id == id);
        }
    }
}
