
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

            // пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
            builder.Services.AddControllers();

            // пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ
            builder.Services.AddDbContext<PlanningContext>(options =>
                options.UseInMemoryDatabase("PlanningDatabase"));

            // пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
            builder.Services.AddScoped<PlanningService>();

            // пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Swagger, пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            
            // пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ HTTP pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<PlanningContext>();
                SeedTestData(context);
            }

            void SeedTestData(PlanningContext context)
            {
                // if (!context.Objects.Any())
                // {
                //     context.Objects.AddRange(
                //         new Object { Id = 1, Name = "Объект 1" },
                //         new Object { Id = 2, Name = "Объект 2" }
                //     );
                //     context.SaveChanges();
                // }

                

                if (!context.Chapters.Any())
                {
                    var testChapter1 = new Chapter 
                    { 
                        ObjectId = 1, // Ссылается на существующий Object
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

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
