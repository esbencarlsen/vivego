# Orleans Based Message Broker

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](#)


Implement storage providers
Implement federation

GroupQueue
Client impl handle last ack

## Credentials
?

## Supported transports

REST short polling
REST long polling
REST SSE - Server Sent Events
WebSocket
SignalR
GRPC



##REST API
###Make a subscription

Parameters:
* string subscriptionId,
* string? glob,
* string? regex

Example. Make a subscription named "a" with GLOB a*, which will receive all events published with topics starting with a.

POST http://localhost:5000/Subscribe/a?glob=a*

###

POST http://localhost:5000/Subscribe/b?regex=b.*


###
POST http://localhost:5000/UnSubscribe/abed


###
GET http://localhost:5000/a?fromId=0
Content-Type: application/json

### Publish something
POST http://localhost:5000/Publish/a
Content-Type: application/json

{
"id": 9199,
"value": "content"
}
