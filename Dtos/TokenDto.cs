namespace TaskTitan.Api.Dtos
{
    public class TokenDto
    {
        public string Token { get; set; } = string.Empty;
        // İsteğe bağlı olarak geçerlilik süresi de eklenebilir
        // public DateTime Expires { get; set; }
    }
}