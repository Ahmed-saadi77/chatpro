namespace chatpro.DTOs
{
    public class SendMessageForm
    {
        public Guid ReceiverId { get; set; }
        public string? Text { get; set; }
        public IFormFile? Image { get; set; }
    }


}
