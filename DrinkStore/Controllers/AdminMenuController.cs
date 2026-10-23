
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DrinkStore.Models;
using DrinkStore.Data;

public class AdminMenuController : Controller
{
    private readonly DrinkStoreDb _context;

    public AdminMenuController(DrinkStoreDb context)
    {
        _context = context;
    }

    // GET: MENUITEMS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.MenuItems.ToListAsync());
    }

    // GET: MENUITEMS/Details/5
    public async Task<IActionResult> Details(int? itemid)
    {
        if (itemid == null)
        {
            return NotFound();
        }

        var menuitem = await _context.MenuItems
            .FirstOrDefaultAsync(m => m.ItemId == itemid);
        if (menuitem == null)
        {
            return NotFound();
        }

        return View(menuitem);
    }

    // GET: MENUITEMS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: MENUITEMS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ItemId,Name,Description,BasePrice,Active,ImageUrl,Recipes,OrderItems")] MenuItem menuitem)
    {
        if (ModelState.IsValid)
        {
            _context.Add(menuitem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(menuitem);
    }

    // GET: MENUITEMS/Edit/5
    public async Task<IActionResult> Edit(int? itemid)
    {
        if (itemid == null)
        {
            return NotFound();
        }

        var menuitem = await _context.MenuItems.FindAsync(itemid);
        if (menuitem == null)
        {
            return NotFound();
        }
        return View(menuitem);
    }

    // POST: MENUITEMS/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? itemid, [Bind("ItemId,Name,Description,BasePrice,Active,ImageUrl,Recipes,OrderItems")] MenuItem menuitem)
    {
        if (itemid != menuitem.ItemId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(menuitem);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MenuItemExists(menuitem.ItemId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(menuitem);
    }

    // GET: MENUITEMS/Delete
    public async Task<IActionResult> Delete(int? itemid)
    {
        if (itemid == null)
        {
            return NotFound();
        }

        var menuitem = await _context.MenuItems
            .FirstOrDefaultAsync(m => m.ItemId == itemid);
        if (menuitem == null)
        {
            return NotFound();
        }

        return View(menuitem);
    }

    // POST: MENUITEMS/Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? itemid)
    {
        var menuitem = await _context.MenuItems.FindAsync(itemid);
        if (menuitem != null)
        {
            _context.MenuItems.Remove(menuitem);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool MenuItemExists(int? itemid)
    {
        return _context.MenuItems.Any(e => e.ItemId == itemid);
    }
}
