using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Tests;

public class RoleAccessTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public RoleAccessTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Unauthenticated_user_cannot_create_event()
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/events", TestHelpers.EventBody());
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_user_cannot_view_profile()
    {
        var res = await _factory.CreateClient().GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_user_cannot_enrol()
    {
        var res = await _factory.CreateClient().PostAsync("/api/categories/1/enrolments", null);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Participant_cannot_create_event()
    {
        var part = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var res = await part.PostAsJsonAsync("/api/events", TestHelpers.EventBody());
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_cannot_add_category()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(org);
        var part = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var res = await part.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "X", distance = 5, maxParticipants = 5, entryFee = 0 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_cannot_capture_result()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var catId = await TestHelpers.CreateCategoryAsync(org, await TestHelpers.CreateEventAsync(org));
        var part = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var enrolmentId = await TestHelpers.EnrolAsync(part, catId);
        var res = await part.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:40:00", position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_cannot_enrol_in_a_category()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var catId = await TestHelpers.CreateCategoryAsync(org, await TestHelpers.CreateEventAsync(org));
        var res = await org.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_cannot_use_participant_only_endpoints()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        Assert.Equal(HttpStatusCode.Forbidden, (await org.GetAsync("/api/users/me/enrolments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await org.GetAsync("/api/users/me/results")).StatusCode);
    }

    [Fact]
    public async Task Participant_cannot_view_category_enrolment_list()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var catId = await TestHelpers.CreateCategoryAsync(org, await TestHelpers.CreateEventAsync(org));
        var part = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        Assert.Equal(HttpStatusCode.Forbidden, (await part.GetAsync($"/api/categories/{catId}/enrolments")).StatusCode);
    }
}
