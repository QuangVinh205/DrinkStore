using DrinkStore.Data;
using DrinkStore.Extensions;
using DrinkStore.Models;
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
            var cart = HttpContext.Session
                .GetObject<List<CartItemViewModel>>(CartKey)
                ?? new List<CartItemViewModel>();
            foreach (var item in cart)
            {
                // Older sessions stored the base price for both sizes.
                item.BasePrice ??= item.Price;
                if (DrinkSizes.IsValid(item.Size))
                    item.Price = DrinkSizes.GetPrice(item.BasePrice.Value, item.Size);
            }
            return cart;
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
            int quantity = 1, string size = DrinkSizes.Default)
        {
            if (!ModelState.IsValid || !DrinkSizes.IsValid(size))
                return BadRequest("Vui lòng chọn size M hoặc L.");

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
                .FirstOrDefault(x => x.MenuItemId == id && x.Size == size);

            if (existingItem != null)
            {
                if (existingItem.Quantity + quantity > 99)
                    return BadRequest(
                        "Số lượng tối đa là 99.");

                existingItem.Quantity += quantity;
                existingItem.BasePrice = product.BasePrice;
                existingItem.Price = DrinkSizes.GetPrice(product.BasePrice, size);
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    MenuItemId = product.ItemId,
                    Name = product.Name,
                    ImageUrl = product.ImageUrl ?? "",
                    BasePrice = product.BasePrice,
                    Price = DrinkSizes.GetPrice(product.BasePrice, size),
                    Quantity = quantity,
                    Size = size
                });
            }

            SaveCart(cart);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int id, int quantity, string size, string newSize)
        {
            if (!ModelState.IsValid || !DrinkSizes.IsValid(newSize) || quantity < 1 || quantity > 99)
                return BadRequest("Size phải là M hoặc L, số lượng từ 1 đến 99.");

            var cart = GetCart();

            var item = cart.FirstOrDefault(
                x => x.MenuItemId == id && x.Size == size);

            if (item == null)
                return NotFound();

            var matchingItem = cart.FirstOrDefault(x =>
                x != item && x.MenuItemId == id && x.Size == newSize);
            if (matchingItem != null)
            {
                if (matchingItem.Quantity + quantity > 99)
                    return BadRequest("Số lượng tối đa cho mỗi món và size là 99.");
                matchingItem.Quantity += quantity;
                cart.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
                item.Size = newSize;
                item.Price = DrinkSizes.GetPrice(item.BasePrice!.Value, newSize);
            }

            SaveCart(cart);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id, string size)
        {
            var cart = GetCart();

            cart.RemoveAll(x => x.MenuItemId == id && x.Size == size);

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
