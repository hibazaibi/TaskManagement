using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManagement.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Project name is required.")]
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Task> Tasks { get; set; } = new List<Task>();

        public int OwnerId { get; set; }
        [JsonIgnore]
        public User Owner { get; set; }
    }
}
