#!/bin/sh
set -xe

NAME=lwnsimulator
HOST=172.20.47.210
rm -rf ./$NAME
cd ..
make build-x64
cd -
mkdir ./$NAME
mv "../bin/$NAME"_x64 ./$NAME

rsync -v ./$NAME.service root@$HOST:/lib/systemd/system/$NAME.service
ssh root@$HOST "systemctl daemon-reload; systemctl stop $NAME.service"
rsync -rv ./$NAME root@$HOST:/opt
ssh root@$HOST "systemctl daemon-reload; systemctl enable $NAME.service; systemctl start $NAME.service; systemctl status $NAME.service"
