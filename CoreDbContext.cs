using Microsoft.EntityFrameworkCore;
using SoapyBackend.Data;

namespace SoapyBackend
{
    public class CoreDbContext : DbContext
    {
        public DbSet<DeviceData> Devices { get; set; }
        public DbSet<ProgramData> Programs { get; set; }
        public DbSet<TriggerData> Triggers { get; set; }
        public DbSet<UserData> Users { get; set; }

        public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var user = modelBuilder.Entity<UserData>();
            user.HasKey(x => new {x.Aud, x.DeviceId});
        }
    }
}