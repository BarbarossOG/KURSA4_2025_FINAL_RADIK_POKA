using KURSA4_2025_FINAL_RADIK_POKA.Data;
using KURSA4_2025_FINAL_RADIK_POKA.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KURSA4_2025_FINAL_RADIK_POKA.Services
{
    public class ReportService
    {
        private readonly PlanningContext _context;

        public ReportService(PlanningContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateWorkSchedulePdfAsync(int planId, DateTime startDate, DateTime endDate)
        {
            // Получаем данные для отчета и информацию о плане
            var plan = await _context.GraphicPlanningsOfWork
                .Include(p => p.Object)
                .FirstOrDefaultAsync(p => p.Id == planId);

            if (plan == null)
                throw new Exception("План не найден");

            var reportData = await GetReportDataAsync(planId, startDate, endDate);
            var reportDate = DateTime.Now;

            // Генерируем PDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Заголовок с информацией об объекте и плане
                    page.Header()
                        .Column(column =>
                        {
                            // Первая строка - название отчёта и период
                            column.Item()
                                .AlignCenter()
                                .Text($"План-график работ (с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy})")
                                .Bold().FontSize(14);

                            // Вторая строка - информация об объекте
                            column.Item()
                                .PaddingTop(5)
                                .AlignCenter()
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Text($"Объект: {plan.Object.Street}")
                                        .FontSize(10);

                                    row.RelativeItem()
                                        .Text($"Район: {plan.Object.District}")
                                        .FontSize(10);

                                    row.RelativeItem()
                                        .Text($"Статус: {plan.Object.Status}")
                                        .FontSize(10);
                                });

                            // Третья строка - информация о плане
                            column.Item()
                                .PaddingTop(5)
                                .AlignCenter()
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Text($"Версия плана: {plan.Version}")
                                        .FontSize(10);

                                    row.RelativeItem()
                                        .Text($"Статус плана: {plan.Status}")
                                        .FontSize(10);

                                    row.RelativeItem()
                                        .Text($"Дата создания отчета: {reportDate:dd.MM.yyyy HH:mm}")
                                        .FontSize(10);
                                });
                        });

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Table(table =>
                        {
                            // Настройка столбцов
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(2, Unit.Centimetre); // ID
                                columns.RelativeColumn(3); // Название
                                columns.ConstantColumn(2, Unit.Centimetre); // ЕИ

                                // Добавляем столбцы для каждой недели
                                var weeks = GetWeeksBetween(startDate, endDate);
                                foreach (var week in weeks)
                                {
                                    columns.ConstantColumn(1.5f, Unit.Centimetre);
                                }
                            });

                            // Заголовок таблицы
                            table.Header(header =>
                            {
                                header.Cell().Text("ID").Bold();
                                header.Cell().Text("Наименование").Bold();
                                header.Cell().Text("ЕИ").Bold();

                                var weeks = GetWeeksBetween(startDate, endDate);
                                foreach (var week in weeks)
                                {
                                    header.Cell().Text($"{week.Start:dd.MM}\n{week.End:dd.MM}").Bold().FontSize(8);
                                }
                            });

                            // Данные
                            foreach (var item in reportData)
                            {
                                // ID
                                var idCell = table.Cell()
                                    .PaddingVertical(2)
                                    .Text(item.Id.ToString())
                                    .FontSize(item.Type == "WorkType" ? 9 : 10);

                                if (item.Type != "WorkType")
                                {
                                    idCell.Bold();
                                }

                                // Название с отступом
                                var nameCell = table.Cell()
                                    .PaddingLeft(item.Level * 10)
                                    .PaddingVertical(2)
                                    .Text(item.Name)
                                    .FontSize(item.Type == "WorkType" ? 9 : 10);

                                if (item.Type != "WorkType")
                                {
                                    nameCell.Bold();
                                }

                                // ЕИ (только для видов работ)
                                table.Cell()
                                    .PaddingVertical(2)
                                    .Text(item.Type == "WorkType" ? item.EI : "");

                                // Значения по неделям
                                var weeks = GetWeeksBetween(startDate, endDate);
                                foreach (var week in weeks)
                                {
                                    var weekKey = $"{week.Start:yyyy-MM-dd}";
                                    var value = item.WeeklyValues.ContainsKey(weekKey) ? item.WeeklyValues[weekKey] : 0;

                                    table.Cell()
                                        .Background(item.Type == "WorkType" ? Colors.Grey.Lighten3 : Colors.Transparent)
                                        .AlignCenter()
                                        .PaddingVertical(2)
                                        .Text(value > 0 ? value.ToString() : "-");
                                }
                            }
                        });
                });
            });

            return document.GeneratePdf();
        }

        private async Task<List<ReportWorkPlanDto>> GetReportDataAsync(int planId, DateTime startDate, DateTime endDate)
        {
            var result = new List<ReportWorkPlanDto>();
            var weeks = GetWeeksBetween(startDate, endDate);

            // Словари для хранения сумм по неделям для разделов и подразделов
            var chapterWeeklySums = new Dictionary<int, Dictionary<string, int>>();
            var subchapterWeeklySums = new Dictionary<int, Dictionary<string, int>>();

            // Получаем план
            var plan = await _context.GraphicPlanningsOfWork
                .Include(p => p.Object)
                .FirstOrDefaultAsync(p => p.Id == planId);

            if (plan == null) return result;

            // Получаем все главы для плана
            var chapters = await _context.Chapters
                .Where(c => c.PlanId == planId)
                .OrderBy(c => c.Number)
                .ToListAsync();

            foreach (var chapter in chapters)
            {
                // Инициализируем словарь для раздела
                chapterWeeklySums[chapter.Id] = weeks.ToDictionary(
                    w => $"{w.Start:yyyy-MM-dd}",
                    _ => 0);

                // Добавляем главу
                result.Add(new ReportWorkPlanDto
                {
                    Id = chapter.Id,
                    Type = "Chapter",
                    Name = chapter.Name,
                    Level = 0,
                    WeeklyValues = new Dictionary<string, int>() // Пока пустой
                });

                // Получаем подглавы для главы
                var subchapters = await _context.Subchapters
                    .Where(s => s.ChapterId == chapter.Id)
                    .OrderBy(s => s.Number)
                    .ToListAsync();

                foreach (var subchapter in subchapters)
                {
                    // Инициализируем словарь для подраздела
                    subchapterWeeklySums[subchapter.Id] = weeks.ToDictionary(
                        w => $"{w.Start:yyyy-MM-dd}",
                        _ => 0);

                    // Добавляем подглаву
                    result.Add(new ReportWorkPlanDto
                    {
                        Id = subchapter.Id,
                        Type = "Subchapter",
                        Name = subchapter.Name,
                        Level = 1,
                        WeeklyValues = new Dictionary<string, int>() // Пока пустой
                    });

                    // Получаем виды работ для подглавы
                    var workTypes = await _context.WorkTypes
                        .Where(w => w.SubchapterId == subchapter.Id)
                        .OrderBy(w => w.Number)
                        .ToListAsync();

                    foreach (var workType in workTypes)
                    {
                        // Получаем планы работ для вида работ
                        var workPlans = await _context.WorkPlans
                            .Where(wp => wp.WorkTypeId == workType.Id &&
                                        wp.Date >= startDate &&
                                        wp.Date <= endDate)
                            .ToListAsync();

                        // Группируем по неделям
                        var weeklyValues = new Dictionary<string, int>();
                        foreach (var week in weeks)
                        {
                            var weekKey = $"{week.Start:yyyy-MM-dd}";
                            var value = workPlans
                                .Where(wp => wp.Date >= week.Start && wp.Date <= week.End)
                                .Sum(wp => wp.Value);

                            weeklyValues[weekKey] = value;

                            // Суммируем для подраздела
                            if (subchapterWeeklySums.TryGetValue(subchapter.Id, out var subchapterSums))
                            {
                                subchapterSums[weekKey] += value;
                            }

                            // Суммируем для раздела
                            if (chapterWeeklySums.TryGetValue(chapter.Id, out var chapterSums))
                            {
                                chapterSums[weekKey] += value;
                            }
                        }

                        // Добавляем вид работ
                        result.Add(new ReportWorkPlanDto
                        {
                            Id = workType.Id,
                            Type = "WorkType",
                            Name = workType.Name,
                            EI = workType.EI,
                            WeeklyValues = weeklyValues,
                            Level = 2
                        });
                    }

                    // Обновляем значения для подраздела в результате
                    var subchapterDto = result.FirstOrDefault(x => x.Type == "Subchapter" && x.Id == subchapter.Id);
                    if (subchapterDto != null && subchapterWeeklySums.TryGetValue(subchapter.Id, out var subSums))
                    {
                        subchapterDto.WeeklyValues = new Dictionary<string, int>(subSums);
                    }
                }

                // Обновляем значения для раздела в результате
                var chapterDto = result.FirstOrDefault(x => x.Type == "Chapter" && x.Id == chapter.Id);
                if (chapterDto != null && chapterWeeklySums.TryGetValue(chapter.Id, out var chapSums))
                {
                    chapterDto.WeeklyValues = new Dictionary<string, int>(chapSums);
                }
            }

            return result;
        }

        private List<(DateTime Start, DateTime End)> GetWeeksBetween(DateTime startDate, DateTime endDate)
        {
            var weeks = new List<(DateTime Start, DateTime End)>();
            var currentStart = startDate.StartOfWeek();

            while (currentStart <= endDate)
            {
                var currentEnd = currentStart.AddDays(6);
                weeks.Add((currentStart, currentEnd));
                currentStart = currentStart.AddDays(7);
            }

            return weeks;
        }
    }

    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt)
        {
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }
}