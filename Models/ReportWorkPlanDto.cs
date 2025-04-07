namespace KURSA4_2025_FINAL_RADIK_POKA.Models
{
    public class ReportWorkPlanDto
    {
        public int Id { get; set; }
        public string Type { get; set; } // Chapter, Subchapter, WorkType
        public string Name { get; set; }
        public string? EI { get; set; } 
        public Dictionary<string, int> WeeklyValues { get; set; } = new();
        public int Level { get; set; } // Уровень вложенности для отступа
    }
}
