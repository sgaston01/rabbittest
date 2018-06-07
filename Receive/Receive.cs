using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;

public class RpcClient
{
    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly string replyQueueName;
    private readonly EventingBasicConsumer consumer;
    private readonly BlockingCollection<string> respQueue = new BlockingCollection<string>();
    private readonly IBasicProperties props;

    public RpcClient()
    {
        //var factory = new ConnectionFactory() { HostName = "localhost" };
        var factory = new ConnectionFactory() { HostName = "192.168.0.25", Port = 5672, UserName = "test", Password = "test" };

        connection = factory.CreateConnection();
        channel = connection.CreateModel();

        var args2= new Dictionary<string, object>();
        args2.Add("x-message-ttl", 1000);

        replyQueueName = channel.QueueDeclare("myqueue", false, false, false, args2).QueueName;
        consumer = new EventingBasicConsumer(channel);
       
      /*  props = channel.CreateBasicProperties();
        var correlationId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.ReplyTo = replyQueueName;

        props.ContentType = "text/plain";
        props.DeliveryMode = 2;
        props.Expiration = "2000";
        */
        
        SimpleRpcClient client = new SimpleRpcClient(channel, "", "", "rpc_queue");
        client.TimeoutMilliseconds = 5000; // 5 sec. defaults to infinity
        
       //var message1 = Encoding.UTF8.GetBytes("1");

        //client.TimedOut += RpcTimedOutHandler;
        //client.Disconnected += RpcDisconnectedHandler;
             

        //byte[] replyMessageBytes1 = client.Call(message1);
        //Console.WriteLine(" [.] Got '{0}'", Encoding.UTF8.GetString(replyMessageBytes1));

        for (int i = 0; i < 100; i++)
        {
            var message = Encoding.UTF8.GetBytes(i.ToString());
            byte[] replyMessageBytes = client.Call(message);

            Console.WriteLine(" [.] Got '{0}'", Encoding.UTF8.GetString(replyMessageBytes));
        }

        /*consumer.Received += (model, ea) =>
        {
            var body = ea.Body;
            var response = Encoding.UTF8.GetString(body);
            if (ea.BasicProperties.CorrelationId == correlationId)
            {
                respQueue.Add(response);
            }
        };*/
    }

    public string Call(string message)
    {

        var messageBytes = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(
            exchange: "",
            routingKey: "rpc_queue",
            basicProperties: props,
            body: messageBytes);


        channel.BasicConsume(
            consumer: consumer,
            queue: replyQueueName,
            autoAck: true);

        return respQueue.Take(); ;
    }

    public void Close()
    {
        connection.Close();
    }
}

public class Rpc
{
    public static void Main()
    {
        var rpcClient = new RpcClient();

        Console.WriteLine(" [x] Requesting fib(30)");

        for (int i = 0; i < 100; i++)
        {
            //var response = rpcClient.Call(i.ToString());
            //Console.WriteLine(" [.] Got '{0}'", response);
        }
        //var response1 = rpcClient.Call("3");
        //Console.WriteLine(" [.] Got '{0}'", response1);

        rpcClient.Close();
    }
}