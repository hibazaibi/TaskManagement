using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Services;
using Task = TaskManagement.Models.Task;
using Microsoft.AspNetCore.Mvc.Rendering;
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
                        .ThenInclude(t => t.AssignedTo) // Include assigned user
                .FirstOrDefault(p => p.Id == projectId);

            if (project == null)
            {
                return NotFound($"Project with ID {projectId} not found.");
            }

            ViewBag.ProjectId = projectId;
            ViewBag.TotalTasks = project.Tasks.Count();
            ViewBag.CompletedTasks = project.Tasks.Count(t => t.IsComplete);
            var tasks = project.Tasks.OrderBy(t => t.Priority).ToList();
            ViewBag.HighPriorityTasks = project.Tasks.Count(t => t.Priority == "High");
            ViewBag.MediumPriorityTasks = project.Tasks.Count(t => t.Priority == "Medium");
            ViewBag.LowPriorityTasks = project.Tasks.Count(t => t.Priority == "Low");
            return View(project.Tasks);
        }
        public IActionResult Create(int projectId)
        {
            var priorities = new List<string> { "Low", "Medium", "High" };
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
            ViewBag.Priorities = new SelectList(priorities);
            // Pass the task object to the view
            var task = new Task { ProjectId = projectId };  // Initialiser un objet Task
            return View(task);  // Passer l'objet task à la vue
        }
        [HttpPost]
        [Authorize(Roles = "Manager,TeamLeader")]
        public IActionResult Create(Task task)
        {
            // Explicitly set navigation properties to null
            task.Project = null;
            task.AssignedTo = null;

            if (!ModelState.IsValid)
            {
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
        [HttpGet]
        public IActionResult Edit(int id, int projectId)
        {
            var task = context.Tasks.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                return NotFound($"Task with ID {id} not found.");
            }

            ViewBag.ProjectId = projectId;
            ViewBag.Users = context.Users
                .Select(u => new { u.Id, Name = u.FirstName })
                .ToList();
            ViewBag.Priorities = new SelectList(new List<string> { "Low", "Medium", "High" }, task.Priority);  // Default selected priority
            return View(task);
        }

        // Edit a task (POST)
        [HttpPost]
        public IActionResult Edit(Task task)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Users = context.Users
                    .Select(u => new { u.Id, Name = u.FirstName })
                    .ToList();
                ViewBag.Priorities = new SelectList(new List<string> { "Low", "Medium", "High" }, task.Priority);
                return View(task);
            }

            var existingTask = context.Tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existingTask == null)
            {
                return NotFound($"Task with ID {task.Id} not found.");
            }

            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.DueDate = task.DueDate;
            existingTask.IsComplete = task.IsComplete;
            existingTask.Priority = task.Priority;
            existingTask.AssignedToId = task.AssignedToId;

            context.SaveChanges();
            return RedirectToAction("Index", new { projectId = task.ProjectId });
        }


        // Supprimer une tâche
        [HttpPost]
        [Authorize(Roles = "Manager,TeamLeader")]
        public IActionResult Delete(int id, int projectId)
        {
            var task = context.Tasks.FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                return NotFound($"Task with ID {id} not found.");
            }

            context.Tasks.Remove(task);
            context.SaveChanges();
            return RedirectToAction("Index", new { projectId });
        }


        public IActionResult ToggleComplete(int id)
        {
            var task = context.Tasks.Find(id);
            if (task == null)
            {
                return NotFound($"Task with ID {id} not found.");
            }

            // Check if the user is a contributor
            var isContributor = User.IsInRole("Contributor");
            if (!isContributor)
            {
                TempData["ErrorMessage"] = "You do not have permission to toggle the task status.";
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }

            // Toggle the task completion status
            task.IsComplete = !task.IsComplete;
            context.SaveChanges();

            return RedirectToAction("Index", new { projectId = task.ProjectId });
        }

    }
}

