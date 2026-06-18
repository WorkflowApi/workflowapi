namespace WorkflowApi.Temporal.Documents;

internal static class DisplayNameUtil
{
    public static string Humanise(string pascal)
    {
        if (string.IsNullOrEmpty(pascal)) return pascal;
        var stripped = pascal.EndsWith("Workflow", StringComparison.Ordinal)
            ? pascal[..^"Workflow".Length]
            : pascal;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < stripped.Length; i++)
        {
            var c = stripped[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(stripped[i - 1]))
                sb.Append(' ');
            sb.Append(i == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
        }
        if (pascal.EndsWith("Workflow", StringComparison.Ordinal)) sb.Append(" workflow");
        return sb.ToString();
    }
}
