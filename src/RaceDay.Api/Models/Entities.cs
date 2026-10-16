namespace RaceDay.Api.Models;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Event> OrganisedEvents { get; set; } = new();
    public List<Enrolment> Enrolments { get; set; } = new();
}

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public User? Organiser { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public string? Description { get; set; }
    public EventType EventType { get; set; }     // added for Part 2 brief
    public decimal Distance { get; set; }        // added for Part 2 brief
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Category> Categories { get; set; } = new();
}

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal Distance { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
    public List<Enrolment> Enrolments { get; set; } = new();
}

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public User? Participant { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public EnrolmentStatus Status { get; set; } = EnrolmentStatus.Enrolled;
    public Result? Result { get; set; }
    public Payment? Payment { get; set; }
}

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public ResultStatus Status { get; set; }
}

public class Payment
{
    public int PaymentId { get; set; }
    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public DateTime PaymentDate { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
}
