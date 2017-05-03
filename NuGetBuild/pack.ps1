if ((Test-Path analyzers\dotnet\cs) -eq $false)
{
    MKDIR analyzers\dotnet\cs
}
COPY ..\src\FUR10N.NullContracts\bin\Release\netstandard1.3\FUR10N.NullContracts.dll analyzers\dotnet\cs

.\nuget pack