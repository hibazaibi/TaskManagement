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
                        .ThenInclude(t => t.AssignedTo) 
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

            var users = context.Users.Select(u => new { u.Id, Name = u.FirstName }).ToList();
            if (!users.Any())
            {
                Console.WriteLine("No users found in the database.");
            }

            ViewBag.Users = users;
            ViewBag.ProjectId = projectId;
            ViewBag.Priorities = new SelectList(priorities);
            var task = new Task { ProjectId = projectId };  
            return View(task);  
        }
        [HttpPost]
        [Authorize(Roles = "Manager,TeamLeader")]
        public IActionResult Create(Task task)
        {
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
            var task = context.Tasks.Include(t => t.AssignedTo).FirstOrDefault(t => t.Id == id);
            if (task == null)
            {
                return NotFound($"Task with ID {id} not found.");
            }

            var currentUserIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdString, out var currentUserId))
            {
                TempData["ErrorMessage"] = "Unable to identify the current user.";
                return RedirectToAction("Index", new { projectId });
            }

            if (task.AssignedToId != currentUserId && !User.IsInRole("TeamLeader"))
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this task.";
                return RedirectToAction("Index", new { projectId });
            }

            ViewBag.ProjectId = projectId;
            ViewBag.Users = context.Users
                .Select(u => new { u.Id, Name = u.FirstName })
                .ToList();
            ViewBag.Priorities = new SelectList(new List<string> { "Low", "Medium", "High" }, task.Priority);

            return View(task);
        }
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

            var currentUserIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdString, out var currentUserId))
            {
                TempData["ErrorMessage"] = "Unable to identify the current user.";
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }

            if (existingTask.AssignedToId != currentUserId && !User.IsInRole("TeamLeader"))
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this task.";
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }

            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.DueDate = task.DueDate;
            existingTask.IsComplete = task.IsComplete;
            existingTask.Priority = task.Priority;

            context.SaveChanges();
            return RedirectToAction("Index", new { projectId = task.ProjectId });
        }



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
            var task = context.Tasks
                .Include(t => t.AssignedTo) 
                .FirstOrDefault(t => t.Id == id);

            if (task == null)
            {
                return NotFound($"Task with ID {id} not found.");
            }

            var currentUserIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdString, out var currentUserId))
            {
                TempData["ErrorMessage"] = "Unable to identify the current user.";
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }

            if (task.AssignedToId != currentUserId)
            {
                TempData["ErrorMessage"] = "Only the assigned user can toggle the task status.";
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }

            task.IsComplete = !task.IsComplete;
            context.SaveChanges();

            return RedirectToAction("Index", new { projectId = task.ProjectId });
        }


    }
}

