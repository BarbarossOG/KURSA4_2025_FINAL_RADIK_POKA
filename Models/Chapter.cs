using KURSA4_2025_FINAL_RADIK_POKA.Models;
using System.Text.Json.Serialization;

public class Chapter
{
    public int Id { get; set; }
    public int PlanId { get; set; } 
    public string Name { get; set; }
    public int Number { get; set; }

    // Навигационные свойства
    [JsonIgnore]
    public GraphicPlanningOfWork Plan { get; set; } 

    [JsonIgnore]
    public ICollection<Subchapter> Subchapters { get; set; } = new List<Subchapter>();
}
