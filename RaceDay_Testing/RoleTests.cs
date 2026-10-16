using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;
using Xunit;

public class RoleTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;
    public RoleTests(RaceDayApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Participant_Cannot_Create_Event()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto("P", "One", "pt_role@test.com", "Pass123!", "Participant", null, null));

        var ev = new EventCreateDto("Test Run", "desc",
            DateTime.UtcNow.AddDays(10), "JHB", 10, "run");

        var res = await client.PostAsJsonAsync("/api/events", ev);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_Cannot_See_Profile()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_Cannot_List_Events()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}