using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models
{
    public class HomeIndexViewModel
    {
        public List<EventDto> Events { get; set; } = new();
        public HashSet<int> EnrolledEventIds { get; set; } = new();
        public string? Type { get; set; }
        public string? Search { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class EventDetailsViewModel
    {
        public EventDto Event { get; set; } = null!;
        public MyEnrolmentDto? Enrolment { get; set; }
        public bool IsParticipant { get; set; }
    }

    public class EventFormViewModel
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Give the event a name."), StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Choose the date and start time."), Display(Name = "Date and start time")]
        public DateTime? EventDate { get; set; }

        [Required(ErrorMessage = "Where does it take place?"), StringLength(150)]
        public string Location { get; set; } = "";

        [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km."), Display(Name = "Distance (km)")]
        public decimal? DistanceKm { get; set; }

        [Required, Display(Name = "Event type")]
        public string EventType { get; set; } = "run";

        [Display(Name = "Banner image")]
        public IFormFile? Banner { get; set; }

        public string? ExistingBannerUrl { get; set; }
    }

    public class OrganiserEventViewModel
    {
        public EventDto Event { get; set; } = null!;
        public List<EventEnrolmentDto> Enrolments { get; set; } = new();
    }

    public class ProfileViewModel
    {
        [Required(ErrorMessage = "First name is required."), StringLength(50), Display(Name = "First name")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Last name is required."), StringLength(50), Display(Name = "Last name")]
        public string LastName { get; set; } = "";

        [Phone, Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date), Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        // Read-only display fields (not posted back)
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}
