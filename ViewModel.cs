using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace ExperienceIndicator
{
    public class ViewModel : INotifyPropertyChanged
    {
        #region Constructor

        public ViewModel() : this(new Model())
        { }

        public ViewModel(Model model)
        {
            _model = model ?? new Model();
            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            _dispatcherTimer.Tick += new EventHandler(DispatcherTimeTick);
            _model.NewImageGrabbed += NewImageGrabbed;
            _model.DetectFacePoints += DetectFacePoints;
            _dispatcherTimer.Start();
        }

        private void DetectFacePoints(PointF[] faces)
        {
            if (faces.Length > 0)
            {
                _pupilXNormalizedPosition = faces.First().X;
                _pupilYNormalizedPosition = faces.First().Y;
            }
        }

        #endregion

        private Model _model;
        private DispatcherTimer _dispatcherTimer = new DispatcherTimer();

        private void NewImageGrabbed(BitmapSource grabbedImage)
        {
            CameraFrame = grabbedImage;
        }

        private const int PUPIL_PROPORTION_SIZE = 46;
        public int PupilProportionSize => PUPIL_PROPORTION_SIZE;

        private float _pupilXNormalizedPosition
        {
            set
            {
                if (value < 0 || value > 1)
                    throw new Exception("The value must be normalized");
                PupilLeftProportionPosition = (int)((100 - PupilProportionSize) * value);
                OnPropertyChanged(nameof(PupilLeftProportionPosition));
                OnPropertyChanged(nameof(PupilRightProportionPosition));
            }
        }

        private float _pupilYNormalizedPosition
        {
            set
            {
                if (value < 0 || value > 1)
                    throw new Exception("The value must be normalized");
                PupilUpProportionPosition = (int)((100 - PupilProportionSize) * value);
                OnPropertyChanged(nameof(PupilUpProportionPosition));
                OnPropertyChanged(nameof(PupilBottomProportionPosition));
            }
        }

        public int PupilLeftProportionPosition { get; private set; } = (100 - PUPIL_PROPORTION_SIZE) / 2;

        public int PupilRightProportionPosition => 100 - PUPIL_PROPORTION_SIZE - PupilLeftProportionPosition;

        public int PupilUpProportionPosition { get; private set; } = (100 - PUPIL_PROPORTION_SIZE) / 2;

        public int PupilBottomProportionPosition => 100 - PUPIL_PROPORTION_SIZE - PupilUpProportionPosition;

        private BitmapSource _cameraFrame;
        public BitmapSource CameraFrame
        {
            get => _cameraFrame;
            set
            {
                _cameraFrame = value;
                OnPropertyChanged();
            }
        }

        private string _choiceWebCamera;
        public string ChoiceWebCamera 
        { 
            get => _choiceWebCamera; 
            set
            {
                _choiceWebCamera = value;
                _model.CaptureTheCamera(_choiceWebCamera);
            }
        }

        private ObservableCollection<string> _webCameras;
        public ObservableCollection<string> WebCameras => _webCameras ?? (_webCameras = new ObservableCollection<string>(_model.GetCameraNames()));

        private float _cpuUsage;
        public float CpuUsage
        {
            get => _cpuUsage;
            set
            {
                _cpuUsage = value;
                OnPropertyChanged();
            }
        }

        private float _ramUsage;
        public float RamUsage
        {
            get => _ramUsage;
            set
            {
                _ramUsage = value;
                OnPropertyChanged();
            }
        }

        private DateTime _dateTimeNow;
        public DateTime DateTimeNow 
        { 
            get => _dateTimeNow;
            set
            {
                _dateTimeNow = value;
                OnPropertyChanged();
            }
        }

        private void DispatcherTimeTick(object sender, EventArgs e)
        {
            CpuUsage = _model.GetCpuUsage();
            RamUsage = _model.GetRamUsage();
            DateTimeNow = DateTime.Now;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion
    }
}
