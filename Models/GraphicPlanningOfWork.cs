namespace KURSA4_2025_FINAL_RADIK_POKA.Models
{
  
    public class GraphicPlanningOfWork
    {
        public int Id { get; set; }
        public int ObjectId { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.Now; 
        public int Version { get; set; }
        public string Status { get; set; } = "Редактируется";

        // Навигационные свойства
        public Models.Object Object { get; set; }
    }
}
