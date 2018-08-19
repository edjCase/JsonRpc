#!/bin/bash

dotnet test ./test/EdjCase.JsonRpc.Router.Tests -c Release

dotnet pack ./src/EdjCase.JsonRpc.Core -c Release  -o "./out/EdjCase.JsonRpc.Core"

dotnet pack ./src/EdjCase.JsonRpc.Router -c Release  -o "./out/EdjCase.JsonRpc.Router"

dotnet pack ./src/EdjCase.JsonRpc.Router.WebSockets -c Release -o "out/EdjCase.JsonRpc.Router.WebSockets"

dotnet pack ./src/EdjCase.JsonRpc.Client -c Release -o "out/EdjCase.JsonRpc.Client"