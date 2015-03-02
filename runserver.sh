#!/bin/bash
# http://qiita.com/ka2n/items/9659cb2b083ab7dcd844

if [ -z $1 ]; then
    exit 1
fi

APP_IMG_NAME=azyobuzin/nugetcalcweb

CURRENT_CONTAINERS=`docker ps | grep $APP_IMG_NAME | awk '{print $1}'`
echo "[Running containers]"
echo "$CURRENT_CONTAINERS"

NEW_ID=`docker run -dtP $1`
NEW_ADDR=`docker port $NEW_ID 5000`

echo "[New container info]"
echo "CONTAINER ID: ${NEW_ID}"
echo "ADDR: ${NEW_ADDR}"

cat <<EOF > /etc/nginx/sites-available/nugetcalcweb
server {
    listen 80;
    server_name nugetcalc.azyobuzi.net;

    location / {
        proxy_pass http://$NEW_ADDR/;
    }
}
EOF

service nginx reload

if [ -n "$CURRENT_CONTAINERS" ]; then
    docker stop $CURRENT_CONTAINERS
fi
