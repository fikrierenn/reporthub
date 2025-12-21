using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
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
        private static readonly string[] AvailableRoles = { "admin", "ik", "mali", "user" };
        private static readonly Dictionary<string, string> RoleDescriptions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "admin", "Tam yetki" },
            { "ik", "IK raporlari ve islemleri" },
            { "mali", "Mali raporlar ve islemler" },
            { "user", "Standart kullanici" }
        };

        public AdminController(ReportPanelContext context, AuditLogService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
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
                Reports = await _context.ReportCatalog.Include(r => r.DataSource).OrderBy(r => r.ReportId).ToListAsync(),
                Users = await _context.Users.OrderBy(u => u.Username).ToListAsync()
            };

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
                            IsActive = Request.Form["IsActive"].ToString() == "true"
                        };
                        _context.DataSources.Add(newDs);
                        await _context.SaveChangesAsync();
                        TempData["Message"] = "Veri kaynağı eklendi";
                        TempData["MessageType"] = "success";
                        break;

                    case "update_datasource":
                        var ds = await _context.DataSources.FindAsync(key);
                        if (ds != null)
                        {
                            ds.Title = Request.Form["Title"].ToString();
                            ds.ConnString = Request.Form["ConnString"].ToString();
                            ds.IsActive = Request.Form["IsActive"].ToString() == "true";
                            await _context.SaveChangesAsync();
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
                            AllowedRoles = NormalizeRoles(Request.Form["SelectedRoles"]),
                            IsActive = Request.Form["IsActive"].ToString() == "true",
                            ParamSchemaJson = NormalizeParamSchema(Request.Form["ParamSchemaJson"].ToString())
                        };
                        _context.ReportCatalog.Add(newReport);
                        await _context.SaveChangesAsync();
                        TempData["Message"] = "Rapor eklendi";
                        TempData["MessageType"] = "success";
                        break;

                    case "update_report":
                        var report = await _context.ReportCatalog.FindAsync(id);
                        if (report != null)
                        {
                            report.Title = Request.Form["Title"].ToString();
                            report.Description = Request.Form["Description"].ToString();
                            report.DataSourceKey = Request.Form["DataSourceKey"].ToString();
                            report.ProcName = Request.Form["ProcName"].ToString();
                            report.AllowedRoles = NormalizeRoles(Request.Form["SelectedRoles"]);
                            report.IsActive = Request.Form["IsActive"].ToString() == "true";
                            report.ParamSchemaJson = NormalizeParamSchema(Request.Form["ParamSchemaJson"].ToString(), report.ParamSchemaJson);
                            await _context.SaveChangesAsync();
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
                                  OldValuesJson = AuditLogService.ToJson(new
                                  {
                                      delUser.UserId,
                                      delUser.Username,
                                      delUser.FullName,
                                      delUser.Email,
                                      delUser.Roles,
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
                                TempData["Message"] = "Bağlantı hatası: " + ex.Message;
                                TempData["MessageType"] = "error";
                            }
                        }
                        break;

                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Hata: " + ex.Message;
                TempData["MessageType"] = "error";
            }
        }

        private string GetTemplateConnectionString(string template)
        {
            return template switch
            {
                "local_windows" => "Server=localhost\\SQLEXPRESS;Database=TestDB;Integrated Security=true;TrustServerCertificate=true;",
                "local_sql" => "Server=localhost\\SQLEXPRESS;Database=TestDB;User Id=sa;Password=;TrustServerCertificate=true;",
                "current" => "Server=localhost\\SQLEXPRESS;Database=PortalHUB;User Id=sa;Password=CHANGE_ME;TrustServerCertificate=true;",
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

            var match = Regex.Match(procName.Trim(), @"^(?<schema>[A-Za-z_][A-Za-z0-9_]*)\.(?<proc>[A-Za-z_][A-Za-z0-9_]*)$");
            if (!match.Success)
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

            var schemaName = match.Groups["schema"].Value;
            var procShortName = match.Groups["proc"].Value;

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
                dataSource.IsActive = Request.Form["IsActive"].ToString().Contains("true");
                
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
                TempData["Message"] = "Hata: " + ex.Message;
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
                dataSource.IsActive = Request.Form["IsActive"].ToString().Contains("true");
                
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
                TempData["Message"] = "Hata: " + ex.Message;
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
                // T??m veri kaynaklar??n?? kontrol et
                var allDataSources = await _context.DataSources.ToListAsync();
                var activeDataSources = allDataSources.Where(d => d.IsActive).ToList();

                var model = new AdminReportFormViewModel
                {
                    Report = new ReportCatalog { IsActive = true, AllowedRoles = "admin" },
                    DataSources = activeDataSources,
                    AvailableRoles = AvailableRoles,
                    SelectedRoles = ParseRoles("admin")
                };

                // Debug bilgisi
                Console.WriteLine($"Toplam veri kayna?Y??: {allDataSources.Count}");
                Console.WriteLine($"Aktif veri kayna?Y??: {activeDataSources.Count}");

                foreach (var ds in allDataSources)
                {
                    Console.WriteLine($"- {ds.DataSourceKey}: {ds.Title} (Aktif: {ds.IsActive})");
                }

                if (!activeDataSources.Any())
                {
                    if (allDataSources.Any())
                    {
                        TempData["Message"] = $"Toplam {allDataSources.Count} veri kayna?Y?? var ama hi??biri aktif de?Yil. Veri kaynaklar??n?? aktif hale getirin.";
                        TempData["MessageType"] = "warning";
                    }
                    else
                    {
                        TempData["Message"] = "Hi?? veri kayna?Y?? bulunamad??. ?-nce veri kayna?Y?? eklemeniz gerekiyor.";
                        TempData["MessageType"] = "warning";
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Veri kaynaklar?? y??klenirken hata: " + ex.Message;
                TempData["MessageType"] = "error";
                return View(new AdminReportFormViewModel
                {
                    Report = new ReportCatalog { IsActive = true, AllowedRoles = "admin" },
                    DataSources = new List<DataSource>(),
                    AvailableRoles = AvailableRoles,
                    SelectedRoles = ParseRoles("admin")
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
                report.IsActive = Request.Form["IsActive"].ToString().Contains("true");
                report.AllowedRoles = NormalizeRoles(Request.Form["SelectedRoles"]);
                report.ParamSchemaJson = NormalizeParamSchema(Request.Form["ParamSchemaJson"].ToString());
                
                _context.ReportCatalog.Add(report);
                await _context.SaveChangesAsync();
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
                TempData["Message"] = "Hata: " + ex.Message;
                TempData["MessageType"] = "error";
                var dataSources = await _context.DataSources.Where(d => d.IsActive).ToListAsync();
                return View(new AdminReportFormViewModel
                {
                    Report = report,
                    DataSources = dataSources,
                    AvailableRoles = AvailableRoles,
                    SelectedRoles = ParseRoles(report.AllowedRoles)
                });
            }
        }
        [Route("Admin/EditReport/{id}")]
        public async Task<IActionResult> EditReport(int id)
        {
            var report = await _context.ReportCatalog.FindAsync(id);
            if (report == null)
            {
                TempData["Message"] = "Rapor bulunamad??";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", new { tab = "reports" });
            }

            var dataSources = await _context.DataSources.Where(d => d.IsActive).ToListAsync();
            var model = new AdminReportFormViewModel
            {
                Report = report,
                DataSources = dataSources,
                AvailableRoles = AvailableRoles,
                SelectedRoles = ParseRoles(report.AllowedRoles)
            };

            // Debug i??in
            if (!dataSources.Any())
            {
                TempData["Message"] = "Aktif veri kayna?Y?? bulunamad??. ?-nce veri kayna?Y?? eklemeniz gerekiyor.";
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
                report.IsActive = Request.Form["IsActive"].ToString().Contains("true");
                report.AllowedRoles = NormalizeRoles(Request.Form["SelectedRoles"]);
                report.ParamSchemaJson = NormalizeParamSchema(Request.Form["ParamSchemaJson"].ToString(), report.ParamSchemaJson);

                _context.ReportCatalog.Update(report);
                await _context.SaveChangesAsync();
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
                TempData["Message"] = "Hata: " + ex.Message;
                TempData["MessageType"] = "error";
                var dataSources = await _context.DataSources.Where(d => d.IsActive).ToListAsync();
                return View(new AdminReportFormViewModel
                {
                    Report = report,
                    DataSources = dataSources,
                    AvailableRoles = AvailableRoles,
                    SelectedRoles = ParseRoles(report.AllowedRoles)
                });
            }
        }
        [Route("Admin/CreateUser")]
        public IActionResult CreateUser()
        {
            return View(new AdminUserFormViewModel
            {
                User = new User { IsActive = true },
                AvailableRoles = AvailableRoles
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/CreateUser")]
        public async Task<IActionResult> CreateUser(User user)
        {
            var password = Request.Form["Password"].ToString();
            var rolesCsv = NormalizeRoles(Request.Form["SelectedRoles"]);
            user.Username = user.Username?.Trim() ?? "";
            user.FullName = user.FullName?.Trim() ?? "";
            user.Email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim();
            user.Roles = rolesCsv;
            user.IsActive = Request.Form["IsActive"].ToString().Contains("true");

            if (string.IsNullOrWhiteSpace(user.Username) ||
                string.IsNullOrWhiteSpace(user.FullName) ||
                string.IsNullOrWhiteSpace(user.Roles))
            {
                return View(new AdminUserFormViewModel
                {
                    User = user,
                    AvailableRoles = AvailableRoles,
                    Message = "Zorunlu alanlar bos birakilamaz.",
                    MessageType = "error"
                });
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return View(new AdminUserFormViewModel
                {
                    User = user,
                    AvailableRoles = AvailableRoles,
                    Message = "Sifre alani zorunludur.",
                    MessageType = "error"
                });
            }

            var exists = await _context.Users.AnyAsync(u => u.Username == user.Username);
            if (exists)
            {
                return View(new AdminUserFormViewModel
                {
                    User = user,
                    AvailableRoles = AvailableRoles,
                    Message = "Bu kullanici adi zaten mevcut.",
                    MessageType = "error"
                });
            }

            user.PasswordHash = PasswordHasher.CreateHash(password);
            user.CreatedAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
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
                    user.Roles,
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

            return View(new AdminUserFormViewModel
            {
                User = user,
                AvailableRoles = AvailableRoles
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

            var username = user.Username?.Trim() ?? "";
            var fullName = user.FullName?.Trim() ?? "";
            var roles = NormalizeRoles(Request.Form["SelectedRoles"]);
            var email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim();

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(roles))
            {
                return View(new AdminUserFormViewModel
                {
                    User = user,
                    AvailableRoles = AvailableRoles,
                    Message = "Zorunlu alanlar bos birakilamaz.",
                    MessageType = "error"
                });
            }

            var exists = await _context.Users.AnyAsync(u => u.Username == username && u.UserId != existing.UserId);
            if (exists)
            {
                return View(new AdminUserFormViewModel
                {
                    User = user,
                    AvailableRoles = AvailableRoles,
                    Message = "Bu kullanici adi zaten mevcut.",
                    MessageType = "error"
                });
            }

            existing.Username = username;
            existing.FullName = fullName;
            existing.Email = email;
            existing.Roles = roles;
            existing.IsActive = Request.Form["IsActive"].ToString().Contains("true");
            if (!string.IsNullOrWhiteSpace(password))
            {
                existing.PasswordHash = PasswordHasher.CreateHash(password);
            }
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
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
                    existing.Roles,
                    existing.IsActive
                })
            });
            TempData["Message"] = "Kullanici guncellendi";
            TempData["MessageType"] = "success";
            return RedirectToAction("Index", new { tab = "users" });
        }

        private static List<object> BuildRoleCatalog()
        {
            var items = new List<object>();
            foreach (var role in AvailableRoles)
            {
                RoleDescriptions.TryGetValue(role, out var description);
                items.Add(new { Name = role, Description = description ?? "" });
            }
            return items;
        }

        private static HashSet<string> ParseRoles(string? rolesCsv)
        {
            if (string.IsNullOrWhiteSpace(rolesCsv))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            return rolesCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(role => role.Trim())
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizeRoles(Microsoft.Extensions.Primitives.StringValues selectedRoles)
        {
            var roles = selectedRoles
                .Select(role => role?.Trim())
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role!.ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(role => AvailableRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            return roles.Length == 0 ? "" : string.Join(",", roles);
        }
    }
}
