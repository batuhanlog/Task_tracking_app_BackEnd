using System.ComponentModel.DataAnnotations; // Validation için

namespace TaskTitan.Api.Dtos
{
    public class CreateCustomerDto
    {
        [Required(ErrorMessage = "Customer name is required.")] // Hata mesajı eklendi
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Customer name must be between 2 and 150 characters.")] // Min uzunluk ve mesaj eklendi
        public string Name { get; set; } = string.Empty;
    }
}