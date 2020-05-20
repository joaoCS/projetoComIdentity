using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyTestApp.Models;

namespace MyTestApp.Controllers
{
    public class AccountController : Controller
    {
        private UserManager<AppUser> UserMgr { get; }
        private SignInManager<AppUser> SignInMgr { get; }
        private readonly ILogger<AccountController> logger;
        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ILogger<AccountController> logger)
        {
            UserMgr = userManager;
            SignInMgr = signInManager;
            this.logger = logger; 
        }

        public async Task<IActionResult> Logout()
        {
            await SignInMgr.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var result = await SignInMgr.PasswordSignInAsync(username, password, false, false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Result = "result is: " + result.ToString();
            }
            return View();
        }


        public async Task<IActionResult> Register()
        {
            try
            {
                ViewBag.Message = "User already registered";
                AppUser user = await UserMgr.FindByNameAsync("TestUser");

                if (user == null )
                {
                    user = new AppUser();

                    user.UserName = "TestUser";
                    user.Email = "TestUser@test.com";
                    user.FirstName = "John";
                    user.LastName = "Doe";

                    IdentityResult result = await UserMgr.CreateAsync(user, "Test123!");
                    ViewBag.Message = "User was created";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.Message;
            }

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword ()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserMgr.FindByEmailAsync(model.Email);
             
                if (user != null && await UserMgr.IsEmailConfirmedAsync(user))
                {

                    var token = await UserMgr.GeneratePasswordResetTokenAsync(user);

                    var passwordResetLink = Url.Action("ResetPassword", "Account",
                            new { email = model.Email, token = token }, Request.Scheme);

                    Console.WriteLine(passwordResetLink.ToString());

                    logger.Log(LogLevel.Warning, passwordResetLink);

                    return View("ForgotPasswordConfirmation");
                }

                Console.WriteLine("Eu não entrei no if");


                return View("ForgotPasswordConfirmation");
            }

            return View(model);
        }
    }
}