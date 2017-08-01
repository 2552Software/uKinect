using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassLibrary1;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "kinectcolor", type: "fanout");

                var queueName = channel.QueueDeclare().QueueName;
                channel.QueueBind(queue: queueName, exchange: "kinectcolor", routingKey: "");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                };

                // If the consumer shutdowns reconnect to rabbit and begin reading from the queue again.
                consumer.Shutdown += (o, e) =>
                {
                    //ConnectToRabbitMq(); bugbug add in restart later
                    //ReadFromQueue(onDequeue, onError, exchangeName, queueName, routingKeyName);
                };
                channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
                while (true)
                {
                    Thread.Sleep(1);
                }
            }
        }
    }
}
