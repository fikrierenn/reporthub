using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.Services;
using ReportPanel.ViewModels;

namespace ReportPanel.Controllers
{
    // Partial split (csharp-conventions hard-limit 500). Ana dosya: ctor + DI fields +
    // Index G+P + HandlePostAction + cross-cutting helpers (ReadFormBool, ReadBool,
    // ParseIds, ApplyResult, BuildReportFormInput, AuditCrudAsync,
    // GetTemplateConnectionString).
    //
    // Diger partial dosyalar:
    //   - AdminController.SpExplorer.cs   : ProcParams, FilterOptions, ValidateFormula,
    //                                       SpList, SpPreview (admin builder runtime)
    //   - AdminController.DataSources.cs  : CreateDataSource G+P, EditDataSource G+P
    //   - AdminController.Reports.cs      : CreateReport G+P, EditReport G+P,
    //                                       CreateReportV2 / EditReportV2 redirects,
    //                                       BuildReportFormViewModel
    //   - AdminController.Users.cs        : CreateUser G+P, EditUser G+P,
    //                                       BuildUserFormInput, BuildCreateUserFormAsync
    //   - AdminController.RolesCategories.cs : EditRole G+P, EditCategory G+P
    [Authorize(Roles = "admin")]
    public partial class AdminController : Controller
    {
        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;
        private readonly IConfiguration _configuration;
        private readonly UserRoleSyncService _userRoleSync;
        private readonly CategoryManagementService _categoryService;
        private readonly RoleManagementService _roleService;
        private readonly DataSourceManagementService _dataSourceService;
        private readonly ReportManagementService _reportService;
        private readonly UserManagementService _userService;
        private readonly SpExplorerService _spExplorer;
        private readonly FilterOptionsService _filterOptions;

