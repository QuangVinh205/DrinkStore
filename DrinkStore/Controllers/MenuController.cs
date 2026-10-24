using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using DrinkStore.Data;

namespace DrinkStore.Controllers
{
    public class MenuController : Controller
    {
        private readonly DrinkStoreDb db;
        private readonly ILogger<MenuController> logger;

        public MenuController(DrinkStoreDb context, ILogger<MenuController> logger)
        {
            db = context;
            this.logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var menuItems = await db.MenuItems
                    .AsNoTracking()
                    .Where(x => x.Active)
                    .ToListAsync();

                return View(menuItems);
            }
            catch (SqlException ex)
            {
                logger.LogError(ex, "Cannot load the drinks menu from SQL Server.");
                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return View("Unavailable");
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return View("NotFound");
            }

            try
            {
                var item = await db.MenuItems
                    .AsNoTracking()
                    .SingleOrDefaultAsync(x => x.ItemId == id && x.Active);

                if (item == null)
                {
                    Response.StatusCode = StatusCodes.Status404NotFound;
                    return View("NotFound");
                }

                return View(item);
            }
            catch (SqlException ex)
            {
                logger.LogError(ex, "Cannot load drink {ItemId} from SQL Server.", id);
                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return View("Unavailable");
            }
        }
    }
}
