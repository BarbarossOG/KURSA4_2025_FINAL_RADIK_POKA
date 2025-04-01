using System.Text.Json.Serialization;

namespace KURSA4_2025_FINAL_RADIK_POKA.Models
{
    public class Chapter
    {
        public int Id { get; set; }
        public int ObjectId { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }

        // Навигационные свойства
        [JsonIgnore]
        public Models.Object Object { get; set; }

        [JsonIgnore]
        public ICollection<Subchapter> Subchapters { get; set; } = new List<Subchapter>();
    }
}
