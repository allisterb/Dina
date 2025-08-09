@echo off
echo Building Dina...
dotnet restore src\Dina.sln
dotnet build src\Dina.Console/Dina.Console.csproj /p:Configuration=Debug