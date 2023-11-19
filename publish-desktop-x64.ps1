dotnet restore
dotnet build .\Rdr.sln -c Release --no-restore --nologo
dotnet publish .\Rdr\Rdr.csproj -c Release -r win-x64 /p:PublishSingleFile=true --no-self-contained --no-restore
