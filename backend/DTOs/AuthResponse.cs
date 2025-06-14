namespace chatpro.DTOs
{
    public class AuthResponse
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public DateTime CreatedAt { get; set; } // Add this

        public string ProfilePictureUrl { get; set; } = null!;
    }
}
