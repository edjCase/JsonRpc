SET configuration=Release
SET out=C:/Publish

call dotnet test ./test/EdjCase.JsonRpc.Router.Tests -c Release

call dotnet pack ./src/EdjCase.JsonRpc.Router -c %configuration% -o "%out%/EdjCase.JsonRpc.Router"

call dotnet pack ./src/EdjCase.JsonRpc.Client -c %configuration% -o "%out%/EdjCase.JsonRpc.Client"
