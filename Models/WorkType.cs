namespace KURSA4_2025_FINAL_RADIK_POKA.Models
{
    public class WorkType
    {
        public int Id { get; set; }
        public int SubchapterId { get; set; } // Внешний ключ
        public string EI { get; set; } 
        public string Name { get; set; }
        public int Number { get; set; }
    }
}
