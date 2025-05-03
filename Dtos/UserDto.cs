namespace TaskTitan.Api.Dtos
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        // İstersen kullanıcının dahil olduğu proje veya görev sayısını ekleyebilirsin
    }
}