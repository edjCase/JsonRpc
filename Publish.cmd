SET configuration=Release
SET out=C:/Publish

call dotnet pack ./src/EdjCase.JsonRpc.Core -c %configuration% -o "%out%/EdjCase.JsonRpc.Core"

call dotnet pack ./src/EdjCase.JsonRpc.Router -c %configuration% -o "%out%/EdjCase.JsonRpc.Router"

call dotnet pack ./src/EdjCase.JsonRpc.Client -c %configuration% -o "%out%/EdjCase.JsonRpc.Client"

call dotnet pack ./src/EdjCase.JsonRpc.Router.WebSockets -c %configuration% -o "%out%/EdjCase.JsonRpc.Router.WebSockets"
