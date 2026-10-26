using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using static LWN_IPMS_Dashboard.Dtos;
using static LWN_IPMS_Dashboard.Entities;

namespace LWN_IPMS_Dashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private const string DefaultColour = "#0000FF";
        private const int DefaultSeconds = 8;
        private const int DefaultRows = 11;

        private readonly AppDbContext _db;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(AppDbContext db, ILogger<DashboardController> logger)
        {
            _db = db;
            _logger = logger;
        }

        private static string GetLevel(decimal stock, ItemMaster i) =>
            stock <= i.DangerLevel ? "Danger" :
            stock <= i.ReorderLevel ? "Re-order" : "Safety";

        private static string FifoCode(string? palletNo)
        {
            if (string.IsNullOrEmpty(palletNo)) return "";
            var idx = palletNo.IndexOf('-');
            return idx > 0 ? palletNo[..idx] : palletNo;   
        }

        private static int Num(string? palletNo)
        {
            var m = Regex.Match(palletNo ?? "", @"(\d+)$");
            return m.Success ? int.Parse(m.Value) : 0;
        }

     
        private async Task<Dictionary<string, string>> LoadColoursAsync()
        {
            var list = await _db.PalletTypes.AsNoTracking().ToListAsync();
            return list
                .Where(x => !string.IsNullOrWhiteSpace(x.ColourCode))
                .GroupBy(x => x.PalletName.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.First().ColourCode!.Trim());
        }

        private static string ColourFor(Dictionary<string, string> map, string? palletNo)
        {
            var code = FifoCode(palletNo).Trim().ToUpperInvariant();
            return map.TryGetValue(code, out var c) ? c : DefaultColour;
        }

       
        private Task<List<int>> LoadConfiguredItemIdsAsync() =>
            _db.DashboardPartConfigs.AsNoTracking()
               .OrderBy(x => x.Position).ThenBy(x => x.Id)
               .Select(x => x.ItemId)
               .ToListAsync();

        [HttpGet("config")]
        public async Task<ActionResult<DashboardConfigDto>> Config()
        {
            try
            {
                var s = await _db.DashboardSettings.AsNoTracking()
                                 .OrderBy(x => x.Id).FirstOrDefaultAsync();
                var ids = await LoadConfiguredItemIdsAsync();

                var seconds = s != null && s.DisplaySeconds > 0 ? s.DisplaySeconds : DefaultSeconds;
                var rows = s != null && s.DisplayRows > 0 ? s.DisplayRows : DefaultRows;

                return Ok(new DashboardConfigDto(rows, seconds, ids));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Config");
                return StatusCode(500, new { message = "Unexpected error while loading dashboard configuration.", detail = ex.Message });
            }
        }

    
        [HttpGet("documents")]
        public async Task<IActionResult> Documents()
        {
            try
            {
                var s = await _db.DashboardSettings.AsNoTracking()
                                 .OrderBy(x => x.Id).FirstOrDefaultAsync();

                var files = new List<object>();
                var folder = s?.InputPath?.Trim();

                if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                {
                    files = new DirectoryInfo(folder)
                        .EnumerateFiles("*.pdf", SearchOption.TopDirectoryOnly)
                        .Where(f => f.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                                 && (f.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                        .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(f => (object)new
                        {
                            name = f.Name,
                            version = f.LastWriteTimeUtc.Ticks   // changes when the file is replaced
                        })
                        .ToList();
                }

                return Ok(new { files });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Documents");
                return StatusCode(500, new { message = "Unexpected error while loading documents.", detail = ex.Message });
            }
        }

        // Streams one PDF from the Input Path (file name only, no sub-folders).
        [HttpGet("document/{name}")]
        public async Task<IActionResult> Document(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) ||
                    name != Path.GetFileName(name) ||
                    !name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "Invalid document name." });
                }

                var s = await _db.DashboardSettings.AsNoTracking()
                                 .OrderBy(x => x.Id).FirstOrDefaultAsync();

                var folder = s?.InputPath?.Trim();
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                    return NotFound(new { message = "Input path is not configured or does not exist." });

                var root = Path.GetFullPath(folder)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                var full = Path.GetFullPath(Path.Combine(root, name));

                if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                    !System.IO.File.Exists(full))
                {
                    return NotFound(new { message = "Document not found." });
                }

                return PhysicalFile(full, "application/pdf", enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Document {Name}", name);
                return StatusCode(500, new { message = "Unexpected error while loading the document." });
            }
        }

      
        [HttpGet("fifo-board")]
        public async Task<ActionResult<List<FifoBoardRow>>> FifoBoard()
        {
            try
            {
                var colours = await LoadColoursAsync();
                var configured = await LoadConfiguredItemIdsAsync();

              
                var pallets = await (from p in _db.GrnPallets.AsNoTracking()
                                     join l in _db.GrnLines.AsNoTracking() on p.GrnLineId equals l.Id
                                     where l.IsPosted &&
                                           !_db.MaterialIssues.Any(m => m.GrnPalletId == p.Id)
                                     orderby p.CreatedDate, p.Id
                                     select new { l.ItemId, p.PalletNo, p.Quantity })
                                    .ToListAsync();

                var allItems = await _db.Items.AsNoTracking().ToListAsync();
                var allIssues = await _db.MaterialIssues.AsNoTracking().ToListAsync();

                // If nothing is configured yet, fall back to every item (old behaviour)
                var items = configured.Count > 0
                    ? configured
                        .Select(id => allItems.FirstOrDefault(i => i.Id == id))
                        .Where(i => i != null)
                        .Select(i => i!)
                        .ToList()
                    : allItems;

                var result = new List<FifoBoardRow>();
                int sl = 1;

                foreach (var item in items)
                {
                    var ip = pallets.Where(x => x.ItemId == item.Id).ToList();
                    var itemIssues = allIssues.Where(i => i.ItemId == item.Id).ToList();

                    // Configured parts always appear, even with no stock / no issues.
                    if (configured.Count == 0 && ip.Count == 0 && itemIssues.Count == 0) continue;

                    var stock = ip.Sum(x => x.Quantity);

                    // Latest GIRN for this item
                    var girn = itemIssues
                        .OrderByDescending(i => i.IssueDate)
                        .ThenByDescending(i => i.Id)
                        .FirstOrDefault()?.IssueNumber;

                    var gi = itemIssues.Where(i => i.IssueNumber == girn).ToList();

                    string? range = null;
                    if (gi.Count > 0)
                    {
                        var nums = gi.Select(i => Num(i.PalletNo)).Where(n => n > 0).ToList();
                        if (nums.Count > 0)
                            range = nums.Min() == nums.Max()
                                ? $"{nums.Min()}"
                                : $"{nums.Min()}~{nums.Max()}";
                    }

                    var sequence = ip.Count > 0
                        ? $"From Pallet No. {Num(ip.First().PalletNo)} ~ {Num(ip.Last().PalletNo)}"
                        : "-";

                    result.Add(new FifoBoardRow(
                        item.Id,
                        sl++,
                        item.ItemNumber,
                        item.ItemName,
                        ip.Count > 0 ? FifoCode(ip[0].PalletNo) : "",
                        ip.Count > 0 ? ColourFor(colours, ip[0].PalletNo) : "",
                        girn,
                        range,
                        gi.Sum(i => i.Quantity),
                        sequence,
                        stock,
                        GetLevel(stock, item)));
                }

                return Ok(result);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error in FifoBoard");
                return StatusCode(500, new { message = "Database error while loading FIFO board.", detail = ex.InnerException?.Message ?? ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Data mapping error in FifoBoard");
                return StatusCode(500, new { message = "Data mapping error while loading FIFO board.", detail = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in FifoBoard");
                return StatusCode(500, new { message = "Unexpected error while loading FIFO board.", detail = ex.Message });
            }
        }

        // ---------- Dashboard 2 : Part board ----------
        [HttpGet("part/{itemId:int}")]
        public async Task<ActionResult<PartBoardDto>> PartBoard(int itemId)
        {
            try
            {
                var item = await _db.Items.AsNoTracking()
                                          .FirstOrDefaultAsync(i => i.Id == itemId);
                if (item == null)
                    return NotFound(new { message = $"Item {itemId} not found." });

                var colours = await LoadColoursAsync();

                var pallets = await (from p in _db.GrnPallets.AsNoTracking()
                                     join l in _db.GrnLines.AsNoTracking() on p.GrnLineId equals l.Id
                                     where l.ItemId == itemId && l.IsPosted &&
                                           !_db.MaterialIssues.Any(m => m.GrnPalletId == p.Id)
                                     orderby p.CreatedDate, p.Id
                                     select new { p.Id, p.PalletNo, p.Quantity })
                                    .ToListAsync();

                // Storage area = rack letter where the next FIFO pallet is stored
                string area = "-";
                if (pallets.Count > 0)
                {
                    var firstId = pallets[0].Id;

                    var rackNo = await (from sm in _db.StoreMovements.AsNoTracking()
                                        join rr in _db.RackRows.AsNoTracking() on sm.RackRowId equals rr.Id
                                        join rc in _db.RackColumns.AsNoTracking() on rr.RackColumnId equals rc.Id
                                        join lr in _db.LocationRacks.AsNoTracking() on rc.LocationRackId equals lr.Id
                                        where sm.GrnPalletId == firstId
                                        orderby sm.Id descending
                                        select lr.RackNo)
                                       .FirstOrDefaultAsync();

                    area = string.IsNullOrWhiteSpace(rackNo) ? "-" : $"Rack {rackNo.Trim()}";
                }

                var stock = pallets.Sum(x => x.Quantity);

                var dto = new PartBoardDto(
                    item.ItemModel ?? "",
                    item.ItemNumber,
                    item.ItemName,
                    pallets.Select(x => new PalletDto(x.PalletNo, x.Quantity, ColourFor(colours, x.PalletNo))).ToList(),
                    area,
                    pallets.FirstOrDefault()?.PalletNo,
                    stock,
                    GetLevel(stock, item));

                return Ok(dto);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Data mapping error in PartBoard for item {ItemId}", itemId);
                return StatusCode(500, new { message = "Data mapping error while loading part board.", detail = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in PartBoard for item {ItemId}", itemId);
                return StatusCode(500, new { message = "Unexpected error while loading part board.", detail = ex.Message });
            }
        }
    }
}
