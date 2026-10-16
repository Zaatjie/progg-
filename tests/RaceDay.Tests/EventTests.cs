using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public EventTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Organiser_can_create_event()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var res = await org.PostAsJsonAsync("/api/events", TestHelpers.EventBody());
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Create_event_with_missing_name_returns_400()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var res = await org.PostAsJsonAsync("/api/events", new
        {
            eventName = "", eventDate = "2027-03-01", location = "Joburg", eventType = "Run", distance = 5
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_event_with_invalid_type_returns_400()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var res = await org.PostAsJsonAsync("/api/events", new
        {
            eventName = "X", eventDate = "2027-03-01", location = "Joburg", eventType = "Swim", distance = 5
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_can_update_own_event()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(org);
        var res = await org.PutAsJsonAsync($"/api/events/{id}", TestHelpers.EventBody("Renamed"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_can_delete_own_event()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(org);
        Assert.Equal(HttpStatusCode.NoContent, (await org.DeleteAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await org.GetAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Other_organiser_cannot_update_or_delete_event()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(owner);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await other.PutAsJsonAsync($"/api/events/{id}", TestHelpers.EventBody("Hijack"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Updating_unknown_event_returns_404()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var res = await org.PutAsJsonAsync("/api/events/999999", TestHelpers.EventBody());
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Anyone_can_list_and_view_events_without_logging_in()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(org);
        var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Participant_can_view_events_and_categories()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(org);
        await TestHelpers.CreateCategoryAsync(org, eventId);
        var part = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        Assert.Equal(HttpStatusCode.OK, (await part.GetAsync($"/api/events/{eventId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await part.GetAsync($"/api/events/{eventId}/categories")).StatusCode);
    }
}
