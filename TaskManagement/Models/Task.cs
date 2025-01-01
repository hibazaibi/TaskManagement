using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TaskManagement.Models
{
    public class Task
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        public bool IsComplete { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [ValidateNever]
        public Project Project { get; set; }

        [Required(ErrorMessage = "The AssignedTo field is required.")]
        public int? AssignedToId { get; set; }

        [ForeignKey("AssignedToId")]
        [ValidateNever]
        public User AssignedTo { get; set; } 
        [Required]
        [StringLength(10)] 
        public string Priority { get; set; }
    }
}