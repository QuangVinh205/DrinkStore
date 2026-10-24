
using DrinkStore.Models;
using DrinkStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DrinkStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;

        public AccountController(
            UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn)
        {
            _users = users;
            _signIn = signIn;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName
            };

            var result = await _users.CreateAsync(
                user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(
                        "", error.Description);

                return View(model);
            }

            var roleResult = await _users.AddToRoleAsync(
                user, "Customer");

            if (!roleResult.Succeeded)
            {
                await _users.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError(
                        "", error.Description);

                return View(model);
            }

            await _signIn.SignInAsync(
                user, isPersistent: false);

            return RedirectToAction("Index", "Menu");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var result = await _signIn.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                var signedInUser = await _users.FindByEmailAsync(model.Email);

                if (signedInUser != null && await _users.IsInRoleAsync(signedInUser, "ShiftManager"))
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }

                return RedirectToAction("Index", "Menu");
            }

            ModelState.AddModelError(
                "",
                "Đăng nhập không thành công. Kiểm tra tài khoản hoặc thử lại sau.");

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
