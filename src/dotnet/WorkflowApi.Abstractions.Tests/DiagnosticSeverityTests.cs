using Xunit;

namespace WorkflowApi.Abstractions.Tests;

public class DiagnosticSeverityTests
{
    [Fact]
    public void DiagnosticSeverity_HasExpectedMembers()
    {
        var values = Enum.GetValues(typeof(DiagnosticSeverity)).Cast<DiagnosticSeverity>().ToList();

        Assert.Contains(DiagnosticSeverity.Info, values);
        Assert.Contains(DiagnosticSeverity.Warning, values);
        Assert.Contains(DiagnosticSeverity.Error, values);
        Assert.Equal(3, values.Count);
    }

    [Fact]
    public void DiagnosticSeverity_Info_RoundTripsCorrectly()
    {
        var severity = DiagnosticSeverity.Info;
        var stringValue = severity.ToString();
        var parsed = Enum.Parse<DiagnosticSeverity>(stringValue);

        Assert.Equal(severity, parsed);
    }

    [Fact]
    public void DiagnosticSeverity_Warning_RoundTripsCorrectly()
    {
        var severity = DiagnosticSeverity.Warning;
        var stringValue = severity.ToString();
        var parsed = Enum.Parse<DiagnosticSeverity>(stringValue);

        Assert.Equal(severity, parsed);
    }

    [Fact]
    public void DiagnosticSeverity_Error_RoundTripsCorrectly()
    {
        var severity = DiagnosticSeverity.Error;
        var stringValue = severity.ToString();
        var parsed = Enum.Parse<DiagnosticSeverity>(stringValue);

        Assert.Equal(severity, parsed);
    }
}
