using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace UdemyRabbitMQWeb.Watermark.Models
{
    public class Product
    {

        [Key]
        public int Id { get; set; }
        [StringLength(100)]
        public string Name { get; set; }
        [Column(TypeName = "decimal(18,2")]
        public decimal Price { get; set; }
        [Range(1, 100)]
        public int Stock { get; set; }
        [StringLength(100)]
        public string? ImageName { get; set; }
    }
}
