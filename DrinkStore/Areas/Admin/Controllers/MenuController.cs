using DrinkStore.Data;
using DrinkStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinkStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MenuController : Controller
    {
        private readonly DrinkStoreDb _context;

        public MenuController(DrinkStoreDb context)
        {
            _context = context;
        }

        // GET: /Admin/Menu
        public async Task<IActionResult> Index()
        {
            var items = await _context.MenuItems
                .AsNoTracking()
                .ToListAsync();

            return View(items);
        }

        // GET: /Admin/Menu/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new MenuItem
            {
                Active = true
            });
        }

        // POST: /Admin/Menu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Description,BasePrice,Active,ImageUrl")]
            MenuItem item)
        {
            if (!ModelState.IsValid)
                return View(item);

            _context.MenuItems.Add(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Menu/Edit/1
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.MenuItems
                .FindAsync(id);

            if (item == null)
                return NotFound();

            return View(item);
        }

        // POST: /Admin/Menu/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("ItemId,Name,Description,BasePrice,Active,ImageUrl")]
            MenuItem input)
        {
            if (id != input.ItemId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(input);

            var item = await _context.MenuItems.FindAsync(id);

            if (item == null)
                return NotFound();

            item.Name = input.Name;
            item.Description = input.Description;
            item.BasePrice = input.BasePrice;
            item.Active = input.Active;
            item.ImageUrl = input.ImageUrl;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: /Admin/Menu/Delete/1
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.MenuItems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ItemId == id);

            if (item == null)
                return NotFound();

            return View(item);
        }

        // POST: /Admin/Menu/Deactivate/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);

            if (item == null)
                return NotFound();

            item.Active = false;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }




        // GET: /Admin/Menu/Details/1
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.MenuItems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ItemId == id);

            if (item == null)
                return NotFound();

            return View(item);
        }

    }
}
