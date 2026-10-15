
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    [Table("Results")]
    public class Result
    {
        [Key]
        public int ResultID { get; set; }

        [Required]
        public int EnrolmentID { get; set; }

        [Column(TypeName = "time")]
        public TimeSpan? FinishTime { get; set; }

        public int? FinishingPosition { get; set; }

        public DateTime RecordedAt { get; set; }

        [ForeignKey(nameof(EnrolmentID))]
        public Enrolment? Enrolment { get; set; }
    }
}
