using DrinkStore.Data;
using DrinkStore.Models;
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

        public OrdersController(DrinkStoreDb context)
        {
            _context = context;
        }

        // GET: /Admin/Orders
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            OrderStatus? status)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .AsQueryable();

            // Tìm kiếm theo mã đơn hoặc tên khách hàng
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                if (int.TryParse(search, out int orderId))
                {
                    query = query.Where(o =>
                        o.OrderId == orderId ||
                        o.Customer.Name.Contains(search));
                }
                else
                {
                    query = query.Where(o =>
                        o.Customer.Name.Contains(search));
                }
            }

            // Lọc theo trạng thái
            if (status.HasValue)
            {
                query = query.Where(o =>
                    o.Status == status.Value);
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderId)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(orders);
        }

        // GET: /Admin/Orders/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // POST: /Admin/Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id,
            OrderStatus newStatus)
        {
            if (!Enum.IsDefined(typeof(OrderStatus), newStatus))
                return BadRequest("Trạng thái không hợp lệ.");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            // Chỉ cho phép chuyển trạng thái theo quy trình
            bool isValidTransition = order.Status switch
            {
                OrderStatus.Pending =>
                    newStatus == OrderStatus.Confirmed ||
                    newStatus == OrderStatus.Cancelled,

                OrderStatus.Confirmed =>
                    newStatus == OrderStatus.Preparing ||
                    newStatus == OrderStatus.Cancelled,

                OrderStatus.Preparing =>
                    newStatus == OrderStatus.Completed,

                _ => false
            };

            if (!isValidTransition)
            {
                TempData["Error"] =
                    "Không thể chuyển sang trạng thái này.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            order.Status = newStatus;

            try
            {
                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Cập nhật trạng thái đơn hàng thành công.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "Không thể cập nhật đơn hàng. Vui lòng thử lại.";
            }

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
    }
}