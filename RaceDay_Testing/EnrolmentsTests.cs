using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;
using Xunit;

public class EnrolmentsTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;
    public EnrolmentsTests(RaceDayApiFactory factory) => _factory = factory;

    private record EventCreated(int EventId);
    private record CategoryCreated(int CategoryId);

    private async Task<(HttpClient organiser, HttpClient participant, int eventId, int categoryId)>
        SetupEventWithCategoryAsync(string suffix)
    {
        var organiser = _factory.CreateClient();
        await organiser.PostAsJsonAsync("/api/auth/register",
            new RegisterDto("Org", "T", $"enr_org_{suffix}@test.com", "Pass123!", "Organiser", null, null));

        var createEv = await organiser.PostAsJsonAsync("/api/events",
            new EventCreateDto("Enrol Test", "desc", DateTime.UtcNow.AddDays(20), "JHB", 10m, "run"));
        var ev = await createEv.Content.ReadFromJsonAsync<EventCreated>();

        var createCat = await organiser.PostAsJsonAsync(
            $"/api/events/{ev!.EventId}/categories",
            new CategoryCreateDto("10km", "Distance"));
        var cat = await createCat.Content.ReadFromJsonAsync<CategoryCreated>();

        var participant = _factory.CreateClient();
        await participant.PostAsJsonAsync("/api/auth/register",
            new RegisterDto("Part", "T", $"enr_pt_{suffix}@test.com", "Pass123!", "Participant", null, null));

        return (organiser, participant, ev.EventId, cat!.CategoryId);
    }

    [Fact]
    public async Task Participant_Can_Enrol_In_Event()
    {
        var (_, participant, eventId, categoryId) = await SetupEventWithCategoryAsync("a");
        var res = await participant.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments",
            new EnrolmentCreateDto(categoryId));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Duplicate_Enrolment_Returns_409()
    {
        var (_, participant, eventId, categoryId) = await SetupEventWithCategoryAsync("b");
        await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments",
            new EnrolmentCreateDto(categoryId));

        var res = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments",
            new EnrolmentCreateDto(categoryId));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_Cannot_Enrol_As_Participant()
    {
        var (organiser, _, eventId, categoryId) = await SetupEventWithCategoryAsync("c");
        var res = await organiser.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments",
            new EnrolmentCreateDto(categoryId));

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_Can_View_Enrolments_For_Own_Event()
    {
        var (organiser, participant, eventId, categoryId) = await SetupEventWithCategoryAsync("d");
        await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments",
            new EnrolmentCreateDto(categoryId));

        var res = await organiser.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}