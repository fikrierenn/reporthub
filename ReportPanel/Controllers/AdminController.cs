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

        [HttpGet]
        [Authorize(Roles = "admin")]
        [Route("Admin/ProcParams")]
        // M-13 R4.1: Logic SpExplorerService.GetParametersAsync'e tasindi (28 Nisan 2026).
        public async Task<IActionResult> ProcParams(string dataSourceKey, string procName)
        {
            var result = await _spExplorer.GetParametersAsync(dataSourceKey, procName);
            if (!result.Success)
            {
                return BadRequest(result.Error ?? "ProcParams failed.");
            }
            return Json(new { fields = result.Fields });
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        [Route("Admin/FilterOptions")]
        // M-13 R4.1: Logic FilterOptionsService.GetAsync'e tasindi (28 Nisan 2026).
        public async Task<IActionResult> FilterOptions(string filterKey, string? dataSourceKey = null)
        {
            var result = await _filterOptions.GetAsync(filterKey, dataSourceKey);
            if (!result.Success && result.Error == "FilterKey gerekli.")
            {
                return BadRequest(result.Error);
            }
            return Json(new { options = result.Options });
        }

        // DataSource'taki stored procedure listesi (builder dropdown'i icin)
        [HttpGet]
        [Route("Admin/SpList")]
        // M-13 R4.1: Logic SpExplorerService.ListAsync'e tasindi (28 Nisan 2026).
        public async Task<IActionResult> SpList(string dataSourceKey)
        {
            var result = await _spExplorer.ListAsync(dataSourceKey);
            return Json(new { success = result.Success, error = result.Error, procedures = result.Procedures });
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
            var input = BuildReportFormInput();
            var result = await _reportService.CreateAsync(input);
            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "reports" });
            }

            return View(await BuildReportFormViewModel(report, input, result.Message));
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
            var input = BuildReportFormInput();
            var result = await _reportService.UpdateAsync(report.ReportId, input);
            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "reports" });
            }

            return View(await BuildReportFormViewModel(report, input, result.Message));
        }

        // Referans placeholder — asagidaki bloklar ayri actions'in bitimini mulayim tutmak icin.
        private async Task<AdminReportFormViewModel> BuildReportFormViewModel(ReportCatalog report, ReportFormInput input, string message)
        {
            var dataSources = await _context.DataSources.Where(d => d.IsActive).ToListAsync();
            var roles = await _context.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
            var categories = await _context.ReportCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            return new AdminReportFormViewModel
            {
                Report = report,
                DataSources = dataSources,
                AvailableRoles = roles,
                SelectedRoleIds = input.SelectedRoleIds,
                AvailableCategories = categories,
                SelectedCategoryIds = input.SelectedCategoryIds,
                Message = message,
                MessageType = "error"
            };
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
            var input = BuildUserFormInput(user);
            var result = await _userService.CreateAsync(input);
            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "users" });
            }
            return View(await BuildCreateUserFormAsync(user, input.SelectedRoleIds, result.Message, "error"));
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
            var input = BuildUserFormInput(user);
            var result = await _userService.UpdateAsync(user.UserId, input);
            if (result.Success)
            {
                TempData["Message"] = result.Message;
                TempData["MessageType"] = "success";
                return RedirectToAction("Index", new { tab = "users" });
            }
            var allRoles = await _context.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
            return View(new AdminUserFormViewModel
            {
                User = user,
                AvailableRoles = allRoles,
                SelectedRoleIds = input.SelectedRoleIds,
                Message = result.Message,
                MessageType = "error"
            });
        }

        // M-01: Form -> UserFormInput. UserManagementService.NormalizeUsername static.
        private UserFormInput BuildUserFormInput(User user)
        {
            var filterKeys = Request.Form["FilterKeys"].ToArray();
            var filterValues = Request.Form["FilterValues"].ToArray();
            var filterDataSources = Request.Form["FilterDataSources"].ToArray();
            var filters = new List<UserFilterInput>();
            for (var i = 0; i < filterKeys.Length; i++)
            {
                var k = filterKeys[i]?.Trim() ?? "";
                var v = i < filterValues.Length ? (filterValues[i]?.Trim() ?? "") : "";
                var ds = i < filterDataSources.Length ? filterDataSources[i]?.Trim() : null;
                if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(v)) continue;
                filters.Add(new UserFilterInput(k, v, ds));
            }
            return new UserFormInput(
                Username: user.Username,
                FullName: user.FullName,
                Email: user.Email,
                IsAdUser: ReadFormBool("IsAdUser"),
                IsActive: ReadFormBool("IsActive"),
                Password: Request.Form["Password"],
                SelectedRoleIds: ParseIds(Request.Form["SelectedRoles"]),
                DataFilters: filters);
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
                await _roleService.PropagateRenameToReportAllowedRolesAsync(oldName, name);
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
