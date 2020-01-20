SET configuration=Release
SET out=C:/Publish

call dotnet test ./test/EdjCase.JsonRpc.Router.Tests -c %configuration%

call dotnet pack ./src/EdjCase.JsonRpc.Router -c %configuration% -o "%out%/EdjCase.JsonRpc.Router"

call dotnet test ./test/EdjCase.JsonRpc.Client.Tests -c %configuration%

call dotnet pack ./src/EdjCase.JsonRpc.Client -c %configuration% -o "%out%/EdjCase.JsonRpc.Client"
