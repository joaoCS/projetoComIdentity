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

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            // If password reset token or email is null, most likely the
            // user tried to tamper the password reset link
            if (token == null || email == null)
            {
                ModelState.AddModelError("", "Invalid password reset token");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            Console.WriteLine(model.Email);
            Console.WriteLine(model.Token);

            if (ModelState.IsValid)
            {
                var user = await UserMgr.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    var result = await UserMgr.ResetPasswordAsync(user, model.Token, model.Password);
                    if (result.Succeeded)
                    {
                        return View("ResetPasswordConfirmation");
                    }
                    
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }

                return View("ResetPasswordConfirmation");
            }
            
            return View(model);
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

        //[HttpPost]
        //public async Task<IActionResult> Login(string username, string password)
        //{
        //    var result = await SignInMgr.PasswordSignInAsync(username, password, false, false);
        //    if (result.Succeeded)
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }
        //    else
        //    {
        //        ViewBag.Result = "result is: " + result.ToString();
        //    }
        //    return View();
        //}

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {   
            if (ModelState.IsValid)
            {
                var user = await UserMgr.FindByNameAsync(username);

                if (user != null && !user.EmailConfirmed &&
                            (await UserMgr.CheckPasswordAsync(user, password)))
                {
                    ModelState.AddModelError(string.Empty, "Email ainda não confirmado");
                    return View();
                }

                var result = await SignInMgr.PasswordSignInAsync(username,
                                        password, false, false);

                if (result.Succeeded)    
                    return RedirectToAction("index", "home");
                
                ModelState.AddModelError(string.Empty, "Invalid Login Attempt");
            }

            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = new AppUser
                    {
                        UserName = model.UserName,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName
                    };

                    var result = await UserMgr.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        var token = await UserMgr.GenerateEmailConfirmationTokenAsync(user);

                        var confirmationLink = Url.Action("ConfirmEmail", "Account",
                            new { userId = user.Id, token = token }, Request.Scheme);

                        logger.Log(LogLevel.Warning, confirmationLink);

                        ViewBag.MessageTitle = "Registro feito com sucesso!";
                        ViewBag.MessageBody = "Antes que você possa fazer login, por favor, confirme " +
                                "seu email clicando no link de confirmação que mandamos para seu email";
                        return View("ConfirmEmail");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View();
                }
                catch(Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);

                    return View(model);
                }
            }

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await UserMgr.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"O Id de usuário {userId} é inválido";
                return View("Error");
            }

            var result = await UserMgr.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                ViewBag.Message = "Email confirmado com sucesso!";

                return View("Messages");
            }

            ViewBag.ErrorMessage = "Email não pôde ser confirmado :(";
            return View("Error");
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