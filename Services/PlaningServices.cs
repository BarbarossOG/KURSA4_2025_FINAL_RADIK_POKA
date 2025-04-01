using KURSA4_2025_FINAL_RADIK_POKA.Data;
using KURSA4_2025_FINAL_RADIK_POKA.Models;
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
        public void LockChanges() => _isLocked = true;
        public void UnlockChanges() => _isLocked = false;
        public bool IsLocked() => _isLocked;
        #endregion

        #region Получение текущий ID
        public async Task<int> GetCurrentObjectIdAsync()
        {
            var lastPlan = await _context.GraphicPlanningsOfWork
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            return lastPlan?.ObjectId ?? throw new Exception("Не найден активный план работ");
        }
        public async Task<int> GetCurrentChapterIdAsync()
        {
            int objectId = await GetCurrentObjectIdAsync();
            var lastChapter = await _context.Chapters
                .Where(c => c.ObjectId == objectId)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            return lastChapter?.Id ?? throw new Exception("Не найден активный раздел");
        }

        public async Task<int> GetCurrentSubchapterIdAsync(int chapterId)
        {
            var lastSubchapter = await _context.Subchapters
                .Where(s => s.ChapterId == chapterId)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            return lastSubchapter?.Id ?? throw new Exception("Не найден активный подраздел");
        }
        #endregion

        #region Работа с планами графиков работ
        public async Task<(bool Success, int PlanId, string Message)> CreateWorkScheduleAsync(int objectId)
        {
            if (_isLocked || await IsObjectLocked(objectId))
                return (false, 0, "Редактирование заблокировано");

            try
            {
                var obj = await _context.Objects.FindAsync(objectId);
                if (obj == null)
                    return (false, 0, "Объект не найден");

                int nextVersion = await _context.GraphicPlanningsOfWork
                    .Where(g => g.ObjectId == objectId)
                    .MaxAsync(g => (int?)g.Version) ?? 0;
                nextVersion++;

                var newPlan = new GraphicPlanningOfWork
                {
                    ObjectId = objectId,
                    Version = nextVersion,
                    Status = "Редактируется"
                };

                await _context.GraphicPlanningsOfWork.AddAsync(newPlan);
                await _context.SaveChangesAsync();

                return (true, newPlan.Id, $"Создан план работ {newPlan.Id}, для объекта {objectId}");
            }
            catch
            {
                return (false, 0, "Ошибка при создании плана работ");
            }
        }

        public async Task<GraphicPlanningOfWork?> GetWorkScheduleByIdAsync(int planId)
        {
            return await _context.GraphicPlanningsOfWork
                .Include(g => g.Object)
                .FirstOrDefaultAsync(g => g.Id == planId);
        }

        public async Task<(bool Success, string Message)> DeleteWorkScheduleAsync(int planId)
        {
            if (_isLocked)
                return (false, "Редактирование заблокировано");

            var plan = await _context.GraphicPlanningsOfWork
                .Include(g => g.Object)
                .FirstOrDefaultAsync(g => g.Id == planId);

            if (plan == null)
                return (false, "План не найден");

            if (plan.Object.Status == "LOCKED")
                return (false, "Объект заблокирован");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Получаем все связанные сущности через навигационные свойства
                var chapters = await _context.Chapters
                    .Include(c => c.Subchapters)
                        .ThenInclude(s => s.WorkTypes)
                            .ThenInclude(w => w.WorkPlans)
                    .Where(c => c.ObjectId == plan.ObjectId)
                    .ToListAsync();

                // Удаляем все WorkPlans
                foreach (var chapter in chapters)
                {
                    foreach (var subchapter in chapter.Subchapters)
                    {
                        foreach (var workType in subchapter.WorkTypes)
                        {
                            _context.WorkPlans.RemoveRange(workType.WorkPlans);
                        }
                    }
                }

                // Удаляем WorkTypes
                foreach (var chapter in chapters)
                {
                    foreach (var subchapter in chapter.Subchapters)
                    {
                        _context.WorkTypes.RemoveRange(subchapter.WorkTypes);
                    }
                }

                // Удаляем Subchapters
                foreach (var chapter in chapters)
                {
                    _context.Subchapters.RemoveRange(chapter.Subchapters);
                }

                // Удаляем Chapters
                _context.Chapters.RemoveRange(chapters);

                // Удаляем сам план
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

        #region Работа с объектами
        public Models.Object? GetObjectById(int objectId)
        {
            return _context.Objects.FirstOrDefault(o => o.Id == objectId);
        }

        public async Task<bool> LockObject(int objectId)
        {
            var obj = await _context.Objects.FindAsync(objectId);
            if (obj == null) return false;

            obj.Status = "LOCKED";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlockObject(int objectId)
        {
            var obj = await _context.Objects.FindAsync(objectId);
            if (obj == null) return false;

            obj.Status = "UNLOCKED";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsObjectLocked(int objectId)
        {
            var obj = await _context.Objects.FindAsync(objectId);
            return obj != null && obj.Status == "LOCKED";
        }
        #endregion

        #region Работа с разделами
        public async Task<IEnumerable<Chapter>> GetAllChaptersAsync()
        {
            int objectId = await GetCurrentObjectIdAsync();
            return await _context.Chapters
                .Include(c => c.Object)
                .Where(c => c.ObjectId == objectId)
                .OrderBy(c => c.Number)
                .ToListAsync();
        }
        
        public async Task<Chapter?> GetChapterByIdAsync(int id)
        {
            return await _context.Chapters
                .Include(c => c.Object)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
        

        public async Task<(bool Success, string Message)> AddChapterAsync(Chapter chapter)
        {
            if (chapter.ObjectId <= 0)
                return (false, "Не указан ID объекта");

            if (_isLocked || await IsObjectLocked(chapter.ObjectId))
                return (false, "Редактирование заблокировано");

            try
            {
                chapter.Id = await _context.Chapters.MaxAsync(c => (int?)c.Id) + 1 ?? 1;

                await _context.Chapters.AddAsync(chapter);
                await _context.SaveChangesAsync();
                return (true, $"Раздел {chapter.Name} создан для объекта {chapter.ObjectId}");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка: {ex.Message}");
            }
        }

        public async Task<bool> UpdateChapterAsync(int id, Chapter updatedChapter)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Object)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chapter == null) return false;

            if (_isLocked || await IsObjectLocked(chapter.ObjectId))
                return false;

            chapter.Name = updatedChapter.Name;
            chapter.Number = updatedChapter.Number;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteChapterAsync(int id)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Object)
                .Include(c => c.Subchapters)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chapter == null) return false;

            if (_isLocked || await IsObjectLocked(chapter.ObjectId))
                return false;

            _context.Chapters.Remove(chapter);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> ReorderChaptersAsync(List<int> newOrder)
        {
            int objectId = await GetCurrentObjectIdAsync();

            if (_isLocked || await IsObjectLocked(objectId))
                return (false, "Редактирование заблокировано");

            var chapters = await _context.Chapters
                .Where(c => c.ObjectId == objectId)
                .ToListAsync();

            if (newOrder.Count != chapters.Count)
                return (false, "Неверное количество элементов для переупорядочивания");

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
                .Include(s => s.Chapter)
                .Where(s => s.ChapterId == chapterId)
                .OrderBy(s => s.Number)
                .ToListAsync();
        }

        public async Task<Subchapter?> GetSubchapterByIdAsync(int id)
        {
            return await _context.Subchapters
                .Include(s => s.Chapter)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<(bool Success, string Message, Subchapter Subchapter)> AddSubchapterAsync(string name, int number)
        {
            try
            {
                int chapterId = await GetCurrentChapterIdAsync();
                var chapter = await _context.Chapters
                    .Include(c => c.Object)
                    .FirstOrDefaultAsync(c => c.Id == chapterId);

                if (chapter == null)
                    return (false, "Раздел не найден", null);

                if (_isLocked || await IsObjectLocked(chapter.ObjectId))
                    return (false, "Редактирование заблокировано", null);

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

        public async Task<bool> UpdateSubchapterAsync(int id, Subchapter updatedSubchapter)
        {
            var subchapter = await _context.Subchapters
                .Include(s => s.Chapter)
                    .ThenInclude(c => c.Object)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subchapter == null) return false;

            if (_isLocked || await IsObjectLocked(subchapter.Chapter.ObjectId))
                return false;

            subchapter.Name = updatedSubchapter.Name;
            subchapter.Number = updatedSubchapter.Number;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSubchapterAsync(int id)
        {
            var subchapter = await _context.Subchapters
                .Include(s => s.Chapter)
                    .ThenInclude(c => c.Object)
                .Include(s => s.WorkTypes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subchapter == null) return false;

            if (_isLocked || await IsObjectLocked(subchapter.Chapter.ObjectId))
                return false;

            _context.Subchapters.Remove(subchapter);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> ReorderSubchaptersAsync(int chapterId, List<int> newOrder)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Object)
                .FirstOrDefaultAsync(c => c.Id == chapterId);

            if (chapter == null)
                return (false, "Раздел не найден");

            if (_isLocked || await IsObjectLocked(chapter.ObjectId))
                return (false, "Редактирование заблокировано");

            var subchapters = await _context.Subchapters
                .Where(s => s.ChapterId == chapterId)
                .ToListAsync();

            if (newOrder.Count != subchapters.Count)
                return (false, "Неверное количество элементов для переупорядочивания");

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

        public async Task<bool> MoveSubchapterAsync(int subchapterId, int newChapterId)
        {
            var subchapter = await _context.Subchapters
                .Include(s => s.Chapter)
                    .ThenInclude(c => c.Object)
                .FirstOrDefaultAsync(s => s.Id == subchapterId);

            if (subchapter == null) return false;

            var newChapter = await _context.Chapters
                .Include(c => c.Object)
                .FirstOrDefaultAsync(c => c.Id == newChapterId);

            if (newChapter == null) return false;

            if (subchapter.Chapter.ObjectId != newChapter.ObjectId)
                return false;

            if (_isLocked || await IsObjectLocked(newChapter.ObjectId))
                return false;

            subchapter.ChapterId = newChapterId;
            subchapter.Number = await _context.Subchapters
                .Where(s => s.ChapterId == newChapterId)
                .CountAsync() + 1;

            await _context.SaveChangesAsync();
            return true;
        }
        #endregion

        #region Работа с видами работ и планами
        public async Task<(bool Success, string Message, WorkType WorkType)> AddWorkTypeAsync(
    string name, int number, string ei)
        {
            try
            {
                int chapterId = await GetCurrentChapterIdAsync();
                int subchapterId = await GetCurrentSubchapterIdAsync(chapterId);

                var subchapter = await _context.Subchapters
                    .Include(s => s.Chapter)
                        .ThenInclude(c => c.Object)
                    .FirstOrDefaultAsync(s => s.Id == subchapterId);

                if (subchapter == null)
                    return (false, "Подраздел не найден", null);

                if (_isLocked || await IsObjectLocked(subchapter.Chapter.ObjectId))
                    return (false, "Редактирование заблокировано", null);

                int workTypeId = await _context.WorkTypes.MaxAsync(w => (int?)w.Id) + 1 ?? 1;

                var workType = new WorkType
                {
                    Id = workTypeId,
                    SubchapterId = subchapterId,
                    Name = name,
                    Number = number,
                    EI = ei
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

        public async Task<bool> DeleteWorkTypeAsync(int workTypeId)
        {
            var workType = await _context.WorkTypes
                .Include(w => w.Subchapter)
                    .ThenInclude(s => s.Chapter)
                        .ThenInclude(c => c.Object)
                .FirstOrDefaultAsync(w => w.Id == workTypeId);

            if (workType == null) return false;

            if (_isLocked || await IsObjectLocked(workType.Subchapter.Chapter.ObjectId))
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _context.WorkPlans
                    .Where(wp => wp.WorkTypeId == workTypeId)
                    .ExecuteDeleteAsync();

                await _context.WorkTypes
                    .Where(w => w.Id == workTypeId)
                    .ExecuteDeleteAsync();

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<(bool Success, string Message, WorkPlan WorkPlan)> AddWorkPlanAsync(
    DateTime date, int value)
        {
            try
            {
                int chapterId = await GetCurrentChapterIdAsync();
                int subchapterId = await GetCurrentSubchapterIdAsync(chapterId);
                int workTypeId = await _context.WorkTypes
                    .Where(w => w.SubchapterId == subchapterId)
                    .MaxAsync(w => (int?)w.Id) ?? throw new Exception("Не найден активный вид работ");

                var workType = await _context.WorkTypes
                    .Include(w => w.Subchapter)
                        .ThenInclude(s => s.Chapter)
                            .ThenInclude(c => c.Object)
                    .FirstOrDefaultAsync(w => w.Id == workTypeId);

                if (workType == null)
                    return (false, "Вид работ не найден", null);

                if (_isLocked || await IsObjectLocked(workType.Subchapter.Chapter.ObjectId))
                    return (false, "Редактирование заблокировано", null);

                int workPlanId = await _context.WorkPlans.MaxAsync(wp => (int?)wp.Id) + 1 ?? 1;

                var workPlan = new WorkPlan
                {
                    Id = workPlanId,
                    WorkTypeId = workTypeId,
                    Date = date,
                    Value = value
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

        public async Task<bool> DeleteWorkPlanAsync(int workPlanId)
        {
            var workPlan = await _context.WorkPlans
                .Include(wp => wp.WorkType)
                    .ThenInclude(w => w.Subchapter)
                        .ThenInclude(s => s.Chapter)
                            .ThenInclude(c => c.Object)
                .FirstOrDefaultAsync(wp => wp.Id == workPlanId);

            if (workPlan == null) return false;

            if (_isLocked || await IsObjectLocked(workPlan.WorkType.Subchapter.Chapter.ObjectId))
                return false;

            try
            {
                _context.WorkPlans.Remove(workPlan);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}