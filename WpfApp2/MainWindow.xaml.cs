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
using System.IO;
using System.ComponentModel;
using System.Diagnostics;

namespace WpfApp2
{
    class TestObject : INotifyPropertyChanged
    {
        public WriteableBitmap Bitmap = null;
        ImageSource src = null;
        public void Refresh(string name)
        {
            OnPropertyChanged(name);
        }
        public ImageSource ImageSource
        {
            get
            {
                return src;
            }
        }
        public void setImage(byte[] buffer)
        {
            src = ConvertBytesToImage(buffer);
            Refresh("ImageSource");
        }
        private ImageSource ConvertBytesToImage(byte[] buffer)
        {
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                BitmapDecoder decoder = BitmapDecoder.Create(stream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad); // enables closing the stream immediately
                return decoder.Frames[0];
            }
        }
        private string _name;

        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (_name == value) return;

                _name = value;
                OnPropertyChanged("Name");
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static IModel channelForEventing;
        static TestObject t = new TestObject();

        private static void EventingBasicConsumer_Received(object sender, BasicDeliverEventArgs e)
        {

            IBasicProperties basicProperties = e.BasicProperties;
            /* tie to xaml some day todo
            Console.WriteLine("Message received by the event based consumer. Check the debug window for details.");
            Debug.WriteLine(string.Concat("Message received from the exchange ", e.Exchange));
            Debug.WriteLine(string.Concat("Content type: ", basicProperties.ContentType));
            Debug.WriteLine(string.Concat("Consumer tag: ", e.ConsumerTag));
            Debug.WriteLine(string.Concat("Delivery tag: ", e.DeliveryTag));
            Debug.WriteLine(string.Concat("Message: ", Encoding.UTF8.GetString(e.Body)));
            */
            var body = e.Body;
            t.setImage(e.Body);
            channelForEventing.BasicAck(e.DeliveryTag, false);
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            t.Name = "foo";
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

            theLabel.DataContext = t; // This is the whole bind operation
            theImage.DataContext = t;
            ReceiveMessagesWithEvents();
            return;
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "kinectcolor", type: "fanout");

                var queueName = channel.QueueDeclare().QueueName;
                channel.QueueBind(queue: queueName, exchange: "kinectcolor", routingKey: "");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += EventingBasicConsumer_Received;

                // If the consumer shutdowns reconnect to rabbit and begin reading from the queue again.
                consumer.Shutdown += (o, e) =>
                {
                    //ConnectToRabbitMq(); bugbug add in restart later
                    //ReadFromQueue(onDequeue, onError, exchangeName, queueName, routingKeyName);
                };
                channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
            }
        }
    }
}
