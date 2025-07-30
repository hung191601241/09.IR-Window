using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VisionInspection;
using UserControl = System.Windows.Controls.UserControl;
using System.ComponentModel;
using System.Text.RegularExpressions;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Point = System.Windows.Point;
using Microsoft.Win32;
using System.Threading;
using VisionTools.ToolDesign;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for ToolEditBase.xaml
    /// </summary>
    public partial class ToolEditBase : UserControl, INotifyPropertyChanged
    {
        //public SvJob Job;
        public string PATH_JOB = "C:\\SVL_Data\\Job";
        public event PropertyChangedEventHandler PropertyChanged;
        public event RoutedEventHandler OnLoadImage;
        private bool _bitStatus = false;
        public event EventHandler BitStatusChanged;
        public event RoutedEventHandler OnSaveTool;
        public event RoutedEventHandler OnPropertyRoi;
        public event RoutedEventHandler OnDeleteRoi;
        public event RoutedEventHandler OnMatrixRoi;
        public List<MenuItem> MnItems = new List<MenuItem>();
        public bool isImgPath = false;


        //Trasnform Image
        ScaleTransform scaleTrans = new ScaleTransform();
        TranslateTransform translateTrans = new TranslateTransform();
        private Point lastMousePosition;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected string TimeRunText
        {
            get { return (string)lbTimeRun.Content; }
            set { lbTimeRun.Content = value; }
        }
        protected Brush ToolStatus
        {
            get { return elipSttRun.Fill; }
            set { elipSttRun.Fill = value; }
        }
        // DependencyProperty để cho phép UserControlB gán nội dung vào TabItem
        public UIElement TabCtrlCusContent
        {
            get { return (UIElement)GetValue(TabCtrlCusContentProperty); }
            set { SetValue(TabCtrlCusContentProperty, value); }
        }

        public static readonly DependencyProperty TabCtrlCusContentProperty =
            DependencyProperty.Register("TabCtrlCusContent", typeof(UIElement), typeof(ToolEditBase), new PropertyMetadata(null, OnTabCtrlCusContentChanged));

        private static void OnTabCtrlCusContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToolEditBase control && e.NewValue is UIElement newContent)
            {
                control.TabCtrlCusContent = newContent;
            }
        }
        public Brush BackgroundBase
        {
            get { return (Brush)GetValue(BackgroundBaseProperty); }
            set { SetValue(BackgroundBaseProperty, value); }
        }
        public static readonly DependencyProperty BackgroundBaseProperty =
            DependencyProperty.Register("BackgroundBase", typeof(Brush), typeof(ToolEditBase), new PropertyMetadata(Brushes.Black));
        public bool IsDragging { get; set; } = false;
        public bool BitStatus
        {
            get => _bitStatus;
            set
            {
                _bitStatus = value;
                OnBitStatusChanged();
            }
        }
        public ToolEditBase()
        {
            InitializeComponent();
            TransFormCoordinate();
            CreateCm();
            btnLoadTool.Click += BtnLoadTool_Click;
            btnSaveTool.Click += BtnSaveTool_Click;
            btnRun.Click += BtnRun_Click;
            this.Loaded += ToolEditBase_Loaded;
            canvasImg.SizeChanged += (sender, e) => FitImage();

            ToolStatus = (Brush)new BrushConverter().ConvertFromString("#FFE90E0E");
            TimeRunText = string.Format("{0:F2}ms", 0);
        }
        protected void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private void CreateCm()
        {
            MenuItem deleteItem = new MenuItem
            {
                Name = "deleteItem",
                Header = "Delete",
                Background = Brushes.Transparent,
                Foreground = Brushes.Black,
            };
            deleteItem.Click += DeleteItem_Click;
            MenuItem createMatrixItem = new MenuItem
            {
                Name = "createMatrixItem",
                Header = "Create Matrix",
                Background = Brushes.Transparent,
                Foreground = Brushes.Black,
            };
            createMatrixItem.Click += CreateMatrixItem_Click;
            MenuItem propertyItem = new MenuItem
            {
                Name = "propertyItem",
                Header = "Property",
                Background = Brushes.Transparent,
                Foreground = Brushes.Black,
            };
            propertyItem.Click += PropertyRoi_Click;

            MnItems.Add(deleteItem);
            MnItems.Add(createMatrixItem);
            MnItems.Add(propertyItem);
        }
        private void PropertyRoi_Click(object sender, RoutedEventArgs e)
        {
            OnPropertyRoi?.Invoke(sender, e);
        }

        private void CreateMatrixItem_Click(object sender, RoutedEventArgs e)
        {
            OnMatrixRoi?.Invoke(sender, e);
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            OnDeleteRoi?.Invoke(sender, e);
        }
        private void ToolEditBase_Loaded(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = this.FindResource("cmRegion") as ContextMenu;
            GridBase gridBase = gridMain.Children.OfType<GridBase>().FirstOrDefault();   
            if(gridBase is TempMatchZeroEdit || gridBase is TemplateMatchEdit)
            {
                cm.Items.Clear();
                cm.Items.Add(MnItems[2]);
            }    
            else if(gridBase is EditRegionEdit)
            {
                cm.Items.Clear();
                foreach(var m in MnItems)
                {
                    cm.Items.Add(m);
                } 
            }     
        }
        private void BtnLoadTool_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select BMP Image",
                Filter = "All Images|*.bmp;*.jpg;*.jpeg;*.png|Bitmap Images (*.bmp)|*.bmp|JPEG Images (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG Images (*.png)|*.png",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                cbxImage.SelectedIndex = 0;
                Thread.Sleep(1);
                imgView.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                FitImage();
                isImgPath = true;
                OnLoadImage?.Invoke(sender, e);
            }
        }
        public void BtnSaveTool_Click(object sender, RoutedEventArgs e)
        {
            OnSaveTool?.Invoke(sender, e);
        }
        virtual protected void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            //this.Focus();
            //subject.GetInParams();
            //if (subject != null)
            //{
            //    subject.InitOutProperty();
            //    DateTime lastProcessTimeStart = DateTime.Now;
            //    subject.Run(true);
            //    subject.OnRan();
            //    subject.lastProcessTime = DateTime.Now.Subtract(lastProcessTimeStart).TotalMilliseconds;
            //}

            //UpdateStatus();


            // A.S.H subject_Ran에 추가되어서 제거
            //float oldZoom = svDisplayViewEdit.Display.ZoomRatio;
            //Point oldLocation = svDisplayViewEdit.Display.imageLocation;

            //svDisplayViewEdit.RefreshImage();

            //svDisplayViewEdit.Display.ZoomRatio = oldZoom;
            //svDisplayViewEdit.Display.imageLocation = oldLocation;
        }

        public void FitImage()
        {
            if (imgView == null || imgView.Source == null) return;

            double canvasWidth = gridImg.ActualWidth;
            double canvasHeight = gridImg.ActualHeight;
            double imageWidth = imgView.Source.Width;
            double imageHeight = imgView.Source.Height;

            double scaleX = canvasWidth / imageWidth;
            double scaleY = canvasHeight / imageHeight;
            double scale = Math.Min(scaleX, scaleY);

            // Đặt ScaleTransform
            scaleTrans.ScaleX = scale;
            scaleTrans.ScaleY = scale;

            // Căn giữa ảnh trong khung
            translateTrans.X = (canvasWidth - imageWidth * scale) / 2;
            translateTrans.Y = (canvasHeight - imageHeight * scale) / 2;

            string zoomItem = string.Format($"{(scaleTrans.ScaleY * 100).ToString("F2")}%");
            cbxZoom.Text = zoomItem;
        }
        private void TransFormCoordinate()
        {
            // Tạo một TransformGroup để kết hợp phóng to/thu nhỏ và di chuyển
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(scaleTrans);
            transformGroup.Children.Add(translateTrans);
            canvasImg.RenderTransform = transformGroup;
        }

        private void canvasImg_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IsDragging = true;
            lastMousePosition = e.GetPosition(gridImg);
            canvasImg.CaptureMouse();
        }

        private void canvasImg_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsDragging && Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Point currentPosition = e.GetPosition(gridImg);
                double offsetX = currentPosition.X - lastMousePosition.X;
                double offsetY = currentPosition.Y - lastMousePosition.Y;

                translateTrans.X += offsetX;
                translateTrans.Y += offsetY;

                lastMousePosition = currentPosition;
            }
            Point mousePosCanvas = e.GetPosition(canvasImg);
            lbCoordinate.Content = string.Format($"[X: {mousePosCanvas.X.ToString("F2")}, Y: {mousePosCanvas.Y.ToString("F2")}]");
        }

        private void canvasImg_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsDragging = false;
            canvasImg.ReleaseMouseCapture();
        }

        private void canvasImg_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Point mousePosition = e.GetPosition(gridImg);
                double zoomFactor = e.Delta > 0 ? 1.1 : (1 / 1.1);

                // Cập nhật giá trị ScaleX và ScaleY
                scaleTrans.ScaleX *= zoomFactor;
                scaleTrans.ScaleY *= zoomFactor;

                // Tính toán lại vị trí của ảnh dựa trên vị trí chuột
                translateTrans.X = (1 - zoomFactor) * (mousePosition.X) + zoomFactor * translateTrans.X;
                translateTrans.Y = (1 - zoomFactor) * (mousePosition.Y) + zoomFactor * translateTrans.Y;

                string zoomItem = string.Format($"{(scaleTrans.ScaleY * 100).ToString("F2")}%");
                cbxZoom.Text = zoomItem;
            }    
        }

        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            scaleTrans.ScaleX /= 1.1d;
            scaleTrans.ScaleY /= 1.1d;
        }

        private void btnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            scaleTrans.ScaleX *= 1.1d;
            scaleTrans.ScaleY *= 1.1d;
        }

        private void MnIt_FitImg_Click(object sender, RoutedEventArgs e)
        {
            // Fit the image source to Image
            FitImage();
        }
        private void MnIt_SaveImg_Click(object sender, RoutedEventArgs e)
        {
            // 1. Tạo hộp thoại lưu file
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp",
                Title = "Save an Image File",
                FileName = "Region Image"
            };
            // 2. Kiểm tra nếu người dùng đã chọn đường dẫn
            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                // 3. lấy ảnh từ Image
                Mat imageSave = (imgView.Source as BitmapSource).ToMat();
                if (imageSave == null) { imageSave = new Mat(300, 300, MatType.CV_8UC3, new Scalar(0, 255, 0)); }
                // 4. Lưu ảnh
                Cv2.ImWrite(filePath, imageSave);
            }
        }

        private void cbxZoom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbxZoom.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedValue = selectedItem.Content.ToString().TrimEnd('%');
                if (double.TryParse(selectedValue, out double scalePercentage))
                {
                    double scale = scalePercentage / 100.0;
                    scaleTrans.ScaleX = scale;
                    scaleTrans.ScaleY = scale;
                    translateTrans.X = 0;
                    translateTrans.Y = 0;
                }
            }
        }

        private void cbxZoom_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                string valueZoom = cbxZoom.Text;
                if (valueZoom.Contains('%'))
                {
                    valueZoom.Replace("%", "");
                }    
                if(IsNumberic(valueZoom))
                {
                    scaleTrans.ScaleX = Convert.ToDouble(valueZoom) / 100.0;
                    scaleTrans.ScaleY = Convert.ToDouble(valueZoom) / 100.0;
                    translateTrans.X = 0;
                    translateTrans.Y = 0;
                    cbxZoom.Text = $"{valueZoom}%";
                }    
            }    
        }
        private bool IsNumberic(string pText)
        {
            Regex regex = new Regex(@"^\d+(\.\d+)?$");
            return regex.IsMatch(pText);
        }
        public void SetLbTime(bool isSuccessed, long timeMs, string ErrMsg = "")
        {
            elipSttRun.Fill = isSuccessed ? (Brush)new BrushConverter().ConvertFromString("#FF00F838") : (Brush)new BrushConverter().ConvertFromString("#FFE90E0E");
            lbTimeRun.Content = string.Format("{0:F2}ms", timeMs);
            if (isSuccessed)
            {
                lbTimeRun.Content = string.Format("{0:F2}ms", timeMs);
            }
            else
            {
                lbTimeRun.Content = string.Format("{0:F2}ms", timeMs) + $" - {ErrMsg}";
            }
            BitStatus = isSuccessed;
        }
        public void OnBitStatusChanged()
        {
            BitStatusChanged?.Invoke(BitStatus, EventArgs.Empty);
        }

        private void ckbCentLine_Checked(object sender, RoutedEventArgs e)
        {
            Line lineX = new Line
            {
                Name = "lineX",
                Stroke = Brushes.Yellow,
                X1 = 0, Y1 = imgView.Source.Height / 2,
                X2 = imgView.Source.Width, Y2 = imgView.Source.Height / 2,
                StrokeThickness = imgView.Source.Height / 300,
            };
            canvasImg.Children.Add(lineX);
            Line lineY = new Line
            {
                Name = "lineY",
                Stroke = Brushes.Yellow,
                X1 = imgView.Source.Width / 2, Y1 = 0, 
                X2 = imgView.Source.Width / 2, Y2 = imgView.Source.Height,
                StrokeThickness = imgView.Source.Height / 300,
            };
            
            canvasImg.Children.Add(lineY);
        }
        private void ckbCentLine_Unchecked(object sender, RoutedEventArgs e)
        {
            List<Line> linesDelete = new List<Line>();    
            for (int i = 0; i < canvasImg.Children.Count; i++)
            {
                Line a = canvasImg.Children[i] as Line;
                if (a != null && (a.Name == "lineX" || a.Name == "lineY"))
                {
                    canvasImg.Children.RemoveRange(i, 2);
                }
            }
        }
        
    }
}
