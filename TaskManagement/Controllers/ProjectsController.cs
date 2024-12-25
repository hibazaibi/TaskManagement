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

        public IActionResult Edit(int id)
        {
            var project = context.Projects.Include(p => p.Owner).FirstOrDefault(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            var currentUser = GetCurrentUser();
            if (currentUser == null || (currentUser.Role != "Manager" && currentUser.Role != "TeamLeader" && project.OwnerId != currentUser.Id))
            {
                return Unauthorized("You do not have permission to edit this project.");
            }

            Console.WriteLine($"Editing project for User: {currentUser.Id}, Project OwnerId: {project.OwnerId}");
            return View(project);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            var currentUser = GetCurrentUser();
            if (currentUser == null || (currentUser.Role != "Manager" && currentUser.Role != "TeamLeader"))
            {
                return Unauthorized("You do not have permission to edit this project.");
            }

            // Remove Owner field from model validation
            ModelState.Remove("Owner");

            // Reload the project entity to prevent unnecessary modifications
            var projectToUpdate = context.Projects
                .AsNoTracking()  // This ensures no tracking of entity changes on the initial query
                .FirstOrDefault(p => p.Id == id);

            if (projectToUpdate == null)
            {
                return NotFound();
            }

            // Only update the necessary fields
            projectToUpdate.Name = project.Name;
            projectToUpdate.Description = project.Description;

            // Ensure the OwnerId is not modified
            context.Entry(projectToUpdate).Property(p => p.OwnerId).IsModified = false;
            context.Entry(projectToUpdate).Reference(p => p.Owner).IsModified = false;

            if (ModelState.IsValid)
            {
                try
                {
                    context.Update(projectToUpdate);  // Update only the necessary fields
                    context.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Log validation errors if any
            foreach (var error in ModelState)
            {
                Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
            }

            return View(project);  // Return the view with validation errors if not valid
        }


        private bool ProjectExists(int id)
        {
            return context.Projects.Any(e => e.Id == id);
        }

        // GET: Projects/Delete/5
        // GET: Projects/Delete/5
        public IActionResult Delete(int id)
        {
            var project = context.Projects.Include(p => p.Owner).FirstOrDefault(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            var currentUser = GetCurrentUser();
            if (currentUser == null || (currentUser.Role != "Manager" && currentUser.Role != "TeamLeader" && project.OwnerId != currentUser.Id))
            {
                return Unauthorized("You do not have permission to delete this project.");
            }

            // Logging the project details
            Console.WriteLine($"Deleting project: {project.Id}, Owner: {project.OwnerId}, CurrentUser: {currentUser.Id}");

            // Confirming that we are passing the project correctly to the view
            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var project = context.Projects.Include(p => p.Tasks).FirstOrDefault(p => p.Id == id); // Include related tasks

            if (project == null)
            {
                return NotFound();
            }

            var currentUser = GetCurrentUser();
            if (currentUser == null || (currentUser.Role != "Manager" && currentUser.Role != "TeamLeader" && project.OwnerId != currentUser.Id))
            {
                return Unauthorized("You do not have permission to delete this project.");
            }

            // Log the project details before deletion
            Console.WriteLine($"Project to be deleted: {project.Id}, Name: {project.Name}, OwnerId: {project.OwnerId}");

            // Delete all related tasks (if any)
            foreach (var task in project.Tasks)
            {
                context.Tasks.Remove(task); // Remove associated tasks
                Console.WriteLine($"Deleting task: {task.Id}");
            }

            // Remove the project itself
            context.Projects.Remove(project);
            Console.WriteLine($"Project {project.Id} marked for deletion.");

            // Save the changes to the database
            try
            {
                context.SaveChanges();
                Console.WriteLine("Project and related tasks deleted successfully.");
            }
            catch (Exception ex)
            {
                // Log any errors that occur during the save
                Console.WriteLine($"Error deleting project: {ex.Message}");
                return StatusCode(500, "An error occurred while trying to delete the project.");
            }

            // Redirect after successful deletion
            return RedirectToAction(nameof(Index));
        }

    }
    }

