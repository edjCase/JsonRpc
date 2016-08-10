SET configuration=Release
SET out=C;\Publish\EdjCase.JsonRpc.Client

call dotnet pack -c %configuration% -o "%out%/EdjCase.JsonRpc.Client"