#!/bin/sh
set -xe

NAME=lwnsim
HOST=172.20.47.214
rm -rf ./$NAME
dotnet publish ../$NAME.csproj -c Release -r linux-x64 --no-self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:DebugType=none -o ./$NAME
#rm ./$NAME/*.xml

#dotnet publish ../$NAME.csproj -c Debug -r osx-x64 --no-self-contained -p:PublishSingleFile=true -o ./$NAME

rsync -v ./$NAME.service root@$HOST:/lib/systemd/system/$NAME.service
ssh root@$HOST "systemctl daemon-reload; systemctl stop $NAME.service"
rsync -rv ./$NAME root@$HOST:/opt
ssh root@$HOST "systemctl daemon-reload; systemctl enable $NAME.service; systemctl start $NAME.service; systemctl status $NAME.service"
