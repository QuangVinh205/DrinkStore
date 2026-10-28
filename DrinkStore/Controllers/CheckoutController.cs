using DrinkStore.Data;
using DrinkStore.Extensions;
using DrinkStore.Models;
using DrinkStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinkStore.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CheckoutController : Controller
    {
        private readonly DrinkStoreDb _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private const string CartKey = "Cart";

        public CheckoutController(
            DrinkStoreDb context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private List<CartItemViewModel> GetCart()
        {
            return HttpContext.Session
                .GetObject<List<CartItemViewModel>>(CartKey)
                ?? new List<CartItemViewModel>();
        }

        // GET: /Checkout
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = GetCart();

            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdentityUserId == user.Id);

            var model = new CheckoutViewModel
            {
                FullName = customer?.Name ?? user.FullName ?? "",
                Phone = customer?.Phone ?? user.PhoneNumber ?? "",
                Address = customer?.Address ?? "",
                CartItems = cart
            };

            return View(model);
        }

        // POST: /Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var cart = GetCart();

            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            model.CartItems = cart;

            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // Kiểm tra dữ liệu giỏ hàng
            if (cart.Any(x => x.Quantity < 1 || x.Quantity > 99) ||
                cart.Select(x => x.MenuItemId).Distinct().Count() != cart.Count)
            {
                ModelState.AddModelError("", "Giỏ hàng không hợp lệ.");
                return View(model);
            }

            var ids = cart.Select(x => x.MenuItemId).ToList();

            // Lấy giá hiện tại từ database
            var products = await _context.MenuItems
                .AsNoTracking()
                .Where(x => ids.Contains(x.ItemId) && x.Active)
                .ToDictionaryAsync(x => x.ItemId);

            if (products.Count != ids.Count)
            {
                ModelState.AddModelError(
                    "",
                    "Một số đồ uống không còn được bán.");

                return View(model);
            }

            var orderItems = cart.Select(item => new OrderItem
            {
                MenuItemId = item.MenuItemId,
                Quantity = item.Quantity,
                Size = "M",
                UnitPriceSnapshot = products[item.MenuItemId].BasePrice
            }).ToList();

            decimal subtotal = orderItems.Sum(x => x.GetSubtotal());

            // Transaction đảm bảo dữ liệu được lưu cùng nhau
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(x => x.IdentityUserId == user.Id);

                if (customer == null)
                {
                    customer = new Customer
                    {
                        IdentityUserId = user.Id,
                        Name = model.FullName,
                        Email = user.Email ?? "",
                        Phone = model.Phone,
                        Address = model.Address
                    };

                    _context.Customers.Add(customer);
                }
                else
                {
                    customer.Name = model.FullName;
                    customer.Phone = model.Phone;
                    customer.Address = model.Address;
                }

                var order = new Order
                {
                    Customer = customer,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending,
                    Subtotal = subtotal,
                    Discount = 0,
                    Total = subtotal,
                    OrderItems = orderItems
                };

                _context.Orders.Add(order);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Chỉ xóa giỏ sau khi lưu thành công
                HttpContext.Session.Remove(CartKey);

                return RedirectToAction(
                    nameof(Success),
                    new { id = order.OrderId });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Không thể lưu đơn hàng. Vui lòng thử lại.");

                return View(model);
            }
        }

        // GET: /Checkout/Success/5
        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .AsNoTracking()
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.MenuItem)
                .FirstOrDefaultAsync(x =>
                    x.OrderId == id &&
                    x.Customer.IdentityUserId == userId);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // GET: /Checkout/MyOrders
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(x => x.Customer.IdentityUserId == userId)
                .Include(x => x.OrderItems)
                .OrderByDescending(x => x.OrderDate)
                .ToListAsync();

            return View(orders);
        }
    }
}