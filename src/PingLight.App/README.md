# PingLight

App for pinging some host and posting info to Telegram channel. Can be useful for tracking host or electricity availability.

## Structure

- PingLight.App - console app, which performs pings and posting
- PingLight.Core - assembly with core logic of pinging and posting

## Building and publishing

To build app just select and build PingLight.App project.
For publishing project two profiles available:

- local folder profile
- docker image profile - Dockerfile included

## Docker Image

- build image:

```sh
docker build -t pingapp -f PingLight.App\Dockerfile .
```

- run container:

```sh
docker run -it pingapp
```

- stop container:

```sh
docker stop pingapp
```

- remove container:

```sh
docker rm pingapp
```

- remove image:

```sh
docker rmi pingapp:latest
```

## Running

To start app open command terminal and run one of these commands:

- to run with TEST configs:

```sh
PingLight.App
```

- to run with PROD configs:

```sh
PingLight.App prod
```

## Author

Any ideas of improvements please send to <sbarsuk88@gmail.com>
