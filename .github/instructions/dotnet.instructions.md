# .NET Instructions

Applies to .NET source and tests.

- Use .NET 10 LTS.
- Keep packages separated: Abstractions, AspNetCore, Temporal, Reference UI host, Catalog, CLI/MSBuild.
- Do not make core packages depend on Temporal.
- Use Scrutor only in scanning/runtime generation packages, not in Abstractions.
- Add unit tests for public APIs and generated document snapshots.
