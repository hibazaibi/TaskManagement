using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Models;
using TaskManagement.Services;

namespace TaskManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext context;

        public UsersController(ApplicationDbContext context)
        { this.context = context; }
        public IActionResult Index()
        {
            var users = context.Users.ToList();
            return View(users);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(User user)
        {

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                return View(user);
            }


            Console.WriteLine($"User: {user.FirstName}, {user.LastName}, {user.Email}, {user.Password}, {user.Role}");

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                user.Password = "Password123";
            }


            context.Users.Add(user);
            context.SaveChanges();

            return RedirectToAction("Index");
        }



        public IActionResult Edit(int id)
        {
            var user = context.Users.Find(id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }
            return View(user);
        }

        [HttpPost]
        public IActionResult Edit(User user)
        {
            if (ModelState.IsValid)
            {
                context.Users.Update(user);
                context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(user);
        }

        public IActionResult Delete(int id)
        {
            var user = context.Users.Find(id);
            if (user != null)
            {
                context.Users.Remove(user);
                context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
