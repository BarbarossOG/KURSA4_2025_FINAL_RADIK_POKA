using KURSA4_2025_FINAL_RADIK_POKA.Models;
using KURSA4_2025_FINAL_RADIK_POKA.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KURSA4_2025_FINAL_RADIK_POKA.Controllers
{
    [ApiController]
    [Route("api/planning")]
    public class PlanningController : ControllerBase
    {
        private readonly PlanningService _service;
        private readonly ReportService _reportService;

        public PlanningController(PlanningService service, ReportService reportService)
        {
            _service = service;
            _reportService = reportService;
        }

        #region Управление блокировкой
        [HttpPost("lock/{objectId}")]
        public IActionResult LockPlanning(int objectId)
        {
            var obj = _service.GetObjectById(objectId);
            if (obj == null)
            {
                return BadRequest(new { Message = "Объект не найден" });
            }

            _service.LockObject(objectId);
            _service.LockChanges();

            return Ok(new { Message = $"Объект {objectId} заблокирован" });
        }

        [HttpPost("unlock/{objectId}")]
        public IActionResult UnlockPlanning(int objectId)
        {
            var obj = _service.GetObjectById(objectId);
            if (obj == null)
            {
                return BadRequest(new { Message = "Объект не найден" });
            }

            _service.UnlockObject(objectId);
            _service.UnlockChanges();

            return Ok(new { Message = $"Объект {objectId} разблокирован" });
        }

        [HttpGet("lock-status/{objectId}")]
        public IActionResult GetLockStatus(int objectId)
        {
            var obj = _service.GetObjectById(objectId);
            if (obj == null)
            {
                return NotFound(new { Message = "Объект не найден" });
            }

            bool isLocked = _service.IsObjectLocked(objectId);
            return Ok(new { ObjectId = objectId, IsLocked = isLocked });
        }
        #endregion


        [HttpPost("create-work-schedule")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateWorkSchedule(
        [FromQuery] int objectId
        // [FromQuery] string district,
        // [FromQuery] string street
        // [FromQuery] string status,
        // [FromQuery] int? chapterId,
        // [FromQuery] string? chapterName,
        // [FromQuery] int? chapterNumber,
        // [FromQuery] int? subchapterId,
        // [FromQuery] string? subchapterName,
        // [FromQuery] int? subchapterNumber,
        // [FromQuery] string workName,
        // [FromQuery] int workNumber,
        // [FromQuery] string workEI,
        // [FromQuery] DateTime workDate,
        // [FromQuery] int workValue
        )
        {
            try
            {
                // Проверка обязательных полей
                if (objectId <= 0)
                    return BadRequest(new { Message = "Некорректный ID объекта" });
                // if (string.IsNullOrEmpty(district))
                    // return BadRequest("Не указан район (district)");
                // if (string.IsNullOrEmpty(street))
                    // return BadRequest("Не указана улица (street)");
                // if (string.IsNullOrEmpty(status))
                    // return BadRequest("Не указан статус (status)");

                // if (!chapterId.HasValue && (string.IsNullOrEmpty(chapterName) || !chapterNumber.HasValue))
                    // return BadRequest("Для нового раздела необходимо указать chapterName и chapterNumber");

                // if (!subchapterId.HasValue && (string.IsNullOrEmpty(subchapterName) || !subchapterNumber.HasValue))
                    // return BadRequest("Для нового подраздела необходимо указать subchapterName и subchapterNumber");

                var result = await _service.CreateWorkScheduleAsync(
                    objectId
                    // district,
                    // street
                    // status,
                    // chapterId,
                    // chapterName,
                    // chapterNumber,
                    // subchapterId,
                    // subchapterName,
                    // subchapterNumber,
                    // workName,
                    // workNumber,
                    // workEI,
                    // workDate,
                    // workValue
                    );
                return result
                    ? Ok("График работ успешно создан")
                    : BadRequest("Не удалось создать график работ");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при создании графика работ: {ex.Message}");
            }
        }


        // Получение плана-графика по ID объекта
        [HttpGet("work-schedule")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkScheduleByObjectId([FromQuery] int objectId)
        {
            try
            {
                var result = await _service.GetWorkScheduleByObjectIdAsync(objectId);

                if (result is { } && result.GetType().GetProperty("Message") == null)
                    return Ok(result);

                return NotFound(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении плана-графика: {ex.Message}");
            }
        }

        // Удаление плана-графика по ID объекта
        [HttpDelete("work-schedule")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteWorkScheduleByObjectId([FromQuery] int objectId)
        {
            try
            {
                var result = await _service.DeleteWorkScheduleByObjectIdAsync(objectId);

                return result
                    ? Ok($"План-график для объекта {objectId} успешно удалён")
                    : BadRequest("Не удалось удалить план-график (возможно, редактирование заблокировано)");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при удалении плана-графика: {ex.Message}");
            }
        }

        #region Работа с разделами
        // [HttpGet("chapters")]
        // public async Task<ActionResult<IEnumerable<Chapter>>> GetAllChapters()
        // {
        //     return Ok(await _service.GetAllChaptersAsync());
        // }

        [HttpGet("chapters/{id}")]
        public async Task<ActionResult<Chapter>> GetChapter(int id)
        {
            var chapter = await _service.GetChapterByIdAsync(id);
            return chapter != null ? Ok(chapter) : NotFound();
        }

        [HttpPost("create-chapters")]
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

        #region Работа с видами работ и планами
        [HttpPost("subchapters/{subchapterId}/work-types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddWorkType(
        int subchapterId,
        [FromQuery] int workTypeId, // Добавляем параметр для ID
        [FromQuery] string name,
        [FromQuery] int number,
        [FromQuery] string ei)
        {
            try
            {
                var result = await _service.AddWorkTypeAsync(
                    workTypeId, // Передаем ID
                    subchapterId,
                    name,
                    number,
                    ei);

                return result
                    ? Ok(new { Message = "Вид работ успешно добавлен" })
                    : BadRequest("Не удалось добавить вид работ (возможно, ID уже существует или редактирование заблокировано)");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при добавлении вида работ: {ex.Message}");
            }
        }

        [HttpDelete("work-types/{workTypeId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteWorkType(int workTypeId)
        {
            try
            {
                var result = await _service.DeleteWorkTypeAsync(workTypeId);
                return result
                    ? Ok(new { Message = "Вид работ успешно удалён" })
                    : BadRequest("Не удалось удалить вид работ (возможно, редактирование заблокировано)");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при удалении вида работ: {ex.Message}");
            }
        }

        [HttpPost("work-types/{workTypeId}/work-plans")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddWorkPlan(
        int workTypeId,
        [FromQuery] int workPlanId, // Новый параметр
        [FromQuery] DateTime date,
        [FromQuery] int value)
        {
            try
            {
                var result = await _service.AddWorkPlanAsync(
                    workPlanId,
                    workTypeId,
                    date,
                    value);

                return result
                    ? Ok(new { Message = "План работ успешно добавлен" })
                    : BadRequest("Не удалось добавить план работ (возможно, ID уже существует или редактирование заблокировано)");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при добавлении плана работ: {ex.Message}");
            }
        }

        [HttpDelete("work-plans/{workPlanId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteWorkPlan(int workPlanId)
        {
            try
            {
                var result = await _service.DeleteWorkPlanAsync(workPlanId);
                return result
                    ? Ok(new { Message = "План работ успешно удалён" })
                    : BadRequest("Не удалось удалить план работ (возможно, редактирование заблокировано)");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при удалении плана работ: {ex.Message}");
            }
        }
        #endregion
        [HttpGet("report")]
        public async Task<IActionResult> GenerateReport(
        [FromQuery] int objectId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
        {
            try
            {
                var pdfBytes = await _reportService.GenerateWorkSchedulePdfAsync(objectId, startDate, endDate);

                return File(pdfBytes, "application/pdf",
                    $"WorkSchedule_{objectId}_{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при генерации отчета: {ex.Message}");
            }
        }

    }
}