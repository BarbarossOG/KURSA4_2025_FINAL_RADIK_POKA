using System.Text.Json.Serialization;

namespace KURSA4_2025_FINAL_RADIK_POKA.Models
{
    public class WorkPlan
    {
        public int Id { get; set; }
        public int WorkTypeId { get; set; }
        public DateTime Date { get; set; }
        public int Value { get; set; }

        // Навигационные свойства
        [JsonIgnore]
        public WorkType WorkType { get; set; }
    }
}
