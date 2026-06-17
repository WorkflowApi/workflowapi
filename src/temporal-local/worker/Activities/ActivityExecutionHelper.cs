using System.Globalization;
using Temporalio.Exceptions;

namespace B2B.RiskService.Activities;

/// <summary>
/// Shared helper for simulated activity delays and failure injection.
/// </summary>
internal static class ActivityExecutionHelper
{
    private const int DefaultGlobalMinDelayMs = 500;
    private const int DefaultGlobalMaxDelayMs = 2000;
    private const double DefaultFailureRate = 0.10d;

    /// <summary>
    /// Performs a simulated activity delay using global environment overrides.
    /// </summary>
    /// <param name="defaultMinDelayMs">Activity default minimum delay.</param>
    /// <param name="defaultMaxDelayMs">Activity default maximum delay.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static Task DelayAsync(
        int defaultMinDelayMs,
        int defaultMaxDelayMs,
        CancellationToken cancellationToken = default)
    {
        var configuredMin = ReadInt("ACTIVITY_MIN_DELAY_MS", DefaultGlobalMinDelayMs);
        var configuredMax = ReadInt("ACTIVITY_MAX_DELAY_MS", DefaultGlobalMaxDelayMs);
        if (configuredMin > configuredMax)
        {
            (configuredMin, configuredMax) = (configuredMax, configuredMin);
        }

        var effectiveMin = Math.Max(defaultMinDelayMs, configuredMin);
        var effectiveMax = Math.Min(defaultMaxDelayMs, configuredMax);
        if (effectiveMin > effectiveMax)
        {
            effectiveMin = defaultMinDelayMs;
            effectiveMax = defaultMaxDelayMs;
        }

        var delayMs = Random.Shared.Next(effectiveMin, effectiveMax + 1);
        return Task.Delay(delayMs, cancellationToken);
    }

    /// <summary>
    /// Throws an application failure based on configured failure probability.
    /// </summary>
    /// <param name="activityName">Name of failing activity.</param>
    public static void MaybeFail(string activityName)
    {
        var failureRate = ReadDouble("ACTIVITY_FAILURE_RATE", DefaultFailureRate);
        var clampedRate = Math.Clamp(failureRate, 0.0d, 1.0d);
        if (Random.Shared.NextDouble() <= clampedRate)
        {
            throw new ApplicationFailureException(
                $"{activityName} simulated transient failure.",
                nonRetryable: false);
        }
    }

    private static int ReadInt(string key, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(key), out var value) ? value : fallback;

    private static double ReadDouble(string key, double fallback)
    {
        var rawValue = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return fallback;
        }

        if (double.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var invariantValue))
        {
            return invariantValue;
        }

        return double.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var currentCultureValue)
            ? currentCultureValue
            : fallback;
    }
}
