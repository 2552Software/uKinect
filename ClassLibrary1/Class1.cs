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
using System.ComponentModel;


namespace ClassLibrary1
{
    public class KinectDataObject : INotifyPropertyChanged
    {
        ImageSource ColorImage = null;
        public void Refresh(string name)
        {
            OnPropertyChanged(name);
        }
        // todo do for all images, get something going for the shapes, use the new graphics toy to tweak on stuff
        public ImageSource ImageSource
        {
            get
            {
                return ColorImage;
            }
        }
        public void setImage(byte[] buffer)
        {
            ColorImage = ConvertBytesToImage(buffer);
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
        private void SaveImage(WriteableBitmap img, ref MemoryStream memory)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(img));
            encoder.Save(memory);
            memory.Close();
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
                        WriteableBitmap colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

                        colorFrame.CopyConvertedFrameDataToIntPtr(
                                    colorBitmap.BackBuffer,
                                    (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                    ColorImageFormat.Bgra);
                        MemoryStream memory = new MemoryStream();
                        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                        encoder.QualityLevel = 30;
                        SaveImage(colorBitmap, ref memory);
                        using (var connection = factory.CreateConnection())
                        using (var channel = connection.CreateModel())
                        {
                            channel.ExchangeDeclare(exchange: "kinectcolor", type: "fanout");
                           
                            //bugbug once all working add in the jpeg stuff (?) or just compress
                            channel.BasicPublish(exchange: "kinectcolor",
                                                 routingKey: "",
                                                 basicProperties: null,
                                                 body: memory.ToArray());
                        }
                    }

                }
            }
        }
    }
}
