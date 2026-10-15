
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    [Table("Enrolments")]
    public class Enrolment
    {
        [Key]
        public int EnrolmentID { get; set; }

        [Required]
        public int ParticipantID { get; set; }

        [Required]
        public int EventID { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public DateTime EnrolmentDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        // Participant who enrolled
        [ForeignKey(nameof(ParticipantID))]
        public AppUser? Participant { get; set; }

        // Event entered
        [ForeignKey(nameof(EventID))]
        public Event? Event { get; set; }

        // Category selected
        [ForeignKey(nameof(CategoryID))]
        public Category? Category { get; set; }

        // Optional result for this enrolment
        public Result? Result { get; set; }
    }
}
