namespace KURSA4_2025_FINAL_RADIK_POKA.Models
{
    public class Period
    {
        public int Id { get; set; }
        public int PeriodTypeId { get; set; } // Внешний ключ
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
