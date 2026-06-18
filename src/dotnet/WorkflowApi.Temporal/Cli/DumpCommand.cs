using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using WorkflowApi.Abstractions;
using WorkflowApi.AspNetCore.Serialization;
using WorkflowApi.Temporal.Documents;
using WorkflowApi.Temporal.Hosting;
using WorkflowApi.Temporal.Mapping;
using WorkflowApi.Temporal.Scanning;

namespace WorkflowApi.Temporal.Cli;

/// <summary>
/// Wires the full scan → read → map → document pipeline and writes YAML files + index.json to disk.
/// </summary>
public sealed class DumpCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>
    /// Executes the dump pipeline against <paramref name="services"/> and writes output to
    /// <paramref name="outputDirectory"/>.
    /// </summary>
    public DumpResult Execute(IServiceCollection services, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(outputDirectory);
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory must not be empty or whitespace.", nameof(outputDirectory));

        Directory.CreateDirectory(outputDirectory);

        // Read registered workers first so we can derive the assemblies to scan.
        // This is more robust than scanning all loaded assemblies, which can include
        // test fixture types with intentionally invalid names (e.g., [Workflow("risk/enrichment")]).
        // The scanner will still emit WF001/ACT001 for any unregistered types it finds
        // within the same assemblies.
        var registeredWorkers = new TemporalWorkerOptionsReader().Read(services);

        var assemblies = registeredWorkers.Count > 0
            ? registeredWorkers
                .SelectMany(w => w.WorkflowTypes.Concat(w.ActivityTypes))
                .Select(t => t.Assembly)
                .Distinct()
                .ToList()
            : (IReadOnlyList<System.Reflection.Assembly>)[];

        var scanResult = new TemporalAttributeScanner().Scan(assemblies);
        var mapping = new WorkerMappingBuilder().Build(scanResult, registeredWorkers);
        var documents = new DocumentFactory().Build(mapping);

        var emitted = new List<EmittedDocument>();

        foreach (var doc in documents)
        {
            var hostId = doc.Host.Id;
            var taskQueue = doc.Bindings.Temporal!.TaskQueue;

            // Hackweek: if hostId == taskQueue the filename doubles the value
            // e.g. "risk-enrichment-risk-enrichment.workflowapi.yaml" — accepted for now.
            var fileName = $"{hostId}-{taskQueue}.workflowapi.yaml";
            var filePath = Path.Combine(outputDirectory, fileName);

            var yaml = WorkflowApiYamlWriter.Write(doc);
            File.WriteAllText(filePath, yaml);

            emitted.Add(new EmittedDocument(
                HostId: hostId,
                TaskQueue: taskQueue,
                FileName: fileName,
                WorkflowCount: doc.Workflows?.Count ?? 0,
                ActivityCount: doc.Activities?.Count ?? 0));
        }

        // Write index.json
        var indexPath = Path.Combine(outputDirectory, "index.json");
        var indexPayload = new IndexPayload(
            Documents: emitted.Select(e => new IndexDocument(
                e.HostId, e.TaskQueue, e.FileName, e.WorkflowCount, e.ActivityCount)).ToList(),
            Diagnostics: mapping.Diagnostics.Select(d => new IndexDiagnostic(
                d.Severity.ToString(), d.Code, d.Message, d.Target)).ToList());

        File.WriteAllText(indexPath, JsonSerializer.Serialize(indexPayload, JsonOptions));

        return new DumpResult(
            Documents: emitted,
            Diagnostics: mapping.Diagnostics,
            IndexFile: Path.GetFullPath(indexPath));
    }

    // Private JSON payload types for index.json serialisation.
    private sealed record IndexPayload(
        List<IndexDocument> Documents,
        List<IndexDiagnostic> Diagnostics);

    private sealed record IndexDocument(
        string HostId,
        string TaskQueue,
        string File,
        int WorkflowCount,
        int ActivityCount);

    private sealed record IndexDiagnostic(
        string Severity,
        string Code,
        string Message,
        string? Target);
}

/// <summary>Result returned by <see cref="DumpCommand.Execute"/>.</summary>
public sealed record DumpResult(
    IReadOnlyList<EmittedDocument> Documents,
    IReadOnlyList<Diagnostic> Diagnostics,
    string IndexFile);

/// <summary>Metadata for a single YAML document written to disk.</summary>
public sealed record EmittedDocument(
    string HostId,
    string TaskQueue,
    string FileName,
    int WorkflowCount,
    int ActivityCount);
