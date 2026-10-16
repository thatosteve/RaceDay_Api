using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;
using Xunit;

public class EventsTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;
    public EventsTests(RaceDayApiFactory factory) => _factory = factory;

    private async Task<HttpClient> LoginAsOrganiserAsync(string email)
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto("Org", "Test", email, "Pass123!", "Organiser", null, null));
        return client;
    }

    private async Task<HttpClient> LoginAsParticipantAsync(string email)
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto("Part", "Test", email, "Pass123!", "Participant", null, null));
        return client;
    }

    [Fact]
    public async Task Organiser_Can_Create_Event()
    {
        var organiser = await LoginAsOrganiserAsync("evt_create@test.com");
        var dto = new EventCreateDto("Comrades 2026", "Long run",
            DateTime.UtcNow.AddDays(30), "Durban", 89.0m, "run");

        var res = await organiser.PostAsJsonAsync("/api/events", dto);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Create_Event_With_Invalid_Type_Returns_400()
    {
        var organiser = await LoginAsOrganiserAsync("evt_badtype@test.com");
        var dto = new EventCreateDto("Bad", "desc",
            DateTime.UtcNow.AddDays(10), "JHB", 10m, "swim");

        var res = await organiser.PostAsJsonAsync("/api/events", dto);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task LoggedIn_User_Can_List_Events()
    {
        var organiser = await LoginAsOrganiserAsync("evt_list@test.com");
        await organiser.PostAsJsonAsync("/api/events",
            new EventCreateDto("Listed Event", "desc", DateTime.UtcNow.AddDays(5), "CPT", 5m, "walk"));

        var participant = await LoginAsParticipantAsync("evt_list_p@test.com");
        var res = await participant.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_Cannot_Update_Other_Organisers_Event()
    {
        var owner = await LoginAsOrganiserAsync("evt_owner@test.com");
        var createRes = await owner.PostAsJsonAsync("/api/events",
            new EventCreateDto("Owned", "desc", DateTime.UtcNow.AddDays(5), "JHB", 5m, "run"));
        var created = await createRes.Content.ReadFromJsonAsync<EventCreated>();
        Assert.NotNull(created);

        var other = await LoginAsOrganiserAsync("evt_other@test.com");
        var update = new EventUpdateDto("Hacked", "desc", DateTime.UtcNow.AddDays(5), "JHB", 5m, "run");
        var res = await other.PutAsJsonAsync($"/api/events/{created!.EventId}", update);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private record EventCreated(int EventId);
}