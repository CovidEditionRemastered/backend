& docker build adminer --tag "kirinnee/soapy-admin:$($args[0])"
& docker build traefik --tag "kirinnee/soapy-traefik:$($args[0])"
& docker build middleware --tag "kirinnee/soapy-middleware:$($args[0])"