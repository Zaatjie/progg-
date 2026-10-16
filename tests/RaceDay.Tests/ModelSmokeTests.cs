using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests;

public class ModelSmokeTests
{
    private static RaceDayDbContext NewContext() =>
        new(new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Can_save_and_read_a_user()
    {
        using var db = NewContext();
        db.Users.Add(new User { FullName = "Test", Email = "t@x.com", PasswordHash = "h", Role = UserRole.Participant });
        await db.SaveChangesAsync();
        Assert.Equal(1, await db.Users.CountAsync());
    }
}
