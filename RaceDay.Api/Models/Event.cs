
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    [Table("Events")]
    public class Event
    {
        [Key]
        public int EventID { get; set; }

        [Required]
        public int OrganiserID { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "date")]
        public DateTime EventDate { get; set; }

        [Required]
        [MaxLength(150)]
        public string Location { get; set; } = string.Empty;

        [Column(TypeName = "decimal(6,2)")]
        public decimal Distance { get; set; }

        [Required]
        [MaxLength(20)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? BannerImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        // The organiser who created this event
        [ForeignKey(nameof(OrganiserID))]
        public AppUser? Organiser { get; set; }

        // Related records
        public ICollection<Category> Categories { get; set; }
            = new List<Category>();

        public ICollection<Route> Routes { get; set; }
            = new List<Route>();

        public ICollection<Enrolment> Enrolments { get; set; }
            = new List<Enrolment>();
    }
}
