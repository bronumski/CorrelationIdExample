using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CorrelationId.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace WebAppB.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly CorrelationContextAccessor _correlationContextAccessor;

        private static readonly string[] Summaries = {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(
            ILogger<WeatherForecastController> logger,
            ConnectionFactory connectionFactory,
            CorrelationContextAccessor correlationContextAccessor)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _correlationContextAccessor = correlationContextAccessor;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            SendEvent();
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                })
                .ToArray();
        }

        private void SendEvent()
        {
            using var connection = _connectionFactory.CreateConnection();
            using var channel = connection.CreateModel();
            
            channel.QueueDeclare(queue: "hello",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            string message = "Hello World!";
            var body = Encoding.UTF8.GetBytes(message);
            IBasicProperties basicProperties = connection.CreateModel().CreateBasicProperties();

            var correlationContext = _correlationContextAccessor.CorrelationContext;
            if (correlationContext != null)
            {
                basicProperties.Headers = new Dictionary<string, object>
                {
                    {"CorrelationId", correlationContext?.CorrelationId}
                };
            }

            channel.BasicPublish(exchange: "",
                routingKey: "hello",
                basicProperties: basicProperties,
                body: body);
            _logger.LogInformation($"Sent {message}", message);
        }
    }
}