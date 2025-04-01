using Microsoft.EntityFrameworkCore;
using KURSA4_2025_FINAL_RADIK_POKA.Models;

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

        private void ConfigureRelations(ModelBuilder modelBuilder)
        {
           
            modelBuilder.Entity<GraphicPlanningOfWork>()
            .HasOne(g => g.Object)
            .WithMany()
            .HasForeignKey(g => g.ObjectId);

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