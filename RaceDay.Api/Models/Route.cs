
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    [Table("Routes")]
    public class Route
    {
        [Key]
        public int RouteID { get; set; }

        [Required]
        public int EventID { get; set; }

        [Required]
        [MaxLength(100)]
        public string RouteName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string StartLocation { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string EndLocation { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? RouteDescription { get; set; }

        [MaxLength(500)]
        public string? RouteMapUrl { get; set; }

        // Event associated with this route
        [ForeignKey(nameof(EventID))]
        public Event? Event { get; set; }
    }
}
