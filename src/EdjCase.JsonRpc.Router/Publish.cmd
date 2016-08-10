SET configuration=Release
SET out=C;\Publish\EdjCase.JsonRpc.Router

call dotnet pack -c %configuration% -o "%out%/EdjCase.JsonRpc.Router"