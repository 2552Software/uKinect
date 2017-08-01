using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using RabbitMQ.Client;
//https://stackoverflow.com/questions/3154198/cant-find-system-windows-media-namepspace
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace ClassLibrary1
{
    public class myKinect
    {
        private KinectSensor kinectSensor = null;
        private ColorFrameReader colorFrameReader = null;
        ConnectionFactory factory = new ConnectionFactory() { HostName = "localhost" };

        public void setup()
        {
            kinectSensor = KinectSensor.GetDefault();
            kinectSensor.Open();
            if (kinectSensor.IsOpen)
            {
                Console.WriteLine("kinect open");
            }
            if (kinectSensor.IsAvailable)
            {
                Console.WriteLine("kinect available");
            }
            // wire handler for frame arrival
            kinectSensor.ColorFrameSource.OpenReader().FrameArrived += updateEvent;
        }
        public void close()
        {
            if (colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                colorFrameReader.Dispose();
                colorFrameReader = null;
            }
            if (kinectSensor != null)
            {
                kinectSensor.Close();
                kinectSensor = null;
            }
        }


        private void updateEvent(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    // bugbug include body id for other items
                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        using (MemoryStream memory = new MemoryStream())
                        {
                            WriteableBitmap colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(colorBitmap));
                            encoder.QualityLevel = 30;
                            encoder.Save(memory);
                            using (var connection = factory.CreateConnection())
                            using (var channel = connection.CreateModel())
                            {
                                channel.ExchangeDeclare(exchange: "kinectcolor", type: "fanout");

                                channel.BasicPublish(exchange: "kinectcolor",
                                                     routingKey: "",
                                                     basicProperties: null,
                                                     body: memory.GetBuffer());
                            }
                        }
                    }
                }
            }
        }
    }
}
