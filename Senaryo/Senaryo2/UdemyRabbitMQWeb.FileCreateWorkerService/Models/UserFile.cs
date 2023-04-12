using System.ComponentModel.DataAnnotations.Schema;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics.Metrics;

namespace UdemyRabbitMQWeb.FileCreateWorkerService.Models
{
    public enum FileStatus
    {
        Creating,
        Completed
    }
    public class UserFile
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public DateTime? CreatedDate { get; set; }
        public FileStatus FileStatus { get; set; }



        // "NotMapped" özniteliği, Entity Framework ile veritabanı nesnesi eşlemesi yaparken, bir sınıfın belirli bir özelliğinin veritabanında bir sütun olarak eşleştirilmemesi gerektiğini belirtmek için kullanılır.
        //Bu örnekte, "NotMapped" özniteliği "GetCreatedDate" özelliğine uygulanmıştır. Bu özellik, CreatedDate özelliğinin değerini bir tarih dizesine dönüştürür ve CreatedDate özelliği veritabanında bir sütun olarak saklanmadığından, "NotMapped" özniteliği ile belirtilir.
        //Yani, burada "NotMapped" özniteliği, "GetCreatedDate" özelliğinin veritabanında saklanmaması gerektiğini belirtir ve bu özelliğin yalnızca bellekte hesaplanan bir değer olduğunu ifade eder. Bu özellik, veritabanından veri çekerken veya veritabanına veri kaydederken dikkate alınmayacaktır.
        [NotMapped]
        public string GetCreatedDate => CreatedDate.HasValue ? CreatedDate.Value.ToShortDateString() : "-";

    }
}
