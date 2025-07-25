using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using OpenCvSharp;
using OpenCvSharp.Extensions;
using VisionInspection;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for ContrastNBrightnessEdit.xaml
    /// </summary>
    public partial class ContrastNBrightnessEdit : GridBase, INotifyPropertyChanged
    {
        //Variables
        private MyLogger logger = new MyLogger("ContrastNBrightnessEdit Edit");
        private Mat loadedImg = new Mat();
        public event RoutedEventHandler OnBtnRunClicked;

        //InOut
        private SvImage _inputImage = new SvImage();
        public SvImage InputImage
        {
            get => _inputImage; set
            {
                if (value == null) return;
                _inputImage = value;
                if (_inputImage.Mat.Height > 0 && _inputImage.Mat.Width > 0)
                {
                    toolBase.imgView.Source = _inputImage.Mat.ToBitmapSource();
                }
            }
        }
        public SvImage OutputImage { get; set; } = new SvImage();

        //Binding
        public event PropertyChangedEventHandler PropertyChanged;
        private double _gammaValue = 0, _alphaValue = 0, _betaValue = 0;
        public double GammaValue { get => _gammaValue; set { _gammaValue = value; OnPropertyChanged(nameof(GammaValue)); } }
        public double AlphaValue { get => _alphaValue; set { _alphaValue = value; OnPropertyChanged(nameof(AlphaValue)); } }
        public double BetaValue { get => _betaValue; set { _betaValue = value; OnPropertyChanged(nameof(BetaValue)); } }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            Run();
        }
        public ContrastNBrightnessEdit()
        {
            InitializeComponent();
            toolBase.DataContext = this;
        }
        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "Contrast Brightness";
            toolBase.cbxImage.Items.Add("[Contrast Brightness] Input Image");
            toolBase.cbxImage.Items.Add("[Contrast Brightness] Output Image");
            toolBase.cbxImage.SelectedIndex = 0;

            Grid parent = toolBase.gridBase.Parent as Grid;
            parent.Children.Add(this);
            parent.Children.Remove(toolBase.gridBase);
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
                var out1 = AdjustContrastBrightness(src, AlphaValue, BetaValue);
                var out2 = AdjustGamma(out1, GammaValue);
                toolBase.imgView.Source = out2.ToBitmapSource();
                OutputImage.Mat = out2;
                toolBase.cbxImage.SelectedIndex = isEditMode ? 1 : 0;
            }
            catch (Exception ex)
            {
                logger.Create("Update Value Error: " + ex.Message, ex);
            }
        }
        public Mat AdjustGamma(Mat src, double gamma)
        {
            Mat mat = new Mat(1, 256, 0);
            byte[] array = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                array[i] = (byte)(Math.Pow((double)i / 255.0, gamma) * 255.0);
            }

            mat.SetArray(0, 0, array);
            Mat mat2 = new Mat();
            Cv2.LUT(src, mat, mat2);
            return mat2;
        }

        public Mat AdjustContrastBrightness(Mat image, double alpha, double beta)
        {
            Mat mat = new Mat();
            image.ConvertTo(mat, -1, alpha, beta);
            return mat;
        }
    }
}
