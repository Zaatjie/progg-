using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using Xunit;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public AuthTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_participant_returns_201_and_no_password()
    {
        var res = await TestHelpers.RegisterAsync(_factory.CreateClient(), TestHelpers.UniqueEmail(), "Participant");
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_organiser_with_valid_code_returns_201()
    {
        var res = await TestHelpers.RegisterAsync(_factory.CreateClient(), TestHelpers.UniqueEmail(),
            "Organiser", TestHelpers.OrganiserCode);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Register_organiser_without_code_returns_400()
    {
        var res = await TestHelpers.RegisterAsync(_factory.CreateClient(), TestHelpers.UniqueEmail(), "Organiser");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_duplicate_email_returns_409()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail();
        await TestHelpers.RegisterAsync(client, email, "Participant");
        var second = await TestHelpers.RegisterAsync(client, email, "Participant");
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_invalid_email_returns_400()
    {
        var res = await TestHelpers.RegisterAsync(_factory.CreateClient(), "not-an-email", "Participant");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_missing_role_returns_400()
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { fullName = "A", email = TestHelpers.UniqueEmail(), password = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Password_is_stored_hashed()
    {
        var email = TestHelpers.UniqueEmail();
        await TestHelpers.RegisterAsync(_factory.CreateClient(), email, "Participant");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        Assert.NotEqual(TestHelpers.Password, user.PasswordHash);
        Assert.StartsWith("$2", user.PasswordHash);
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_token()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail();
        await TestHelpers.RegisterAsync(client, email, "Participant");
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(json.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail();
        await TestHelpers.RegisterAsync(client, email, "Participant");
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_unknown_email_returns_401()
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = TestHelpers.UniqueEmail(), password = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Profile_can_be_viewed_and_updated_when_logged_in()
    {
        var client = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);

        var put = await client.PutAsJsonAsync("/api/users/me", new { fullName = "New Name", phone = "0821234567" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var json = await put.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("New Name", json.GetProperty("fullName").GetString());
    }
}
