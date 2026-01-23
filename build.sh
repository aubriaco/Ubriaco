#!/bin/bash
DOTNET_CLI_TELEMETRY_OPTOUT=1
cd Ubriaco/
dotnet build --configuration Release
cd -
cd UbriacoUtil/
dotnet build --configuration Release
cd -
cd Toolkit/
dotnet build --configuration Release
cd -
mkdir -p output
cp Ubriaco/bin/Release/net8.0/Ubriaco.dll output/Ubriaco.dll
cp UbriacoUtil/bin/Release/net8.0/UbriacoUtil.dll output/UbriacoUtil.dll
mkdir -p output/toolkit
cp -r Toolkit/bin/Release/net8.0/* output/toolkit/

# net 10
mkdir -p output/net10
cp Ubriaco/bin/Release/net8.0/Ubriaco.dll output/net10/Ubriaco.dll
cp UbriacoUtil/bin/Release/net8.0/UbriacoUtil.dll output/net10/UbriacoUtil.dll
mkdir -p output/net10/toolkit
cp -r Toolkit/bin/Release/net8.0/* output/net10/toolkit/
