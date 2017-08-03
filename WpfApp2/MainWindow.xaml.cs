using System.Windows;
using s552ClassLibrary1;
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
        static KinectDataObject kinectData = new KinectDataObject();

        private static void EventingBasicKinectBodyReceived(object sender, BasicDeliverEventArgs e)
        {
            GenericKinectBody body = GenericKinectBody.fromBytes(e.Body);
            channelForEventing.BasicAck(e.DeliveryTag, false);
        }
        private static void EventingBasicIRReceived(object sender, BasicDeliverEventArgs e)
        {
            kinectData.setIRImage(e.Body);
            channelForEventing.BasicAck(e.DeliveryTag, false);
        }
        private static void EventingBasicColorReceived(object sender, BasicDeliverEventArgs e)
        {
            kinectData.setColorImage(e.Body);
            channelForEventing.BasicAck(e.DeliveryTag, false);
        }
        private static void EventingBasicDepthReceived(object sender, BasicDeliverEventArgs e)
        {
            kinectData.setDepthImage(e.Body);
            channelForEventing.BasicAck(e.DeliveryTag, false);
        }
        private static void EventingBasicBodyIndexReceived(object sender, BasicDeliverEventArgs e)
        {
            kinectData.setBodyIndexImage(e.Body);
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
            eventingBasicConsumer.Received += EventingBasicColorReceived;
            channelForEventing.BasicConsume(queue: queueName, autoAck: false, consumer: eventingBasicConsumer);

            channelForEventing.ExchangeDeclare(exchange: "kinectdepth", type: "fanout");
            queueName = channelForEventing.QueueDeclare().QueueName;
            channelForEventing.QueueBind(queue: queueName, exchange: "kinectdepth", routingKey: "");
            EventingBasicConsumer eventingBasicConsumerDepth = new EventingBasicConsumer(channelForEventing);
            eventingBasicConsumerDepth.Received += EventingBasicDepthReceived;
            channelForEventing.BasicConsume(queue: queueName, autoAck: false, consumer: eventingBasicConsumerDepth);

            channelForEventing.ExchangeDeclare(exchange: "kinectir", type: "fanout");
            queueName = channelForEventing.QueueDeclare().QueueName;
            channelForEventing.QueueBind(queue: queueName, exchange: "kinectir", routingKey: "");
            EventingBasicConsumer eventingBasicConsumerIR = new EventingBasicConsumer(channelForEventing);
            eventingBasicConsumerIR.Received += EventingBasicIRReceived;
            channelForEventing.BasicConsume(queue: queueName, autoAck: false, consumer: eventingBasicConsumerIR);

            channelForEventing.ExchangeDeclare(exchange: "kinectbodyindex", type: "fanout");
            queueName = channelForEventing.QueueDeclare().QueueName;
            channelForEventing.QueueBind(queue: queueName, exchange: "kinectbodyindex", routingKey: "");
            EventingBasicConsumer eventingBasicConsumerkinectbodyIndex = new EventingBasicConsumer(channelForEventing);
            eventingBasicConsumerkinectbodyIndex.Received += EventingBasicBodyIndexReceived;
            channelForEventing.BasicConsume(queue: queueName, autoAck: false, consumer: eventingBasicConsumerkinectbodyIndex);

            channelForEventing.ExchangeDeclare(exchange: "kinectbodyindex", type: "fanout");
            queueName = channelForEventing.QueueDeclare().QueueName;
            channelForEventing.QueueBind(queue: queueName, exchange: "kinectbodyindex", routingKey: "");
            EventingBasicConsumer eventingBasicConsumerkinectbody = new EventingBasicConsumer(channelForEventing);
            eventingBasicConsumerkinectbody.Received += EventingBasicKinectBodyReceived;
            channelForEventing.BasicConsume(queue: queueName, autoAck: false, consumer: eventingBasicConsumerkinectbody);
            

        }
        public MainWindow()
        {
            InitializeComponent();
            theImage.DataContext = kinectData;
            theDepth.DataContext = kinectData;
            theIR.DataContext = kinectData;
            theBodyIndex.DataContext = kinectData;
            ReceiveMessagesWithEvents();
        }
    }
}
