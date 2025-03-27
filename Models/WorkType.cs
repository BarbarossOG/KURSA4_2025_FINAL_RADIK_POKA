namespace KURSA4_2025_FINAL_RADIK_POKA.Models
{
    public class WorkType
    {
        public int Id { get; set; }
        public int SubchapterId { get; set; } // Внешний ключ
        public int EIId { get; set; } // Внешний ключ
        public string Name { get; set; }
        public int Number { get; set; }
        public int Value { get; set; }
        public int Norm { get; set; }
        public int Count { get; set; }
        public string Comment { get; set; }
    }
}
