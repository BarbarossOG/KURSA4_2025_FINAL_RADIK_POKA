namespace KURSA4_2025_FINAL_RADIK_POKA.Models
{
    public class WorkType
{
    public int Id { get; set; }
    public int SubchapterId { get; set; }
    public int EIId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public int Value { get; set; }
    public int Norm { get; set; }
    public int Count { get; set; }
    public string Comment { get; set; } = string.Empty;
    
    // Навигационное свойство для WorkPlan
    // public List<WorkPlan> WorkPlans { get; set; } = new();
}
}
