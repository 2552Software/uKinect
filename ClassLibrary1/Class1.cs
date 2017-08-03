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
using System.Runtime.InteropServices;

namespace s552ClassLibrary1
{
    public static class myColor
    {
        public static HSB RGBToHSB(System.Drawing.Color c)
        {
            HSB hsb = new HSB();
            hsb.H = c.GetHue();
            hsb.S = c.GetSaturation();
            hsb.B = c.GetBrightness();
            hsb.A = c.A;
            return hsb;
        }
        public class HSB
        {
            public double H { get; set; }
            public double S { get; set; }
            public double B { get; set; }
            public int A { get; set; } //0 - 255
        }
        public static System.Drawing.Color FromHSB(HSB hsb)
        {
            if (0 > hsb.A || 255 < hsb.A)
            {
                throw new ArgumentOutOfRangeException("alpha", hsb.A,"Value must be within a range of 0 - 255.");
            }

            if (0f > hsb.H || 360f < hsb.H)
            {
                throw new ArgumentOutOfRangeException("hue", hsb.H, "Value must be within a range of 0 - 360.");
            }

            if (0f > hsb.S || 1f < hsb.S)
            {
                throw new ArgumentOutOfRangeException("saturation", hsb.S,"Value must be within a range of 0 - 1.");
            }

            if (0f > hsb.B || 1f < hsb.B)
            {
                throw new ArgumentOutOfRangeException("brightness", hsb.B, "Value must be within a range of 0 - 1.");
            }

            if (0 == hsb.S)
            {
                return System.Drawing.Color.FromArgb(hsb.A, Convert.ToInt32(hsb.B * 255), Convert.ToInt32(hsb.B * 255), Convert.ToInt32(hsb.B * 255));
            }

            double fMax, fMid, fMin;
            int iSextant, iMax, iMid, iMin;

            if (0.5 < hsb.B)
            {
                fMax = hsb.B - (hsb.B * hsb.S) + hsb.S;
                fMin = hsb.B + (hsb.B * hsb.S) - hsb.S;
            }
            else
            {
                fMax = hsb.B + (hsb.B * hsb.S);
                fMin = hsb.B - (hsb.B * hsb.S);
            }

            iSextant = (int)Math.Floor(hsb.H / 60f);
            if (300f <= hsb.H)
            {
                hsb.H -= 360f;
            }

            hsb.H /= 60f;
            hsb.H -= 2f * (float)Math.Floor(((iSextant + 1f) % 6f) / 2f);
            if (0 == iSextant % 2)
            {
                fMid = (hsb.H * (fMax - fMin)) + fMin;
            }
            else
            {
                fMid = fMin - (hsb.H * (fMax - fMin));
            }

            iMax = Convert.ToInt32(fMax * 255);
            iMid = Convert.ToInt32(fMid * 255);
            iMin = Convert.ToInt32(fMin * 255);

            switch (iSextant)
            {
                case 1:
                    return System.Drawing.Color.FromArgb(hsb.A, iMid, iMax, iMin);
                case 2:
                    return System.Drawing.Color.FromArgb(hsb.A, iMin, iMax, iMid);
                case 3:
                    return System.Drawing.Color.FromArgb(hsb.A, iMin, iMid, iMax);
                case 4:
                    return System.Drawing.Color.FromArgb(hsb.A, iMid, iMin, iMax);
                case 5:
                    return System.Drawing.Color.FromArgb(hsb.A, iMax, iMin, iMid);
                default:
                    return System.Drawing.Color.FromArgb(hsb.A, iMax, iMid, iMin);
            }

        }

    }

    public static class Numbers
    {
        public static double ConvertToRange(double value, double originalMin, double originalMax, double targetMin, double targetMax, bool clamp = true)
        {
            var v = ((value - originalMin) / (originalMax - originalMin) * (targetMax - targetMin)) + targetMin;
            return clamp ? Clamp(Math.Min(targetMin, targetMax), v, Math.Max(targetMin, targetMax)) : v;
        }

        public static double Clamp(double minValue, double value, double maxValue)
        {
            return Math.Min(maxValue, Math.Max(minValue, value));
        }

        static Random random = new Random();

        public static double Random(double min, double max)
        {
            return random.NextDouble() * (max - min) + min;
        }

        public static float Random(float min, float max)
        {
            return (float)random.NextDouble() * (max - min) + min;
        }

        public static int Random(int min, int max)
        {
            return random.Next(min, max);
        }

        public static T RandomOneOf<T>(params T[] values)
        {
            return values[random.Next(0, values.Length)];
        }

        public static T RandomOneOf<T>(List<T> values)
        {
            return values[random.Next(0, values.Count)];
        }

