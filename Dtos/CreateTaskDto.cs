using System;
using System.ComponentModel.DataAnnotations;
using TaskTitanData.Entities;

namespace TaskTitan.Api.Dtos
{
    public class CreateTaskDto
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Due date is required.")]
        public DateTime DueDate { get; set; }

        [EnumDataType(typeof(TaskTitanData.Entities.TaskStatus), ErrorMessage = "Invalid Status value.")]
        public string? Status { get; set; }

        [Required(ErrorMessage = "ProjectId is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid ProjectId.")]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "AssignedUserId is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid AssignedUserId.")]
        public int AssignedUserId { get; set; }

        // Bu zaten vardı, doğru. Nullable yapmaya gerek yok.
        public bool IsDaily { get; set; } = false;
    }
}