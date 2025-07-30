using Development;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VisionInspection;
using Rect = OpenCvSharp.Rect;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for SaveImageEdit.xaml
    /// </summary>
    public partial class ImageBuffEdit : GridBase, INotifyPropertyChanged
    {
        //Variables
        private MyLogger logger = new MyLogger("ImageBuff Edit");
        public event RoutedEventHandler OnBtnRunClicked;
        public List<SvImage> ImageBuffer = new List<SvImage>();
        private int counter = 0;
        
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

        //[*********************************** BINDING ***********************************]
        public event PropertyChangedEventHandler PropertyChanged;

        #region Internal Field

        #endregion

        #region Property
        public Array DeviceCodes => Enum.GetValues(typeof(DeviceCode));
        public DeviceCode SelectDevReset { get; set; } = DeviceCode.D;
        protected void OnPropertyChanged(string pptName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pptName));
        }
        #endregion
        public ImageBuffEdit()
        {
            InitializeComponent();
            DisplayInit();
            RegisterEvent();

            toolBase.DataContext = this;
        }
        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "Image Buffer";
            toolBase.cbxImage.Items.Add("[Image Buffer] Input Image");
            toolBase.cbxImage.Items.Add("[Image Buffer] Output Image");
            toolBase.cbxImage.SelectedIndex = 0;

            try
            {
                Grid parent = toolBase.gridBase.Parent as Grid;
                parent.Children.Add(this);
                parent.Children.Remove(toolBase.gridBase);

                stViewImgBuff.Children.Clear();
            }
            catch (Exception ex)
            {
                logger.Create("Display Init Error: " + ex.Message, ex);
            }
        }
        protected override void RegisterEvent()
        {
            toolBase.btnRun.Click += BtnRun_Click;
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
        private void FitImage(Image srcImage, Canvas boundImage)
        {
            try
            {
                if (srcImage.Source == null) return;

                double canvasWidth = boundImage.Width;
                double canvasHeight = boundImage.Height;
                double imageWidth = srcImage.Source.Width;
                double imageHeight = srcImage.Source.Height;

                double scaleX = canvasWidth / imageWidth;
                double scaleY = canvasHeight / imageHeight;
                double scale = Math.Min(scaleX, scaleY);

                // Đặt ScaleTransform
                ScaleTransform sclTrans = new ScaleTransform();
                sclTrans.ScaleX = scale;
                sclTrans.ScaleY = scale;

                // Căn giữa ảnh trong khung
                TranslateTransform transTrans = new TranslateTransform();
                transTrans.X = (canvasWidth - imageWidth * scale) / 2;
                transTrans.Y = (canvasHeight - imageHeight * scale) / 2;

                TransformGroup transformGroup = new TransformGroup();
                transformGroup.Children.Add(sclTrans);
                transformGroup.Children.Add(transTrans);
                srcImage.RenderTransform = transformGroup;
            }
            catch (Exception ex)
            {
                logger.Create("Fit Image Error: " + ex.Message, ex);
            }
        }
        private void CreateImgBuffView(int index, Mat matImg)
        {
            if (matImg == null || matImg.Width == 0 || matImg.Height == 0)
                return;
            Expander expd = new Expander
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC")
            };
            Label lbHeader = new Label
            {
                FontStyle = FontStyles.Italic,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(3, 5, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(72, 158, 55)), // #FF489E37
                Content = $"Image {index}"
            };
            expd.Header = lbHeader;
            StackPanel mainPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = Brushes.Cornsilk
            };
            // Canvas with Image
            Canvas canvas = new Canvas
            {
                Background = new SolidColorBrush(Color.FromRgb(153, 153, 153)), // #FF999999
                Width = 250,
                Height = 150,
                Margin = new Thickness(10, 0, 0, 0)
            };
            Image img = new Image { Source = matImg.ToBitmapSource() };
            canvas.Children.Add(img);
            FitImage(img, canvas);

            // Right StackPanel
            StackPanel rightPanel = new StackPanel();

            // Shared Style for Label
            Style labelStyle = new Style(typeof(Label));
            labelStyle.Setters.Add(new Setter(Label.PaddingProperty, new Thickness(1)));
            labelStyle.Setters.Add(new Setter(Label.ForegroundProperty, new SolidColorBrush(Color.FromRgb(72, 158, 55)))); // #FF489E37
            labelStyle.Setters.Add(new Setter(Label.FontStyleProperty, FontStyles.Italic));

            // Add labels with style
            Label label1 = new Label { Content = $"Index : {index}", Padding = new Thickness(0, 5, 0, 1), Style = labelStyle };
            Label label2 = new Label { Content = $"Time : {DateTime.Now: dd/MM/yyyy HH:mm:ss:fff}", Style = labelStyle };
            Label label3 = new Label { Content = $"Width x Height : {matImg.Width} x {matImg.Height}", Style = labelStyle };

            rightPanel.Children.Add(label1);
            rightPanel.Children.Add(label2);
            rightPanel.Children.Add(label3);

            // Assemble
            mainPanel.Children.Add(canvas);
            mainPanel.Children.Add(rightPanel);
            expd.Content = mainPanel;

            //Add Expander
            stViewImgBuff.Children.Add(expd);
        }
        public bool IsPositiveInt(string strDigi)
        {
            return int.TryParse(strDigi, out int value) && value > 0;
        }

        private SvImage runImage = new SvImage();
        public override void Run()
        {
            if (this.InputImage == null || this.InputImage.Mat == null || this.InputImage.Mat.Width <= 0 || this.InputImage.Mat.Height <= 0)
            {
                if (toolBase.isImgPath && isEditMode)
                {
                    runImage.Mat = (ImgView.Source as BitmapSource).ToMat();
                    runImage.RegionRect.Rect = new Rect(0, 0, (int)ImgView.Source.Width, (int)ImgView.Source.Height);
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
                runImage.Mat = (ImgView.Source as BitmapSource).ToMat();
                runImage.RegionRect.Rect = new Rect(0, 0, (int)ImgView.Source.Width, (int)ImgView.Source.Height);
            }
            else if (InputImage.Mat != null && InputImage.Mat.Width > 0 && InputImage.Mat.Height > 0 && !toolBase.isImgPath)
            {
                runImage = this.InputImage.Clone(true);
            }
            try
            {
                if(!IsPositiveInt(txtCacheNum.Text))
                {
                    MessageBox.Show("Address PLC Syntax Error!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Address PLC Syntax Error!");
                    return;
                }    
                if(ImageBuffer.Count < int.Parse(txtCacheNum.Text) - 1)
                {
                    ImageBuffer.Add(runImage);
                    CreateImgBuffView(ImageBuffer.Count - 1, runImage.Mat.Clone());
                }
                else if(ImageBuffer.Count ==  int.Parse(txtCacheNum.Text) - 1)
                {
                    ImageBuffer.Add(runImage);
                    CreateImgBuffView(ImageBuffer.Count - 1, runImage.Mat.Clone());
                }    
            }
            catch (Exception ex)
            {
                logger.Create("Run Error: " + ex.Message, ex);
            }
        }
    }
}
