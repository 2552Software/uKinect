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

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private WriteableBitmap colorBitmap = null;
        public event PropertyChangedEventHandler PropertyChanged;
        ImageSource image;
        public ImageSource ImageSource
        {
            get
            {
                return this.image;
            }
            set
            {
                if (this.image != value)
                {
                    this.image = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("ImageSource"));
                    }
                }
            }
        }

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
                    Stream imageStreamSource = new MemoryStream(ea.Body);
                    JpegBitmapDecoder decoder = new JpegBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    BitmapSource bitmapSource = decoder.Frames[0];
                    colorBitmap = new WriteableBitmap(bitmapSource);
                    colorBitmap.Lock();
                    colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                    colorBitmap.Unlock();
                    ImageSource = colorBitmap;
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
