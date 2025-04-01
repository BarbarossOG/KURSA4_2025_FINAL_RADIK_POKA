using Microsoft.EntityFrameworkCore;
using KURSA4_2025_FINAL_RADIK_POKA.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KURSA4_2025_FINAL_RADIK_POKA.Data
{
    public class PlanningContext : DbContext
    {
        public PlanningContext(DbContextOptions<PlanningContext> options) : base(options) { }

        // DbSet свойства
        public DbSet<Models.Object> Objects { get; set; }
        public DbSet<GraphicPlanningOfWork> GraphicPlanningsOfWork { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Subchapter> Subchapters { get; set; }
        public DbSet<WorkType> WorkTypes { get; set; }
        public DbSet<WorkPlan> WorkPlans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Настройка связей
            ConfigureRelations(modelBuilder);
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseInMemoryDatabase("PlanningDatabase")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        }

        private void ConfigureRelations(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Chapter>()
                .HasOne(c => c.Object)
                .WithMany()
                .HasForeignKey(c => c.ObjectId);

            modelBuilder.Entity<Subchapter>()
                .HasOne(s => s.Chapter)
                .WithMany(c => c.Subchapters)
                .HasForeignKey(s => s.ChapterId);

            modelBuilder.Entity<WorkType>()
                .HasOne(w => w.Subchapter)
                .WithMany(s => s.WorkTypes)
                .HasForeignKey(w => w.SubchapterId);

            modelBuilder.Entity<WorkPlan>()
                .HasOne(wp => wp.WorkType)
                .WithMany(w => w.WorkPlans)
                .HasForeignKey(wp => wp.WorkTypeId);

            modelBuilder.Entity<GraphicPlanningOfWork>()
                .HasOne(g => g.Object)
                .WithMany()
                .HasForeignKey(g => g.ObjectId);

        }
    }
}