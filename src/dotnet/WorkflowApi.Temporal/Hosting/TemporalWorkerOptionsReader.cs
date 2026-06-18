using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;

namespace WorkflowApi.Temporal.Hosting;

/// <summary>
/// Represents a Temporal worker registered via AddHostedTemporalWorker.
/// </summary>
public sealed record RegisteredWorker(
    string TaskQueue,
    string? Namespace,
    IReadOnlyList<Type> WorkflowTypes,
    IReadOnlyList<Type> ActivityTypes);

/// <summary>
/// Reads Temporal worker registrations from a configured <see cref="IServiceCollection"/>.
/// </summary>
public sealed class TemporalWorkerOptionsReader
{
    /// <summary>
    /// Inspects <paramref name="services"/> and returns every worker registered via
    /// <c>AddHostedTemporalWorker</c>, sorted by task queue (ordinal).
    /// </summary>
    public IReadOnlyList<RegisteredWorker> Read(IServiceCollection services)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        // Discover distinct task queue (options) names.
        //
        // AddHostedTemporalWorker(taskQueue) calls ConfigureOptions().Configure(action) which
        // registers ConfigureNamedOptions<TemporalWorkerServiceOptions> as an
        // IConfigureOptions<TemporalWorkerServiceOptions> ImplementationInstance whose .Name
        // equals the task queue string (for workers without deployment versioning).
        // This mirrors the duplicate-detection logic in the SDK itself.
        var taskQueueNames = services
            .Where(sd => sd.ServiceType == typeof(IConfigureOptions<TemporalWorkerServiceOptions>))
            .Select(sd =>
            {
                // Accessing ImplementationInstance on a keyed service throws in .NET 8+.
                try { return sd.ImplementationInstance as ConfigureNamedOptions<TemporalWorkerServiceOptions>; }
                catch (InvalidOperationException) { return null; }
            })
            .Where(cno => cno is not null && !string.IsNullOrEmpty(cno.Name) && cno.Name != Options.DefaultName)
            .Select(cno => cno!.Name!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (taskQueueNames.Count == 0) return Array.Empty<RegisteredWorker>();

        ServiceProvider? provider = null;
        try
        {
            provider = services.BuildServiceProvider();

            // Namespace comes from TemporalClientConnectOptions, present only when
            // AddTemporalClient was called. GetService returns null if not configured.
            string? ns = provider.GetService<IOptions<TemporalClientConnectOptions>>()?.Value.Namespace;

            var monitor = provider.GetRequiredService<IOptionsMonitor<TemporalWorkerServiceOptions>>();

            var workers = new List<RegisteredWorker>(taskQueueNames.Count);
            foreach (var taskQueue in taskQueueNames)
            {
                // Resolving the named options triggers both Configure and PostConfigure actions,
                // including the PostConfigure<IServiceProvider> that materialises activity
                // definitions from the registered activity types.
                var opts = monitor.Get(taskQueue);

                // WorkflowDefinition.Type is the workflow class.
                var workflowTypes = opts.Workflows
                    .Select(wf => wf.Type)
                    .Distinct()
                    .OrderBy(t => t.FullName, StringComparer.Ordinal)
                    .ToList();

                // ActivityDefinition.MethodInfo.DeclaringType is the activity class.
                var activityTypes = opts.Activities
                    .Select(act => act.MethodInfo?.DeclaringType)
                    .Where(t => t is not null)
                    .Distinct()
                    .OrderBy(t => t!.FullName, StringComparer.Ordinal)
                    .Select(t => t!)
                    .ToList();

                workers.Add(new RegisteredWorker(taskQueue, ns, workflowTypes, activityTypes));
            }

            return workers.OrderBy(w => w.TaskQueue, StringComparer.Ordinal).ToList();
        }
        finally
        {
            provider?.Dispose();
        }
    }
}
