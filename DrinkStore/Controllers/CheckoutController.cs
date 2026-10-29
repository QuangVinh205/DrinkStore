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

        public CheckoutController(DrinkStoreDb context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private List<CartItemViewModel> GetCart() =>
            HttpContext.Session.GetObject<List<CartItemViewModel>>(CartKey)
            ?? new List<CartItemViewModel>();

        // Trả về giá hiện tại; không dùng giá được lưu trong Session.
        private async Task<(List<OrderItem>? Items, string? Error)> BuildOrderItemsAsync(
            List<CartItemViewModel> cart)
        {
            if (cart.Count == 0 ||
                cart.Any(x => x.Quantity < 1 || x.Quantity > 99) ||
                cart.Select(x => x.MenuItemId).Distinct().Count() != cart.Count)
                return (null, "Giỏ hàng không hợp lệ.");

            var ids = cart.Select(x => x.MenuItemId).ToList();
            var products = await _context.MenuItems.AsNoTracking()
                .Where(x => ids.Contains(x.ItemId) && x.Active)
                .ToDictionaryAsync(x => x.ItemId);

            if (products.Count != ids.Count)
                return (null, "Một số đồ uống không còn được bán.");

            return (cart.Select(x => new OrderItem
            {
                MenuItemId = x.MenuItemId,
                Quantity = x.Quantity,
                Size = "M", // Giữ nguyên size đang dùng trong Checkout hiện tại.
                UnitPriceSnapshot = products[x.MenuItemId].BasePrice
            }).ToList(), null);
        }

        private async Task<(Coupon? Coupon, decimal Discount, string? Error)> GetCouponAsync(
            string? rawCode, decimal subtotal)
        {
            var code = rawCode?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code)) return (null, 0m, null);

            var coupon = await _context.Coupons.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code);
            if (coupon == null) return (null, 0m, "Mã giảm giá không tồn tại.");

            var now = DateTime.UtcNow;
            // Quy ước: ValidFrom và ValidTo được lưu theo UTC.
            if (now < coupon.ValidFrom || now > coupon.ValidTo)
                return (null, 0m, "Mã giảm giá chưa có hiệu lực hoặc đã hết hạn.");
            if (coupon.Percentage <= 0 || coupon.Percentage > 100)
                return (null, 0m, "Mã giảm giá có tỷ lệ không hợp lệ.");
            if (subtotal < coupon.MinOrderValue)
                return (null, 0m, $"Đơn hàng phải đạt tối thiểu {coupon.MinOrderValue:N0} đ để dùng mã này.");

            var discount = Math.Round(subtotal * coupon.Percentage / 100m, 2,
                MidpointRounding.AwayFromZero);
            return (coupon, Math.Min(subtotal, discount), null);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index", "Cart");
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var customer = await _context.Customers.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdentityUserId == user.Id);
            var model = new CheckoutViewModel
            {
                FullName = customer?.Name ?? user.FullName ?? "",
                Phone = customer?.Phone ?? user.PhoneNumber ?? "",
                Address = customer?.Address ?? "",
                CartItems = cart
            };
            var (items, error) = await BuildOrderItemsAsync(cart);
            if (error != null) ModelState.AddModelError("", error);
            model.Subtotal = items?.Sum(x => x.GetSubtotal()) ?? 0;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model, string? intent)
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index", "Cart");
            model.CartItems = cart;

            var (orderItems, cartError) = await BuildOrderItemsAsync(cart);
            if (cartError != null) ModelState.AddModelError("", cartError);
            model.Subtotal = orderItems?.Sum(x => x.GetSubtotal()) ?? 0;

            var (coupon, discount, couponError) = await GetCouponAsync(model.CouponCode, model.Subtotal);
            if (couponError != null) ModelState.AddModelError(nameof(model.CouponCode), couponError);
            model.Discount = discount;
            model.AppliedCouponCode = coupon?.Code;

            // Preview the discount without requiring delivery details or creating an order.
            if (intent == "applyCoupon")
            {
                ModelState.Remove(nameof(model.FullName));
                ModelState.Remove(nameof(model.Phone));
                ModelState.Remove(nameof(model.Address));
                return View(model);
            }

            if (!ModelState.IsValid || orderItems == null) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            await using var transaction = await _context.Database.BeginTransactionAsync();
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
                    Subtotal = model.Subtotal,
                    Discount = discount,
                    Total = model.Total,
                    CouponCode = coupon?.Code,
                    OrderItems = orderItems
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                HttpContext.Session.Remove(CartKey);
                return RedirectToAction(nameof(Success), new { id = order.OrderId });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Không thể lưu đơn hàng. Vui lòng thử lại.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders.AsNoTracking()
                .Include(x => x.OrderItems).ThenInclude(x => x.MenuItem)
                .FirstOrDefaultAsync(x => x.OrderId == id && x.Customer.IdentityUserId == userId);
            return order == null ? NotFound() : View(order);
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);
            var orders = await _context.Orders.AsNoTracking()
                .Where(x => x.Customer.IdentityUserId == userId)
                .Include(x => x.OrderItems)
                .OrderByDescending(x => x.OrderDate).ToListAsync();
            return View(orders);
        }
    }
}
