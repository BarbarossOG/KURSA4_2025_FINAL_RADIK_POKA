namespace KURSA4_2025_FINAL_RADIK_POKA.Models
{
    public class Subchapter
    {
        public int Id { get; set; }
        public int ChapterId { get; set; } // Внешний ключ
        public string Name { get; set; }
        public int Number { get; set; }
    }
}
