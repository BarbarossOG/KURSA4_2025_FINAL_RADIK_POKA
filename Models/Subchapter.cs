using System.Text.Json.Serialization;

namespace KURSA4_2025_FINAL_RADIK_POKA.Models
{
    public class Subchapter
    {
        public int Id { get; set; }
        public int ChapterId { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }

        // Навигационные свойства
        [JsonIgnore]
        public Chapter Chapter { get; set; }
        [JsonIgnore]
        public ICollection<WorkType> WorkTypes { get; set; } = new List<WorkType>();
    }
}
