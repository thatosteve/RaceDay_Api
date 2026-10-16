using System;
using System.Collections.Generic;

namespace RaceDay.Api.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryType { get; set; } = string.Empty;  // "Age" or "Distance"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Event Event { get; set; } = null!;
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}