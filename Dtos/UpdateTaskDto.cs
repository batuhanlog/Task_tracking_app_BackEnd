using System;
using System.ComponentModel.DataAnnotations;
using TaskTitanData.Entities;

namespace TaskTitan.Api.Dtos
{
    public class UpdateTaskDto
    {
        [StringLength(200)]
        public string? Title { get; set; }
        [StringLength(1000)]
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public TaskTitanData.Entities.TaskStatus? Status { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Invalid AssignedUserId.")]
        public int? AssignedUserId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Invalid ProjectId.")]
        public int? ProjectId { get; set; }
        public bool? IsDaily { get; set; } // Bu zaten vardı, doğru.
    }
}