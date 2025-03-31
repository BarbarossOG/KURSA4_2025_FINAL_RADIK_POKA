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
        #region Работа с объектами
        
        public Models.Object? GetObjectById(int objectId)
        {
            return _context.Objects.FirstOrDefault(o => o.Id == objectId);
        }

        public bool LockObject(int objectId)
        {
            var obj = GetObjectById(objectId);
            if (obj == null) return false;

            obj.Status = "LOCKED";
            _context.SaveChanges();
            return true;
        }

        public bool UnlockObject(int objectId)
        {
            var obj = GetObjectById(objectId);
            if (obj == null) return false;

            obj.Status = "UNLOCKED";
            _context.SaveChanges();
            return true;
        }

        public bool IsObjectLocked(int objectId)
        {
            var obj = GetObjectById(objectId);
            return obj != null && obj.Status == "LOCKED";
        }
        #endregion

        public int GetMaxObjectId()
    {
        return _context.Objects.Any() ? _context.Objects.Max(o => o.Id) : 0;
    }

        public async Task<bool> CreateWorkScheduleAsync(
        int objectId
        // string district,
        // string street,
        // string status,
        // int? chapterId,
        // string? chapterName,
        // int? chapterNumber,
        // int? subchapterId,
        // string? subchapterName,
        // int? subchapterNumber,
        // string workName,
        // int workNumber,
        // string workEI,
        // DateTime workDate,
        // int workValue
        )
        {
            if (_isLocked)
                return false;

            try
            {
                // 1. Создаем/обновляем объект
                int maxId = GetMaxObjectId();
                var constructionObject = await _context.Objects.FindAsync(objectId);
                if (constructionObject == null)
                {
                    constructionObject = new Models.Object
                    {
                        Id = objectId,
                        // Id = maxId,
                        // District = district,
                        // Street = street,
                        Status = "Edit" 
                    };
                    await _context.Objects.AddAsync(constructionObject);
                }
                else
                {
                    // Обновляем существующий объект
                    // constructionObject.District = district;
                    // constructionObject.Street = street;
                    constructionObject.Status = "Edit";
                    _context.Objects.Update(constructionObject);
                }


                // 2. Обработка раздела
                // Chapter? chapter = null;

                // if (chapterId.HasValue && chapterId > 0)
                // {
                //     chapter = await _context.Chapters.FindAsync(chapterId);
                // }

                // if (chapter == null && (!string.IsNullOrEmpty(chapterName) && chapterNumber.HasValue))
                // {
                //     chapter = new Chapter
                //     {
                //         ObjectId = objectId,
                //         Name = chapterName,
                //         Number = chapterNumber.Value
                //     };
                //     await _context.Chapters.AddAsync(chapter);
                //     await _context.SaveChangesAsync();
                // }

                // if (chapter == null) return false;

                // // 3. Обработка подраздела
                // Subchapter? subchapter = null;

                // if (subchapterId.HasValue && subchapterId > 0)
                // {
                //     subchapter = await _context.Subchapters.FindAsync(subchapterId);
                // }

                // if (subchapter == null && (!string.IsNullOrEmpty(subchapterName) && subchapterNumber.HasValue))
                // {
                //     subchapter = new Subchapter
                //     {
                //         ChapterId = chapter.Id,
                //         Name = subchapterName,
                //         Number = subchapterNumber.Value
                //     };
                //     await _context.Subchapters.AddAsync(subchapter);
                //     await _context.SaveChangesAsync();
                // }

                // if (subchapter == null) return false;

                // // 4. Создаем вид работ (WorkType)
                // var workType = new WorkType
                // {
                //     SubchapterId = subchapter.Id,
                //     EI = workEI,
                //     Name = workName,
                //     Number = workNumber
                // };
                // await _context.WorkTypes.AddAsync(workType);
                // await _context.SaveChangesAsync();

                // // 5. Создаем план работ (WorkPlan)
                // var workPlan = new WorkPlan
                // {
                //     WorkTypeId = workType.Id,
                //     Date = workDate,
                //     Value = workValue
                // };
                // await _context.WorkPlans.AddAsync(workPlan);
                // await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }


        
        // Получение плана-графика по ID объекта
        public async Task<object> GetWorkScheduleByObjectIdAsync(int objectId)
        {
            var result = await _context.Objects
                .Where(o => o.Id == objectId)
                .Select(o => new
                {
                    Object = o,
                    Chapters = _context.Chapters
                        .Where(c => c.ObjectId == o.Id)
                        .Select(c => new
                        {
                            Chapter = c,
                            Subchapters = _context.Subchapters
                                .Where(s => s.ChapterId == c.Id)
                                .Select(s => new
                                {
                                    Subchapter = s,
                                    WorkTypes = _context.WorkTypes
                                        .Where(w => w.SubchapterId == s.Id)
                                        .Select(w => new
                                        {
                                            WorkType = w,
                                            WorkPlans = _context.WorkPlans
                                                .Where(wp => wp.WorkTypeId == w.Id)
                                                .ToList()
                                        })
                                        .ToList()
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (result == null)
            {
                return new { Message = "Объект не найден" };
            }

            return result;
        }

        // Удаление плана-графика по ID объекта
        public async Task<bool> DeleteWorkScheduleByObjectIdAsync(int objectId)
        {
            if (_isLocked)
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Находим все связанные сущности
                var chapters = await _context.Chapters
                    .Where(c => c.ObjectId == objectId)
                    .ToListAsync();

                var subchapterIds = chapters
                    .SelectMany(c => _context.Subchapters
                        .Where(s => s.ChapterId == c.Id)
                        .Select(s => s.Id))
                    .ToList();

                var workTypeIds = subchapterIds
                    .SelectMany(sId => _context.WorkTypes
                        .Where(w => w.SubchapterId == sId)
                        .Select(w => w.Id))
                    .ToList();

                // Удаляем в правильном порядке (от зависимых к главным)
                await _context.WorkPlans
                    .Where(wp => workTypeIds.Contains(wp.WorkTypeId))
                    .ExecuteDeleteAsync();

                await _context.WorkTypes
                    .Where(w => subchapterIds.Contains(w.SubchapterId))
                    .ExecuteDeleteAsync();

                await _context.Subchapters
                    .Where(s => chapters.Select(c => c.Id).Contains(s.ChapterId))
                    .ExecuteDeleteAsync();

                await _context.Chapters
                    .Where(c => c.ObjectId == objectId)
                    .ExecuteDeleteAsync();

                var objectToDelete = await _context.Objects.FindAsync(objectId);
                if (objectToDelete != null)
                {
                    _context.Objects.Remove(objectToDelete);
                }

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

        #region Работа с разделами
        public async Task<IEnumerable<Chapter>> GetAllChaptersAsync()
        {
            return await _context.Chapters.OrderBy(c => c.Number).ToListAsync();
        }

        public async Task<Chapter?> GetChapterByIdAsync(int id)
        {
            return await _context.Chapters.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddChapterAsync(Chapter chapter)
        {
            if (_isLocked) throw new InvalidOperationException("Редактирование заблокировано");

            if (chapter.Id == 0)
            {
                
                // chapter.Id = await _context.Chapters.MaxAsync(c => (int?)c.Id) + 1 ?? 1;
            }

            _context.Chapters.Add(chapter);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateChapterAsync(int id, Chapter updatedChapter)
        {
            if (_isLocked) return false;

            var chapter = await _context.Chapters.FirstOrDefaultAsync(c => c.Id == id);
            if (chapter == null) return false;

            chapter.Name = updatedChapter.Name;
            chapter.Number = updatedChapter.Number;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteChapterAsync(int id)
        {
            if (_isLocked) return false;

            var chapter = await _context.Chapters.FirstOrDefaultAsync(c => c.Id == id);
            if (chapter == null) return false;

            _context.Chapters.Remove(chapter);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReorderChaptersAsync(List<int> newOrder)
        {
            if (_isLocked) return false;

            var chapters = await _context.Chapters.ToListAsync();
            if (newOrder.Count != chapters.Count) return false;

            for (int i = 0; i < newOrder.Count; i++)
            {
                var chapter = chapters.FirstOrDefault(c => c.Id == newOrder[i]);
                if (chapter == null) return false;
                chapter.Number = i + 1;
            }

            await _context.SaveChangesAsync();
            return true;
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

        public async Task<Subchapter?> GetSubchapterByIdAsync(int id)
        {
            return await _context.Subchapters.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AddSubchapterAsync(Subchapter subchapter)
        {
            if (_isLocked) throw new InvalidOperationException("Редактирование заблокировано");

            subchapter.Id = await _context.Subchapters.MaxAsync(s => (int?)s.Id) + 1 ?? 1;
            _context.Subchapters.Add(subchapter);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateSubchapterAsync(int id, Subchapter updatedSubchapter)
        {
            if (_isLocked) return false;

            var subchapter = await _context.Subchapters.FirstOrDefaultAsync(s => s.Id == id);
            if (subchapter == null) return false;

            subchapter.Name = updatedSubchapter.Name;
            subchapter.Number = updatedSubchapter.Number;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSubchapterAsync(int id)
        {
            if (_isLocked) return false;

            var subchapter = await _context.Subchapters.FirstOrDefaultAsync(s => s.Id == id);
            if (subchapter == null) return false;

            _context.Subchapters.Remove(subchapter);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReorderSubchaptersAsync(int chapterId, List<int> newOrder)
        {
            if (_isLocked) return false;

            var subchapters = await _context.Subchapters
                .Where(s => s.ChapterId == chapterId)
                .ToListAsync();

            if (newOrder.Count != subchapters.Count) return false;

            for (int i = 0; i < newOrder.Count; i++)
            {
                var subchapter = subchapters.FirstOrDefault(s => s.Id == newOrder[i]);
                if (subchapter == null) return false;
                subchapter.Number = i + 1;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MoveSubchapterAsync(int subchapterId, int newChapterId)
        {
            if (_isLocked) return false;

            var subchapter = await _context.Subchapters.FirstOrDefaultAsync(s => s.Id == subchapterId);
            if (subchapter == null) return false;

            subchapter.ChapterId = newChapterId;
            subchapter.Number = await _context.Subchapters
                .Where(s => s.ChapterId == newChapterId)
                .CountAsync() + 1;

            await _context.SaveChangesAsync();
            return true;
        }
        #endregion
        #region Работа с видами работ и планами
        public async Task<bool> AddWorkTypeAsync(
            int workTypeId, // Добавляем параметр для ручного ввода ID
            int subchapterId,
            string name,
            int number,
            string ei)
        {
            if (_isLocked) return false;

            try
            {
                // Проверяем, существует ли уже WorkType с таким ID
                if (await _context.WorkTypes.AnyAsync(w => w.Id == workTypeId))
                {
                    return false; // или можно выбросить исключение
                }

                var workType = new WorkType
                {
                    Id = workTypeId, // Устанавливаем переданный ID
                    SubchapterId = subchapterId,
                    Name = name,
                    Number = number,
                    EI = ei
                };

                await _context.WorkTypes.AddAsync(workType);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteWorkTypeAsync(int workTypeId)
{
            if (_isLocked) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
    
            try
            {
                // Сначала удаляем все связанные WorkPlans
                await _context.WorkPlans
                    .Where(wp => wp.WorkTypeId == workTypeId)
                    .ExecuteDeleteAsync();

                // Затем удаляем сам WorkType
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

        public async Task<bool> AddWorkPlanAsync(
        int workPlanId, // Добавляем параметр для ID
        int workTypeId,
        DateTime date,
        int value)
        {
            if (_isLocked) return false;

            try
            {
                if (await _context.WorkPlans.AnyAsync(wp => wp.Id == workPlanId))
                    return false;

                var workPlan = new WorkPlan
                {
                    Id = workPlanId, // Устанавливаем переданный ID
                    WorkTypeId = workTypeId,
                    Date = date,
                    Value = value
                };

                await _context.WorkPlans.AddAsync(workPlan);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteWorkPlanAsync(int workPlanId)
        {
            if (_isLocked) return false;

            try
            {
                await _context.WorkPlans
                    .Where(wp => wp.Id == workPlanId)
                    .ExecuteDeleteAsync();
            
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