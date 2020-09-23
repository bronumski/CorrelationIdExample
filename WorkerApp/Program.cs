using System;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace WorkerApp
{
    class Program
    {
        static void Main(string[] args)
        { 
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("App", "Worker App")
                .WriteTo.Seq("http://localhost:5341")
                .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
                .CreateLogger();
            
            var factory = new ConnectionFactory { HostName = "localhost" };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            
            channel.QueueDeclare("hello", true, false, false, null);

            var consumer = new EventingBasicConsumer(channel);
            
            var count = 1;
            
            consumer.Received += (model, ea) =>
            {
                IDisposable logContextProperty = null;
                if (ea.BasicProperties.Headers?.ContainsKey("CorrelationId") == true)
                {
                    var correlationId = Encoding.UTF8.GetString(ea.BasicProperties.Headers["CorrelationId"] as byte[]);
                    logContextProperty = LogContext.PushProperty("CorrelationId", correlationId);
                }
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                try
                {
                    Log.Information("Received {count} {message}", count, message);
                    
                    if (count++ % 5 == 0) throw new Exception("Some exception occured");
                    
                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception e)
                {
                    Log.Error("Unexpected error:", e);
                }
                logContextProperty?.Dispose();
            };
            channel.BasicConsume(queue: "hello",
                                 autoAck: false,
                                 consumer: consumer);

            Console.WriteLine(" [*] Waiting for logs. Any key to exit");

            Console.ReadLine();
        }
    }
}