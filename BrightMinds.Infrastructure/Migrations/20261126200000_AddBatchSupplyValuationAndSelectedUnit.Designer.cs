using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Blendd.Infrastructure.DataAccess;

#nullable disable

namespace BrightMinds.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261126200000_AddBatchSupplyValuationAndSelectedUnit")]
    partial class AddBatchSupplyValuationAndSelectedUnit
    {
    }
}