        public AdminController(
            ReportPanelContext context,
            AuditLogService auditLog,
            IConfiguration configuration,
            UserRoleSyncService userRoleSync,
            CategoryManagementService categoryService,
            RoleManagementService roleService,
            DataSourceManagementService dataSourceService,
            ReportManagementService reportService,
            UserManagementService userService,
            SpExplorerService spExplorer,
            FilterOptionsService filterOptions)
        {
            _context = context;
            _auditLog = auditLog;
            _configuration = configuration;
            _userRoleSync = userRoleSync;
            _categoryService = categoryService;
            _roleService = roleService;
            _dataSourceService = dataSourceService;
            _reportService = reportService;
            _userService = userService;
            _spExplorer = spExplorer;
            _filterOptions = filterOptions;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string tab = "datasources")
        {
            if (string.Equals(tab, "logs", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Logs");
            }
            var model = new AdminIndexViewModel
            {
                ActiveTab = tab,
                Message = TempData["Message"]?.ToString() ?? "",
                MessageType = TempData["MessageType"]?.ToString() ?? "",
                DataSources = await _context.DataSources.AsNoTracking().OrderBy(d => d.DataSourceKey).ToListAsync(),
                Reports = await _context.ReportCatalog
                    .AsNoTracking()
                    .Include(r => r.DataSource)
                    .Include(r => r.ReportCategories)
                        .ThenInclude(rc => rc.Category)
                    .OrderBy(r => r.ReportId)
                    .ToListAsync(),
                Users = await _context.Users.AsNoTracking().OrderBy(u => u.Username).ToListAsync(),
                Roles = await _context.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(),
                Categories = await _context.ReportCategories.AsNoTracking().OrderBy(c => c.Name).ToListAsync()
            };

            // M-03 Faz B: user -> rol isimleri UserRole junction'dan (deprecate User.Roles CSV yerine).
            model.UserRoleNames = await _context.UserRoles
                .Include(ur => ur.Role)
                .GroupBy(ur => ur.UserId)
                .Select(g => new { UserId = g.Key, Names = g.Where(x => x.Role != null).Select(x => x.Role!.Name).ToList() })
                .ToDictionaryAsync(x => x.UserId, x => x.Names);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string tab = "datasources", string action = "", string key = "", int id = 0)
        {
            await HandlePostAction(action, key, id);
            return RedirectToAction("Index", new { tab });
        }

        private async Task HandlePostAction(string action, string key, int id)
        {
            try
            {
                switch (action)
                {
                    case "create_datasource":
                        ApplyResult(await _dataSourceService.CreateAsync(
                            Request.Form["DataSourceKey"],
                            Request.Form["Title"],
                            Request.Form["ConnString"],
                            ReadFormBool("IsActive")));
                        break;

                    case "update_datasource":
                        ApplyResult(await _dataSourceService.UpdateAsync(
                            key,
                            Request.Form["Title"],
                            Request.Form["ConnString"],
                            ReadFormBool("IsActive")));
                        break;

                    case "delete_datasource":
                        ApplyResult(await _dataSourceService.DeleteAsync(key));
                        break;

                    case "create_report":
                        ApplyResult(await _reportService.CreateAsync(BuildReportFormInput()));
                        break;
                    case "update_report":
                        ApplyResult(await _reportService.UpdateAsync(id, BuildReportFormInput()));
                        break;
                    case "delete_report":
                        ApplyResult(await _reportService.DeleteAsync(id));
                        break;
                    case "create_role":
                        ApplyResult(await _roleService.CreateAsync(
                            Request.Form["Name"],
                            Request.Form["Description"],
                            ReadFormBool("IsActive")));
                        break;
                    case "update_role":
                        ApplyResult(await _roleService.UpdateAsync(
                            id,
                            Request.Form["Name"],
                            Request.Form["Description"],
                            ReadFormBool("IsActive")));
                        break;
                    case "delete_role":
                        ApplyResult(await _roleService.DeleteAsync(id));
                        break;
                    case "create_category":
                        ApplyResult(await _categoryService.CreateAsync(
                            Request.Form["Name"],
                            Request.Form["Description"],
                            ReadFormBool("IsActive")));
                        break;
                    case "update_category":
                        ApplyResult(await _categoryService.UpdateAsync(
                            id,
                            Request.Form["Name"],
                            Request.Form["Description"],
                            ReadFormBool("IsActive")));
                        break;
                    case "delete_category":
                        ApplyResult(await _categoryService.DeleteAsync(id));
                        break;
                    case "delete_user":
                        ApplyResult(await _userService.DeleteAsync(id));
                        break;

                    case "test_datasource":
                        ApplyResult(await _dataSourceService.TestConnectionAsync(key));
                        break;

                }
            }
            catch (Exception ex)
            {
                // M-02: ex.Message user'a gosterilmez. HandlePostAction'in generic hata yolu.
                _ = ex;
                TempData["Message"] = "Beklenmedik bir hata oluştu. Lütfen sistem yöneticisine bildirin.";
                TempData["MessageType"] = "error";
            }
        }

        private string GetTemplateConnectionString(string template)
        {
            return template switch
            {
                "local_windows" => "Server=localhost\\SQLEXPRESS;Database=TestDB;Integrated Security=true;TrustServerCertificate=true;",
                "local_sql" => "Server=localhost\\SQLEXPRESS;Database=TestDB;User Id=sa;Password=;TrustServerCertificate=true;",
                "current" => _configuration.GetConnectionString("DefaultConnection") ?? "",
                _ => ""
            };
        }

        private bool ReadFormBool(string key)
        {
            return ReadBool(Request.Form[key]);
        }

        private static bool ReadBool(Microsoft.Extensions.Primitives.StringValues values)
        {
            foreach (var value in values)
            {
                if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "on", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "1", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static HashSet<int> ParseIds(Microsoft.Extensions.Primitives.StringValues values)
        {
            var ids = new HashSet<int>();
            foreach (var value in values)
            {
                if (int.TryParse(value, out var id))
                {
                    ids.Add(id);
                }
            }
            return ids;
        }

        // M-04: SyncUserRoles Services/UserRoleSyncService'e tasindi (testable).

        // M-01: Admin servislerinden donen AdminOperationResult'u TempData'ya yansitir.
        private void ApplyResult(AdminOperationResult result)
        {
            TempData["Message"] = result.Message;
            TempData["MessageType"] = result.TempDataType;
        }

        // M-01: ReportManagementService icin form -> DTO donusumu.
        private ReportFormInput BuildReportFormInput() => new(
            Title: Request.Form["Title"],
            Description: Request.Form["Description"],
            DataSourceKey: Request.Form["DataSourceKey"],
            ProcName: Request.Form["ProcName"],
            SelectedRoleIds: ParseIds(Request.Form["SelectedRoles"]),
            SelectedCategoryIds: ParseIds(Request.Form["SelectedCategories"]),
            IsActive: ReadFormBool("IsActive"),
            ReportType: "dashboard", // ADR-009 · M-11 F-1.5: form alanı kaldırıldı, hep dashboard.
            ParamSchemaJson: Request.Form["ParamSchemaJson"],
            DashboardConfigJson: Request.Form["DashboardConfigJson"]);

        // G-04: CRUD audit shortcut'u — HandlePostAction'da tekrarlayan AuditLogEntry dolumunu tek yere al.
        private Task AuditCrudAsync(
            string eventType,
            string targetType,
            string targetKey,
            string description,
            object? newValues = null,
            object? oldValues = null,
            string? dataSourceKey = null,
            int? reportId = null)
        {
            return _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = eventType,
                TargetType = targetType,
                TargetKey = targetKey,
                Description = description,
                NewValuesJson = newValues != null ? AuditLogService.ToJson(newValues) : null,
                OldValuesJson = oldValues != null ? AuditLogService.ToJson(oldValues) : null,
                DataSourceKey = dataSourceKey,
                ReportId = reportId,
                IsSuccess = true
            });
        }

        // M-01: SyncUserDataFilters UserManagementService.SyncDataFiltersAsync'e tasindi.

        // M-01: SyncReportRolesAndCategories + NormalizeRolesByRoleIds + NormalizeParamSchema
        // ReportManagementService'e tasindi (servis ici private).

        // M-01: Role CSV propagation helper'lari Services/RoleManagementService'e tasindi.
    }
}
