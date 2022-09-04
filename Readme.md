Ensure having a running Redis service on localhost:6379

you can create one via docker with:
docker run -p 6379:6379 --name redis -e ALLOW_EMPTY_PASSWORD=yes bitnami/redis:latest