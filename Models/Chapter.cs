using KURSA4_2025_FINAL_RADIK_POKA.Models;
using System.Text.Json.Serialization;

public class Chapter
{
    public int Id { get; set; }
    public int PlanId { get; set; } // Заменяем ObjectId на PlanId
    public string Name { get; set; }
    public int Number { get; set; }

    // Навигационные свойства
    [JsonIgnore]
    public GraphicPlanningOfWork Plan { get; set; } // Заменяем Object на Plan

    [JsonIgnore]
    public ICollection<Subchapter> Subchapters { get; set; } = new List<Subchapter>();
}
