﻿using System.Windows;
using ClassLibrary1;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static IModel channelForEventing;
        static KinectDataObject data = new KinectDataObject();

        private static void EventingBasicConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            data.setImage(e.Body);
            channelForEventing.BasicAck(e.DeliveryTag, false);
        }

        private static void ReceiveMessagesWithEvents()
        {
            ConnectionFactory connectionFactory = new ConnectionFactory();

            //todo connectionFactory.Port = 5672;
            connectionFactory.HostName = "localhost";
            //todo connectionFactory.UserName = "accountant";
            //todo connectionFactory.Password = "accountant";
            //todo connectionFactory.VirtualHost = "accounting";
            IConnection connection = connectionFactory.CreateConnection();
            channelForEventing = connection.CreateModel();
            channelForEventing.BasicQos(0, 1, false);
            channelForEventing.ExchangeDeclare(exchange: "kinectcolor", type: "fanout");
            var queueName = channelForEventing.QueueDeclare().QueueName;
            channelForEventing.QueueBind(queue: queueName, exchange: "kinectcolor", routingKey: "");
            EventingBasicConsumer eventingBasicConsumer = new EventingBasicConsumer(channelForEventing);
            eventingBasicConsumer.Received += EventingBasicConsumer_Received;
            channelForEventing.BasicConsume(queue: queueName, autoAck: false, consumer: eventingBasicConsumer);
        }
        public MainWindow()
        {
            InitializeComponent();

            theImage.DataContext = data;
            ReceiveMessagesWithEvents();
        }
    }
}
