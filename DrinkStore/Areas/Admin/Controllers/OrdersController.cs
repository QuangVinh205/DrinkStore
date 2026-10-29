using System.Data;
using DrinkStore.Data;
using DrinkStore.Models;
using DrinkStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinkStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "ShiftManager")]
    public class OrdersController : Controller
    {
        private readonly DrinkStoreDb _context;
        public OrdersController(DrinkStoreDb context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index(string? search, OrderStatus? status)
        {
            var query = _context.Orders.AsNoTracking().Include(x => x.Customer).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                if (int.TryParse(search, out var id))
                    query = query.Where(x => x.OrderId == id || x.Customer.Name.Contains(search));
                else
                    query = query.Where(x => x.Customer.Name.Contains(search));
            }
            if (status.HasValue) query = query.Where(x => x.Status == status.Value);
            ViewBag.Search = search;
            ViewBag.Status = status;
            return View(await query.OrderByDescending(x => x.OrderDate)
                .ThenByDescending(x => x.OrderId).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders.AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.OrderItems).ThenInclude(x => x.MenuItem)
                .FirstOrDefaultAsync(x => x.OrderId == id);
            return order == null ? NotFound() : View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            if (!Enum.IsDefined(typeof(OrderStatus), newStatus))
                return BadRequest("Trạng thái không hợp lệ.");

            // Khóa và transaction để không thể chuẩn bị/trừ kho hai lần đồng thời.
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var current = await _context.Orders.AsNoTracking()
                    .Where(x => x.OrderId == id)
                    .Select(x => (OrderStatus?)x.Status)
                    .FirstOrDefaultAsync();
                if (current == null) return NotFound();

                bool allowed = current.Value switch
                {
                    OrderStatus.Pending => newStatus == OrderStatus.Confirmed || newStatus == OrderStatus.Cancelled,
                    OrderStatus.Confirmed => newStatus == OrderStatus.Preparing || newStatus == OrderStatus.Cancelled,
                    OrderStatus.Preparing => newStatus == OrderStatus.Completed,
                    _ => false
                };
                if (!allowed)
                {
                    TempData["Error"] = "Không thể chuyển sang trạng thái này.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Điều kiện trạng thái cũ ngăn ghi đè do thao tác đồng thời.
                var changed = await _context.Orders
                    .Where(x => x.OrderId == id && x.Status == current.Value)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, newStatus));
                if (changed != 1)
                {
                    TempData["Error"] = "Đơn hàng vừa được cập nhật. Hãy tải lại trang.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (current == OrderStatus.Confirmed && newStatus == OrderStatus.Preparing)
                {
                    var inventory = new InventoryService(_context);
                    var result = await inventory.DeductForOrderAsync(id);
                    if (!result.Success)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = result.Error;
                        return RedirectToAction(nameof(Details), new { id });
                    }
                }

                await transaction.CommitAsync();
                TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công.";
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Không thể cập nhật đơn hàng hoặc kho. Vui lòng thử lại.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
