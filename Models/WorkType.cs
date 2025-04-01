using System.Text.Json.Serialization;

namespace KURSA4_2025_FINAL_RADIK_POKA.Models
{
    public class WorkType
    {
        public int Id { get; set; }
        public int SubchapterId { get; set; }
        public string EI { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }

        // Навигационные свойства
        [JsonIgnore]
        public Subchapter Subchapter { get; set; }

        [JsonIgnore]
        public ICollection<WorkPlan> WorkPlans { get; set; } = new List<WorkPlan>();
    }
}
