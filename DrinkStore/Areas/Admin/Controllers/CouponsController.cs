using DrinkStore.Data;
using DrinkStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinkStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "ShiftManager")]
public class CouponsController(DrinkStoreDb db) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        var query = db.Coupons.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Code.Contains(search.Trim()));
        ViewBag.Search = search;
        return View(await query.OrderByDescending(x => x.ValidTo).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View(new Coupon
    {
        ValidFrom = DateTime.UtcNow.Date,
        ValidTo = DateTime.UtcNow.Date.AddDays(30)
    });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Code,Percentage,ValidFrom,ValidTo,MinOrderValue")] Coupon input)
    {
        Validate(input);
        if (ModelState.IsValid && await db.Coupons.AnyAsync(x => x.Code == input.Code.Trim().ToUpper()))
            ModelState.AddModelError(nameof(input.Code), "Mã giảm giá đã tồn tại.");
        if (!ModelState.IsValid) return View(input);
        input.Code = input.Code.Trim().ToUpperInvariant();
        db.Coupons.Add(new Coupon
        {
            Code = input.Code, Percentage = input.Percentage,
            ValidFrom = input.ValidFrom, ValidTo = input.ValidTo,
            MinOrderValue = input.MinOrderValue
        });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(input.Code), "Không thể lưu mã. Hãy kiểm tra mã có bị trùng không.");
            return View(input);
        }
        TempData["Success"] = "Đã tạo mã giảm giá.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var item = await db.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.Code == id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("Percentage,ValidFrom,ValidTo,MinOrderValue")] Coupon input)
    {
        input.Code = id;
        ModelState.Remove(nameof(Coupon.Code));
        Validate(input);
        if (!ModelState.IsValid) return View(input);
        var item = await db.Coupons.FindAsync(id);
        if (item is null) return NotFound();
        item.Percentage = input.Percentage;
        item.ValidFrom = input.ValidFrom;
        item.ValidTo = input.ValidTo;
        item.MinOrderValue = input.MinOrderValue;
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật mã giảm giá.";
        return RedirectToAction(nameof(Index));
    }

    private void Validate(Coupon item)
    {
        if (string.IsNullOrWhiteSpace(item.Code) || item.Code.Trim().Length > 30 ||
            !System.Text.RegularExpressions.Regex.IsMatch(item.Code.Trim(), @"^[a-zA-Z0-9_-]+$"))
            ModelState.AddModelError(nameof(item.Code), "Mã phải có 1–30 ký tự chữ, số, _ hoặc -. ");
        if (item.Percentage <= 0 || item.Percentage > 100 || decimal.Round(item.Percentage, 2) != item.Percentage)
            ModelState.AddModelError(nameof(item.Percentage), "Phần trăm giảm phải lớn hơn 0 và không vượt quá 100.");
        if (item.MinOrderValue < 0 || decimal.Round(item.MinOrderValue, 2) != item.MinOrderValue)
            ModelState.AddModelError(nameof(item.MinOrderValue), "Giá trị đơn tối thiểu phải không âm.");
        if (item.ValidTo <= item.ValidFrom)
            ModelState.AddModelError(nameof(item.ValidTo), "Ngày kết thúc phải sau ngày bắt đầu.");
    }
}
