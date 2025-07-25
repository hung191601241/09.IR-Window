using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using VisionInspection;
using static VisionTools.ToolEdit.BlobEdit;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for ImageProcessEdit.xaml
    /// </summary>
    public partial class ImageProcessEdit : GridBase, INotifyPropertyChanged
    {
        //Variables
        private MyLogger logger = new MyLogger("ImageProcess Edit");
        private Mat loadedImg = new Mat();
        public event RoutedEventHandler OnBtnRunClicked;

        //InOut
        private SvImage _inputImage = new SvImage();
        public SvImage InputImage
        {
            get => _inputImage; set
            {
                _inputImage = value;
                if (value == null) return;
                if (_inputImage.Mat.Height > 0 && _inputImage.Mat.Width > 0)
                {
                    toolBase.imgView.Source = _inputImage.Mat.ToBitmapSource();
                }
            }
        }
        public SvImage OutputImage { get; set; } = new SvImage();

        //Binding
        public event PropertyChangedEventHandler PropertyChanged;
        public enum ImageProcessMode { Threshold }
        public Array ImageProcessModes => Enum.GetValues(typeof(ImageProcessMode));
        public enum ThresholdMode { White, Black }
        public Array ThresholdModes => Enum.GetValues(typeof(ThresholdMode));
        private ImageProcessMode _selectedImageProcessMode = ImageProcessMode.Threshold;
        private ThresholdMode _selectedThresholdMode = ThresholdMode.White;
        private int _thresholdValue = 0;
        public ImageProcessMode SelectedImageProcessMode { get => _selectedImageProcessMode; set { _selectedImageProcessMode = value; OnPropertyChanged(nameof(SelectedImageProcessMode)); } }
        public ThresholdMode SelectedThresholdMode { get => _selectedThresholdMode; set { _selectedThresholdMode = value; OnPropertyChanged(nameof(SelectedThresholdMode)); } }
        public int ThresholdValue { get => _thresholdValue; set { _thresholdValue = value; OnPropertyChanged(nameof(ThresholdValue)); } }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if(propertyName == nameof(ThresholdValue))
            {
                Run();
            }
        }

        public ImageProcessEdit()
        {
            InitializeComponent();
            DisplayInit();
            RegisterEvent();
            toolBase.DataContext = this;
        }
        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "Image Process";
            toolBase.cbxImage.Items.Add("[Image Process] Input Image");
            toolBase.cbxImage.Items.Add("[Image Process] Output Image");
            toolBase.cbxImage.SelectedIndex = 0;
            try
            {
                Grid parent = toolBase.gridBase.Parent as Grid;
                parent.Children.Add(this);
                parent.Children.Remove(toolBase.gridBase);
            }
            catch (Exception ex)
            {
                logger.Create("Display Init Error: " + ex.Message, ex);
            }
        }
        protected override void RegisterEvent()
        {
            toolBase.btnRun.Click += BtnRun_Click;
            toolBase.cbxImage.SelectionChanged += CbxImage_SelectionChanged;
            toolBase.OnLoadImage += ToolBase_OnLoadImage;
        }
        private void CbxImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (toolBase.cbxImage.SelectedIndex == 0)
                {
                    if (runImage.Mat.Height > 0 && runImage.Mat.Width > 0)
                    {
                        toolBase.imgView.Source = runImage.Mat.ToBitmapSource();
                    }
                    oldSelect = 0;
                }
                else if (toolBase.cbxImage.SelectedIndex == 1)
                {
                    if (OutputImage.Mat.Height > 0 && OutputImage.Mat.Width > 0)
                    {
                        toolBase.imgView.Source = OutputImage.Mat.ToBitmapSource();
                    }
                    oldSelect = 1;
                }
            }
            catch (Exception ex)
            {
                logger.Create("Cbx Image Error: " + ex.Message, ex);
            }
        }

        public void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            OnBtnRunClicked?.Invoke(sender, e);
        }
        private void ToolBase_OnLoadImage(object sender, RoutedEventArgs e)
        {
            loadedImg = (ImgView.Source as BitmapSource).ToMat();
        }
        private SvImage runImage = new SvImage();
        public override void Run()
        {
            try
            {
                if (this.InputImage == null || this.InputImage.Mat == null || this.InputImage.Mat.Width <= 0 || this.InputImage.Mat.Height <= 0)
                {
                    if (toolBase.isImgPath && isEditMode)
                    {
                        runImage.Mat = loadedImg.Clone();
                    }
                    else
                    {
                        meaRunTime.Stop();
                        toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "InputImage is null or error!");
                        return;
                    }
                }
                else if (InputImage.Mat != null && toolBase.isImgPath && isEditMode)
                {
                    runImage.Mat = loadedImg.Clone();
                }
                else if (InputImage.Mat != null && InputImage.Mat.Width > 0 && InputImage.Mat.Height > 0 && !toolBase.isImgPath)
                {
                    runImage = this.InputImage.Clone(true);
                }

                var src = this.runImage.Mat.Clone();
                Mat dst = this.runImage.Mat.Clone();
                switch (SelectedImageProcessMode)
                {
                    case ImageProcessMode.Threshold:
                        switch (SelectedThresholdMode)
                        {
                            case ThresholdMode.White:
                                dst = src.Threshold(ThresholdValue, 255, ThresholdTypes.Binary);
                                break;
                            case ThresholdMode.Black:
                                dst = src.Threshold(ThresholdValue, 255, ThresholdTypes.BinaryInv);
                                break;
                        }
                        break;
                }

                toolBase.imgView.Source = dst.ToBitmapSource();
                OutputImage.Mat = dst;
                toolBase.cbxImage.SelectedIndex = isEditMode ? 1 : 0;
                //dst.SaveImage($"{DateTime.Now.ToString("HH-mm-ss-ffff")}_TestMulImg.bmp");
            }
            catch (Exception ex)
            {
                logger.Create("Update Value Error: " + ex.Message, ex);
            }
        }
    }
}
