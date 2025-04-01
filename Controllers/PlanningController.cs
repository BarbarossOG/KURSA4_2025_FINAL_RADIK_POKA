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
        [HttpPost("plans/{planId}/lock")]
        public async Task<IActionResult> LockPlan(int planId)
        {
            var success = await _service.LockPlan(planId);
            return success
                ? Ok(new { Message = $"План {planId} заблокирован" })
                : BadRequest(new { Message = "Ошибка блокировки" });
        }

        [HttpPost("plans/{planId}/unlock")]
        public async Task<IActionResult> UnlockPlan(int planId)
        {
            var success = await _service.UnlockPlan(planId);
            return success
                ? Ok(new { Message = $"План {planId} разблокирован" })
                : BadRequest(new { Message = "Ошибка разблокировки" });
        }

        [HttpGet("plans/{planId}/lock-status")]
        public async Task<IActionResult> GetLockStatus(int planId)
        {
            bool isPlanLocked = await _service.IsPlanLocked(planId);
            bool isGlobalLocked = _service.IsLocked();

            return Ok(new
            {
                Message = $"Статус редактирования для плана {planId}",
                IsLocked = isPlanLocked || isGlobalLocked
            });
        }
        #endregion

        #region Работа с версиями планов
        [HttpPost("objects/{objectId}/plans")]
        public async Task<IActionResult> CreatePlanVersion(int objectId)
        {
            var result = await _service.CreateWorkScheduleAsync(objectId);
            return result.Success
                ? Ok(new
                {
                    PlanId = result.PlanId,
                    Version = result.Version,
                    Message = result.Message
                })
                : BadRequest(new { Message = result.Message });
        }

        [HttpGet("objects/{objectId}/plans")]
        public async Task<IActionResult> GetPlanVersions(int objectId)
        {
            var versions = await _service.GetPlanVersions(objectId);
            return Ok(versions);
        }

        [HttpGet("plans/{planId}/structure")]
        public async Task<IActionResult> GetPlanStructure(int planId)
        {
            var structure = await _service.GetPlanStructure(planId);
            return structure != null
                ? Ok(structure)
                : NotFound(new { Message = "План не найден" });
        }

        [HttpPost("plans/{planId}/activate")]
        public async Task<IActionResult> ActivatePlan(int planId)
        {
            var success = await _service.SetActivePlan(planId);
            return success
                ? Ok(new { Message = "План активирован" })
                : BadRequest(new { Message = "Ошибка активации плана" });
        }

        [HttpGet("plans/{planId}")]
        public async Task<IActionResult> GetWorkSchedule(int planId)
        {
            try
            {
                var plan = await _service.GetWorkScheduleByIdAsync(planId);
                return plan != null
                    ? Ok(plan)
                    : NotFound(new { Message = "План не найден" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при получении плана: {ex.Message}" });
            }
        }

        [HttpDelete("plans/{planId}")]
        public async Task<IActionResult> DeleteWorkSchedule(int planId)
        {
            try
            {
                var result = await _service.DeleteWorkScheduleAsync(planId);
                return result.Success
                    ? Ok(new { Message = result.Message })
                    : BadRequest(new { Message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при удалении плана: {ex.Message}" });
            }
        }
        #endregion

        #region Работа с разделами
        [HttpGet("chapters")]
        public async Task<IActionResult> GetAllChapters()
        {
            try
            {
                var chapters = await _service.GetAllChaptersAsync();
                return Ok(chapters);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при получении разделов: {ex.Message}" });
            }
        }

        [HttpGet("chapters/{id}")]
        public async Task<IActionResult> GetChapter(int id)
        {
            var chapter = await _service.GetChapterByIdAsync(id);
            return chapter != null
                ? Ok(chapter)
                : NotFound(new { Message = "Раздел не найден" });
        }

        [HttpPost("chapters")]
        public async Task<IActionResult> CreateChapter([FromBody] ChapterCreateRequest request)
        {
            try
            {
                var chapter = new Chapter
                {
                    Name = request.Name,
                    Number = request.Number
                };

                var result = await _service.AddChapterAsync(chapter);
                return result.Success
                    ? CreatedAtAction(nameof(GetChapter), new { id = chapter.Id }, new
                    {
                        Message = result.Message,
                        Chapter = chapter
                    })
                    : BadRequest(new { Message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpPut("chapters/{id}")]
        public async Task<IActionResult> UpdateChapter(int id, [FromBody] ChapterUpdateRequest request)
        {
            var success = await _service.UpdateChapterAsync(id, new Chapter
            {
                Id = id,
                Name = request.Name,
                Number = request.Number
            });

            return success
                ? NoContent()
                : NotFound(new { Message = "Раздел не найден или редактирование заблокировано" });
        }

        [HttpDelete("chapters/{id}")]
        public async Task<IActionResult> DeleteChapter(int id)
        {
            var success = await _service.DeleteChapterAsync(id);
            return success
                ? NoContent()
                : NotFound(new { Message = "Раздел не найден или редактирование заблокировано" });
        }

        [HttpPost("chapters/reorder")]
        public async Task<IActionResult> ReorderChapters([FromBody] List<int> newOrder)
        {
            var result = await _service.ReorderChaptersAsync(newOrder);
            return result.Success
                ? Ok(new { Message = result.Message })
                : BadRequest(new { Message = result.Message });
        }
        #endregion

        #region Работа с подразделами
        [HttpGet("chapters/{chapterId}/subchapters")]
        public async Task<IActionResult> GetSubchapters(int chapterId)
        {
            try
            {
                var subchapters = await _service.GetSubchaptersByChapterAsync(chapterId);
                return Ok(subchapters);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при получении подразделов: {ex.Message}" });
            }
        }

        [HttpGet("subchapters/{id}")]
        public async Task<IActionResult> GetSubchapter(int id)
        {
            var subchapter = await _service.GetSubchapterByIdAsync(id);
            return subchapter != null
                ? Ok(subchapter)
                : NotFound(new { Message = "Подраздел не найден" });
        }

        [HttpPost("subchapters")]
        public async Task<IActionResult> CreateSubchapter([FromBody] SubchapterCreateRequest request)
        {
            try
            {
                var result = await _service.AddSubchapterAsync(request.Name, request.Number);
                return result.Success
                    ? CreatedAtAction(nameof(GetSubchapter), new { id = result.Subchapter.Id }, new
                    {
                        Message = result.Message,
                        Subchapter = result.Subchapter
                    })
                    : BadRequest(new { Message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при создании подраздела: {ex.Message}" });
            }
        }

        [HttpPut("subchapters/{id}")]
        public async Task<IActionResult> UpdateSubchapter(int id, [FromBody] SubchapterUpdateRequest request)
        {
            var success = await _service.UpdateSubchapterAsync(id, new Subchapter
            {
                Id = id,
                Name = request.Name,
                Number = request.Number
            });

            return success
                ? NoContent()
                : NotFound(new { Message = "Подраздел не найден или редактирование заблокировано" });
        }

        [HttpDelete("subchapters/{id}")]
        public async Task<IActionResult> DeleteSubchapter(int id)
        {
            var success = await _service.DeleteSubchapterAsync(id);
            return success
                ? NoContent()
                : NotFound(new { Message = "Подраздел не найден или редактирование заблокировано" });
        }

        [HttpPost("chapters/{chapterId}/subchapters/reorder")]
        public async Task<IActionResult> ReorderSubchapters(int chapterId, [FromBody] List<int> newOrder)
        {
            var result = await _service.ReorderSubchaptersAsync(chapterId, newOrder);
            return result.Success
                ? Ok(new { Message = result.Message })
                : BadRequest(new { Message = result.Message });
        }

        [HttpPost("subchapters/{id}/move/{newChapterId}")]
        public async Task<IActionResult> MoveSubchapter(int id, int newChapterId)
        {
            var success = await _service.MoveSubchapterAsync(id, newChapterId);
            return success
                ? NoContent()
                : BadRequest(new { Message = "Не удалось переместить или редактирование заблокировано" });
        }
        #endregion

        #region Работа с видами работ и планами
        [HttpPost("work-types")]
        public async Task<IActionResult> AddWorkType([FromBody] WorkTypeCreateRequest request)
        {
            try
            {
                var result = await _service.AddWorkTypeAsync(request.Name, request.Number, request.EI);
                return result.Success
                    ? Ok(new
                    {
                        Message = result.Message,
                        WorkType = result.WorkType
                    })
                    : BadRequest(new { Message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при добавлении вида работ: {ex.Message}" });
            }
        }

        [HttpDelete("work-types/{workTypeId}")]
        public async Task<IActionResult> DeleteWorkType(int workTypeId)
        {
            var success = await _service.DeleteWorkTypeAsync(workTypeId);
            return success
                ? Ok(new { Message = "Вид работ успешно удалён" })
                : BadRequest(new { Message = "Не удалось удалить вид работ" });
        }

        [HttpPost("work-plans")]
        public async Task<IActionResult> AddWorkPlan([FromBody] WorkPlanCreateRequest request)
        {
            try
            {
                var result = await _service.AddWorkPlanAsync(request.Date, request.Value);
                return result.Success
                    ? Ok(new
                    {
                        Message = result.Message,
                        WorkPlan = result.WorkPlan
                    })
                    : BadRequest(new { Message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при добавлении плана работ: {ex.Message}" });
            }
        }

        [HttpDelete("work-plans/{workPlanId}")]
        public async Task<IActionResult> DeleteWorkPlan(int workPlanId)
        {
            var success = await _service.DeleteWorkPlanAsync(workPlanId);
            return success
                ? Ok(new { Message = "План работ успешно удалён" })
                : BadRequest(new { Message = "Не удалось удалить план работ" });
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
                return StatusCode(500, new { Message = $"Ошибка при генерации отчета: {ex.Message}" });
            }
        }
    }

    public class ChapterCreateRequest
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }

    public class ChapterUpdateRequest
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }

    public class SubchapterCreateRequest
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }

    public class SubchapterUpdateRequest
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }

    public class WorkTypeCreateRequest
    {
        public string Name { get; set; }
        public int Number { get; set; }
        public string EI { get; set; }
    }

    public class WorkPlanCreateRequest
    {
        public DateTime Date { get; set; }
        public int Value { get; set; }
    }
}