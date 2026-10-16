using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

// ---------- Auth / users ----------
public class RegisterRequest
{
    /// <summary>The user's full name.</summary>
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    /// <summary>Unique email address, used to log in.</summary>
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = "";
    /// <summary>Password (8-100 characters). Stored only as a BCrypt hash.</summary>
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = "";
    /// <summary>Organiser or Participant.</summary>
    [Required] public UserRole? Role { get; set; }
    /// <summary>Secret code required only when registering as an Organiser.</summary>
    public string? OrganiserCode { get; set; }
}

public class LoginRequest
{
    [Required] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class UpdateProfileRequest
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [StringLength(20)] public string? Phone { get; set; }
}

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public UserRole Role { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }

    public static UserDto From(User u) => new()
    {
        UserId = u.UserId, FullName = u.FullName, Email = u.Email,
        Role = u.Role, Phone = u.Phone, CreatedAt = u.CreatedAt
    };
}

public class AuthResponse
{
    /// <summary>JWT to send as 'Authorization: Bearer {token}'.</summary>
    public string Token { get; set; } = "";
    public UserDto User { get; set; } = new();
}

// ---------- Events ----------
public class EventRequest
{
    [Required, StringLength(150)] public string EventName { get; set; } = "";
    [Required] public DateTime? EventDate { get; set; }
    [Required, StringLength(150)] public string Location { get; set; } = "";
    [StringLength(500)] public string? Description { get; set; }
    /// <summary>Run, Walk or Cycle.</summary>
    [Required] public EventType? EventType { get; set; }
    /// <summary>Overall event distance in km.</summary>
    [Range(typeof(decimal), "0.01", "999.99")] public decimal Distance { get; set; }
}

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string? OrganiserName { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public string? Description { get; set; }
    public EventType EventType { get; set; }
    public decimal Distance { get; set; }
    public List<CategoryDto>? Categories { get; set; }

    public static EventDto From(Event e, bool includeCategories = false) => new()
    {
        EventId = e.EventId, OrganiserId = e.OrganiserId, OrganiserName = e.Organiser?.FullName,
        EventName = e.EventName, EventDate = e.EventDate, Location = e.Location,
        Description = e.Description, EventType = e.EventType, Distance = e.Distance,
        Categories = includeCategories ? e.Categories.Select(CategoryDto.From).ToList() : null
    };
}

// ---------- Categories ----------
public class CategoryRequest
{
    /// <summary>For example "Under 20", "Senior", "10km" or "21km".</summary>
    [Required, StringLength(100)] public string CategoryName { get; set; } = "";
    [Range(typeof(decimal), "0.01", "999.99")] public decimal Distance { get; set; }
    [Range(1, 100000)] public int MaxParticipants { get; set; }
    [Range(typeof(decimal), "0", "999999.99")] public decimal EntryFee { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal Distance { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }

    public static CategoryDto From(Category c) => new()
    {
        CategoryId = c.CategoryId, EventId = c.EventId, CategoryName = c.CategoryName,
        Distance = c.Distance, MaxParticipants = c.MaxParticipants, EntryFee = c.EntryFee
    };
}

// ---------- Enrolments ----------
public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EnrolmentDate { get; set; }
    public EnrolmentStatus Status { get; set; }

    public static EnrolmentDto From(Enrolment e) => new()
    {
        EnrolmentId = e.EnrolmentId, ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant?.FullName ?? "",
        CategoryId = e.CategoryId, CategoryName = e.Category?.CategoryName ?? "",
        EventId = e.Category?.EventId ?? 0, EventName = e.Category?.Event?.EventName ?? "",
        EnrolmentDate = e.EnrolmentDate, Status = e.Status
    };
}

// ---------- Results ----------
public class ResultRequest
{
    /// <summary>Finish time as hh:mm:ss, for example 00:45:30.</summary>
    public TimeSpan FinishTime { get; set; }
    /// <summary>Finishing position (must be 1 or more when Status is Finished).</summary>
    [Range(0, 100000)] public int Position { get; set; }
    /// <summary>Finished, DNF or DNS.</summary>
    [Required] public ResultStatus? Status { get; set; }
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string EventName { get; set; } = "";
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public ResultStatus Status { get; set; }

    public static ResultDto From(Result r) => new()
    {
        ResultId = r.ResultId, EnrolmentId = r.EnrolmentId,
        ParticipantName = r.Enrolment?.Participant?.FullName ?? "",
        CategoryId = r.Enrolment?.CategoryId ?? 0,
        CategoryName = r.Enrolment?.Category?.CategoryName ?? "",
        EventName = r.Enrolment?.Category?.Event?.EventName ?? "",
        FinishTime = r.FinishTime, Position = r.Position, Status = r.Status
    };
}
