using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

/// <summary>Runs the real API in memory, with an in-memory database instead of SQL Server.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove every registration tied to RaceDayDbContext's options (works on EF Core 8 through 10).
            var toRemove = services.Where(s => s.ServiceType.IsGenericType &&
                s.ServiceType.GenericTypeArguments.Contains(typeof(RaceDayDbContext))).ToList();
            foreach (var d in toRemove) services.Remove(d);
            services.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

public static class TestHelpers
{
    public const string Password = "Passw0rd!";
    public const string OrganiserCode = "RACEDAY-ORG-2026"; // matches appsettings.json

    public static string UniqueEmail() => $"{Guid.NewGuid():N}@test.com";

    public static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client, string email, string role, string? code = null) =>
        client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User", email, password = Password, role, organiserCode = code
        });

    /// <summary>Registers a new user, logs in, and returns a client carrying the bearer token.</summary>
    public static async Task<HttpClient> LoggedInClientAsync(ApiFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();
        (await RegisterAsync(client, email, role, role == "Organiser" ? OrganiserCode : null))
            .EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", json.GetProperty("token").GetString());
        return client;
    }

    public static object EventBody(string name = "Joburg 10km") => new
    {
        eventName = name, eventDate = "2027-03-01", location = "Johannesburg",
        description = "Test event", eventType = "Run", distance = 10
    };

    public static async Task<int> CreateEventAsync(HttpClient organiser)
    {
        var res = await organiser.PostAsJsonAsync("/api/events", EventBody());
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("eventId").GetInt32();
    }

    public static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId, int max = 10)
    {
        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "Senior", distance = 10, maxParticipants = max, entryFee = 150 });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("categoryId").GetInt32();
    }

    public static async Task<int> EnrolAsync(HttpClient participant, int categoryId)
    {
        var res = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("enrolmentId").GetInt32();
    }
}
