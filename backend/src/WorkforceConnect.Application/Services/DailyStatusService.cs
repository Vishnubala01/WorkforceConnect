using System.Text;
using Microsoft.EntityFrameworkCore;
using WorkforceConnect.Application.Common;
using WorkforceConnect.Application.DTOs;
using WorkforceConnect.Application.Interfaces;
using WorkforceConnect.Application.Security;
using WorkforceConnect.Domain.Entities;

namespace WorkforceConnect.Application.Services;

public sealed class DailyStatusService(
    IApplicationDbContext db,
    IDataScopeService scopes,
    ICurrentUser currentUser) : IDailyStatusService
{
    private static DailyStatusResponse MapToResponse(DailyStatus s) => new(
        s.Id,
        s.Date,
        s.EmployeeId,
        s.Employee?.EmployeeCode ?? string.Empty,
        s.Employee?.FullName ?? string.Empty,
        s.Employee?.Email ?? string.Empty,
        s.OrganizationId,
        s.Organization?.Name ?? string.Empty,
        s.TeamId,
        s.Team?.Name,
        s.ProjectOrTeamName ?? s.Team?.Name,
        s.WorkItemId,
        s.TaskActivity,
        s.PlannedWorkForToday,
        s.WorkCompletedProgress,
        s.Status,
        s.HoursSpent,
        s.BlockerDependency,
        s.SupportRequired,
        s.ReviewerLead,
        s.Remarks,
        s.CreatedAt,
        s.UpdatedAt);

    public async Task<PagedResponse<DailyStatusResponse>> ListAsync(DailyStatusQuery query, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var statuses = scope.Filter(db.DailyStatuses.AsNoTracking());

        if (query.FromDate.HasValue)
        {
            var from = query.FromDate.Value.Date;
            statuses = statuses.Where(s => s.Date >= from);
        }

        if (query.ToDate.HasValue)
        {
            var to = query.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            statuses = statuses.Where(s => s.Date <= to);
        }

        if (query.OrganizationId is int orgId)
        {
            statuses = statuses.Where(s => s.OrganizationId == orgId);
        }

        if (query.TeamId is int teamId)
        {
            statuses = statuses.Where(s => s.TeamId == teamId);
        }

        if (query.EmployeeId is int empId)
        {
            statuses = statuses.Where(s => s.EmployeeId == empId);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var st = query.Status.Trim();
            statuses = statuses.Where(s => s.Status == st);
        }

        var search = query.Search.NullIfBlank();
        if (search is not null)
        {
            statuses = statuses.Where(s =>
                s.TaskActivity.Contains(search)
                || (s.WorkItemId != null && s.WorkItemId.Contains(search))
                || (s.ProjectOrTeamName != null && s.ProjectOrTeamName.Contains(search))
                || s.Employee.FirstName.Contains(search)
                || s.Employee.LastName.Contains(search)
                || s.Employee.EmployeeCode.Contains(search));
        }

        var total = await statuses.CountAsync(ct);

        var items = await statuses
            .OrderByDescending(s => s.Date)
            .ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.Employee)
            .Include(s => s.Organization)
            .Include(s => s.Team)
            .Select(s => MapToResponse(s))
            .ToListAsync(ct);

        return new PagedResponse<DailyStatusResponse>(items, total, page, pageSize);
    }

    public async Task<DailyStatusResponse> GetAsync(int id, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        var status = await scope.Filter(db.DailyStatuses.AsNoTracking())
            .Include(s => s.Employee)
            .Include(s => s.Organization)
            .Include(s => s.Team)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("Daily status not found.");

        return MapToResponse(status);
    }

    public async Task<DailyStatusResponse> CreateAsync(SaveDailyStatusRequest request, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        // Determine employee: if user is Employee / TL without explicit EmployeeId or self-scoped, use current user's employee
        int targetEmployeeId;
        if (request.EmployeeId.HasValue && request.EmployeeId.Value > 0)
        {
            targetEmployeeId = request.EmployeeId.Value;
        }
        else if (scope.EmployeeId.HasValue)
        {
            targetEmployeeId = scope.EmployeeId.Value;
        }
        else
        {
            // Try resolving employee from CurrentUser
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct);
            if (user?.EmployeeId == null)
            {
                throw new AppValidationException("An employee record must be linked to create daily status.");
            }
            targetEmployeeId = user.EmployeeId.Value;
        }

        var employee = await db.Employees
            .Include(e => e.Organization)
            .Include(e => e.Team)
            .FirstOrDefaultAsync(e => e.Id == targetEmployeeId && e.IsActive, ct)
            ?? throw new NotFoundException("Employee not found or inactive.");

        if (!scope.CanAccessEmployee(employee.OrganizationId, employee.TeamId, employee.Id))
        {
            throw new ForbiddenException("You cannot submit daily status for this employee.");
        }

        var orgId = request.OrganizationId ?? employee.OrganizationId;
        var teamId = request.TeamId ?? employee.TeamId;
        var projectTeam = !string.IsNullOrWhiteSpace(request.ProjectOrTeamName)
            ? request.ProjectOrTeamName.Trim()
            : employee.Team?.Name;

        var entity = new DailyStatus
        {
            Date = request.Date.Date,
            EmployeeId = employee.Id,
            OrganizationId = orgId,
            TeamId = teamId,
            ProjectOrTeamName = projectTeam,
            WorkItemId = request.WorkItemId?.Trim(),
            TaskActivity = request.TaskActivity.Trim(),
            PlannedWorkForToday = request.PlannedWorkForToday.Trim(),
            WorkCompletedProgress = request.WorkCompletedProgress.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "In Progress" : request.Status.Trim(),
            HoursSpent = request.HoursSpent,
            BlockerDependency = request.BlockerDependency?.Trim(),
            SupportRequired = request.SupportRequired?.Trim(),
            ReviewerLead = request.ReviewerLead?.Trim(),
            Remarks = request.Remarks?.Trim()
        };

        db.DailyStatuses.Add(entity);
        await db.SaveChangesAsync(ct);

        return await GetAsync(entity.Id, ct);
    }

    public async Task<DailyStatusResponse> UpdateAsync(int id, SaveDailyStatusRequest request, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        var entity = await db.DailyStatuses
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("Daily status not found.");

        if (!scope.CanAccessEmployee(entity.OrganizationId, entity.TeamId, entity.EmployeeId))
        {
            throw new ForbiddenException("You cannot edit this daily status.");
        }

        entity.Date = request.Date.Date;
        if (!string.IsNullOrWhiteSpace(request.ProjectOrTeamName))
        {
            entity.ProjectOrTeamName = request.ProjectOrTeamName.Trim();
        }
        entity.WorkItemId = request.WorkItemId?.Trim();
        entity.TaskActivity = request.TaskActivity.Trim();
        entity.PlannedWorkForToday = request.PlannedWorkForToday.Trim();
        entity.WorkCompletedProgress = request.WorkCompletedProgress.Trim();
        entity.Status = string.IsNullOrWhiteSpace(request.Status) ? "In Progress" : request.Status.Trim();
        entity.HoursSpent = request.HoursSpent;
        entity.BlockerDependency = request.BlockerDependency?.Trim();
        entity.SupportRequired = request.SupportRequired?.Trim();
        entity.ReviewerLead = request.ReviewerLead?.Trim();
        entity.Remarks = request.Remarks?.Trim();

        await db.SaveChangesAsync(ct);

        return await GetAsync(entity.Id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);

        var entity = await db.DailyStatuses
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("Daily status not found.");

        if (!scope.CanAccessEmployee(entity.OrganizationId, entity.TeamId, entity.EmployeeId))
        {
            throw new ForbiddenException("You cannot delete this daily status.");
        }

        db.DailyStatuses.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<byte[]> ExportCsvAsync(DailyStatusQuery query, CancellationToken ct = default)
    {
        var scope = await scopes.GetAsync(ct);
        var statuses = scope.Filter(db.DailyStatuses.AsNoTracking());

        if (query.FromDate.HasValue) statuses = statuses.Where(s => s.Date >= query.FromDate.Value.Date);
        if (query.ToDate.HasValue) statuses = statuses.Where(s => s.Date <= query.ToDate.Value.Date.AddDays(1).AddTicks(-1));
        if (query.OrganizationId is int orgId) statuses = statuses.Where(s => s.OrganizationId == orgId);
        if (query.TeamId is int teamId) statuses = statuses.Where(s => s.TeamId == teamId);
        if (query.EmployeeId is int empId) statuses = statuses.Where(s => s.EmployeeId == empId);
        if (!string.IsNullOrWhiteSpace(query.Status)) statuses = statuses.Where(s => s.Status == query.Status.Trim());

        var list = await statuses
            .OrderByDescending(s => s.Date)
            .ThenBy(s => s.Employee.FirstName)
            .Include(s => s.Employee)
            .Include(s => s.Organization)
            .Include(s => s.Team)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        // Matching template columns exactly
        sb.AppendLine("Date,Team Member,Project / Team,Work Item ID,Task / Activity,Planned Work for Today,Work Completed / Progress,Status,Hours Spent,Blocker / Dependency,Support Required,Reviewer / Lead,Remarks");

        foreach (var item in list)
        {
            var dateStr = item.Date.ToString("dd-MMM-yyyy");
            var member = EscapeCsv(item.Employee?.FullName ?? "");
            var proj = EscapeCsv(item.ProjectOrTeamName ?? item.Team?.Name ?? "");
            var workItem = EscapeCsv(item.WorkItemId ?? "");
            var task = EscapeCsv(item.TaskActivity);
            var planned = EscapeCsv(item.PlannedWorkForToday);
            var completed = EscapeCsv(item.WorkCompletedProgress);
            var status = EscapeCsv(item.Status);
            var hours = item.HoursSpent.ToString("0.##");
            var blocker = EscapeCsv(item.BlockerDependency ?? "");
            var support = EscapeCsv(item.SupportRequired ?? "");
            var reviewer = EscapeCsv(item.ReviewerLead ?? "");
            var remarks = EscapeCsv(item.Remarks ?? "");

            sb.AppendLine($"{dateStr},{member},{proj},{workItem},{task},{planned},{completed},{status},{hours},{blocker},{support},{reviewer},{remarks}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<byte[]> ExportExcelAsync(DailyStatusQuery query, CancellationToken ct = default)
    {
        // Generates an XML-based Excel Spreadsheet 2003 (.xls/.xml) or clean formatted OpenXML-compatible workbook
        var scope = await scopes.GetAsync(ct);
        var statuses = scope.Filter(db.DailyStatuses.AsNoTracking());

        if (query.FromDate.HasValue) statuses = statuses.Where(s => s.Date >= query.FromDate.Value.Date);
        if (query.ToDate.HasValue) statuses = statuses.Where(s => s.Date <= query.ToDate.Value.Date.AddDays(1).AddTicks(-1));
        if (query.OrganizationId is int orgId) statuses = statuses.Where(s => s.OrganizationId == orgId);
        if (query.TeamId is int teamId) statuses = statuses.Where(s => s.TeamId == teamId);
        if (query.EmployeeId is int empId) statuses = statuses.Where(s => s.EmployeeId == empId);
        if (!string.IsNullOrWhiteSpace(query.Status)) statuses = statuses.Where(s => s.Status == query.Status.Trim());

        var list = await statuses
            .OrderByDescending(s => s.Date)
            .ThenBy(s => s.Employee.FirstName)
            .Include(s => s.Employee)
            .Include(s => s.Organization)
            .Include(s => s.Team)
            .ToListAsync(ct);

        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\"?>");
        xml.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        xml.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
        xml.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
        xml.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
        xml.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
        xml.AppendLine(" <Styles>");
        xml.AppendLine("  <Style ss:ID=\"Header\">");
        xml.AppendLine("   <Font ss:Bold=\"1\" ss:Color=\"#FFFFFF\"/>");
        xml.AppendLine("   <Interior ss:Color=\"#1B365D\" ss:Pattern=\"Solid\"/>");
        xml.AppendLine("   <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Center\" ss:WrapText=\"1\"/>");
        xml.AppendLine("   <Borders><Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#FFFFFF\"/></Borders>");
        xml.AppendLine("  </Style>");
        xml.AppendLine("  <Style ss:ID=\"DataCell\">");
        xml.AppendLine("   <Alignment ss:Vertical=\"Top\" ss:WrapText=\"1\"/>");
        xml.AppendLine("   <Borders>");
        xml.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#D1D5DB\"/>");
        xml.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#D1D5DB\"/>");
        xml.AppendLine("   </Borders>");
        xml.AppendLine("  </Style>");
        xml.AppendLine("  <Style ss:ID=\"DataCenter\">");
        xml.AppendLine("   <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Top\"/>");
        xml.AppendLine("   <Borders>");
        xml.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#D1D5DB\"/>");
        xml.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#D1D5DB\"/>");
        xml.AppendLine("   </Borders>");
        xml.AppendLine("  </Style>");
        xml.AppendLine(" </Styles>");

        xml.AppendLine(" <Worksheet ss:Name=\"Team_Daily_Status_Tracker\">");
        xml.AppendLine("  <Table ss:DefaultRowHeight=\"22\">");
        xml.AppendLine("   <Column ss:Width=\"90\"/>");  // Date
        xml.AppendLine("   <Column ss:Width=\"130\"/>"); // Team Member
        xml.AppendLine("   <Column ss:Width=\"120\"/>"); // Project / Team
        xml.AppendLine("   <Column ss:Width=\"90\"/>");  // Work Item ID
        xml.AppendLine("   <Column ss:Width=\"140\"/>"); // Task / Activity
        xml.AppendLine("   <Column ss:Width=\"220\"/>"); // Planned Work
        xml.AppendLine("   <Column ss:Width=\"220\"/>"); // Work Completed
        xml.AppendLine("   <Column ss:Width=\"90\"/>");  // Status
        xml.AppendLine("   <Column ss:Width=\"75\"/>");  // Hours Spent
        xml.AppendLine("   <Column ss:Width=\"140\"/>"); // Blocker / Dependency
        xml.AppendLine("   <Column ss:Width=\"130\"/>"); // Support Required
        xml.AppendLine("   <Column ss:Width=\"120\"/>"); // Reviewer / Lead
        xml.AppendLine("   <Column ss:Width=\"140\"/>"); // Remarks

        // Header Row matching template exactly
        xml.AppendLine("   <Row ss:Height=\"30\">");
        var headers = new[]
        {
            "Date", "Team Member", "Project / Team", "Work Item ID", "Task / Activity",
            "Planned Work for Today", "Work Completed / Progress", "Status",
            "Hours Spent", "Blocker / Dependency", "Support Required", "Reviewer / Lead", "Remarks"
        };
        foreach (var h in headers)
        {
            xml.AppendLine($"    <Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">{EscapeXml(h)}</Data></Cell>");
        }
        xml.AppendLine("   </Row>");

        // Data Rows
        foreach (var item in list)
        {
            xml.AppendLine("   <Row>");
            xml.AppendLine($"    <Cell ss:StyleID=\"DataCenter\"><Data ss:Type=\"String\">{EscapeXml(item.Date.ToString("dd-MMM-yyyy"))}</Data></Cell>");
            xml.AppendLine($"    <Cell ss:StyleID=\"DataCell\"><Data ss:Type=\"String\">{EscapeXml(item.Employee?.FullName ?? "")}</Data></Cell>");
            xml.AppendLine($"    <Cell ss:StyleID=\"DataCell\"><Data ss:Type=\"String\">{EscapeXml(item.ProjectOrTeamName ?? item.Team?.Name ?? "")}</Data></Cell>");
            xml.AppendLine($"    <Cell ss:StyleID=\"DataCell\"><Data ss:Type=\"String\">{EscapeXml(item.WorkItemId ?? "")}</Data></Cell>");
            xml.AppendLine($"    <Cell ss:StyleID=\"DataCell\"><Data ss:Type=\"String\">{EscapeXml(item.TaskActivity)}</Data></Cell>");
            xml.AppendLine($"    <Cell ss:StyleID=\"DataCell\"><Data ss:Type=\"String\">{EscapeXml(item.PlannedWorkForToday)}</Data></Cell>");
            xml.AppendLine($"    <Cell ss:StyleID=\"DataCell\"><Data ss:Type=\"String\">{EscapeXml(item.WorkCompletedProgress)}</Data></Cell>");
            xml.AppendLine($"    <Cell ss:StyleID=\"DataCenter\"><Data ss:Type=\"String\">{EscapeXml(item.Status)}</Data></Cell>");
            xml.AppendLine($"    <Cell ss:StyleID=\"DataCenter\"><Data ss:Type=\"Number\">{item.HoursSpent}</Data></Cell>");
            xml.AppendLine($"    <Cell ss:StyleID=\"DataCell\"><Data ss:Type=\"String\">{EscapeXml(item.BlockerDependency ?? "")}</Data></Cell>");
            xml.AppendLine($"    <Cell ss:StyleID=\"DataCell\"><Data ss:Type=\"String\">{EscapeXml(item.SupportRequired ?? "")}</Data></Cell>");
            xml.AppendLine($"    <Cell ss:StyleID=\"DataCell\"><Data ss:Type=\"String\">{EscapeXml(item.ReviewerLead ?? "")}</Data></Cell>");
            xml.AppendLine($"    <Cell ss:StyleID=\"DataCell\"><Data ss:Type=\"String\">{EscapeXml(item.Remarks ?? "")}</Data></Cell>");
            xml.AppendLine("   </Row>");
        }

        xml.AppendLine("  </Table>");
        xml.AppendLine(" </Worksheet>");
        xml.AppendLine("</Workbook>");

        return Encoding.UTF8.GetBytes(xml.ToString());
    }

    private static string EscapeCsv(string text)
    {
        if (string.IsNullOrEmpty(text)) return "\"\"";
        var escaped = text.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Replace("&", "&amp;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\"", "&quot;")
                   .Replace("'", "&apos;");
    }
}
