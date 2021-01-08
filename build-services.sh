#!/usr/bin/env bash

docker build adminer --tag=kirinnee/soapy-admin:$1
docker build traefik --tag=kirinnee/soapy-traefik:$1