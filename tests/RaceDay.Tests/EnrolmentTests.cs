using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using Xunit;

namespace RaceDay.Tests;

public class EnrolmentTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public EnrolmentTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Participant_can_enrol_and_enrolment_is_recorded_correctly()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(org);
        var catId = await TestHelpers.CreateCategoryAsync(org, eventId);
        var part = await TestHelpers.LoggedInClientAsync(_factory, "Participant");

        var res = await part.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        var enrolmentId = json.GetProperty("enrolmentId").GetInt32();
        var participantId = json.GetProperty("participantId").GetInt32();

        // Check the database row links participant, category and (via category) the event.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var row = await db.Enrolments.Include(e => e.Category)
            .SingleAsync(e => e.EnrolmentId == enrolmentId);
        Assert.Equal(participantId, row.ParticipantId);
        Assert.Equal(catId, row.CategoryId);
        Assert.Equal(eventId, row.Category!.EventId);

        // And it shows up in the participant's own list.
        var mine = await part.GetFromJsonAsync<JsonElement>("/api/users/me/enrolments");
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(catId, mine[0].GetProperty("categoryId").GetInt32());
    }

    [Fact]
    public async Task Enrolling_twice_in_same_category_returns_409()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var catId = await TestHelpers.CreateCategoryAsync(org, await TestHelpers.CreateEventAsync(org));
        var part = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        await TestHelpers.EnrolAsync(part, catId);
        var second = await part.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrolling_in_a_full_category_returns_409()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var catId = await TestHelpers.CreateCategoryAsync(org, await TestHelpers.CreateEventAsync(org), max: 1);
        var first = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var second = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        await TestHelpers.EnrolAsync(first, catId);
        var res = await second.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Enrolling_in_unknown_category_returns_404()
    {
        var part = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var res = await part.PostAsync("/api/categories/999999/enrolments", null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Owning_organiser_can_view_enrolments_for_category_and_event()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(org);
        var catId = await TestHelpers.CreateCategoryAsync(org, eventId);
        var part = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        await TestHelpers.EnrolAsync(part, catId);

        var byCategory = await org.GetFromJsonAsync<JsonElement>($"/api/categories/{catId}/enrolments");
        var byEvent = await org.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}/enrolments");
        Assert.Equal(1, byCategory.GetArrayLength());
        Assert.Equal(1, byEvent.GetArrayLength());
    }

    [Fact]
    public async Task Other_organiser_cannot_view_enrolments()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var catId = await TestHelpers.CreateCategoryAsync(owner, await TestHelpers.CreateEventAsync(owner));
        var res = await other.GetAsync($"/api/categories/{catId}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_can_cancel_own_enrolment_but_not_someone_elses()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var catId = await TestHelpers.CreateCategoryAsync(org, await TestHelpers.CreateEventAsync(org));
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var other = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var enrolmentId = await TestHelpers.EnrolAsync(owner, catId);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/enrolments/{enrolmentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/enrolments/{enrolmentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.DeleteAsync($"/api/enrolments/{enrolmentId}")).StatusCode);
    }
}
