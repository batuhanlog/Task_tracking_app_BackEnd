using System.ComponentModel.DataAnnotations;

namespace TaskTitan.Api.Dtos
{
    public class CreateProjectDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;
        [Required]
        public int CustomerId { get; set; }
    }
}