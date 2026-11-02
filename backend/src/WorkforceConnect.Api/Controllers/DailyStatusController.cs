using Microsoft.AspNetCore.Mvc;
using WorkforceConnect.Api.Authorization;
using WorkforceConnect.Application.DTOs;
using WorkforceConnect.Application.Interfaces;
using WorkforceConnect.Domain.Security;

namespace WorkforceConnect.Api.Controllers;

[ApiController]
[Route("api/dailystatus")]
public sealed class DailyStatusController(IDailyStatusService dailyStatusService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.DailyStatus.View)]
    public async Task<ActionResult<PagedResponse<DailyStatusResponse>>> List([FromQuery] DailyStatusQuery query, CancellationToken ct) =>
        Ok(await dailyStatusService.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.DailyStatus.View)]
    public async Task<ActionResult<DailyStatusResponse>> Get(int id, CancellationToken ct) =>
        Ok(await dailyStatusService.GetAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.DailyStatus.Create)]
    public async Task<ActionResult<DailyStatusResponse>> Create(SaveDailyStatusRequest request, CancellationToken ct)
    {
        var created = await dailyStatusService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.DailyStatus.Edit)]
    public async Task<ActionResult<DailyStatusResponse>> Update(int id, SaveDailyStatusRequest request, CancellationToken ct) =>
        Ok(await dailyStatusService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.DailyStatus.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await dailyStatusService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("export/excel")]
    [HasPermission(Permissions.DailyStatus.Export)]
    public async Task<IActionResult> ExportExcel([FromQuery] DailyStatusQuery query, CancellationToken ct)
    {
        var bytes = await dailyStatusService.ExportExcelAsync(query, ct);
        var filename = $"Team_Daily_Status_Tracker_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xls";
        return File(bytes, "application/vnd.ms-excel", filename);
    }

    [HttpGet("export/csv")]
    [HasPermission(Permissions.DailyStatus.Export)]
    public async Task<IActionResult> ExportCsv([FromQuery] DailyStatusQuery query, CancellationToken ct)
    {
        var bytes = await dailyStatusService.ExportCsvAsync(query, ct);
        var filename = $"Team_Daily_Status_Tracker_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", filename);
    }
}
