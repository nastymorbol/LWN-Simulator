#!/bin/sh
set -xe
rm -rf ./rftasmd
dotnet publish ../MqttFhem.Main.csproj -c Release -r linux-x64 --no-self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:DebugType=none -o ./rftasmd

#dotnet publish ../MqttFhem.Main.csproj -c Debug -r osx-x64 --no-self-contained -p:PublishSingleFile=true -o ./rftasmd
exit 0;

ssh root@192.168.123.243 "systemctl daemon-reload; systemctl stop rftasmd.service"
rsync -rv ./rftasmd root@192.168.123.243:/opt
rsync -v ./rftasmd.service root@192.168.123.243:/lib/systemd/system/rftasmd.service
ssh root@192.168.123.243 "systemctl daemon-reload; systemctl start rftasmd.service; systemctl status rftasmd.service"
