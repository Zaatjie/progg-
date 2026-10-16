using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Tests;

public class ResultTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public ResultTests(ApiFactory factory) => _factory = factory;

    private static readonly object Finished = new { finishTime = "00:45:30", position = 1, status = "Finished" };

    [Fact]
    public async Task Organiser_captures_result_and_participant_sees_it()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var catId = await TestHelpers.CreateCategoryAsync(org, await TestHelpers.CreateEventAsync(org));
        var part = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var enrolmentId = await TestHelpers.EnrolAsync(part, catId);

        var res = await org.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", Finished);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var mine = await part.GetFromJsonAsync<JsonElement>("/api/users/me/results");
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(1, mine[0].GetProperty("position").GetInt32());

        var board = await _factory.CreateClient().GetFromJsonAsync<JsonElement>($"/api/categories/{catId}/results");
        Assert.Equal(1, board.GetArrayLength());
    }

    [Fact]
    public async Task Capturing_a_second_result_returns_409()
    {
        var org = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var catId = await TestHelpers.CreateCategoryAsync(org, await TestHelpers.CreateEventAsync(org));
        var part = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var enrolmentId = await TestHelpers.EnrolAsync(part, catId);
        await org.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", Finished);
        var second = await org.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", Finished);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Other_organiser_cannot_capture_result()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.LoggedInClientAsync(_factory, "Organiser");
        var catId = await TestHelpers.CreateCategoryAsync(owner, await TestHelpers.CreateEventAsync(owner));
        var part = await TestHelpers.LoggedInClientAsync(_factory, "Participant");
        var enrolmentId = await TestHelpers.EnrolAsync(part, catId);
        var res = await other.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", Finished);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Leaderboard_for_unknown_category_returns_404()
    {
        var res = await _factory.CreateClient().GetAsync("/api/categories/999999/results");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
