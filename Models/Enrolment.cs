using System;

namespace RaceDay.Api.Models
{
    public class Enrolment
    {
        public int EnrolmentId { get; set; }
        public int UserId { get; set; }       // the Participant
        public int EventId { get; set; }
        public int CategoryId { get; set; }
        public string Status { get; set; } = "Pending";   // "Pending" or "Confirmed"
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public Event Event { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public Result? Result { get; set; }
    }
}
