SET configuration=Release
SET out=C:\Publish

call dotnet pack -c %configuration% -o "%out%/EdjCase.JsonRpc.Router.Swagger"