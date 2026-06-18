#r "/Users/Robert.Harris/.nuget/packages/temporalio/1.15.0/lib/netstandard2.0/Temporalio.dll"
using System.Reflection;
var asm = Assembly.LoadFrom("/Users/Robert.Harris/.nuget/packages/temporalio/1.15.0/lib/netstandard2.0/Temporalio.dll");
var types = new[] { "Temporalio.Workflows.WorkflowAttribute", "Temporalio.Workflows.WorkflowSignalAttribute", "Temporalio.Workflows.WorkflowQueryAttribute", "Temporalio.Workflows.WorkflowUpdateAttribute", "Temporalio.Workflows.WorkflowRunAttribute", "Temporalio.Activities.ActivityAttribute" };
foreach (var tn in types) {
    var t = asm.GetType(tn);
    if (t == null) { Console.WriteLine($"NOT FOUND: {tn}"); continue; }
    var props = t.GetProperties().Select(p => p.Name);
    Console.WriteLine($"{tn}: {string.Join(", ", props)}");
}
