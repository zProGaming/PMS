using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Models.Core;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Hotel> Hotels => Set<Hotel>();

        public DbSet<Property> Properties => Set<Property>();

        public DbSet<Department> Departments => Set<Department>();

        public DbSet<RoomType> RoomTypes => Set<RoomType>();

        public DbSet<Room> Rooms => Set<Room>();

        public DbSet<Guest> Guests => Set<Guest>();

        public DbSet<Reservation> Reservations => Set<Reservation>();

        public DbSet<Folio> Folios => Set<Folio>();

        public DbSet<FolioItem> FolioItems => Set<FolioItem>();

        public DbSet<Payment> Payments => Set<Payment>();
    }
}
