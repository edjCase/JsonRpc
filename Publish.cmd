SET configuration=Release
SET out=C:/Publish

cd src/JsonRpc.Core

dnu pack --configuration %configuration% --out "%out%/JsonRpc.Core"
