# CorrelationIdExample
Demonstrate correlation id being passed from chained services using web client and message queue

- Spin up an instance of Seq to capture logging

```
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
```

- Spin up an instance of RabbitMQ

```
docker run -d --hostname my-rabbit --name some-rabbit -p 8080:15672 -p 5672:5672 rabbitmq:3-management
```

- Run the three applications (in different terminals)

```
dotnet run -project WebAppA
dotnet run -project WebAppB
dotnet run -project Worker
```

- Hit the WebApp endpoint

```
curl --location --request GET 'http://localhost:5000/WeatherForecast' \
--header 'User: LoggedInUser'
```

https://datalust.co/seq
https://hub.docker.com/r/datalust/seq/
https://www.rabbitmq.com/
https://hub.docker.com/_/rabbitmq
https://github.com/stevejgordon/CorrelationId
