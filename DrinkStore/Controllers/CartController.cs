using DrinkStore.Data;
using DrinkStore.Extensions;
using DrinkStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinkStore.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly DrinkStoreDb _context;

        private const string CartKey = "Cart";

        public CartController(DrinkStoreDb context)
        {
            _context = context;
        }

        // Lấy giỏ hàng từ Session
        private List<CartItemViewModel> GetCart()
        {
            return HttpContext.Session
                .GetObject<List<CartItemViewModel>>(CartKey)
                ?? new List<CartItemViewModel>();
        }

        // Lưu giỏ hàng
        private void SaveCart(List<CartItemViewModel> cart)
        {
            HttpContext.Session.SetObject(CartKey, cart);
        }

        // GET: /Cart
        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCart();

            return View(cart);
        }

        // POST: /Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            int id,
            int quantity = 1)
        {
            if (quantity < 1 || quantity > 99)
                return BadRequest("Số lượng không hợp lệ.");

            var product = await _context.MenuItems
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ItemId == id && x.Active);

            if (product == null)
                return NotFound();

            var cart = GetCart();

            var existingItem = cart
                .FirstOrDefault(x => x.MenuItemId == id);

            if (existingItem != null)
            {
                if (existingItem.Quantity + quantity > 99)
                    return BadRequest(
                        "Số lượng tối đa là 99.");

                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    MenuItemId = product.ItemId,
                    Name = product.Name,
                    ImageUrl = product.ImageUrl ?? "",
                    Price = product.BasePrice,
                    Quantity = quantity
                });
            }

            SaveCart(cart);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int id, int quantity)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(
                x => x.MenuItemId == id);

            if (item == null)
                return NotFound();

            if (quantity <= 0)
            {
                cart.Remove(item);
            }
            else
            {
                if (quantity > 99)
                    return BadRequest(
                        "Số lượng tối đa là 99.");

                item.Quantity = quantity;
            }

            SaveCart(cart);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            var cart = GetCart();

            cart.RemoveAll(x => x.MenuItemId == id);

            SaveCart(cart);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartKey);

            return RedirectToAction(nameof(Index));
        }
    }
}