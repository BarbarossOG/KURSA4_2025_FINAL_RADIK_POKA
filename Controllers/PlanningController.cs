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
            try
            {
                var success = await _service.LockPlan(planId);
                return success
                    ? Ok(new { Message = $"План {planId} заблокирован" })
                    : BadRequest(new { Message = "Ошибка блокировки: план не найден" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при блокировке плана: {ex.Message}" });
            }
        }

        [HttpPost("plans/{planId}/unlock")]
        public async Task<IActionResult> UnlockPlan(int planId)
        {
            try
            {
                var success = await _service.UnlockPlan(planId);
                return success
                    ? Ok(new { Message = $"План {planId} разблокирован" })
                    : BadRequest(new { Message = "Ошибка разблокировки: план не найден" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при разблокировке плана: {ex.Message}" });
            }
        }

        [HttpGet("plans/{planId}/lock-status")]
        public async Task<IActionResult> GetLockStatus(int planId)
        {
            try
            {
                bool isPlanLocked = await _service.IsPlanLocked(planId);
                bool isGlobalLocked = _service.IsLocked();

                return Ok(new
                {
                    PlanId = planId,
                    IsPlanLocked = isPlanLocked,
                    IsGlobalLocked = isGlobalLocked,
                    IsLocked = isPlanLocked || isGlobalLocked,
                    Message = $"Статус блокировки для плана {planId}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при проверке статуса блокировки: {ex.Message}" });
            }
        }
        #endregion

        #region Работа с версиями планов
        [HttpPost("objects/{objectId}/plans")]
        public async Task<IActionResult> CreatePlanVersion(int objectId)
        {
            try
            {
                var result = await _service.CreateWorkScheduleAsync(objectId);
                return result.Success
                    ? Ok(new
                    {
                        result.PlanId,
                        result.Version,
                        result.Message
                    })
                    : BadRequest(new { result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при создании версии плана: {ex.Message}" });
            }
        }

        [HttpGet("plans/all")]
        public async Task<IActionResult> GetAllPlans()
        {
            try
            {
                var allPlans = await _service.GetAllPlansAsync();

                return Ok(new
                {
                    TotalCount = allPlans.Count(),
                    Plans = allPlans.Select(p => new
                    {
                        p.Id,
                        p.ObjectId,
                        p.Version,
                        p.Status,
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при получении списка планов: {ex.Message}" });
            }
        }

        /*
        [HttpGet("objects/{objectId}/plans")]
        public async Task<IActionResult> GetPlanVersions(int objectId)
        {
            try
            {
                var versions = await _service.GetPlanVersions(objectId);
                return Ok(new
                {
                    ObjectId = objectId,
                    Plans = versions,
                    Count = versions.Count()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при получении версий плана: {ex.Message}" });
            }
        }*/

        [HttpGet("plans/{planId}/structure")]
        public async Task<IActionResult> GetPlanStructure(int planId)
        {
            try
            {
                var structure = await _service.GetPlanStructure(planId);
                return structure != null
                    ? Ok(structure)
                    : NotFound(new { Message = $"Структура плана {planId} не найдена" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при получении структуры плана: {ex.Message}" });
            }
        }

        /*
        [HttpGet("plans/{planId}")]
        public async Task<IActionResult> GetWorkSchedule(int planId)
        {
            try
            {
                var plan = await _service.GetWorkScheduleByIdAsync(planId);
                return plan != null
                    ? Ok(plan)
                    : NotFound(new { Message = $"План {planId} не найден" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при получении плана: {ex.Message}" });
            }
        }*/

        [HttpDelete("plans/{planId}")]
        public async Task<IActionResult> DeleteWorkSchedule(int planId)
        {
            try
            {
                var result = await _service.DeleteWorkScheduleAsync(planId);
                return result.Success
                    ? Ok(new { result.Message })
                    : BadRequest(new { result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при удалении плана: {ex.Message}" });
            }
        }
        #endregion

        #region Работа с разделами
        [HttpGet("chapters/all")]
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

       
        /*
        [HttpGet("chapters/{id}")]
        public async Task<IActionResult> GetChapter(int id)
        {
            try
            {
                var chapter = await _service.GetChapterByIdAsync(id);
                return chapter != null
                    ? Ok(chapter)
                    : NotFound(new { Message = $"Раздел {id} не найден" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при получении раздела: {ex.Message}" });
            }
        }*/

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
                    ? StatusCode(201, new  
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
            try
            {
                var success = await _service.UpdateChapterAsync(id, new Chapter
                {
                    Id = id,
                    Name = request.Name,
                    Number = request.Number
                });

                return success
                    ? NoContent()
                    : NotFound(new { Message = $"Раздел {id} не найден или редактирование заблокировано" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при обновлении раздела: {ex.Message}" });
            }
        }

        [HttpDelete("chapters/{id}")]
        public async Task<IActionResult> DeleteChapter(int id)
        {
            try
            {
                var success = await _service.DeleteChapterAsync(id);
                return success
                    ? NoContent()
                    : NotFound(new { Message = $"Раздел {id} не найден или редактирование заблокировано" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при удалении раздела: {ex.Message}" });
            }
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
        [HttpGet("subchapters/all")]
        public async Task<IActionResult> GetAllSubchapters()
        {
            try
            {
                var subchapters = await _service.GetAllSubchaptersAsync();
                return Ok(subchapters);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при получении подразделов: {ex.Message}" });
            }
        }
        /*
        [HttpGet("chapters/{chapterId}/subchapters")]
        public async Task<IActionResult> GetSubchapters(int chapterId)
        {
            try
            {
                var subchapters = await _service.GetSubchaptersByChapterAsync(chapterId);
                return Ok(new
                {
                    ChapterId = chapterId,
                    Subchapters = subchapters,
                    Count = subchapters.Count()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при получении подразделов: {ex.Message}" });
            }
        }
        
        
        [HttpGet("subchapters/{id}")]
        public async Task<IActionResult> GetSubchapter(int id)
        {
            try
            {
                var subchapter = await _service.GetSubchapterByIdAsync(id);
                return subchapter != null
                    ? Ok(subchapter)
                    : NotFound(new { Message = $"Подраздел {id} не найден" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при получении подраздела: {ex.Message}" });
            }
        }*/

        [HttpPost("subchapters")]
        public async Task<IActionResult> CreateSubchapter([FromBody] SubchapterCreateRequest request)
        {
            try
            {
                var result = await _service.AddSubchapterAsync(request.Name, request.Number);
                return result.Success
                    ? StatusCode(201, new
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
            try
            {
                var success = await _service.UpdateSubchapterAsync(id, new Subchapter
                {
                    Id = id,
                    Name = request.Name,
                    Number = request.Number
                });

                return success
                    ? NoContent()
                    : NotFound(new { Message = $"Подраздел {id} не найден или редактирование заблокировано" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при обновлении подраздела: {ex.Message}" });
            }
        }

        [HttpDelete("subchapters/{id}")]
        public async Task<IActionResult> DeleteSubchapter(int id)
        {
            try
            {
                var success = await _service.DeleteSubchapterAsync(id);
                return success
                    ? NoContent()
                    : NotFound(new { Message = $"Подраздел {id} не найден или редактирование заблокировано" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при удалении подраздела: {ex.Message}" });
            }
        }

        [HttpPost("chapters/{chapterId}/subchapters/reorder")]
        public async Task<IActionResult> ReorderSubchapters(int chapterId, [FromBody] List<int> newOrder)
        {
            try
            {
                var result = await _service.ReorderSubchaptersAsync(chapterId, newOrder);
                return result.Success
                    ? Ok(new { result.Message })
                    : BadRequest(new { result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при переупорядочивании подразделов: {ex.Message}" });
            }
        }

        [HttpPost("subchapters/{id}/move/{newChapterId}")]
        public async Task<IActionResult> MoveSubchapter(int id, int newChapterId)
        {
            try
            {
                var success = await _service.MoveSubchapterAsync(id, newChapterId);
                return success
                    ? Ok(new { Message = $"Подраздел {id} успешно перемещен в раздел {newChapterId}" })
                    : BadRequest(new { Message = $"Не удалось переместить подраздел {id} или редактирование заблокировано" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при перемещении подраздела: {ex.Message}" });
            }
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
                        result.Message,
                        WorkType = new
                        {
                            result.WorkType.Id,
                            result.WorkType.Name,
                            result.WorkType.Number,
                            result.WorkType.EI,
                            result.WorkType.SubchapterId
                        }
                    })
                    : BadRequest(new { result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при добавлении вида работ: {ex.Message}" });
            }
        }

        [HttpDelete("work-types/{workTypeId}")]
        public async Task<IActionResult> DeleteWorkType(int workTypeId)
        {
            var result = await _service.DeleteWorkTypeAsync(workTypeId);
            return result.Success
                ? Ok(new { result.Message })
                : BadRequest(new { result.Message });
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
                        result.Message,
                        WorkPlan = new
                        {
                            result.WorkPlan.Id,
                            result.WorkPlan.WorkTypeId,
                            result.WorkPlan.Date,
                            result.WorkPlan.Value
                        }
                    })
                    : BadRequest(new { result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Ошибка при добавлении плана работ: {ex.Message}" });
            }
        }

        [HttpDelete("work-plans/{workPlanId}")]
        public async Task<IActionResult> DeleteWorkPlan(int workPlanId)
        {
            var result = await _service.DeleteWorkPlanAsync(workPlanId);
            return result.Success
                ? Ok(new { result.Message })
                : BadRequest(new { result.Message });
        }
        #endregion

        [HttpGet("plans/{planId}/report")]
        public async Task<IActionResult> GenerateReport(
            [FromRoute] int planId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var pdfBytes = await _reportService.GenerateWorkSchedulePdfAsync(planId, startDate, endDate);
                var reportDate = DateTime.Now;
                return File(pdfBytes, "application/pdf",
                    $"WorkSchedule_Plan_{planId}_{reportDate:yyyyMMdd_HHmm}.pdf");
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