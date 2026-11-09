namespace RaceDay.Mvc.Models
{
    // Shapes returned by the RaceDay API (matched to the Part 2 controllers).
    public record CategoryDto(int CategoryId, int EventId, string Name, string CategoryType);

    public record EventDto(int EventId, int OrganiserId, string Name, string? Description, DateTime EventDate,
        string Location, decimal? DistanceKm, string EventType, string? BannerImageUrl, List<CategoryDto>? Categories);

    public record OrganiserEventDto(int EventId, string Name, string? Description, DateTime EventDate, string Location,
        decimal? DistanceKm, string EventType, string? BannerImageUrl, int CategoryCount, int EnrolmentCount, int ResultCount);

    public record MyEnrolmentDto(int EnrolmentId, int EventId, string EventName, DateTime EventDate,
        int CategoryId, string CategoryName, string Status, DateTime EnrolledAt);

    public record EventEnrolmentDto(int EnrolmentId, string Status, DateTime EnrolledAt, int ParticipantId,
        string ParticipantName, string ParticipantEmail, int CategoryId, string CategoryName,
        int? ResultId, TimeSpan? FinishTime, int? FinishPosition);

    public record MyResultDto(int ResultId, int EventId, string EventName, DateTime EventDate, string Category,
        TimeSpan? FinishTime, int? FinishPosition, int TotalFinishers);

    public record ProfileDto(int UserId, string FirstName, string LastName, string Email, string Role,
        string? PhoneNumber, DateTime? DateOfBirth, string? ProfileImageUrl);

    public record UrlDto(string Url);
}
