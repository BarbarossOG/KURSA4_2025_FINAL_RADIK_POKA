
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
            builder.Services.AddScoped<ReportService>();

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
        }
    }
}
