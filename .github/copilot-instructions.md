# Copilot instructions

## Repository context

- Build and test from the repository root.
- Build with `dotnet build .\ProjNet4GeoAPI.sln --tl:off -v minimal`.
- Test with `dotnet test --project .\test\ProjNet.Tests\ProjNET.Tests.csproj`.
- The library ships `netstandard2.0`, `netstandard2.1`, and `net8.0`.
- Public API changes must update `src/ProjNet/PublicAPI.Shipped.txt` intentionally.
- In touched C# code, prefer `is null` / `is not null` checks and avoid broad warning suppressions.

