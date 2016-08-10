SET configuration=Release
SET out=C\Publish\EdjCase.JsonRpc.Core

call dotnet pack -c %configuration% -o "%out%/EdjCase.JsonRpc.Core"