REM Expects .NET Core SDK listed in global.json to be installed (other versions might work as well, but it was tested with this one)
dotnet pack -c Release .\src\Compilers\Core\Portable\CodeAnalysis.csproj

dotnet nuget push .\Binaries\Release\Aquila.Microsoft.CodeAnalysis.0.0.5.nupkg -k 4267837e-9316-452e-9c3c-e26055f4cdba --source https://www.myget.org/F/aquila/api/v2/package