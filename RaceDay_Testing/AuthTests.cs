using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;
using Xunit;

public class AuthTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;
    public AuthTests(RaceDayApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_Then_Me_Returns_Profile()
    {
        var client = _factory.CreateClient();
        var dto = new RegisterDto("Org", "One", "auth1@test.com", "Pass123!", "Organiser", null, null);

        var res = await client.PostAsJsonAsync("/api/auth/register", dto);
        var body = await res.Content.ReadAsStringAsync();
        Assert.True(res.StatusCode == HttpStatusCode.Created,
            $"Register failed. Status={(int)res.StatusCode} Body={body}");

        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Login_With_Correct_Password_Returns_200()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto("U", "X", "auth2@test.com", "Pass123!", "Participant", null, null));

        // New client — no shared cookies
        var freshClient = _factory.CreateClient();
        var res = await freshClient.PostAsJsonAsync("/api/auth/login",
            new LoginDto("auth2@test.com", "Pass123!"));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Returns_401()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto("U", "X", "auth3@test.com", "Pass123!", "Participant", null, null));

        var freshClient = _factory.CreateClient();
        var res = await freshClient.PostAsJsonAsync("/api/auth/login",
            new LoginDto("auth3@test.com", "wrong"));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}