if ((Test-Path analyzers\dotnet\cs) -eq $false)
{
    MKDIR analyzers\dotnet\cs
}
COPY ..\src\FUR10N.NullContracts.PCL\bin\Release\FUR10N.NullContracts.PCL.dll analyzers\dotnet\cs

.\nuget pack