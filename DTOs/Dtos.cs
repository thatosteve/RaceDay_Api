    using System;

namespace RaceDay.Api.DTOs
{
    public record RegisterDto(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string Role,                    // "Organiser" or "Participant"
        string? PhoneNumber,
        DateTime? DateOfBirth);

    public record LoginDto(string Email, string Password);

    public record UpdateProfileDto(
        string FirstName,
        string LastName,
        string? PhoneNumber,
        DateTime? DateOfBirth);

    public record EventCreateDto(
        string Name,
        string? Description,
        DateTime EventDate,
        string Location,
        decimal? DistanceKm,
        string EventType);              // "run", "walk", "cycle"

    public record EventUpdateDto(
        string Name,
        string? Description,
        DateTime EventDate,
        string Location,
        decimal? DistanceKm,
        string EventType);

    public record CategoryCreateDto(string Name, string CategoryType);  // "Age" or "Distance"
    public record CategoryUpdateDto(string Name, string CategoryType);

    public record EnrolmentCreateDto(int CategoryId);

    public record EnrolmentStatusUpdateDto(string Status);   // "Pending" or "Confirmed"

    public record ResultCreateDto(TimeSpan FinishTime, int FinishPosition);
    public record ResultUpdateDto(TimeSpan FinishTime, int FinishPosition);
}