﻿using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Models
{
    public class User
    {

        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        // Default value provided internally
        public string Password { get; set; } = "password123";

        [Required]
        public string Role { get; set; }
        public List<Project> OwnedProjects { get; set; } = new List<Project>();
            public List<Project> ContributedProjects { get; set; } = new List<Project>();
            public List<Task> AssignedTasks { get; set; } = new List<Task>();
        

    }
}