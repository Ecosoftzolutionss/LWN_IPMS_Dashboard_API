using Microsoft.EntityFrameworkCore;
using static LWN_IPMS_Dashboard.Entities;

namespace LWN_IPMS_Dashboard
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<ItemMaster> Items => Set<ItemMaster>();
        public DbSet<GrnLine> GrnLines => Set<GrnLine>();
        public DbSet<GrnPallet> GrnPallets => Set<GrnPallet>();
        public DbSet<MaterialIssue> MaterialIssues => Set<MaterialIssue>();
        public DbSet<StoreMovement> StoreMovements => Set<StoreMovement>();
        public DbSet<RackRow> RackRows => Set<RackRow>();
        public DbSet<RackColumn> RackColumns => Set<RackColumn>();
        public DbSet<LocationRack> LocationRacks => Set<LocationRack>();
        public DbSet<PalletTypeMaster> PalletTypes => Set<PalletTypeMaster>();
        public DbSet<DashboardSetting> DashboardSettings => Set<DashboardSetting>();
        public DbSet<DashboardPartConfig> DashboardPartConfigs => Set<DashboardPartConfig>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<ItemMaster>().ToTable("ITEM_MASTER");
            b.Entity<GrnLine>().ToTable("GRN_LINE");
            b.Entity<GrnPallet>().ToTable("GRN_PALLET");
            b.Entity<MaterialIssue>().ToTable("MATERIAL_ISSUE");
            b.Entity<StoreMovement>().ToTable("STORE_MOVEMENT");
            b.Entity<RackRow>().ToTable("RACK_ROW");
            b.Entity<RackColumn>().ToTable("RACK_COLUMN");
            b.Entity<LocationRack>().ToTable("LOCATION_RACK");
            b.Entity<PalletTypeMaster>().ToTable("PALLET_TYPE_MASTER");

            // Column names match the tables exactly, so no HasColumnName is needed.
            b.Entity<DashboardSetting>().ToTable("FIFO_DashboardSettings");
            b.Entity<DashboardPartConfig>().ToTable("FIFO_DashboardPartConfiguration");
        }
    }
}