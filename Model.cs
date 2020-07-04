using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Emgu;
using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.Util;
using DirectShowLib;
using Emgu.CV.Structure;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Interop;
using System.Drawing;
using Emgu.CV.CvEnum;

namespace ExperienceIndicator
{
    public class Model
    {
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;
        private long _ramTotal;
        
        private VideoCapture _videoCapture;
        private DsDevice[] _webCameras;
        private int _selectedCamerID;

        private CascadeClassifier _cascadeFaceClassifier;

        public Model()
        {
            _cpuCounter = new PerformanceCounter(@"Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter(@"Memory", "Available MBytes");
            _ramTotal = GetTotalMemoryInMiB();
            _webCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            _cascadeFaceClassifier = new CascadeClassifier("haarcascade_frontalface_default.xml");
        }

        public delegate void ImageGrabbedHandler(BitmapSource grabbedImage);
        public event ImageGrabbedHandler NewImageGrabbed;

        public delegate void FaceNormalizePointsHandler(PointF[] faces);
        public event FaceNormalizePointsHandler DetectFacePoints;

        public string[] GetCameraNames() => _webCameras.Select(i => i.Name).ToArray();

        public void CaptureTheCamera(string cameraName)
        {
            _selectedCamerID = _webCameras.Select(i => i.Name).ToList().IndexOf(cameraName);
            _videoCapture = new VideoCapture(_selectedCamerID);
            _videoCapture.ImageGrabbed += VideoCapture_ImageGrabbed;
            _videoCapture.Start();
        }

        private void VideoCapture_ImageGrabbed(object sender, EventArgs e)
        {
            try
            {
                var m = new Mat();
                _videoCapture.Retrieve(m);
                var image = m.ToImage<Bgr, byte>().Flip(FlipType.Horizontal);
                DetectFacesRectangles(image);

                var bitmapSrc = ToBitmapSource(image);
                bitmapSrc.Freeze();
                NewImageGrabbed(bitmapSrc);
            }
            catch (Exception)
            {
                
            }
        }

        private void DetectFacesRectangles(Image<Bgr, byte> image)
        {
            var grayImage = image.Convert<Gray, byte>();
            var points = new List<PointF>();
            foreach(var face in _cascadeFaceClassifier.DetectMultiScale(grayImage))
            {
                image.Draw(face, new Bgr(Color.Yellow));
                var x = face.X + face.Width / 2;
                var y = face.Y + face.Height / 2;
                image.Draw(new CircleF(new PointF(x, y), 2), new Bgr(Color.Yellow), 2);
                image.Draw($"{(double)x / image.Width : 0.00}; {(double)y / image.Height : 0.00}", new System.Drawing.Point(x, y), FontFace.HersheyComplex, 1, new Bgr(Color.Yellow));
                points.Add(new PointF((float)x / image.Width, (float)y / image.Height));
            }
            DetectFacePoints(points.ToArray());
        }

        public float GetCpuUsage() => _cpuCounter.NextValue();

        public float GetRamUsage() => (_ramTotal - _ramCounter.NextValue()) / _ramTotal * 100;

        #region Total RAM

        // Taken from https://stackoverflow.com/questions/10027341/c-sharp-get-used-memory-in

        [StructLayout(LayoutKind.Sequential)]
        private  struct PerformanceInformation
        {
            public int Size;
            public IntPtr CommitTotal;
            public IntPtr CommitLimit;
            public IntPtr CommitPeak;
            public IntPtr PhysicalTotal;
            public IntPtr PhysicalAvailable;
            public IntPtr SystemCache;
            public IntPtr KernelTotal;
            public IntPtr KernelPaged;
            public IntPtr KernelNonPaged;
            public IntPtr PageSize;
            public int HandlesCount;
            public int ProcessCount;
            public int ThreadCount;
        }

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPerformanceInfo([Out] out PerformanceInformation PerformanceInformation, [In] int Size);

        private long GetTotalMemoryInMiB()
        {
            PerformanceInformation pi = new PerformanceInformation();
            if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
            {
                return Convert.ToInt64((pi.PhysicalTotal.ToInt64() * pi.PageSize.ToInt64() / 1048576));
            }
            else
            {
                return -1;
            }
        }

        #endregion

        #region Bitmap2BitmapSource

        // Taken from https://stackoverflow.com/questions/16596915/emgu-with-c-sharp-wpf/16597958#16597958

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        private BitmapSource ToBitmapSource(Image<Bgr, byte> image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap();

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr);
                return bs;
            }
        }

        #endregion
    }
}
