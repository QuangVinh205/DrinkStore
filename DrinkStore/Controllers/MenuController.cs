
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DrinkStore.Data;

namespace DrinkStore.Controllers
{
    public class MenuController : Controller
    {
        private readonly DrinkStoreDb db;

        public MenuController(DrinkStoreDb context)
        {
            db = context;
        }

        public async Task<IActionResult> Index()
        {
            var menuItems = await db.MenuItems
                .AsNoTracking()
                .Where(x => x.Active)
                .ToListAsync();

            return View(menuItems);
        }
    }
}
