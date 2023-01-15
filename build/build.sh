#!/bin/sh
set -xe

NAME=lwnsimulator
HOST=172.20.47.214
rm -rf ./$NAME
cd ..
make build-x64
cd -
mkdir ./$NAME
mv ../bin/$NAME_x64 ./$NAME
exit 0

#dotnet publish ../$NAME.csproj -c Debug -r osx-x64 --no-self-contained -p:PublishSingleFile=true -o ./$NAME

ssh root@$HOST "systemctl daemon-reload; systemctl stop $NAME.service"
rsync -rv ./$NAME root@$HOST:/opt
rsync -v ./$NAME.service root@$HOST:/lib/systemd/system/$NAME.service
ssh root@$HOST "systemctl daemon-reload; systemctl enable $NAME.service; systemctl start $NAME.service; systemctl status $NAME.service"
