using System;
// using System.Collections.Generic; // Şimdilik gerek yok

namespace TaskTitan.Api.Dtos
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        public int TaskCount { get; set; }
    }
}