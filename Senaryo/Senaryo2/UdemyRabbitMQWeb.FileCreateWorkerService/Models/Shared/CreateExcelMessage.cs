namespace UdemyRabbitMQWeb.FileCreateWorkerService.Models.Shared
{
    public class CreateExcelMessage
    {
        public string UserId { get; set; }
        public int FileId { get; set; }
    }
}
