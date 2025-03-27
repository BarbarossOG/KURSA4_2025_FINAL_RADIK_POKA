namespace KURSA4_2025_FINAL_RADIK_POKA.Models
{
    public class Section
    {
        public int Id { get; set; }
        public int ObjectId { get; set; } // Внешний ключ
        public int Number { get; set; }
        public string Name { get; set; }
    }
}
