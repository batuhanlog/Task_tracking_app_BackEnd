namespace TaskTitan.Api.Dtos
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        // İleride projelerinin sayısı gibi ek bilgiler eklenebilir
        // public int ProjectCount { get; set; }
    }
}