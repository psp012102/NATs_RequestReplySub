using NATS.Client;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace RequestReplySub
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var subscription = nameof(Order);

            var connection = GetConnection();

            var sAsync = connection.SubscribeAsync(subscription);

            sAsync.MessageHandler += (sender, args) =>
            {
                var receivedOrder = JsonSerializer.Deserialize<Order>(args.Message.Data);

                receivedOrder.ProcessOrder();

                Console.ForegroundColor = receivedOrder.OrderStatus == OrderStatus.approved
                    ? ConsoleColor.Green : ConsoleColor.Red;

                Console.WriteLine($"Received Order / Id: {receivedOrder.Id}"
                    + $"/ Amount: {receivedOrder.Amount:c} / Status: Id: {receivedOrder.OrderStatus}");

                var reply = args.Message.Reply;
                var replyMessage = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(receivedOrder));

                connection.Publish(reply, replyMessage);
            };

            sAsync.Start();

            Console.ReadLine();

            sAsync.Unsubscribe();
            connection.Drain();
            connection.Close();
        }

        public static IConnection GetConnection()
        {
            var cf = new ConnectionFactory();
            var c = cf.CreateConnection();

            return c;
        }

        public enum OrderStatus { pending, approved, canceled }

        public class Order
        {
            public ulong Id { get; set; }
            public ulong Amount { get; set; }
            public OrderStatus OrderStatus { get; set; }

            public void ProcessOrder()
            {
                this.OrderStatus = this.Amount > 10000 ?
                    OrderStatus.approved : OrderStatus.canceled;
            }
        }
    }
}