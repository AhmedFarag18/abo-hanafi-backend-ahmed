using BrightMinds.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Blendd.Infrastructure.DataAccess
{
    public partial class AppDbContext
    {
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
