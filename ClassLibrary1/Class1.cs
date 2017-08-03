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
        ImageSource DepthImage = null;
        ImageSource IRImage = null;
        ImageSource BodyIndexImage = null;
        private static readonly uint[] BodyIndexColor =
        {
            0x0000FF00,
            0x00FF0000,
            0xFFFF4000,
            0x40FFFF00,
            0xFF40FF00,
            0xFF808000,
        };
        public void Refresh(string name)
        {
            OnPropertyChanged(name);
        }
        // todo do for all images, get something going for the shapes, use the new graphics toy to tweak on stuff
        public ImageSource ColorSource
        {
            get { return ColorImage; }   
        }
        public ImageSource DepthSource
        {
            get { return DepthImage; }
        }
        public ImageSource IRSource
        {
            get { return IRImage; }
        }
        public ImageSource BodyIndexSource
        {
            get { return BodyIndexImage; }
        }
        public void setColorImage(byte[] buffer)
        {
            ColorImage = ConvertBytesToImage(buffer);
            Refresh("ColorSource");
        }
        public void setDepthImage(byte[] buffer)
        {
            DepthImage = ConvertBytesToImage(buffer);
            Refresh("DepthSource");
        }
        public void setIRImage(byte[] buffer)
        {
            IRImage = ConvertBytesToImage(buffer);
            Refresh("IRSource");
        }
        public void setBodyIndexImage(byte[] buffer)
        {
            BodyIndexImage = ConvertBytesToImage(buffer);
            Refresh("BodyIndexSource");
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
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;
        private const float InfraredOutputValueMinimum = 0.01f;
        private const float InfraredOutputValueMaximum = 1.0f;
        private const float InfraredSceneValueAverage = 0.08f;
        private const float InfraredSceneStandardDeviations = 3.0f;

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
            kinectSensor.ColorFrameSource.OpenReader().FrameArrived += updateColorEvent;
            kinectSensor.DepthFrameSource.OpenReader().FrameArrived += updateDepthEvent;
            kinectSensor.InfraredFrameSource.OpenReader().FrameArrived += updateIREvent;
            kinectSensor.BodyIndexFrameSource.OpenReader().FrameArrived += updateBodyIndexEvent;
        }
        public void close()
        {
            if (kinectSensor != null)
            {
                kinectSensor.Close();
                kinectSensor = null;
            }
        }
        private MemoryStream SaveImage(WriteableBitmap img, int quality=30)
        {
            MemoryStream memory = new MemoryStream();
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = quality;
            encoder.Frames.Add(BitmapFrame.Create(img));
            encoder.Save(memory);
            memory.Close();
            return memory;
        }
        private void SendImage(WriteableBitmap img, string name, int quality = 30)
        {
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: name, type: "fanout");

                //bugbug once all working add in the jpeg stuff (?) or just compress
                channel.BasicPublish(exchange: name,
                                     routingKey: "",
                                     basicProperties: null,
                                     body: SaveImage(img, quality).ToArray());
            }
        }
        private void updateIREvent(object sender, InfraredFrameArrivedEventArgs e)
        {

            // ColorFrame is IDisposable
            using (InfraredFrame frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    FrameDescription frameDescription = frame.FrameDescription;
                    using (KinectBuffer buffer = frame.LockImageBuffer())
                    {
                        ushort[] frameData = new ushort[frameDescription.Width * frameDescription.Height];

                        WriteableBitmap Bitmap = BitmapFactory.New(frameDescription.Width, frameDescription.Height);
                        frame.CopyFrameDataToArray(frameData);
                        for (int y = 0; y < frameDescription.Height; y++)
                        {
                            for (int x = 0; x < frameDescription.Width; x++)
                            {
                                int index = y * frameDescription.Width + x;
                                float intensityRatio = (float)frameData[index] / InfraredSourceValueMaximum;

                                // 2. dividing by the (average scene value * standard deviations)
                                intensityRatio /= InfraredSceneValueAverage * InfraredSceneStandardDeviations;

                                // 3. limiting the value to InfraredOutputValueMaximum
                                intensityRatio = Math.Min(InfraredOutputValueMaximum, intensityRatio);

                                // 4. limiting the lower value InfraredOutputValueMinimum
                                intensityRatio = Math.Max(InfraredOutputValueMinimum, intensityRatio);

                                // 5. converting the normalized value to a byte and using the result
                                // as the RGB components required by the image
                                byte intensity = (byte)(intensityRatio * 255.0f);
                                Bitmap.SetPixel(x, y, 255, intensity, intensity, intensity);
                            }
                        }
                        SendImage(Bitmap, "kinectir");
                    }

                }
            }
        }
        private void updateDepthEvent(object sender, DepthFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    FrameDescription depthFrameDescription = depthFrame.FrameDescription;
                    using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        ushort[] frameData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];

                        WriteableBitmap Bitmap = BitmapFactory.New(depthFrameDescription.Width, depthFrameDescription.Height);
                        depthFrame.CopyFrameDataToArray(frameData);
                        ushort minDepth = depthFrame.DepthMinReliableDistance;
                        ushort maxDepth = depthFrame.DepthMaxReliableDistance;
                        int mapDepthToByte = maxDepth / 256;
                        for (int y = 0; y < depthFrameDescription.Height; y++)
                        {
                            for (int x = 0; x < depthFrameDescription.Width; x++)
                            {
                                int index = y * depthFrameDescription.Width + x;
                                ushort depth = frameData[index];
                                byte intensity = (byte)(depth >= minDepth &&
                                    depth <= maxDepth ? (depth / mapDepthToByte) : 0);

                                Bitmap.SetPixel(x, y, 255, intensity, intensity, intensity);
                            }
                        }
                        SendImage(Bitmap, "kinectdepth");
                    }
                }
            }
        }
        private void updateColorEvent(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                    WriteableBitmap colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 196.0, 196.0, PixelFormats.Bgr32, null);

                    colorFrame.CopyConvertedFrameDataToIntPtr(
                                colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);
                    SendImage(colorBitmap, "kinectcolor");
                }
            }
        }
        private void updateBodyIndexEvent(object sender, BodyIndexFrameArrivedEventArgs e)
        {
            using (BodyIndexFrame frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    using (Microsoft.Kinect.KinectBuffer bodyIndexBuffer = frame.LockImageBuffer())
                    {
                        FrameDescription frameDescription = frame.FrameDescription;
                        // verify data and write the color data to the display bitmap
                        if (((frameDescription.Width * frameDescription.Height) == bodyIndexBuffer.Size) &&
                            (frameDescription.Width == this.bodyIndexBitmap.PixelWidth) && (frameDescription.Height == this.bodyIndexBitmap.PixelHeight))
                        {
                            this.ProcessBodyIndexFrameData(bodyIndexBuffer.UnderlyingBuffer, bodyIndexBuffer.Size);
                            bodyIndexFrameProcessed = true;
                        }
                    }
                }
            }
        }
    }
}