        public static int RandomSign()
        {
            return Numbers.RandomOneOf(new int[] { -1, 1 });
        }

        public static bool RandomProbability(double chance)
        {
            return Numbers.Random(0.0, 1.0) < chance;
        }


    }
    public class KinectDataObject : INotifyPropertyChanged
    {
        ImageSource ColorImage = null;
        ImageSource DepthImage = null;
        ImageSource IRImage = null;
        ImageSource BodyIndexImage = null;
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
            //kinectSensor.ColorFrameSource.OpenReader().FrameArrived += updateColorEvent;
            kinectSensor.DepthFrameSource.OpenReader().FrameArrived += updateDepthEvent;
            //kinectSensor.InfraredFrameSource.OpenReader().FrameArrived += updateIREvent;
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
        /*
         * The infrared frame is great for computer vision algorithms where texture is important, such as facial recognition. 
         * Data is stored as 16-bit unsigned integers. 
         * The infrared frame is also great for green screening, tracking reflective markers, and filtering out low-return (and therefore jittery) depth pixels. 
         * Note that the infrared frame is derived from the same sensor as depth, so the images are perfectly aligned. 
         * For example, the infrared pixel at row 5 col 9 goes with the depth pixel at row 5 col 9.
         */
        private void updateIREvent(object sender, InfraredFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (InfraredFrame frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    FrameDescription frameDescription = frame.FrameDescription;
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
        /*
         * The data for this frame is stored as 16-bit unsigned integers, where each value represents the distance in millimeters. 
         * The maximum depth distance is 8 meters, although reliability starts to degrade at around 4.5 meters. 
         * Developers can use the depth frame to build custom tracking algorithms in cases where the BodyFrame isn’t enough.
         */
        private void updateDepthEvent(object sender, DepthFrameArrivedEventArgs e)
        {
            // DepthFrame is IDisposable
            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    FrameDescription depthFrameDescription = depthFrame.FrameDescription;
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
        private void updateColorEvent(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    FrameDescription frameDescription = frame.FrameDescription;
                    WriteableBitmap Bitmap = new WriteableBitmap(frameDescription.Width, frameDescription.Height, 196.0, 196.0, PixelFormats.Bgr32, null);

                    frame.CopyConvertedFrameDataToIntPtr(Bitmap.BackBuffer,   (uint)(frameDescription.Width * frameDescription.Height * 4),  ColorImageFormat.Bgra);
                    SendImage(Bitmap, "kinectcolor");
                }
            }
        }
        /*
         * The pixel values in this frame are 8-bit unsigned integers, where 0-5 map directly to the BodyData index in the BodyFrame. 
         * Values greater than the value obtained from BodyCount indicate the pixel is part of the background, not associated with a tracked body. 
         * This frame is useful for green screening applications, or any scenario where you want to display the silhouette of the user. 
         * It also provides a good starting bounds for custom depth algorithms.
         */
        private void updateBodyIndexEvent(object sender, BodyIndexFrameArrivedEventArgs e)
        {
            using (BodyIndexFrame frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    FrameDescription frameDescription = frame.FrameDescription;
                    // verify data and write the data to the display bitmap
                    if ((frameDescription.Width * frameDescription.Height) == 424*512)
                    {
                        WriteableBitmap Bitmap = BitmapFactory.New(frameDescription.Width, frameDescription.Height);
                        byte[] frameData = new byte[frameDescription.Width * frameDescription.Height];
                        frame.CopyFrameDataToArray(frameData);

                        for (int y = 0; y < frameDescription.Height; y++)
                        {
                            for (int x = 0; x < frameDescription.Width; x++)
                            {
                                int index = y * frameDescription.Width + x;
                                if (frameData[index] != 0xff)
                                {
                                    myColor.HSB hsb = new myColor.HSB();
                                    hsb.H = x / frameDescription.Height * 255;
                                    hsb.S = Numbers.ConvertToRange(y, 0, frameDescription.Height / 2, 0, 1, true);
                                    hsb.B = Numbers.ConvertToRange(y, frameDescription.Height / 2, frameDescription.Height, 1, 0, true);
                                    System.Drawing.Color color = myColor.FromHSB(hsb);
                                    // make a dynamic image, also there can be up to 6 images so we need them to be a little different 
                                    Bitmap.SetPixel(x, y, color.A, color.R, color.G, color.B);
                                }
                                else
                                {
                                    Bitmap.SetPixel(x, y, 255, 0, 255, 0); // green
                                }
                            }
                        }
                        SendImage(Bitmap, "kinectbodyindex");
                    }
                }
            }
        }
    }
}
