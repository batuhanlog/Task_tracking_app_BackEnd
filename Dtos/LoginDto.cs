using System.ComponentModel.DataAnnotations;

namespace TaskTitan.Api.Dtos
{
    public class LoginDto
    {
        [Required]
        [Range(1, int.MaxValue)] // ID 0 veya negatif olamaz
        public int UserId { get; set; }

        // Şimdilik şifre yok, sadece ID ile login
        // public string Password { get; set; }
    }
}