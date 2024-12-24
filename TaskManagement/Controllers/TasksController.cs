using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Services;
using Task = TaskManagement.Models.Task;

namespace TaskManagement.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext context;

        public TasksController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public IActionResult Index(int projectId)
        {
            var project = context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefault(p => p.Id == projectId);

            if (project == null)
            {
                return NotFound($"Project with ID {projectId} not found.");
            }

            ViewBag.ProjectId = projectId;
            return View(project.Tasks);
        }
        public IActionResult Create(int projectId)
        {
            var project = context.Projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null)
            {
                return NotFound($"Project with ID {projectId} not found.");
            }

            // Fetch users and ensure we have them
            var users = context.Users.Select(u => new { u.Id, Name = u.FirstName }).ToList();
            if (!users.Any())
            {
                Console.WriteLine("No users found in the database.");
            }

            ViewBag.Users = users;
            ViewBag.ProjectId = projectId;

            // Pass the task object to the view
            var task = new Task { ProjectId = projectId };  // Initialiser un objet Task
            return View(task);  // Passer l'objet task à la vue
        }
        [HttpPost]
        [Authorize(Roles = "Manager,TeamLeader")]
        public IActionResult Create(Task task)
        {
            if (!ModelState.IsValid)
            {
                // Log errors (optional)
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                foreach (var error in errors)
                {
                    Console.WriteLine($"Validation Error: {error}");
                }

                // Repopulate ViewBag.Users in case of validation error
                ViewBag.Users = context.Users.Select(u => new { u.Id, Name = u.FirstName }).ToList();
                return View(task);
            }

            var project = context.Projects.FirstOrDefault(p => p.Id == task.ProjectId);
            if (project == null)
            {
                ModelState.AddModelError("ProjectId", "The specified project does not exist.");
                ViewBag.Users = context.Users.Select(u => new { u.Id, Name = u.FirstName }).ToList();
                return View(task);
            }

            var assignedTo = context.Users.FirstOrDefault(u => u.Id == task.AssignedToId);
            if (assignedTo == null)
            {
                ModelState.AddModelError("AssignedToId", "The specified user does not exist.");
                ViewBag.Users = context.Users.Select(u => new { u.Id, Name = u.FirstName }).ToList();
                return View(task);
            }

            // Save the task
            try
            {
                context.Tasks.Add(task);
                context.SaveChanges();
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while saving the task.");
                return View(task);
            }
        }



        public IActionResult ToggleComplete(int id)
        {
            var task = context.Tasks.Find(id);
            if (task == null)
            {
                return NotFound($"Task with ID {id} not found.");
            }

            task.IsComplete = !task.IsComplete;
            context.SaveChanges();
            return RedirectToAction("Index", new { projectId = task.ProjectId });
        }
    }
}

