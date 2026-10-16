-- RaceDay database schema (SQL Server)
-- Mirrors the EF Core model in src/RaceDay.Api/Models. Enum columns store readable text.

IF DB_ID('RaceDayDb') IS NULL CREATE DATABASE RaceDayDb;
GO
USE RaceDayDb;
GO

CREATE TABLE Users (
    UserId        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FullName      NVARCHAR(100) NOT NULL,
    Email         NVARCHAR(150) NOT NULL,
    PasswordHash  NVARCHAR(255) NOT NULL,            -- BCrypt hash, never the raw password
    Role          NVARCHAR(20)  NOT NULL,
    Phone         NVARCHAR(20)  NULL,
    CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant'))
);

CREATE TABLE Events (
    EventId      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrganiserId  INT           NOT NULL,
    EventName    NVARCHAR(150) NOT NULL,
    EventDate    DATE          NOT NULL,
    Location     NVARCHAR(150) NOT NULL,
    Description  NVARCHAR(500) NULL,
    EventType    NVARCHAR(20)  NOT NULL,              -- added for Part 2
    Distance     DECIMAL(5,2)  NOT NULL,              -- added for Part 2 (km)
    CreatedAt    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId) REFERENCES Users(UserId),
    CONSTRAINT CK_Events_Type CHECK (EventType IN ('Run', 'Walk', 'Cycle'))
);

CREATE TABLE Categories (
    CategoryId       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    EventId          INT           NOT NULL,
    CategoryName     NVARCHAR(100) NOT NULL,
    Distance         DECIMAL(5,2)  NOT NULL,
    MaxParticipants  INT           NOT NULL,
    EntryFee         DECIMAL(8,2)  NOT NULL,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) REFERENCES Events(EventId) ON DELETE CASCADE
);

CREATE TABLE Enrolments (
    EnrolmentId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ParticipantId  INT          NOT NULL,
    CategoryId     INT          NOT NULL,
    EnrolmentDate  DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
    Status         NVARCHAR(20) NOT NULL,
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId) REFERENCES Users(UserId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId) ON DELETE CASCADE,
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantId, CategoryId),
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Enrolled', 'Cancelled'))
);

CREATE TABLE Results (
    ResultId     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    EnrolmentId  INT          NOT NULL,
    FinishTime   TIME         NOT NULL,
    Position     INT          NOT NULL,
    Status       NVARCHAR(20) NOT NULL,
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId) ON DELETE CASCADE,
    CONSTRAINT UQ_Results_Enrolment UNIQUE (EnrolmentId),
    CONSTRAINT CK_Results_Status CHECK (Status IN ('Finished', 'DNF', 'DNS'))
);

CREATE TABLE Payments (
    PaymentId      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    EnrolmentId    INT          NOT NULL,
    Amount         DECIMAL(8,2) NOT NULL,
    PaymentMethod  NVARCHAR(30) NOT NULL,
    PaymentDate    DATETIME2    NOT NULL,
    PaymentStatus  NVARCHAR(20) NOT NULL,
    CONSTRAINT FK_Payments_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId) ON DELETE CASCADE,
    CONSTRAINT UQ_Payments_Enrolment UNIQUE (EnrolmentId),
    CONSTRAINT CK_Payments_Status CHECK (PaymentStatus IN ('Pending', 'Paid', 'Refunded'))
);
GO
