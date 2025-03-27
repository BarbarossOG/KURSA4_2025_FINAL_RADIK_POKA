using Microsoft.EntityFrameworkCore;
using KURSA4_2025_FINAL_RADIK_POKA.Models;

namespace KURSA4_2025_FINAL_RADIK_POKA.Data
{
    public class PlanningContext : DbContext
    {
        public PlanningContext(DbContextOptions<PlanningContext> options) : base(options) { }

        // DbSet свойства
        public DbSet<Models.Object> Objects { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Floor> Floors { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Subchapter> Subchapters { get; set; }
        public DbSet<WorkType> WorkTypes { get; set; }
        public DbSet<WorkPlan> WorkPlans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Настройка связей
            ConfigureRelations(modelBuilder);
        }

        private void ConfigureRelations(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Room>()
                .HasOne<Models.Object>()
                .WithMany()
                .HasForeignKey(r => r.ObjectId);

            modelBuilder.Entity<Floor>()
                .HasOne<Models.Object>()
                .WithMany()
                .HasForeignKey(f => f.ObjectId);

            modelBuilder.Entity<Section>()
                .HasOne<Models.Object>()
                .WithMany()
                .HasForeignKey(s => s.ObjectId);

            modelBuilder.Entity<Chapter>()
                .HasOne<Models.Object>()
                .WithMany()
                .HasForeignKey(c => c.ObjectId);

            modelBuilder.Entity<Subchapter>()
                .HasOne<Chapter>()
                .WithMany()
                .HasForeignKey(s => s.ChapterId);

            modelBuilder.Entity<WorkType>()
                .HasOne<Subchapter>()
                .WithMany()
                .HasForeignKey(w => w.SubchapterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkPlan>()
                .HasOne<WorkType>()
                .WithMany()
                .HasForeignKey(w => w.WorkTypeId);

        }
    }
}