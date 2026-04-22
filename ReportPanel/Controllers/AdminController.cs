using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using ReportPanel.Services;
using ReportPanel.ViewModels;

namespace ReportPanel.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;
        private readonly IConfiguration _configuration;
        private readonly UserRoleSyncService _userRoleSync;

        public AdminController(ReportPanelContext context, AuditLogService auditLog, IConfiguration configuration, UserRoleSyncService userRoleSync)
        {
            _context = context;
            _auditLog = auditLog;
            _configuration = configuration;
            _userRoleSync = userRoleSync;
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
                DataSources = await _context.DataSources.OrderBy(d => d.DataSourceKey).ToListAsync(),
                Reports = await _context.ReportCatalog
                    .Include(r => r.DataSource)
                    .Include(r => r.ReportCategories)
                        .ThenInclude(rc => rc.Category)
                    .OrderBy(r => r.ReportId)
                    .ToListAsync(),
                Users = await _context.Users.OrderBy(u => u.Username).ToListAsync(),
                Roles = await _context.Roles.OrderBy(r => r.Name).ToListAsync(),
                Categories = await _context.ReportCategories.OrderBy(c => c.Name).ToListAsync()
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
                        var newDs = new DataSource
                        {
                            DataSourceKey = Request.Form["DataSourceKey"].ToString().ToUpper(),
                            Title = Request.Form["Title"].ToString(),
                            ConnString = Request.Form["ConnString"].ToString(),
                            IsActive = ReadFormBool("IsActive")
                        };
                        _context.DataSources.Add(newDs);
                        await _context.SaveChangesAsync();
                        await AuditCrudAsync("datasource_create", "datasource", newDs.DataSourceKey, "Data source created",
                            newValues: new { newDs.DataSourceKey, newDs.Title, newDs.IsActive },
                            dataSourceKey: newDs.DataSourceKey);
                        TempData["Message"] = "Veri kaynağı eklendi";
                        TempData["MessageType"] = "success";
                        break;

                    case "update_datasource":
                        var ds = await _context.DataSources.FindAsync(key);
                        if (ds != null)
                        {
                            var dsOld = new { ds.DataSourceKey, ds.Title, ds.IsActive };
                            ds.Title = Request.Form["Title"].ToString();
                            ds.ConnString = Request.Form["ConnString"].ToString();
                            ds.IsActive = ReadFormBool("IsActive");
                            await _context.SaveChangesAsync();
                            await AuditCrudAsync("datasource_update", "datasource", ds.DataSourceKey, "Data source updated",
                                oldValues: dsOld,
                                newValues: new { ds.DataSourceKey, ds.Title, ds.IsActive },
                                dataSourceKey: ds.DataSourceKey);
                            TempData["Message"] = "Veri kaynağı güncellendi";
                            TempData["MessageType"] = "success";
                        }
                        break;

                    case "delete_datasource":
                        var delDs = await _context.DataSources.FindAsync(key);
                        if (delDs != null)
                        {
                            _context.DataSources.Remove(delDs);
                            await _context.SaveChangesAsync();
                        await _auditLog.LogAsync(new AuditLogEntry
                        {
                            EventType = "datasource_delete",
                            TargetType = "datasource",
                            TargetKey = delDs.DataSourceKey,
                            Description = "Data source deleted",
                            OldValuesJson = AuditLogService.ToJson(new
                            {
                                delDs.DataSourceKey,
                                delDs.Title,
                                delDs.ConnString,
                                delDs.IsActive
                            })
                        });
                            TempData["Message"] = "Veri kaynağı silindi";
                            TempData["MessageType"] = "success";
                        }
                        break;

                    case "create_report":
                        var newReport = new ReportCatalog
                        {
                            Title = Request.Form["Title"].ToString(),
                            Description = Request.Form["Description"].ToString(),
                                                        DataSourceKey = Request.Form["DataSourceKey"].ToString(),
                            ProcName = Request.Form["ProcName"].ToString(),
                            AllowedRoles = NormalizeRolesByRoleIds(Request.Form["SelectedRoles"]),
                            IsActive = ReadFormBool("IsActive"),
                            ParamSchemaJson = NormalizeParamSchema(Request.Form["ParamSchemaJson"].ToString())
                        };
                        _context.ReportCatalog.Add(newReport);
                        await _context.SaveChangesAsync();
                        await SyncReportRolesAndCategories(newReport.ReportId);
                        await AuditCrudAsync("report_create", "report", newReport.ReportId.ToString(), "Report created",
                            newValues: new { newReport.ReportId, newReport.Title, newReport.DataSourceKey, newReport.ProcName, newReport.AllowedRoles, newReport.IsActive },
                            dataSourceKey: newReport.DataSourceKey,
                            reportId: newReport.ReportId);
                        TempData["Message"] = "Rapor eklendi";
                        TempData["MessageType"] = "success";
                        break;

                    case "update_report":
                        var report = await _context.ReportCatalog.FindAsync(id);
                        if (report != null)
                        {
                            var reportOld = new { report.ReportId, report.Title, report.DataSourceKey, report.ProcName, report.AllowedRoles, report.IsActive };
                            report.Title = Request.Form["Title"].ToString();
                            report.Description = Request.Form["Description"].ToString();
                            report.DataSourceKey = Request.Form["DataSourceKey"].ToString();
                            report.ProcName = Request.Form["ProcName"].ToString();
                            report.AllowedRoles = NormalizeRolesByRoleIds(Request.Form["SelectedRoles"]);
                            report.IsActive = ReadFormBool("IsActive");
                            report.ParamSchemaJson = NormalizeParamSchema(Request.Form["ParamSchemaJson"].ToString(), report.ParamSchemaJson);
                            report.ReportType = Request.Form["ReportType"].ToString() is "dashboard" ? "dashboard" : "table";
                            report.DashboardHtml = report.ReportType == "dashboard" ? Request.Form["DashboardHtml"].ToString() : null;
                report.DashboardConfigJson = report.ReportType == "dashboard" ? Request.Form["DashboardConfigJson"].ToString() : null;
                            await _context.SaveChangesAsync();
                            await SyncReportRolesAndCategories(report.ReportId);
                            await AuditCrudAsync("report_update", "report", report.ReportId.ToString(), "Report updated",
                                oldValues: reportOld,
                                newValues: new { report.ReportId, report.Title, report.DataSourceKey, report.ProcName, report.AllowedRoles, report.IsActive },
                                dataSourceKey: report.DataSourceKey,
                                reportId: report.ReportId);
                            TempData["Message"] = "Rapor güncellendi";
                            TempData["MessageType"] = "success";
                        }
                        break;

                    case "delete_report":
                        var delReport = await _context.ReportCatalog.FindAsync(id);
                        if (delReport != null)
                        {
                            _context.ReportCatalog.Remove(delReport);
                            await _context.SaveChangesAsync();
                        await _auditLog.LogAsync(new AuditLogEntry
                        {
                            EventType = "report_delete",
                            TargetType = "report",
                            TargetKey = delReport.ReportId.ToString(),
                            ReportId = delReport.ReportId,
                            DataSourceKey = delReport.DataSourceKey,
                            Description = "Report deleted",
                            OldValuesJson = AuditLogService.ToJson(new
                            {
                                delReport.ReportId,
                                delReport.Title,
                                delReport.DataSourceKey,
                                delReport.ProcName,
                                delReport.AllowedRoles,
                                delReport.IsActive
                            })
                        });
                            TempData["Message"] = "Rapor silindi";
                            TempData["MessageType"] = "success";
                        }
                        break;
                    case "create_role":
                        var roleName = Request.Form["Name"].ToString().Trim();
                        if (string.IsNullOrWhiteSpace(roleName))
                        {
                            TempData["Message"] = "Rol adi zorunludur.";
                            TempData["MessageType"] = "error";
                            break;
                        }
                        var roleExists = await _context.Roles
                            .AnyAsync(r => r.Name.ToLower() == roleName.ToLower());
                        if (roleExists)
                        {
                            TempData["Message"] = "Ayni isimde rol zaten var.";
                            TempData["MessageType"] = "error";
                            break;
                        }
                        var newRole = new Role
                        {
                            Name = roleName,
                            Description = Request.Form["Description"].ToString(),
                            IsActive = ReadFormBool("IsActive")
                        };
                        _context.Roles.Add(newRole);
                        await _context.SaveChangesAsync();
                        await AuditCrudAsync("role_create", "role", newRole.RoleId.ToString(), "Role created",
                            newValues: new { newRole.RoleId, newRole.Name, newRole.Description, newRole.IsActive });
                        TempData["Message"] = "Rol eklendi.";
                        TempData["MessageType"] = "success";
                        break;
                    case "update_role":
                        var role = await _context.Roles.FindAsync(id);
                        if (role != null)
                        {
                            var oldName = role.Name;
                            var newName = Request.Form["Name"].ToString().Trim();
                            if (string.IsNullOrWhiteSpace(newName))
                            {
                                TempData["Message"] = "Rol adi zorunludur.";
                                TempData["MessageType"] = "error";
                                break;
                            }
                            var duplicate = await _context.Roles
                                .AnyAsync(r => r.RoleId != role.RoleId && r.Name.ToLower() == newName.ToLower());
                            if (duplicate)
                            {
                                TempData["Message"] = "Ayni isimde rol zaten var.";
                                TempData["MessageType"] = "error";
                                break;
                            }
                            var roleOldSnap = new { role.RoleId, Name = oldName, role.Description, role.IsActive };
                            role.Name = newName;
                            role.Description = Request.Form["Description"].ToString();
                            role.IsActive = ReadFormBool("IsActive");
                            await _context.SaveChangesAsync();
                            if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
                            {
                                await ReplaceRoleNameInCsv(oldName, newName);
                            }
                            await AuditCrudAsync("role_update", "role", role.RoleId.ToString(), "Role updated",
                                oldValues: roleOldSnap,
                                newValues: new { role.RoleId, role.Name, role.Description, role.IsActive });
                            TempData["Message"] = "Rol guncellendi.";
                            TempData["MessageType"] = "success";
                        }
                        break;
                    case "delete_role":
                        var delRole = await _context.Roles.FindAsync(id);
                        if (delRole != null)
                        {
                            await RemoveRoleNameFromCsv(delRole.Name);
                            _context.Roles.Remove(delRole);
                            await _context.SaveChangesAsync();
                            await AuditCrudAsync("role_delete", "role", delRole.RoleId.ToString(), "Role deleted",
                                oldValues: new { delRole.RoleId, delRole.Name, delRole.Description, delRole.IsActive });
                            TempData["Message"] = "Rol silindi.";
                            TempData["MessageType"] = "success";
                        }
                        break;
                    case "create_category":
                        var categoryName = Request.Form["Name"].ToString().Trim();
                        if (string.IsNullOrWhiteSpace(categoryName))
                        {
                            TempData["Message"] = "Kategori adi zorunludur.";
                            TempData["MessageType"] = "error";
                            break;
                        }
                        var categoryExists = await _context.ReportCategories
                            .AnyAsync(c => c.Name.ToLower() == categoryName.ToLower());
                        if (categoryExists)
                        {
                            TempData["Message"] = "Ayni isimde kategori zaten var.";
                            TempData["MessageType"] = "error";
                            break;
                        }
                        var newCategory = new ReportCategory
                        {
                            Name = categoryName,
                            Description = Request.Form["Description"].ToString(),
                            IsActive = ReadFormBool("IsActive")
                        };
                        _context.ReportCategories.Add(newCategory);
                        await _context.SaveChangesAsync();
                        await AuditCrudAsync("category_create", "category", newCategory.CategoryId.ToString(), "Category created",
                            newValues: new { newCategory.CategoryId, newCategory.Name, newCategory.Description, newCategory.IsActive });
                        TempData["Message"] = "Kategori eklendi.";
                        TempData["MessageType"] = "success";
                        break;
                    case "update_category":
                        var category = await _context.ReportCategories.FindAsync(id);
                        if (category != null)
                        {
                            var newCategoryName = Request.Form["Name"].ToString().Trim();
                            if (string.IsNullOrWhiteSpace(newCategoryName))
                            {
                                TempData["Message"] = "Kategori adi zorunludur.";
                                TempData["MessageType"] = "error";
                                break;
                            }
                            var duplicateCategory = await _context.ReportCategories
                                .AnyAsync(c => c.CategoryId != category.CategoryId && c.Name.ToLower() == newCategoryName.ToLower());
                            if (duplicateCategory)
                            {
                                TempData["Message"] = "Ayni isimde kategori zaten var.";
                                TempData["MessageType"] = "error";
                                break;
                            }
                            var categoryOldSnap = new { category.CategoryId, category.Name, category.Description, category.IsActive };
                            category.Name = newCategoryName;
                            category.Description = Request.Form["Description"].ToString();
                            category.IsActive = ReadFormBool("IsActive");
                            await _context.SaveChangesAsync();
                            await AuditCrudAsync("category_update", "category", category.CategoryId.ToString(), "Category updated",
                                oldValues: categoryOldSnap,
                                newValues: new { category.CategoryId, category.Name, category.Description, category.IsActive });
                            TempData["Message"] = "Kategori guncellendi.";
                            TempData["MessageType"] = "success";
                        }
                        break;
                    case "delete_category":
                        var delCategory = await _context.ReportCategories.FindAsync(id);
                        if (delCategory != null)
                        {
                            _context.ReportCategories.Remove(delCategory);
                            await _context.SaveChangesAsync();
                            await AuditCrudAsync("category_delete", "category", delCategory.CategoryId.ToString(), "Category deleted",
                                oldValues: new { delCategory.CategoryId, delCategory.Name, delCategory.Description, delCategory.IsActive });
                            TempData["Message"] = "Kategori silindi.";
                            TempData["MessageType"] = "success";
                        }
                        break;
                    case "delete_user":
                        var delUser = await _context.Users.FindAsync(id);
                        if (delUser != null)
                        {
                            _context.Users.Remove(delUser);
                            await _context.SaveChangesAsync();
                              await _auditLog.LogAsync(new AuditLogEntry
                              {
                                  EventType = "user_delete",
                                  TargetType = "user",
                                  TargetKey = delUser.UserId.ToString(),
                                  Description = "User deleted",
                                  // M-03: Audit snapshot rolleri UserRole junction'dan (deprecate CSV yerine).
                                  OldValuesJson = AuditLogService.ToJson(new
                                  {
                                      delUser.UserId,
                                      delUser.Username,
                                      delUser.FullName,
                                      delUser.Email,
                                      Roles = _context.UserRoles
                                          .Where(ur => ur.UserId == delUser.UserId)
                                          .Select(ur => ur.Role!.Name)
                                          .ToList(),
                                      delUser.IsActive
                                  })
                              });
                            TempData["Message"] = "Kullanici silindi";
                            TempData["MessageType"] = "success";
                        }
                        break;

                    case "test_datasource":
                        var testDs = await _context.DataSources.FindAsync(key);
                        if (testDs != null)
                        {
                            try
                            {
                                using var connection = new Microsoft.Data.SqlClient.SqlConnection(testDs.ConnString);
                                await connection.OpenAsync();
                                using var command = new Microsoft.Data.SqlClient.SqlCommand("SELECT 1", connection);
                                await command.ExecuteScalarAsync();
                                  await _auditLog.LogAsync(new AuditLogEntry
                                  {
                                      EventType = "datasource_test",
                                      TargetType = "datasource",
                                      TargetKey = testDs.DataSourceKey,
                                      DataSourceKey = testDs.DataSourceKey,
                                      Description = "Data source test OK",
                                      IsSuccess = true
                                  });
                                TempData["Message"] = "Bağlantı testi başarılı";
                                TempData["MessageType"] = "success";
                            }
                            catch (Exception ex)
                            {
                                await _auditLog.LogAsync(new AuditLogEntry
                                {
                                    EventType = "datasource_test",
                                    TargetType = "datasource",
                                    TargetKey = testDs.DataSourceKey,
                                    DataSourceKey = testDs.DataSourceKey,
                                    Description = "Data source test failed",
                                    IsSuccess = false,
                                    ErrorMessage = ex.Message
                                });
                                // M-02: ex.Message user'a gosterilmez (connection string sizabilir). Detay audit log'ta.
                                TempData["Message"] = "Veri kaynağına bağlanılamadı. Bağlantı ayarlarını kontrol edin.";
                                TempData["MessageType"] = "error";
                            }
                        }
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

        private static string NormalizeParamSchema(string? raw, string? fallback = "{}")
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.IsNullOrWhiteSpace(fallback) ? "{}" : fallback;
            }

            return raw.Trim();
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        [Route("Admin/ProcParams")]
        public async Task<IActionResult> ProcParams(string dataSourceKey, string procName)
        {
            if (string.IsNullOrWhiteSpace(dataSourceKey) || string.IsNullOrWhiteSpace(procName))
            {
                return BadRequest("Missing parameters.");
            }

            // F-02 takip: SP adi schema.proc formatinda veya sadece proc adi olarak gelebilir.
            // Sadece proc verilmisse varsayilan schema = dbo.
            var trimmed = procName.Trim();
            var match = Regex.Match(trimmed, @"^(?<schema>[A-Za-z_][A-Za-z0-9_]*)\.(?<proc>[A-Za-z_][A-Za-z0-9_]*)$");
            string schemaName, procShortName;
            if (match.Success)
            {
                schemaName = match.Groups["schema"].Value;
                procShortName = match.Groups["proc"].Value;
            }
            else if (Regex.IsMatch(trimmed, @"^[A-Za-z_][A-Za-z0-9_]*$"))
            {
                schemaName = "dbo";
                procShortName = trimmed;
            }
            else
            {
                return BadRequest("Invalid procedure name.");
            }

            var dataSource = await _context.DataSources
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DataSourceKey == dataSourceKey && d.IsActive);

            if (dataSource == null)
            {
                return BadRequest("Data source not found.");
            }

            var parameters = new List<object>();
            const string sql = @"
