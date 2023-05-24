using System.ComponentModel.DataAnnotations;

namespace DaprPOC.Infrastructure.Models
{
    public class Cage
    {
        [Key]
        public int Id { get; set; }
        public string ExternalId { get; set; }
        public string Name { get; set; }
    }
}
