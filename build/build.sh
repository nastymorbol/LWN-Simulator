#!/bin/sh
set -xe

NAME=lwnsimulator
# First Setup
HOST=172.20.47.210
# Setup @Home
HOST=192.168.123.244

# Sim02
HOST=172.20.47.230

rm -rf ./$NAME
cd ..
make build-x64
cd -
mkdir ./$NAME
mv "../bin/$NAME"_x64 ./$NAME
cp ../config.json config.json

scp ./$NAME.service root@$HOST:/lib/systemd/system/$NAME.service
ssh root@$HOST "systemctl daemon-reload; systemctl stop $NAME.service"
ssh root@$HOST "mkdir -p /opt/$NAME"
#rsync -rv ./$NAME root@$HOST:/opt
#rsync -rv ./config.json root@$HOST:/opt/$NAME

scp ./$NAME/${NAME}_x64 root@$HOST:/opt/$NAME/${NAME}_x64
scp ./config.json root@$HOST:/opt/$NAME/config.json


ssh root@$HOST "systemctl daemon-reload; systemctl enable $NAME.service; systemctl start $NAME.service; systemctl status $NAME.service"