SELECT p.name, t.name AS type_name, p.has_default_value, p.is_output
FROM sys.parameters p
JOIN sys.objects o ON p.object_id = o.object_id
JOIN sys.types t ON p.user_type_id = t.user_type_id
WHERE o.type IN ('P','PC')
  AND o.name = @ProcName
  AND SCHEMA_NAME(o.schema_id) = @SchemaName
ORDER BY p.parameter_id;";

            await using var connection = new SqlConnection(dataSource.ConnString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ProcName", procShortName);
            command.Parameters.AddWithValue("@SchemaName", schemaName);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                if (name.StartsWith("@", StringComparison.Ordinal))
                {
                    name = name.Substring(1);
                }
                var typeName = reader.GetString(1);
                var hasDefault = reader.GetBoolean(2);
                var isOutput = reader.GetBoolean(3);
                var required = !hasDefault && !isOutput;

                parameters.Add(new
                {
                    name,
                    label = name,
                    type = MapSqlType(typeName),
                    required
                });
            }

            return Json(new { fields = parameters });
        }

        private static string MapSqlType(string sqlType)
        {
            var lower = sqlType.ToLowerInvariant();
            return lower switch
            {
                "int" => "number",
                "bigint" => "number",
                "smallint" => "number",
                "tinyint" => "number",
                "decimal" => "decimal",
                "numeric" => "decimal",
                "money" => "decimal",
                "smallmoney" => "decimal",
                "float" => "decimal",
                "real" => "decimal",
                "bit" => "checkbox",
                "date" => "date",
                "datetime" => "date",
                "datetime2" => "date",
                "smalldatetime" => "date",
                "datetimeoffset" => "date",
                _ => "text"
            };
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        [Route("Admin/FilterOptions")]
        public async Task<IActionResult> FilterOptions(string filterKey, string? dataSourceKey = null)
        {
            if (string.IsNullOrWhiteSpace(filterKey))
                return BadRequest("FilterKey gerekli.");

            // Data source bul — belirtilmişse onu, yoksa PDKS'i dene
            var dsKey = dataSourceKey;
            if (string.IsNullOrWhiteSpace(dsKey))
            {
                // İlk aktif data source'u bul
                var first = await _context.DataSources.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.IsActive);
                dsKey = first?.DataSourceKey;
            }

            var ds = await _context.DataSources.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DataSourceKey == dsKey && d.IsActive);
            if (ds == null)
                return Json(new { options = Array.Empty<object>() });

            string sql = filterKey.ToLowerInvariant() switch
            {
                "sube" => "SELECT CAST(SubeNo AS varchar(10)) AS Value, SubeAd AS Label FROM vrd.SubeListe ORDER BY SubeAd",
                "bolum" => "SELECT DISTINCT Bolum AS Value, Bolum AS Label FROM vrd.VardiyaDetay WHERE Bolum IS NOT NULL AND Bolum <> '' ORDER BY Bolum",
                _ => ""
            };

            if (string.IsNullOrEmpty(sql))
                return Json(new { options = Array.Empty<object>() });

            var options = new List<object>();
            try
            {
                await using var conn = new SqlConnection(ds.ConnString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    options.Add(new
                    {
                        value = reader["Value"]?.ToString() ?? "",
                        label = reader["Label"]?.ToString() ?? ""
                    });
                }
            }
            catch
            {
                // Bağlantı hatası — boş liste dön
            }

            return Json(new { options });
        }

        // DataSource'taki stored procedure listesi (builder dropdown'i icin)
        [HttpGet]
        [Route("Admin/SpList")]
        public async Task<IActionResult> SpList(string dataSourceKey)
        {
            if (string.IsNullOrWhiteSpace(dataSourceKey))
                return Json(new { success = false, error = "DataSource secilmedi.", procedures = Array.Empty<object>() });

            var ds = await _context.DataSources.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DataSourceKey == dataSourceKey && d.IsActive);
            if (ds == null)
                return Json(new { success = false, error = "DataSource bulunamadi veya pasif.", procedures = Array.Empty<object>() });

            var procs = new List<object>();
            try
            {
                await using var conn = new SqlConnection(ds.ConnString);
                await conn.OpenAsync();
                const string sql = @"
                    SELECT SCHEMA_NAME(schema_id) AS SchemaName, name AS ProcName
                    FROM sys.procedures
                    WHERE is_ms_shipped = 0
                    ORDER BY SCHEMA_NAME(schema_id), name";
                await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 15 };
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var schema = reader["SchemaName"]?.ToString() ?? "dbo";
                    var name = reader["ProcName"]?.ToString() ?? "";
                    var full = schema == "dbo" ? name : $"{schema}.{name}";
                    procs.Add(new { name = full, schema, shortName = name });
                }
            }
            catch (Exception ex)
            {
                // M-02: connection exception message user'a gosterilmez (credentials sizinti riski).
                _ = ex;
                return Json(new { success = false, error = "Veri kaynağına bağlanılamadı. Bağlantı ayarlarını kontrol edin.", procedures = Array.Empty<object>() });
            }

            return Json(new { success = true, procedures = procs });
        }

        // Stored procedure onizleme: SP'yi tip-bazli default'larla calistir; admin override
        // isterse paramsJson query parametresiyle (key = param adi @ prefix'siz, value = string)
        // belirli parametrelerin degerini gecersiz kilar.
        [HttpGet]
        [Route("Admin/SpPreview")]
        public async Task<IActionResult> SpPreview(string dataSourceKey, string procName, int maxRows = 10, string? paramsJson = null)
        {
            if (string.IsNullOrWhiteSpace(dataSourceKey) || string.IsNullOrWhiteSpace(procName))
                return Json(new { success = false, error = "DataSource ve ProcName gerekli." });

            var ds = await _context.DataSources.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DataSourceKey == dataSourceKey && d.IsActive);
            if (ds == null)
                return Json(new { success = false, error = "DataSource bulunamadi." });

            // maxRows guvenlik: 1..100 arasinda olsun
            if (maxRows < 1) maxRows = 10;
            if (maxRows > 100) maxRows = 100;

            // Admin override: parametre adi -> string deger haritasi (case-insensitive).
            Dictionary<string, string> overrides = new(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(paramsJson))
            {
                try
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(paramsJson);
                    if (parsed != null)
                    {
                        foreach (var kv in parsed)
                        {
                            if (!string.IsNullOrWhiteSpace(kv.Key)) overrides[kv.Key.TrimStart('@')] = kv.Value ?? "";
                        }
                    }
                }
                catch { /* gecersiz JSON -> override yok, default'larla devam */ }
            }

            var resultSets = new List<object>();
            try
            {
                await using var conn = new SqlConnection(ds.ConnString);
                await conn.OpenAsync();

                // SP parametrelerini bul, zorunlu olanlari NULL ile doldur (SP NULL kabul etmezse hata verecek, mesaj iletilir)
                var paramList = new List<SqlParameter>();
                try
                {
                    await using var metaCmd = new SqlCommand(
                        @"SELECT PARAMETER_NAME, DATA_TYPE
                          FROM INFORMATION_SCHEMA.PARAMETERS
                          WHERE SPECIFIC_NAME = @sp
                          ORDER BY ORDINAL_POSITION", conn) { CommandTimeout = 10 };
                    // SCHEMA.NAME formatinda gelirse kisa adi al
                    var shortName = procName.Contains('.') ? procName[(procName.LastIndexOf('.') + 1)..] : procName;
                    metaCmd.Parameters.AddWithValue("@sp", shortName);
                    await using var metaReader = await metaCmd.ExecuteReaderAsync();
                    while (await metaReader.ReadAsync())
                    {
                        var pname = metaReader["PARAMETER_NAME"]?.ToString() ?? "";
                        if (string.IsNullOrWhiteSpace(pname)) continue;
                        var ptype = metaReader["DATA_TYPE"]?.ToString()?.ToLowerInvariant() ?? "";

                        // F-02: SP NULL kabul etmezse patlamasin diye tip-bazli sensible default.
                        object defaultValue = ptype switch
                        {
                            "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => DateTime.UtcNow.Date,
                            "time" => TimeSpan.Zero,
                            "int" or "bigint" or "smallint" or "tinyint" => 0,
                            "bit" => false,
                            "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => 0m,
                            "char" or "varchar" or "nchar" or "nvarchar" or "text" or "ntext" => string.Empty,
                            "uniqueidentifier" => Guid.Empty,
                            _ => DBNull.Value
                        };

                        // F-02 override: admin'in verdigi deger varsa tip'e cast ederek default yerine kullan.
                        var pnameClean = pname.TrimStart('@');
                        object finalValue = defaultValue;
                        if (overrides.TryGetValue(pnameClean, out var overrideRaw) && !string.IsNullOrWhiteSpace(overrideRaw))
                        {
                            finalValue = ptype switch
                            {
                                "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset"
                                    => DateTime.TryParse(overrideRaw, out var d) ? (object)d : defaultValue,
                                "time"
                                    => TimeSpan.TryParse(overrideRaw, out var t) ? (object)t : defaultValue,
                                "int" or "bigint" or "smallint" or "tinyint"
                                    => long.TryParse(overrideRaw, out var n) ? (object)n : defaultValue,
                                "bit"
                                    => overrideRaw == "1" || overrideRaw.Equals("true", StringComparison.OrdinalIgnoreCase),
                                "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real"
                                    => decimal.TryParse(overrideRaw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var m) ? (object)m : defaultValue,
                                "uniqueidentifier"
                                    => Guid.TryParse(overrideRaw, out var g) ? (object)g : defaultValue,
                                _
                                    => overrideRaw
                            };
                        }

                        paramList.Add(new SqlParameter(pname, finalValue));
                    }
                }
                catch { /* parametre cikartma basarisiz olursa yine de SP'yi parametresiz deneriz */ }

                await using var cmd = new SqlCommand(procName, conn)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 30
                };
                if (paramList.Count > 0) cmd.Parameters.AddRange(paramList.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();
                var rsIndex = 0;
                do
                {
                    var columns = new List<object>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        columns.Add(new
                        {
                            name = reader.GetName(i),
                            type = reader.GetFieldType(i)?.Name ?? "object"
                        });
                    }

                    var rows = new List<Dictionary<string, object?>>();
                    var rowCount = 0;
                    while (await reader.ReadAsync() && rowCount < maxRows)
                    {
                        var row = new Dictionary<string, object?>();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                        }
                        rows.Add(row);
                        rowCount++;
                    }
                    // geri kalan satirlari at (sayim icin)
                    var totalRows = rowCount;
                    while (await reader.ReadAsync()) totalRows++;

                    resultSets.Add(new
                    {
                        index = rsIndex,
                        columns,
                        rows,
                        rowCount = totalRows,
                        truncated = totalRows > maxRows
                    });
                    rsIndex++;
                } while (await reader.NextResultAsync());
            }
            catch (SqlException sx)
            {
                // Admin-only endpoint: SQL hatasini admin'e gostermek SP debug icin faydali.
                // Number ile birlikte gonderiyoruz; Message zaten SqlException'in kisa ozeti.
                return Json(new { success = false, error = $"SQL hatası ({sx.Number}): {sx.Message}", resultSets });
            }
            catch (Exception ex)
            {
                // M-02: generic connection exception user'a sizintisiz.
                _ = ex;
                return Json(new { success = false, error = "Veri kaynağına bağlanılamadı.", resultSets });
            }

            return Json(new { success = true, resultSets });
        }

        // Ayrı sayfa action'ları
        [Route("Admin/CreateDataSource")]
        public IActionResult CreateDataSource()
        {
            return View(new AdminDataSourceFormViewModel
            {
                DataSource = new DataSource { IsActive = true },
                TemplateConnString = ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/CreateDataSource")]
        public async Task<IActionResult> CreateDataSource(DataSource dataSource)
        {
            try
            {
                // Debug: Form değerlerini kontrol et
                var isActiveFormValue = Request.Form["IsActive"].ToString();
                Console.WriteLine($"Form IsActive değeri: '{isActiveFormValue}'");
                Console.WriteLine($"Model IsActive değeri: {dataSource.IsActive}");
                
                // Manuel olarak IsActive değerini set et
                dataSource.IsActive = ReadFormBool("IsActive");
                
                Console.WriteLine($"Final IsActive değeri: {dataSource.IsActive}");
                
                dataSource.DataSourceKey = dataSource.DataSourceKey.ToUpper();
                _context.DataSources.Add(dataSource);
                await _context.SaveChangesAsync();
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "datasource_create",
                    TargetType = "datasource",
                    TargetKey = dataSource.DataSourceKey,
                    Description = "Data source created",
                    NewValuesJson = AuditLogService.ToJson(new
                    {
                        dataSource.DataSourceKey,
                        dataSource.Title,
                        dataSource.IsActive
                    })
                });
                TempData["Message"] = "Veri kaynağı başarıyla eklendi";
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "datasources" });
            }
            catch (Exception ex)
            {
                // M-02: CreateDataSource (EF SaveChangesAsync fail path).
                _ = ex;
                TempData["Message"] = "Veri kaynağı oluşturulurken hata oluştu.";
                TempData["MessageType"] = "error";
                return View(new AdminDataSourceFormViewModel
                {
                    DataSource = dataSource,
                    TemplateConnString = ""
                });
            }
        }

        [Route("Admin/EditDataSource/{key}")]
        public async Task<IActionResult> EditDataSource(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                TempData["Message"] = "Veri kaynağı anahtarı belirtilmedi";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "datasources" });
            }
            
            var dataSource = await _context.DataSources
                .Where(d => d.DataSourceKey == key)
                .FirstOrDefaultAsync();
            if (dataSource == null)
            {
                TempData["Message"] = $"Veri kaynağı bulunamadı: '{key}'";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "datasources" });
            }
            return View(new AdminDataSourceFormViewModel
            {
                DataSource = dataSource,
                TemplateConnString = ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EditDataSource/{key}")]
        public async Task<IActionResult> EditDataSource(DataSource dataSource)
        {
            try
            {
                // Manuel olarak IsActive değerini set et
                dataSource.IsActive = ReadFormBool("IsActive");
                
                _context.DataSources.Update(dataSource);
                await _context.SaveChangesAsync();
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "datasource_update",
                    TargetType = "datasource",
                    TargetKey = dataSource.DataSourceKey,
                    Description = "Data source updated",
                    NewValuesJson = AuditLogService.ToJson(new
                    {
                        dataSource.DataSourceKey,
                        dataSource.Title,
                        dataSource.ConnString,
                        dataSource.IsActive
                    })
                });
                TempData["Message"] = "Veri kaynağı başarıyla güncellendi";
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "datasources" });
            }
            catch (Exception ex)
            {
                // M-02: EditDataSource.
                _ = ex;
                TempData["Message"] = "Veri kaynağı güncellenirken hata oluştu.";
                TempData["MessageType"] = "error";
                return View(new AdminDataSourceFormViewModel
                {
                    DataSource = dataSource,
                    TemplateConnString = ""
                });
            }
        }
        [Route("Admin/CreateReport")]
        public async Task<IActionResult> CreateReport()
        {
            try
            {
                // Tum veri kaynaklarini kontrol et
                var allDataSources = await _context.DataSources.ToListAsync();
                var activeDataSources = allDataSources.Where(d => d.IsActive).ToList();
                var roles = await _context.Roles
                    .AsNoTracking()
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Name)
                    .ToListAsync();
                var categories = await _context.ReportCategories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                var model = new AdminReportFormViewModel
                {
                    Report = new ReportCatalog { IsActive = true, AllowedRoles = "admin" },
                    DataSources = activeDataSources,
                    AvailableRoles = roles,
                    SelectedRoleIds = new HashSet<int>(roles
                        .Where(r => string.Equals(r.Name, "admin", StringComparison.OrdinalIgnoreCase))
                        .Select(r => r.RoleId)),
                    AvailableCategories = categories,
                    SelectedCategoryIds = new HashSet<int>()
                };

                // Debug bilgisi
                Console.WriteLine($"Toplam veri kaynagi: {allDataSources.Count}");
                Console.WriteLine($"Aktif veri kaynagi: {activeDataSources.Count}");

                foreach (var ds in allDataSources)
                {
                    Console.WriteLine($"- {ds.DataSourceKey}: {ds.Title} (Aktif: {ds.IsActive})");
                }

                if (!activeDataSources.Any())
                {
                    if (allDataSources.Any())
                    {
                        TempData["Message"] = $"Toplam {allDataSources.Count} veri kaynagi var ama hicbiri aktif degil. Veri kaynaklarini aktif hale getirin.";
                        TempData["MessageType"] = "warning";
                    }
                    else
                    {
                        TempData["Message"] = "Hic veri kaynagi bulunamadi. Once veri kaynagi eklemeniz gerekiyor.";
                        TempData["MessageType"] = "warning";
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                // M-02: generic mesaj, detay log'a.
                _ = ex;
                TempData["Message"] = "Veri kaynakları yüklenirken hata oluştu.";
                TempData["MessageType"] = "error";
                return View(new AdminReportFormViewModel
                {
                    Report = new ReportCatalog { IsActive = true, AllowedRoles = "admin" },
                    DataSources = new List<DataSource>(),
                    AvailableRoles = new List<Role>(),
                    SelectedRoleIds = new HashSet<int>(),
                    AvailableCategories = new List<ReportCategory>(),
                    SelectedCategoryIds = new HashSet<int>()
                });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/CreateReport")]
        public async Task<IActionResult> CreateReport(ReportCatalog report)
        {
            try
            {
                // Manuel olarak IsActive değerini set et
                report.IsActive = ReadFormBool("IsActive");
                report.AllowedRoles = NormalizeRolesByRoleIds(Request.Form["SelectedRoles"]);
                report.ParamSchemaJson = NormalizeParamSchema(Request.Form["ParamSchemaJson"].ToString());
                report.ReportType = Request.Form["ReportType"].ToString() is "dashboard" ? "dashboard" : "table";
                report.DashboardHtml = report.ReportType == "dashboard" ? Request.Form["DashboardHtml"].ToString() : null;
                report.DashboardConfigJson = report.ReportType == "dashboard" ? Request.Form["DashboardConfigJson"].ToString() : null;

                _context.ReportCatalog.Add(report);
                await _context.SaveChangesAsync();
                await SyncReportRolesAndCategories(report.ReportId);
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "report_create",
                    TargetType = "report",
                    TargetKey = report.ReportId.ToString(),
                    ReportId = report.ReportId,
                    DataSourceKey = report.DataSourceKey,
                    Description = "Report created",
                    NewValuesJson = AuditLogService.ToJson(new
                    {
                        report.ReportId,
                        report.Title,
                        report.DataSourceKey,
                        report.ProcName,
                        report.AllowedRoles,
                        report.IsActive
                    })
                });
                TempData["Message"] = "Rapor başarıyla eklendi";
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "reports" });
            }
            catch (Exception ex)
            {
                // M-02: CreateReport.
                _ = ex;
                TempData["Message"] = "Rapor oluşturulurken hata oluştu.";
                TempData["MessageType"] = "error";
                var dataSources = await _context.DataSources.Where(d => d.IsActive).ToListAsync();
                var roles = await _context.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
                var categories = await _context.ReportCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
                return View(new AdminReportFormViewModel
                {
                    Report = report,
                    DataSources = dataSources,
                    AvailableRoles = roles,
                    SelectedRoleIds = ParseIds(Request.Form["SelectedRoles"]),
                    AvailableCategories = categories,
                    SelectedCategoryIds = ParseIds(Request.Form["SelectedCategories"])
                });
            }
        }
        [Route("Admin/EditReport/{id}")]
        public async Task<IActionResult> EditReport(int id)
        {
            var report = await _context.ReportCatalog.FindAsync(id);
            if (report == null)
            {
                TempData["Message"] = "Rapor bulunamadi";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "reports" });
            }

            var dataSources = await _context.DataSources.Where(d => d.IsActive).ToListAsync();
            var roles = await _context.Roles
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();
            var categories = await _context.ReportCategories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            var selectedRoleIds = await _context.ReportAllowedRoles
                .Where(ar => ar.ReportId == report.ReportId)
                .Select(ar => ar.RoleId)
                .ToListAsync();
            var selectedCategoryIds = await _context.ReportCategoryLinks
                .Where(rc => rc.ReportId == report.ReportId)
                .Select(rc => rc.CategoryId)
                .ToListAsync();
            var model = new AdminReportFormViewModel
            {
                Report = report,
                DataSources = dataSources,
                AvailableRoles = roles,
                SelectedRoleIds = selectedRoleIds.ToHashSet(),
                AvailableCategories = categories,
                SelectedCategoryIds = selectedCategoryIds.ToHashSet()
            };

            // Debug icin
            if (!dataSources.Any())
            {
                TempData["Message"] = "Aktif veri kaynagi bulunamadi. Once veri kaynagi eklemeniz gerekiyor.";
                TempData["MessageType"] = "warning";
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EditReport/{id}")]
        public async Task<IActionResult> EditReport(ReportCatalog report)
        {
            try
            {
                // Manuel olarak IsActive değerini set et
                report.IsActive = ReadFormBool("IsActive");
                report.AllowedRoles = NormalizeRolesByRoleIds(Request.Form["SelectedRoles"]);
                report.ParamSchemaJson = NormalizeParamSchema(Request.Form["ParamSchemaJson"].ToString(), report.ParamSchemaJson);
                report.ReportType = Request.Form["ReportType"].ToString() is "dashboard" ? "dashboard" : "table";
                report.DashboardHtml = report.ReportType == "dashboard" ? Request.Form["DashboardHtml"].ToString() : null;
                report.DashboardConfigJson = report.ReportType == "dashboard" ? Request.Form["DashboardConfigJson"].ToString() : null;

                _context.ReportCatalog.Update(report);
                await _context.SaveChangesAsync();
                await SyncReportRolesAndCategories(report.ReportId);
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "report_update",
                    TargetType = "report",
                    TargetKey = report.ReportId.ToString(),
                    ReportId = report.ReportId,
                    DataSourceKey = report.DataSourceKey,
                    Description = "Report updated",
                    NewValuesJson = AuditLogService.ToJson(new
                    {
                        report.ReportId,
                        report.Title,
                        report.Description,
                        report.DataSourceKey,
                        report.ProcName,
                        report.AllowedRoles,
                        report.IsActive,
                        report.ParamSchemaJson
                    })
                });
                TempData["Message"] = "Rapor başarıyla güncellendi";
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "reports" });
            }
            catch (Exception ex)
            {
                // M-02: EditReport.
                _ = ex;
                TempData["Message"] = "Rapor güncellenirken hata oluştu.";
                TempData["MessageType"] = "error";
                var dataSources = await _context.DataSources.Where(d => d.IsActive).ToListAsync();
                var roles = await _context.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
                var categories = await _context.ReportCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
                return View(new AdminReportFormViewModel
                {
                    Report = report,
                    DataSources = dataSources,
                    AvailableRoles = roles,
                    SelectedRoleIds = ParseIds(Request.Form["SelectedRoles"]),
                    AvailableCategories = categories,
                    SelectedCategoryIds = ParseIds(Request.Form["SelectedCategories"])
                });
            }
        }
        [Route("Admin/CreateUser")]
        public IActionResult CreateUser()
        {
            var roles = _context.Roles
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToList();
            var dataSources = _context.DataSources
                .AsNoTracking()
                .Where(ds => ds.IsActive)
                .OrderBy(ds => ds.Title)
                .ToList();
            return View(new AdminUserFormViewModel
            {
                User = new User { IsActive = true },
                AvailableRoles = roles,
                SelectedRoleIds = new HashSet<int>(),
                DataFilters = new List<UserDataFilter>(),
                DataSources = dataSources
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/CreateUser")]
        public async Task<IActionResult> CreateUser(User user)
        {
            var password = Request.Form["Password"].ToString();
            user.IsAdUser = ReadFormBool("IsAdUser");
            var selectedRoleIds = ParseIds(Request.Form["SelectedRoles"]);
            user.Username = NormalizeUsername(user.Username);
            user.FullName = user.FullName?.Trim() ?? "";
            user.Email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim();
            // M-03 Faz B: User.Roles deprecate + nullable — bos atama gereksiz, default null.
            user.IsActive = ReadFormBool("IsActive");

            if (string.IsNullOrWhiteSpace(user.Username) ||
                string.IsNullOrWhiteSpace(user.FullName) ||
                selectedRoleIds.Count == 0)
            {
                return View(await BuildCreateUserFormAsync(user, selectedRoleIds, "Zorunlu alanlar bos birakilamaz.", "error"));
            }

            if (!user.IsAdUser && string.IsNullOrWhiteSpace(password))
            {
                return View(await BuildCreateUserFormAsync(user, selectedRoleIds, "Sifre alani zorunludur.", "error"));
            }

            var exists = await _context.Users.AnyAsync(u => u.Username == user.Username);
            if (exists)
            {
                return View(await BuildCreateUserFormAsync(user, selectedRoleIds, "Bu kullanici adi zaten mevcut.", "error"));
            }

            user.PasswordHash = user.IsAdUser
                ? PasswordHasher.CreateHash(Guid.NewGuid().ToString("N"))
                : PasswordHasher.CreateHash(password);
            user.CreatedAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await _userRoleSync.SyncAsync(user.UserId, selectedRoleIds);
            await SyncUserDataFilters(user.UserId);

            // M-03: Audit snapshot'ta rol isimleri UserRole junction'dan hesaplanir.
            var createdRoleNames = await _context.Roles
                .Where(r => selectedRoleIds.Contains(r.RoleId))
                .Select(r => r.Name)
                .ToListAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "user_create",
                TargetType = "user",
                TargetKey = user.UserId.ToString(),
                Description = "User created",
                NewValuesJson = AuditLogService.ToJson(new
                {
                    user.UserId,
                    user.Username,
                    user.FullName,
                    user.Email,
                    Roles = createdRoleNames,
                    user.IsAdUser,
                    user.IsActive
                })
            });
            TempData["Message"] = "Kullanici eklendi";
            TempData["MessageType"] = "success";
            return RedirectToAction("Index", new { tab = "users" });
        }
        [Route("Admin/EditUser/{id}")]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["Message"] = "Kullanici bulunamadi";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "users" });
            }

            var roles = await _context.Roles
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();
            var selectedRoleIds = await _context.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .Select(ur => ur.RoleId)
                .ToListAsync();
            var dataFilters = await _context.UserDataFilters
                .Where(f => f.UserId == user.UserId)
                .OrderBy(f => f.FilterKey)
                .ThenBy(f => f.FilterValue)
                .ToListAsync();
            var dataSources = await _context.DataSources
                .Where(ds => ds.IsActive)
                .OrderBy(ds => ds.Title)
                .ToListAsync();
            return View(new AdminUserFormViewModel
            {
                User = user,
                AvailableRoles = roles,
                SelectedRoleIds = selectedRoleIds.ToHashSet(),
                DataFilters = dataFilters,
                DataSources = dataSources
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EditUser/{id}")]
        public async Task<IActionResult> EditUser(User user)
        {
            var password = Request.Form["Password"].ToString();
            var existing = await _context.Users.FindAsync(user.UserId);
            if (existing == null)
            {
                TempData["Message"] = "Kullanici bulunamadi";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "users" });
            }

            var username = NormalizeUsername(user.Username);
            var fullName = user.FullName?.Trim() ?? "";
            var selectedRoleIds = ParseIds(Request.Form["SelectedRoles"]);
            // M-03: User.Roles CSV deprecate — rol yazimi SyncUserRoles'ta.
            var email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim();
            var isAdUser = ReadFormBool("IsAdUser");
            var wasAdUser = existing.IsAdUser;

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(fullName) ||
                selectedRoleIds.Count == 0)
            {
                var allRoles = await _context.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
                return View(new AdminUserFormViewModel
                {
                    User = user,
                    AvailableRoles = allRoles,
                    SelectedRoleIds = selectedRoleIds,
                    Message = "Zorunlu alanlar bos birakilamaz.",
                    MessageType = "error"
                });
            }

            var exists = await _context.Users.AnyAsync(u => u.Username == username && u.UserId != existing.UserId);
            if (exists)
            {
                var allRoles = await _context.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
                return View(new AdminUserFormViewModel
                {
                    User = user,
                    AvailableRoles = allRoles,
                    SelectedRoleIds = selectedRoleIds,
                    Message = "Bu kullanici adi zaten mevcut.",
                    MessageType = "error"
                });
            }

            existing.Username = username;
            existing.FullName = fullName;
            existing.Email = email;
            // M-03: existing.Roles yazilmaz (UserRole junction kullanilir).
            existing.IsAdUser = isAdUser;
            existing.IsActive = ReadFormBool("IsActive");
            if (existing.IsAdUser)
            {
                if (!string.IsNullOrWhiteSpace(password))
                {
                    existing.PasswordHash = PasswordHasher.CreateHash(Guid.NewGuid().ToString("N"));
                }
            }
            else if (!string.IsNullOrWhiteSpace(password))
            {
                existing.PasswordHash = PasswordHasher.CreateHash(password);
            }
            else if (!existing.IsAdUser && wasAdUser && string.IsNullOrWhiteSpace(password))
            {
                var allRoles = await _context.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
                return View(new AdminUserFormViewModel
                {
                    User = user,
                    AvailableRoles = allRoles,
                    SelectedRoleIds = selectedRoleIds,
                    Message = "Sifre alani zorunludur.",
                    MessageType = "error"
                });
            }
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await _userRoleSync.SyncAsync(existing.UserId, selectedRoleIds);
            await SyncUserDataFilters(existing.UserId);

            // M-03: Audit snapshot rolleri UserRole junction'dan hesaplar.
            var updatedRoleNames = await _context.Roles
                .Where(r => selectedRoleIds.Contains(r.RoleId))
                .Select(r => r.Name)
                .ToListAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "user_update",
                TargetType = "user",
                TargetKey = existing.UserId.ToString(),
                Description = "User updated",
                NewValuesJson = AuditLogService.ToJson(new
                {
                    existing.UserId,
                    existing.Username,
                    existing.FullName,
                    existing.Email,
                    Roles = updatedRoleNames,
                    existing.IsAdUser,
                    existing.IsActive
                })
            });
            TempData["Message"] = "Kullanici guncellendi";
            TempData["MessageType"] = "success";
            return RedirectToAction("Index", new { tab = "users" });
        }

        private static string NormalizeUsername(string? raw)
        {
            var value = raw?.Trim() ?? "";
            var slashIndex = value.IndexOf('\\');
            if (slashIndex > 0 && slashIndex < value.Length - 1)
            {
                return value.Substring(slashIndex + 1);
            }
            return value;
        }

        [Route("Admin/EditRole/{id}")]
        public async Task<IActionResult> EditRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                TempData["Message"] = "Rol bulunamadi.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "roles" });
            }

            return View(new AdminRoleFormViewModel
            {
                Role = role
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EditRole/{id}")]
        public async Task<IActionResult> EditRole(Role role)
        {
            var existing = await _context.Roles.FindAsync(role.RoleId);
            if (existing == null)
            {
                TempData["Message"] = "Rol bulunamadi.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "roles" });
            }

            var name = role.Name?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                return View(new AdminRoleFormViewModel
                {
                    Role = role,
                    Message = "Rol adi zorunludur.",
                    MessageType = "error"
                });
            }

            var duplicate = await _context.Roles
                .AnyAsync(r => r.RoleId != existing.RoleId && r.Name.ToLower() == name.ToLower());
            if (duplicate)
            {
                return View(new AdminRoleFormViewModel
                {
                    Role = role,
                    Message = "Ayni isimde rol zaten var.",
                    MessageType = "error"
                });
            }

            var oldName = existing.Name;
            existing.Name = name;
            existing.Description = role.Description?.Trim();
            existing.IsActive = ReadFormBool("IsActive");
            await _context.SaveChangesAsync();
            if (!string.Equals(oldName, name, StringComparison.OrdinalIgnoreCase))
            {
                await ReplaceRoleNameInCsv(oldName, name);
            }

            TempData["Message"] = "Rol guncellendi.";
            TempData["MessageType"] = "success";
            return RedirectToAction("Index", new { tab = "roles" });
        }

        [Route("Admin/EditCategory/{id}")]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.ReportCategories.FindAsync(id);
            if (category == null)
            {
                TempData["Message"] = "Kategori bulunamadi.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "categories" });
            }

            return View(new AdminCategoryFormViewModel
            {
                Category = category
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EditCategory/{id}")]
        public async Task<IActionResult> EditCategory(ReportCategory category)
        {
            var existing = await _context.ReportCategories.FindAsync(category.CategoryId);
            if (existing == null)
            {
                TempData["Message"] = "Kategori bulunamadi.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "categories" });
            }

            var name = category.Name?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                return View(new AdminCategoryFormViewModel
                {
                    Category = category,
                    Message = "Kategori adi zorunludur.",
                    MessageType = "error"
                });
            }

            var duplicate = await _context.ReportCategories
                .AnyAsync(c => c.CategoryId != existing.CategoryId && c.Name.ToLower() == name.ToLower());
            if (duplicate)
            {
                return View(new AdminCategoryFormViewModel
                {
                    Category = category,
                    Message = "Ayni isimde kategori zaten var.",
                    MessageType = "error"
                });
            }

            existing.Name = name;
            existing.Description = category.Description?.Trim();
            existing.IsActive = ReadFormBool("IsActive");
            await _context.SaveChangesAsync();

            TempData["Message"] = "Kategori guncellendi.";
            TempData["MessageType"] = "success";
            return RedirectToAction("Index", new { tab = "categories" });
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

        private async Task<string> BuildRolesCsv(HashSet<int> roleIds)
        {
            if (roleIds.Count == 0)
            {
                return "";
            }

            var names = await _context.Roles
                .Where(r => roleIds.Contains(r.RoleId))
                .Select(r => r.Name)
                .ToListAsync();
            return string.Join(",", names);
        }

        private async Task<AdminUserFormViewModel> BuildCreateUserFormAsync(User user, HashSet<int> selectedRoleIds, string message, string messageType)
        {
            var roles = await _context.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
            var dataSources = await _context.DataSources.Where(ds => ds.IsActive).OrderBy(ds => ds.Title).ToListAsync();
            // Form tarafindan gonderilen filtreleri geri yukle ki kullanici kayip hissetmesin
            var filterKeys = Request.Form["FilterKeys"].ToArray();
            var filterValues = Request.Form["FilterValues"].ToArray();
            var filterDataSources = Request.Form["FilterDataSources"].ToArray();
            var postedFilters = new List<UserDataFilter>();
            for (var i = 0; i < filterKeys.Length; i++)
            {
                var key = filterKeys[i]?.Trim();
                var value = i < filterValues.Length ? filterValues[i]?.Trim() : null;
                var ds = i < filterDataSources.Length ? filterDataSources[i]?.Trim() : null;
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) continue;
                postedFilters.Add(new UserDataFilter
                {
                    FilterKey = key!,
                    FilterValue = value!,
                    DataSourceKey = string.IsNullOrWhiteSpace(ds) ? null : ds
                });
            }
            return new AdminUserFormViewModel
            {
                User = user,
                AvailableRoles = roles,
                SelectedRoleIds = selectedRoleIds,
                DataFilters = postedFilters,
                DataSources = dataSources,
                Message = message,
                MessageType = messageType
            };
        }

        // M-04: SyncUserRoles Services/UserRoleSyncService'e tasindi (testable).

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

        private async Task SyncUserDataFilters(int userId)
        {
            // Form'dan filtre verilerini oku: FilterKeys[], FilterValues[], FilterDataSources[]
            var keys = Request.Form["FilterKeys"].ToArray();
            var values = Request.Form["FilterValues"].ToArray();
            var dataSources = Request.Form["FilterDataSources"].ToArray();

            // Mevcut filtreleri sil
            var existing = await _context.UserDataFilters
                .Where(f => f.UserId == userId)
                .ToListAsync();
            _context.UserDataFilters.RemoveRange(existing);

            // Yeni filtreleri ekle
            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i]?.Trim();
                var value = i < values.Length ? values[i]?.Trim() : null;
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) continue;

                var ds = i < dataSources.Length ? dataSources[i]?.Trim() : null;
                _context.UserDataFilters.Add(new UserDataFilter
                {
                    UserId = userId,
                    FilterKey = key,
                    FilterValue = value,
                    DataSourceKey = string.IsNullOrWhiteSpace(ds) ? null : ds,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task SyncReportRolesAndCategories(int reportId)
        {
            var selectedRoleIds = ParseIds(Request.Form["SelectedRoles"]);
            var selectedCategoryIds = ParseIds(Request.Form["SelectedCategories"]);

            var existingRoles = await _context.ReportAllowedRoles
                .Where(ar => ar.ReportId == reportId)
                .ToListAsync();
            _context.ReportAllowedRoles.RemoveRange(existingRoles);

            foreach (var roleId in selectedRoleIds)
            {
                _context.ReportAllowedRoles.Add(new ReportAllowedRole
                {
                    ReportId = reportId,
                    RoleId = roleId,
                    CreatedAt = DateTime.Now
                });
            }

            var existingCategories = await _context.ReportCategoryLinks
                .Where(rc => rc.ReportId == reportId)
                .ToListAsync();
            _context.ReportCategoryLinks.RemoveRange(existingCategories);

            foreach (var categoryId in selectedCategoryIds)
            {
                _context.ReportCategoryLinks.Add(new ReportCategoryLink
                {
                    ReportId = reportId,
                    CategoryId = categoryId,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

        private string NormalizeRolesByRoleIds(Microsoft.Extensions.Primitives.StringValues selectedRoles)
        {
            var ids = ParseIds(selectedRoles);
            var names = _context.Roles
                .Where(r => ids.Contains(r.RoleId))
                .Select(r => r.Name)
                .ToList();

            return names.Count == 0 ? "" : string.Join(",", names);
        }

        private async Task ReplaceRoleNameInCsv(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            // M-03: User.Roles CSV deprecate — UserRole junction FK ile otomatik guncellenir, loop gerek yok.
            var reports = await _context.ReportCatalog.ToListAsync();
            foreach (var report in reports)
            {
                var updated = ReplaceCsvValue(report.AllowedRoles, oldName, newName);
                if (!string.Equals(updated, report.AllowedRoles, StringComparison.Ordinal))
                {
                    report.AllowedRoles = updated;
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task RemoveRoleNameFromCsv(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return;
            }

            // M-03: User.Roles CSV deprecate — UserRole junction FK ile otomatik yonetilir.
            var reports = await _context.ReportCatalog.ToListAsync();
            foreach (var report in reports)
            {
                var updated = RemoveCsvValue(report.AllowedRoles, roleName);
                if (!string.Equals(updated, report.AllowedRoles, StringComparison.Ordinal))
                {
                    report.AllowedRoles = updated;
                }
            }

            await _context.SaveChangesAsync();
        }

        private static string ReplaceCsvValue(string csv, string oldValue, string newValue)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                return "";
            }

            var values = csv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => string.Equals(v, oldValue, StringComparison.OrdinalIgnoreCase) ? newValue : v)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return values.Count == 0 ? "" : string.Join(",", values);
        }

        private static string RemoveCsvValue(string csv, string valueToRemove)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                return "";
            }

            var values = csv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(v => !string.Equals(v, valueToRemove, StringComparison.OrdinalIgnoreCase))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return values.Count == 0 ? "" : string.Join(",", values);
        }
    }
}
