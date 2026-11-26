# Build and test instructions

Run from the repository root with .NET 8 SDK:

```bash
dotnet restore BrightMinds.Api/BrightMinds.Api.csproj
dotnet build BrightMinds.Api/BrightMinds.Api.csproj --no-restore
dotnet test BrightMinds.Tests/BrightMinds.Tests.csproj
```

Database migrations must be validated against a disposable database before deployment. No production database should be used for migration verification.
