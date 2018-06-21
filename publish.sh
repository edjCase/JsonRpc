#!/bin/bash

dotnet pack ./src/EdjCase.JsonRpc.Core -c Release  -o "./out/EdjCase.JsonRpc.Core"

dotnet pack ./src/EdjCase.JsonRpc.Router -c Release  -o "./out/EdjCase.JsonRpc.Router"

dotnet pack ./src/EdjCase.JsonRpc.Client -c Release -o "out/EdjCase.JsonRpc.Client"