#!/bin/bash

dotnet test ./test/EdjCase.JsonRpc.Router.Tests -c Release

dotnet pack ./src/EdjCase.JsonRpc.Router -c Release  -o "./out/EdjCase.JsonRpc.Router"

dotnet pack ./src/EdjCase.JsonRpc.Router -c Release  -o "./out/EdjCase.JsonRpc.Router.Swagger"

dotnet pack ./src/EdjCase.JsonRpc.Client -c Release -o "out/EdjCase.JsonRpc.Client"