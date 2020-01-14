#!/bin/sh
sudo systemctl stop mywhitelist
sudo systemctl disable mywhitelist
workdir=$(cd $(dirname $0); pwd)
cd $workdir/src
sudo rm -rf ../out
sudo dotnet publish -r linux-x64 -c Release -o ../out --no-self-contained
sudo cp $workdir/mywhitelist.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable mywhitelist
sudo systemctl start mywhitelist
