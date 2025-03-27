
using KURSA4_2025_FINAL_RADIK_POKA.Data;
using KURSA4_2025_FINAL_RADIK_POKA.Models;
using KURSA4_2025_FINAL_RADIK_POKA.Services;
using Microsoft.EntityFrameworkCore;

namespace KURSA4_2025_FINAL_RADIK_POKA
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ���������� ��������
            builder.Services.AddControllers();

            // ����������� ��������� ��
            builder.Services.AddDbContext<PlanningContext>(options =>
                options.UseInMemoryDatabase("PlanningDatabase"));

            // ����������� ������ �������
            builder.Services.AddScoped<PlanningService>();

            // �������� Swagger, ���� �����
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            


            // ������������ HTTP pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<PlanningContext>();
                SeedTestData(context);
            }

            void SeedTestData(PlanningContext context)
            {
                if (!context.Chapters.Any())
                {
                    var testChapter1 = new Chapter 
                    { 
                        ObjectId = 1,
                        Name = "Проектирование", 
                        Number = 1 
                    };
                    
                    var testChapter2 = new Chapter 
                    { 
                        ObjectId = 2, 
                        Name = "Строительство", 
                        Number = 2 
                    };

                    context.Chapters.AddRange(testChapter1, testChapter2);
                    
                    var testSubchapters = new List<Subchapter>
                    {
                        new() { ChapterId = testChapter1.Id, Name = "Разработка чертежей", Number = 1 },
                        new() { ChapterId = testChapter1.Id, Name = "Согласование документации", Number = 2 },
                        new() { ChapterId = testChapter2.Id, Name = "Земляные работы", Number = 1 },
                        new() { ChapterId = testChapter2.Id, Name = "Монтаж конструкций", Number = 2 }
                    };

                    context.Subchapters.AddRange(testSubchapters);
                    context.SaveChanges();
                }
            }
        }
    }
}
