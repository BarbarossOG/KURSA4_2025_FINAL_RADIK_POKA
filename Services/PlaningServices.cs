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
                chapter.Id = await _context.Chapters.MaxAsync(c => (int?)c.Id) + 1 ?? 1;
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
      
    }

}