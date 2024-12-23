﻿using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Services;
using TaskManagement.Models;

namespace TaskManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext context;
public AccountController(ApplicationDbContext context)
        {
            this.context = context;
        }
        public IActionResult Login()
        {
            return View(new LoginViewModel());  // Pass an empty LoginViewModel to the view
        }
      
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Remove any existing validation error for ErrorMessage
            ModelState.Remove(nameof(LoginViewModel.ErrorMessage));

            if (ModelState.IsValid)
            {
                var user = context.Users.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);

                if (user != null)
                {
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

                    var identity = new ClaimsIdentity(claims, "CookieAuth");
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync("CookieAuth", principal);
                    return RedirectToAction("Index", "Home");
                }

                model.ErrorMessage = "Invalid email or password.";
            }

            return View(model);
        }


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }
    }
}