using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Models;
using TaskManagement.Services;

namespace TaskManagement.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext context;
        public ProjectsController(ApplicationDbContext context
           )
        { this.context = context; }
        private User GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Debugging: Check if userId is null
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("User.FindFirstValue returned null.");
                return null;
            }

            var user = context.Users.FirstOrDefault(u => u.Id == int.Parse(userId));

            // Debugging: Check if user is null
            if (user == null)
            {
                Console.WriteLine($"No user found for Id: {userId}");
            }
            else
            {
                Console.WriteLine($"User found: Id = {user.Id}, Role = {user.Role}");
            }

            return user;
        }

        public IActionResult Index()
        {
            var projects = context.Projects.Include(p => p.Owner).ToList();
            ViewBag.CurrentUser = GetCurrentUser();
            return View(projects);
        }

        public IActionResult Create()
        {
            var currentUser = GetCurrentUser();

            if (currentUser == null || (currentUser.Role != "Manager" && currentUser.Role != "TeamLeader"))
            {
                return Unauthorized("You do not have permission to create a project.");
            }

            var model = new Project();
            return View(model);
        }

        [HttpPost]
        public IActionResult Create(Project project)
        {
            var currentUser = GetCurrentUser();

            if (currentUser == null || (currentUser.Role != "Manager" && currentUser.Role != "TeamLeader"))
            {
                return Unauthorized("You do not have permission to create a project.");
            }

            // Assign the current user as the Owner
            project.OwnerId = currentUser.Id;

            Console.WriteLine($"Creating project for User: {currentUser.Id}, Role: {currentUser.Role}");
            Console.WriteLine($"Project Details - Name: {project.Name}, OwnerId: {project.OwnerId}, Description: {project.Description}");

            // Remove 'Owner' from ModelState validation
            ModelState.Remove("Owner");

            if (ModelState.IsValid)
            {
                context.Projects.Add(project);
                context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            // Log ModelState errors
            foreach (var error in ModelState)
            {
                Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
            }

            return View(project);
        }
    }
}

