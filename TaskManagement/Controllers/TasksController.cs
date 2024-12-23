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

            var users = context.Users.Select(u => new { u.Id, Name = u.FirstName }).ToList();
            ViewBag.Users = users;
            ViewBag.ProjectId = projectId;

            return View(new Task { ProjectId = projectId });
        }
        [HttpPost]
        [Authorize(Roles = "Manager,TeamLeader")]
        public IActionResult Create(Task task)
        {
            Console.WriteLine("Received POST request to create a task.");
            Console.WriteLine($"Task Details: ProjectId={task.ProjectId}, Title={task.Title}, AssignedToId={task.AssignedToId}, DueDate={task.DueDate}, Description={task.Description}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid. Errors:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"- {error.ErrorMessage}");
                }

                var users = context.Users.Select(u => new { u.Id, Name = u.FirstName }).ToList();
                ViewBag.Users = users;
                return View(task);
            }

            var project = context.Projects.FirstOrDefault(p => p.Id == task.ProjectId);
            if (project == null)
            {
                Console.WriteLine($"Error: Project with ID {task.ProjectId} not found.");
                return NotFound($"Project with ID {task.ProjectId} not found.");
            }

            task.Project = project;

            try
            {
                context.Tasks.Add(task);
                context.SaveChanges();
                Console.WriteLine("Task successfully created.");
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

