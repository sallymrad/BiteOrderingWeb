using BiteOrderWeb.Models;
using BiteOrderWeb.Services;
using BiteOrderWeb.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using System.Data;

namespace BiteOrderWeb.Controllers
{
    public class AccountController :Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly EmailSenderService _emailSender;
        public AccountController(SignInManager<Users> signInManager, UserManager<Users> userManager, RoleManager<IdentityRole> roleManager, EmailSenderService emailSender)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
            _emailSender = emailSender;
        }

        //
        [HttpGet]
        public async Task<IActionResult> SendTest()
        {
            await _emailSender.SendEmailAsync("biteordering@gmail.com", "Test Email", "<h1>Hello BiteOrdering!</h1>");

            return Content("Email Sent!");
        }



        [HttpGet]
        public IActionResult Login()
        {
            ModelState.Clear(); 
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null || user.IsDeactivated)
            {
                ModelState.AddModelError(string.Empty, "This account has been deactivated or does not exist.");
                return View(model);
            }

            var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                TempData.Remove("AccountDeactivated"); //  Remove message after successful login
                return RedirectToAction("Index", "Home");
            }

            else
            {
                ModelState.AddModelError(string.Empty, "Email or password is incorrect.");
                return View(model);
            }
        }



            [HttpGet]
        
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
       [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); }

            var users = new Users
            {
                FullName = model.Name,
                Email = model.Email,
                UserName = model.Email,
                NormalizedUserName = model.Email.ToUpper(),
                NormalizedEmail = model.Email.ToUpper(),
                EmailConfirmed = true
            };

                var result = await userManager.CreateAsync(users, model.Password);

                if (result.Succeeded) {
                var roleExist = await roleManager.RoleExistsAsync("User");
                if (!roleExist)
                {
                    var role = new IdentityRole("User");
                    await roleManager.CreateAsync(role);
                    
                }

                await userManager.AddToRoleAsync(users, "User");
                await signInManager.SignInAsync(users, isPersistent:false);

                return RedirectToAction("Login", "Account");
                }
            
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);


            }

        [HttpGet]
         public IActionResult VerifyEmail()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {

                return View(model); }
               
                var user = await userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                ModelState.AddModelError("", "Invalid Email");
                return View(model);
            }
                else
                {
                    return RedirectToAction("ChangePassword", "Account", new { username = user.Email });
                }
            }

        [HttpGet]
        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("VerifyEmail", "Account");
            }

            return View(new ChangePasswordViewModel { Email = username });
        }

        [HttpPost]
        public async Task<IActionResult>ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(" ", "Something Wrong.");
                return View(model);
            }
            var user = await userManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(" ", "User not found.");
                return View(model);
            }

            var result = await userManager.RemovePasswordAsync(user);

            if (result.Succeeded)
            {
                result = await userManager.AddPasswordAsync(user, model.NewPassword);
        
                    return RedirectToAction("Login", "Account");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                      

                    }
                    return View(model);
                }
            }
        
        [HttpPost]          
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await userManager.IsEmailConfirmedAsync(user)))
            {
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            
            var otp = new Random().Next(100000, 999999).ToString();
            user.OTP = otp;
            user.OTPGeneratedAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);


            var resetLink = Url.Action("ResetPassword", "Account", new
            {
                email = model.Email,
                token = token
            }, protocol: HttpContext.Request.Scheme);


            await _emailSender.SendEmailAsync(
               model.Email,
                  "Reset Your Password",
                    $"Click <a href='{resetLink}'>here</a> to reset your password.<br><br><b>Your OTP is: {otp}</b>"
                            );



            TempData["ResetLink"] = resetLink;
            return RedirectToAction("ForgotPasswordConfirmation");
        }


        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View(); 
        }
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (email == null || token == null)
            {
                return BadRequest();
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }
            if (user.OTP != model.OTP || user.OTPGeneratedAt == null || user.OTPGeneratedAt.Value.AddMinutes(10) < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "Invalid or expired OTP.");
                return View(model);
            }

            var result = await userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password reset successfully!";
                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}
