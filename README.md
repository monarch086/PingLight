# PingLight
App for pinging some host and posting info to Telegram channel. Can be useful for tracking host or electricity availability.

## Structure
 - PingLight.App - console app, which performs pings and posting
 - PingLight.Core - assembly with core logic of pinging and posting
 - PingLight.Lambda - shell for making posts from AWS Lambda (not finished)

## Building and publishing
To build app just select and build PingLight.App project.
For publishing project two profiles available:
 - local folder profile
 - docker image profile - Dockerfile included

## Running
To start app open command terminal and run one of these commands:
 - to run with TEST configs:
```
PingLight.App
```
 - to run with PROD configs:
```
PingLight.App prod
```

## Author
Any ideas of improvements please send to sbarsuk88@gmail.com