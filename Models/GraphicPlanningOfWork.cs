namespace KURSA4_2025_FINAL_RADIK_POKA.Models
{
    public class GraphicPlanningOfWork
    {
        public int Id { get; set; } // Первичный ключ (автоинкремент)
        public int ObjectId { get; set; } // Внешний ключ
        public DateTime CreationDate { get; set; } = DateTime.Now; // Автоматически
        public string Status { get; set; } = "Редактируется"; // По умолчанию
        public int Version { get; set; } // Версия (инкрементируется для каждого объекта)

        // Навигационное свойство
        public Models.Object Object { get; set; }
    }
}
