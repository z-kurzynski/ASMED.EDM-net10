using ASMED.EDM.Core.Entities;
using ASMED.EDM.P1.Clients;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ASMED.EDM.P1.Tests;

public class StubP1ClientTests
{
    private static StubP1Client CreateClient() => new(NullLogger<StubP1Client>.Instance);

    [Fact]
    public async Task VerifyPatientByPeselAsync_ReturnsFoundAndMatching()
    {
        var patient = new Patient
        {
            Id = 5,
            FirstName = "Jan",
            LastName = "Kowalski",
            IdentificationNumber = "80010112345",
            DateOfBirth = new DateTime(1980, 1, 1)
        };

        var result = await CreateClient().VerifyPatientByPeselAsync(patient);

        Assert.True(result.Found);
        Assert.True(result.DataMatches);
        Assert.Equal("Jan", result.FirstName);
        Assert.Equal("Kowalski", result.LastName);
        Assert.NotNull(result.P1PatientId);
    }

    [Fact]
    public async Task RegisterVisitAsync_ReturnsSuccess_WithGeneratedEventId()
    {
        var visit = new Visit { Id = 7, PatientId = 1, DoctorId = 2 };

        var result = await CreateClient().RegisterVisitAsync(visit);

        Assert.True(result.Success);
        Assert.NotNull(result.P1EventId);
        Assert.StartsWith("stub-event-7", result.P1EventId);
    }

    [Fact]
    public async Task UpdateVisitAsync_ReturnsSuccess()
    {
        var visit = new Visit { Id = 7, P1EventId = "stub-event-7-abc" };

        var result = await CreateClient().UpdateVisitAsync(visit);

        Assert.True(result.Success);
        Assert.Equal("stub-event-7-abc", result.P1EventId);
    }

    [Fact]
    public async Task CancelVisitAsync_ReturnsSuccess()
    {
        var visit = new Visit { Id = 7, P1EventId = "stub-event-7-abc" };

        var result = await CreateClient().CancelVisitAsync(visit);

        Assert.True(result.Success);
    }
}
