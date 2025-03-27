using KURSA4_2025_FINAL_RADIK_POKA.Models;
using KURSA4_2025_FINAL_RADIK_POKA.Services;
using Microsoft.AspNetCore.Mvc;

namespace KURSA4_2025_FINAL_RADIK_POKA.Controllers
{
    [ApiController]
    [Route("api/planning")]
    public class PlanningController : ControllerBase
    {
        private readonly PlanningService _service;

        public PlanningController(PlanningService service)
        {
            _service = service;
        }

        #region Управление блокировкой
        [HttpPost("lock")]
        public IActionResult LockPlanning()
        {
            _service.LockChanges();
            return Ok(new { Message = "Изменения заблокированы" });
        }

        [HttpPost("unlock")]
        public IActionResult UnlockPlanning()
        {
            _service.UnlockChanges();
            return Ok(new { Message = "Изменения разблокированы" });
        }

        [HttpGet("lock-status")]
        public IActionResult GetLockStatus()
        {
            return Ok(new { IsLocked = _service.IsLocked() });
        }
        #endregion

        #region Работа с разделами
        [HttpGet("chapters")]
        public async Task<ActionResult<IEnumerable<Chapter>>> GetAllChapters()
        {
            return Ok(await _service.GetAllChaptersAsync());
        }

        [HttpGet("chapters/{id}")]
        public async Task<ActionResult<Chapter>> GetChapter(int id)
        {
            var chapter = await _service.GetChapterByIdAsync(id);
            return chapter != null ? Ok(chapter) : NotFound();
        }

        [HttpPost("chapters")]
        public async Task<ActionResult<Chapter>> CreateChapter([FromBody] Chapter chapter)
        {
            try
            {
                await _service.AddChapterAsync(chapter);
                return CreatedAtAction(nameof(GetChapter), new { id = chapter.Id }, chapter);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("chapters/{id}")]
        public async Task<IActionResult> UpdateChapter(int id, [FromBody] Chapter chapter)
        {
            if (id != chapter.Id) return BadRequest();
            return await _service.UpdateChapterAsync(id, chapter)
                ? NoContent()
                : NotFound();
        }

        [HttpDelete("chapters/{id}")]
        public async Task<IActionResult> DeleteChapter(int id)
        {
            return await _service.DeleteChapterAsync(id)
                ? NoContent()
                : NotFound();
        }

        [HttpPost("chapters/reorder")]
        public async Task<IActionResult> ReorderChapters([FromBody] List<int> newOrder)
        {
            return await _service.ReorderChaptersAsync(newOrder)
                ? NoContent()
                : BadRequest("Неверный порядок или заблокировано");
        }
        #endregion

        #region Работа с подразделами
        [HttpGet("chapters/{chapterId}/subchapters")]
        public async Task<ActionResult<IEnumerable<Subchapter>>> GetSubchapters(int chapterId)
        {
            return Ok(await _service.GetSubchaptersByChapterAsync(chapterId));
        }

        [HttpGet("subchapters/{id}")]
        public async Task<ActionResult<Subchapter>> GetSubchapter(int id)
        {
            var subchapter = await _service.GetSubchapterByIdAsync(id);
            return subchapter != null ? Ok(subchapter) : NotFound();
        }

        [HttpPost("subchapters")]
        public async Task<ActionResult<Subchapter>> CreateSubchapter([FromBody] Subchapter subchapter)
        {
            try
            {
                await _service.AddSubchapterAsync(subchapter);
                return CreatedAtAction(nameof(GetSubchapter), new { id = subchapter.Id }, subchapter);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("subchapters/{id}")]
        public async Task<IActionResult> UpdateSubchapter(int id, [FromBody] Subchapter subchapter)
        {
            if (id != subchapter.Id) return BadRequest();
            return await _service.UpdateSubchapterAsync(id, subchapter)
                ? NoContent()
                : NotFound();
        }

        [HttpDelete("subchapters/{id}")]
        public async Task<IActionResult> DeleteSubchapter(int id)
        {
            return await _service.DeleteSubchapterAsync(id)
                ? NoContent()
                : NotFound();
        }

        [HttpPost("chapters/{chapterId}/subchapters/reorder")]
        public async Task<IActionResult> ReorderSubchapters(int chapterId, [FromBody] List<int> newOrder)
        {
            return await _service.ReorderSubchaptersAsync(chapterId, newOrder)
                ? NoContent()
                : BadRequest("Неверный порядок или заблокировано");
        }

        [HttpPost("subchapters/{id}/move/{newChapterId}")]
        public async Task<IActionResult> MoveSubchapter(int id, int newChapterId)
        {
            return await _service.MoveSubchapterAsync(id, newChapterId)
                ? NoContent()
                : BadRequest("Не удалось переместить или заблокировано");
        }
        #endregion
    }
}