
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        public int EventID { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string CategoryType { get; set; } = string.Empty;

        public int? MinAge { get; set; }

        public int? MaxAge { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal? Distance { get; set; }

        // The event this category belongs to
        [ForeignKey(nameof(EventID))]
        public Event? Event { get; set; }

        // Participants enrolled in this category
        public ICollection<Enrolment> Enrolments { get; set; }
            = new List<Enrolment>();
    }
}
