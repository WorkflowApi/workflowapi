using System;
using System.Reflection;
using System.Linq;

var dll = Assembly.LoadFrom("/Users/Robert.Harris/.nuget/packages/temporalio.extensions.hosting/1.15.0/lib/net6.0/Temporalio.Extensions.Hosting.dll");
foreach (var t in dll.GetExportedTypes().OrderBy(t => t.FullName))
    Console.WriteLine(t.FullName);
