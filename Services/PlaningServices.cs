using KURSA4_2025_FINAL_RADIK_POKA.Data;
using KURSA4_2025_FINAL_RADIK_POKA.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KURSA4_2025_FINAL_RADIK_POKA.Services
{
    public class PlanningService
    {
        private readonly PlanningContext _context;
        private bool _isLocked = false;

        public PlanningService(PlanningContext context)
        {
            _context = context;
        }

        #region Блокировка графика
        public async Task<bool> LockPlan(int planId)
        {
            var plan = await _context.GraphicPlanningsOfWork.FindAsync(planId);
            if (plan == null) return false;

            plan.Status = "Заблокирован";
            await _context.SaveChangesAsync();
            _isLocked = true;
            return true;
        }

        public async Task<bool> UnlockPlan(int planId)
        {
            var plan = await _context.GraphicPlanningsOfWork.FindAsync(planId);
            if (plan == null) return false;

            plan.Status = "Редактируется";
            await _context.SaveChangesAsync();
            _isLocked = false;
            return true;
        }

        public async Task<bool> IsPlanLocked(int planId)
        {
            var plan = await _context.GraphicPlanningsOfWork.FindAsync(planId);
            return plan != null && plan.Status == "Заблокирован";
        }

        public void LockChanges() => _isLocked = true;
        public void UnlockChanges() => _isLocked = false;
        public bool IsLocked() => _isLocked;
        #endregion

        #region Работа с версиями планов
        public async Task<(bool Success, int PlanId, int Version, string Message)> CreateWorkScheduleAsync(int objectId)
        {
            try
            {
                var obj = await _context.Objects.FindAsync(objectId);
                if (obj == null)
                    return (false, 0, 0, "Объект не найден");

                // Проверяем, есть ли уже активный план
                var activePlan = await _context.GraphicPlanningsOfWork
                    .FirstOrDefaultAsync(p => p.ObjectId == objectId && p.Status == "Редактируется");

                if (activePlan != null)
                    return (false, 0, 0, "Уже есть активный план для этого объекта");

                int nextVersion = await _context.GraphicPlanningsOfWork
                    .Where(g => g.ObjectId == objectId)
                    .MaxAsync(g => (int?)g.Version) ?? 0;
                nextVersion++;

                var newPlan = new GraphicPlanningOfWork
                {
                    ObjectId = objectId,
                    Version = nextVersion,
                    Status = "Редактируется",
                    CreationDate = DateTime.Now
                };

                await _context.GraphicPlanningsOfWork.AddAsync(newPlan);
                await _context.SaveChangesAsync();

                return (true, newPlan.Id, nextVersion, $"Создан план версии {nextVersion} для объекта {objectId}");
            }
            catch (Exception ex)
            {
                return (false, 0, 0, $"Ошибка: {ex.Message}");
            }
        }

        public async Task<IEnumerable<GraphicPlanningOfWork>> GetAllPlansAsync()
        {
            return await _context.GraphicPlanningsOfWork
                .Include(p => p.Object)
                .OrderByDescending(p => p.CreationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<GraphicPlanningOfWork>> GetPlanVersions(int objectId)
        {
            return await _context.GraphicPlanningsOfWork
                .Where(p => p.ObjectId == objectId)
                .OrderByDescending(p => p.Version)
                .ToListAsync();
        }

    
        public async Task<PlanStructureDto> GetPlanStructure(int planId)
        {
            var plan = await _context.GraphicPlanningsOfWork
                .Include(p => p.Object)
                .FirstOrDefaultAsync(p => p.Id == planId);

            if (plan == null) return null;

            var chapters = await _context.Chapters
                .Where(c => c.PlanId == planId)
                .OrderBy(c => c.Number)
                .ToListAsync();

            var subchapters = await _context.Subchapters
                .Where(s => chapters.Select(c => c.Id).Contains(s.ChapterId))
                .OrderBy(s => s.Number)
                .ToListAsync();

            var workTypes = await _context.WorkTypes
                .Where(w => subchapters.Select(s => s.Id).Contains(w.SubchapterId))
                .OrderBy(w => w.Number)
                .ToListAsync();

            var workPlans = await _context.WorkPlans
                .Where(wp => workTypes.Select(w => w.Id).Contains(wp.WorkTypeId))
                .OrderBy(wp => wp.Date)
                .ToListAsync();

            return new PlanStructureDto
            {
                Plan = plan,
                Chapters = chapters,
                Subchapters = subchapters,
                WorkTypes = workTypes,
                WorkPlans = workPlans
            };
        }

        public async Task<GraphicPlanningOfWork?> GetWorkScheduleByIdAsync(int planId)
        {
            return await _context.GraphicPlanningsOfWork
                .Include(g => g.Object)
                .FirstOrDefaultAsync(g => g.Id == planId);
        }

        public async Task<(bool Success, string Message)> DeleteWorkScheduleAsync(int planId)
        {
            var plan = await _context.GraphicPlanningsOfWork
                .Include(g => g.Object)
                .FirstOrDefaultAsync(g => g.Id == planId);

            if (plan == null)
                return (false, "План не найден");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var chapters = await _context.Chapters
                    .Where(c => c.PlanId == planId)
                    .Include(c => c.Subchapters)
                        .ThenInclude(s => s.WorkTypes)
                            .ThenInclude(w => w.WorkPlans)
                    .ToListAsync();

                foreach (var chapter in chapters)
                {
                    foreach (var subchapter in chapter.Subchapters)
                    {
                        foreach (var workType in subchapter.WorkTypes)
                        {
                            _context.WorkPlans.RemoveRange(workType.WorkPlans);
                        }
                        _context.WorkTypes.RemoveRange(subchapter.WorkTypes);
                    }
                    _context.Subchapters.RemoveRange(chapter.Subchapters);
                }
                _context.Chapters.RemoveRange(chapters);

                _context.GraphicPlanningsOfWork.Remove(plan);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"План {planId} успешно удалён");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Ошибка при удалении плана: {ex.Message}");
            }
        }
        #endregion

        #region Работа с разделами
        public async Task<IEnumerable<Chapter>> GetAllChaptersAsync(int planId)
        {
            return await _context.Chapters
                .Where(c => c.PlanId == planId)
                .OrderBy(c => c.Number)
                .ToListAsync();
        }

        public async Task<Chapter?> GetChapterByIdAsync(int id)
        {
            return await _context.Chapters
                .Include(c => c.Plan)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<(bool Success, string Message)> AddChapterAsync(Chapter chapter, int planId)
        {
            var plan = await _context.GraphicPlanningsOfWork.FindAsync(planId);

            if (plan == null || plan.Status == "Заблокирован")
                return (false, "План заблокирован или не найден");

            try
            {
                chapter.PlanId = planId;
                chapter.Id = await _context.Chapters.MaxAsync(c => (int?)c.Id) + 1 ?? 1;
                await _context.Chapters.AddAsync(chapter);
                await _context.SaveChangesAsync();
                return (true, $"Раздел {chapter.Name} создан");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка: {ex.Message}");
            }
        }

        public async Task<bool> UpdateChapterAsync(int id, Chapter updatedChapter, int planId)
        {
            var plan = await _context.GraphicPlanningsOfWork.FindAsync(planId);

            if (plan == null || plan.Status == "Заблокирован")
                return false;

            var chapter = await _context.Chapters.FirstOrDefaultAsync(c => c.Id == id && c.PlanId == planId);
            if (chapter == null) return false;

            chapter.Name = updatedChapter.Name;
            chapter.Number = updatedChapter.Number;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteChapterAsync(int id, int planId)
        {
            var plan = await _context.GraphicPlanningsOfWork.FindAsync(planId);

            if (plan == null || plan.Status == "Заблокирован")
                return false;

            var chapter = await _context.Chapters
                .Include(c => c.Subchapters)
                .FirstOrDefaultAsync(c => c.Id == id && c.PlanId == planId);

            if (chapter == null) return false;

            _context.Chapters.Remove(chapter);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> ReorderChaptersAsync(int planId, List<int> newOrder)
        {
            var plan = await _context.GraphicPlanningsOfWork.FindAsync(planId);

            if (plan == null || plan.Status == "Заблокирован")
                return (false, "План заблокирован или не найден");

            var chapters = await _context.Chapters
                .Where(c => c.PlanId == planId)
                .ToListAsync();

            if (newOrder.Count != chapters.Count)
                return (false, "Неверное количество элементов");

            for (int i = 0; i < newOrder.Count; i++)
            {
                var chapter = chapters.FirstOrDefault(c => c.Id == newOrder[i]);
                if (chapter == null)
                    return (false, $"Раздел с ID {newOrder[i]} не найден");
                chapter.Number = i + 1;
            }

            await _context.SaveChangesAsync();
            return (true, "Разделы успешно переупорядочены");
        }
        #endregion

        #region Работа с подразделами
        public async Task<IEnumerable<Subchapter>> GetSubchaptersByChapterAsync(int chapterId)
        {
            return await _context.Subchapters
                .Where(s => s.ChapterId == chapterId)
                .OrderBy(s => s.Number)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message, Subchapter Subchapter)> AddSubchapterAsync(string name, int number, int chapterId)
        {
            try
            {
                var chapter = await _context.Chapters
                    .Include(c => c.Plan)
                    .FirstOrDefaultAsync(c => c.Id == chapterId);

                if (chapter == null || chapter.Plan.Status == "Заблокирован")
                    return (false, "Раздел не найден или план заблокирован", null);

                var subchapter = new Subchapter
                {
                    ChapterId = chapterId,
                    Name = name,
                    Number = number,
                    Id = await _context.Subchapters.MaxAsync(s => (int?)s.Id) + 1 ?? 1
                };

                await _context.Subchapters.AddAsync(subchapter);
                await _context.SaveChangesAsync();
                return (true, $"Подраздел {name} создан", subchapter);
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка: {ex.Message}", null);
            }
        }

        public async Task<bool> UpdateSubchapterAsync(int id, Subchapter updatedSubchapter, int chapterId)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Plan)
                .FirstOrDefaultAsync(c => c.Id == chapterId);

            if (chapter == null || chapter.Plan.Status == "Заблокирован")
                return false;

            var subchapter = await _context.Subchapters
                .FirstOrDefaultAsync(s => s.Id == id && s.ChapterId == chapterId);

            if (subchapter == null) return false;

            subchapter.Name = updatedSubchapter.Name;
            subchapter.Number = updatedSubchapter.Number;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSubchapterAsync(int id, int chapterId)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Plan)
                .FirstOrDefaultAsync(c => c.Id == chapterId);

            if (chapter == null || chapter.Plan.Status == "Заблокирован")
                return false;

            var subchapter = await _context.Subchapters
                .FirstOrDefaultAsync(s => s.Id == id && s.ChapterId == chapterId);

            if (subchapter == null) return false;

            _context.Subchapters.Remove(subchapter);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> ReorderSubchaptersAsync(int chapterId, List<int> newOrder)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Plan)
                .FirstOrDefaultAsync(c => c.Id == chapterId);

            if (chapter == null || chapter.Plan.Status == "Заблокирован")
                return (false, "Раздел не найден или план заблокирован");

            var subchapters = await _context.Subchapters
                .Where(s => s.ChapterId == chapterId)
                .ToListAsync();

            if (newOrder.Count != subchapters.Count)
                return (false, "Неверное количество элементов");

            for (int i = 0; i < newOrder.Count; i++)
            {
                var subchapter = subchapters.FirstOrDefault(s => s.Id == newOrder[i]);
                if (subchapter == null)
                    return (false, $"Подраздел с ID {newOrder[i]} не найден");
                subchapter.Number = i + 1;
            }

            await _context.SaveChangesAsync();
            return (true, "Подразделы успешно переупорядочены");
        }

        public async Task<bool> MoveSubchapterAsync(int subchapterId, int newChapterId, int planId)
        {
            var plan = await _context.GraphicPlanningsOfWork.FindAsync(planId);
            if (plan == null || plan.Status == "Заблокирован")
                return false;

            var subchapter = await _context.Subchapters.FirstOrDefaultAsync(s => s.Id == subchapterId);
            if (subchapter == null) return false;

            var newChapter = await _context.Chapters.FirstOrDefaultAsync(c => c.Id == newChapterId && c.PlanId == planId);
            if (newChapter == null) return false;

            subchapter.ChapterId = newChapterId;
            subchapter.Number = await _context.Subchapters
                .Where(s => s.ChapterId == newChapterId)
                .CountAsync() + 1;

            await _context.SaveChangesAsync();
            return true;
        }
        #endregion

        #region Работа с видами работ
        public async Task<(bool Success, string Message, WorkType WorkType)> AddWorkTypeAsync(string name, int number, string ei, int subchapterId)
        {
            try
            {
                var subchapter = await _context.Subchapters
                    .Include(s => s.Chapter)
                    .ThenInclude(c => c.Plan)
                    .FirstOrDefaultAsync(s => s.Id == subchapterId);

                if (subchapter == null || subchapter.Chapter.Plan.Status == "Заблокирован")
                    return (false, "Подраздел не найден или план заблокирован", null);

                var workType = new WorkType
                {
                    SubchapterId = subchapterId,
                    Name = name,
                    Number = number,
                    EI = ei,
                    Id = await _context.WorkTypes.MaxAsync(w => (int?)w.Id) + 1 ?? 1
                };

                await _context.WorkTypes.AddAsync(workType);
                await _context.SaveChangesAsync();
                return (true, $"Вид работ {name} добавлен", workType);
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> DeleteWorkTypeAsync(int workTypeId, int planId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var plan = await _context.GraphicPlanningsOfWork.FindAsync(planId);
                if (plan == null)
                    return (false, "План не найден");

                if (plan.Status == "Заблокирован")
                    return (false, "План заблокирован, удаление невозможно");

                var workType = await _context.WorkTypes
                    .Include(w => w.Subchapter)
                    .ThenInclude(s => s.Chapter)
                    .Include(w => w.WorkPlans)
                    .FirstOrDefaultAsync(w => w.Id == workTypeId && w.Subchapter.Chapter.PlanId == planId);

                if (workType == null)
                    return (false, "Вид работ не найден или не принадлежит указанному плану");

                _context.WorkPlans.RemoveRange(workType.WorkPlans);
                _context.WorkTypes.Remove(workType);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Вид работ '{workType.Name}' успешно удален");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Ошибка при удалении вида работ: {ex.Message}");
            }
        }
        #endregion

        #region Работа с планами работ
        public async Task<(bool Success, string Message, WorkPlan WorkPlan)> AddWorkPlanAsync(DateTime date, int value, int workTypeId)
        {
            try
            {
                var workType = await _context.WorkTypes
                    .Include(w => w.Subchapter)
                    .ThenInclude(s => s.Chapter)
                    .ThenInclude(c => c.Plan)
                    .FirstOrDefaultAsync(w => w.Id == workTypeId);

                if (workType == null || workType.Subchapter.Chapter.Plan.Status == "Заблокирован")
                    return (false, "Вид работ не найден или план заблокирован", null);

                var workPlan = new WorkPlan
                {
                    WorkTypeId = workTypeId,
                    Date = date,
                    Value = value,
                    Id = await _context.WorkPlans.MaxAsync(wp => (int?)wp.Id) + 1 ?? 1
                };

                await _context.WorkPlans.AddAsync(workPlan);
                await _context.SaveChangesAsync();
                return (true, "План работ добавлен", workPlan);
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> DeleteWorkPlanAsync(int workPlanId, int planId)
        {
            try
            {
                var plan = await _context.GraphicPlanningsOfWork.FindAsync(planId);
                if (plan == null)
                    return (false, "План не найден");

                if (plan.Status == "Заблокирован")
                    return (false, "План заблокирован, удаление невозможно");

                var workPlan = await _context.WorkPlans
                    .Include(wp => wp.WorkType)
                    .ThenInclude(wt => wt.Subchapter)
                    .ThenInclude(s => s.Chapter)
                    .FirstOrDefaultAsync(wp => wp.Id == workPlanId && wp.WorkType.Subchapter.Chapter.PlanId == planId);

                if (workPlan == null)
                    return (false, "План работ не найден или не принадлежит указанному плану");

                _context.WorkPlans.Remove(workPlan);
                await _context.SaveChangesAsync();
                return (true, "План работ успешно удален");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка при удалении плана работ: {ex.Message}");
            }
        }
        #endregion
        public class PlanStructureDto
    {
        public GraphicPlanningOfWork Plan { get; set; }
        public IEnumerable<Chapter> Chapters { get; set; }
        public IEnumerable<Subchapter> Subchapters { get; set; }
        public IEnumerable<WorkType> WorkTypes { get; set; }
        public IEnumerable<WorkPlan> WorkPlans { get; set; }
    }
    }
}