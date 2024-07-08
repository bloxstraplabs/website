#!/bin/sh

PROJECTDIR=../BloxstrapWebsite
PUBLISHDIR=bin/Release/net8.0/linux-x64/publish

MACHINE=pizza-server.internal.pizzaboxer.xyz
SERVICE=bloxstrap.pizzaboxer.xyz.service

DOMAIN=pizzaboxer.xyz
SUBDOMAIN=bloxstrap

rm -r $PROJECTDIR/$PUBLISHDIR
dotnet publish $PROJECTDIR -c Release -r linux-x64 --no-self-contained
scp -r $PROJECTDIR/$PUBLISHDIR $MACHINE:/var/www/$DOMAIN/$SUBDOMAIN.staging
echo "sudo password required for server"
ssh -t $MACHINE "cd /var/www/$DOMAIN; sudo systemctl stop $SERVICE; cp -p $SUBDOMAIN/appsettings.json $SUBDOMAIN.staging/appsettings.json; rm -r $SUBDOMAIN; mv $SUBDOMAIN.staging $SUBDOMAIN; sudo systemctl start $SERVICE; systemctl status $SERVICE;"