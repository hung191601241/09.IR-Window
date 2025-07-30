using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.XFeatures2D;
using VisionInspection;
using Xceed.Wpf.AvalonDock.Controls;
using Xceed.Wpf.Toolkit;
using Point = System.Windows.Point;
using Rect = OpenCvSharp.Rect;
using System.Threading;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for TemplateMatchEdit.xaml
    /// </summary>
    public partial class TemplateMatchEdit : GridBase, INotifyPropertyChanged
    {
        private MyLogger logger = new MyLogger("TemplateMatch Edit");
        ContextMenu contextMenu = new ContextMenu();

        //Variable
        ShapeEditor shapeEditor = new ShapeEditor(UiManager.appSettings.Property.rectSize.Width, UiManager.appSettings.Property.labelFontSize);
        Rectangle rect = new Rectangle();
        private Mat mTransform2D;
        public List<PatternData> mPatternDataList = new List<PatternData>();
        List<UIElement> trainEle = new List<UIElement>();
        List<UIElement> outEle = new List<UIElement>();
        private Brush colorRectFill = (Brush)new BrushConverter().ConvertFromString("#40DC143C");
        private Brush colorRectStroke = (Brush)new BrushConverter().ConvertFromString("#DC143C");
        public Point3d cpPattern = new Point3d();
        public event RoutedEventHandler OnBtnRunClicked;

        //In/Out
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
        public double OutScore = 0;
        public double OutTranslateX = 0;
        public double OutTranslateY = 0;
        public double OutRotation = 0;

        //Binding
        public event PropertyChangedEventHandler PropertyChanged;
        public enum Priority { None, Left, Top, Right, Bottom }
        public Array Priorities => Enum.GetValues(typeof(Priority));
        private double _scaleFirst, _scaleLast, _degMin, _degMax, _firstStep, _precision, _priorityCreteria, _tempScaleMin, _tempScaleMax;
        private int _maxCount;
        private bool _isUseEdge, _isAutoMatchPara;
        private Priority _selectedPriority;
        public double ScaleFirst { get => _scaleFirst; set { _scaleFirst = value; OnPropertyChanged(nameof(ScaleFirst)); } }
        public double ScaleLast { get => _scaleLast; set { _scaleLast = value; OnPropertyChanged(nameof(ScaleLast)); } }
        public double DegMin { get => _degMin; set { _degMin = value; OnPropertyChanged(nameof(DegMin)); } }
        public double DegMax { get => _degMax; set { _degMax = value; OnPropertyChanged(nameof(DegMax)); } }
        public double FirstStep { get => _firstStep; set { _firstStep = value; OnPropertyChanged(nameof(FirstStep)); } }
        public double Precision { get => _precision; set { _precision = value; OnPropertyChanged(nameof(Precision)); } }
        public double PriorityCreteria { get => _priorityCreteria; set { _priorityCreteria = value; OnPropertyChanged(nameof(PriorityCreteria)); } }
        public int MaxCount { get => _maxCount; set { _maxCount = value; OnPropertyChanged(nameof(MaxCount)); } }
        public bool IsUseEdge { get => _isUseEdge; set { _isUseEdge = value; OnPropertyChanged(nameof(IsUseEdge)); } }
        public bool IsAutoMatchPara { get => _isAutoMatchPara; set { _isAutoMatchPara = value; OnPropertyChanged(nameof(IsAutoMatchPara)); } }
        public double TempScaleMin { get => _tempScaleMin; set { _tempScaleMin = value; OnPropertyChanged(nameof(TempScaleMin)); } }
        public double TempScaleMax { get => _tempScaleMax; set { _tempScaleMax = value; OnPropertyChanged(nameof(TempScaleMax)); } }
        public Priority SelectedPriority { get => _selectedPriority; set { _selectedPriority = value; OnPropertyChanged(nameof(SelectedPriority)); } }

        public Mat Transform2D
        {
            get
            {
                if (mTransform2D == null)
                    mTransform2D = Mat.Eye(3, 3, MatType.CV_32SC1);
                return mTransform2D;
            }
            set
            {

                if (mTransform2D != null) mTransform2D.Dispose();

                if (value == null)
                    mTransform2D = Mat.Eye(3, 3, MatType.CV_64FC1);
                else
                    mTransform2D = value.Clone();
            }
        }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public TemplateMatchEdit()
        {
            InitializeComponent();
            DisplayInit();
            RegisterEvent();
            toolBase.DataContext = this; // Set the DataContext

            //InputImage = new SvImage();
            //refPoint = new SvPoint();

            m_desROI = new Mat();
            ScaleFirst = 10;
            ScaleLast = 2;
            DegMin = 20;
            DegMax = 20;
            PriorityCreteria = 0.75;
            Precision = 0.02;
            FirstStep = 5;
            MaxCount = 1;
            SelectedPriority = Priority.None;
            TempScaleMin = 1;
            TempScaleMax = 1;

            //thLow = 50;
            //thHigh = 150;
            IsAutoMatchPara = true;
            InitOutProperty();
        }
        protected override void RegisterEvent()
        {
            toolBase.cbxImage.SelectionChanged += CbxImage_SelectionChanged;
            ImgView.MouseLeftButtonDown += ImgView_MouseLeftButtonDown;
            ImgView.MouseLeave += ImgView_MouseLeave;
            toolBase.btnRun.Click += BtnRun_Click;
            toolBase.OnLoadImage += ToolBase_OnLoadImage;
            toolBase.OnPropertyRoi += ToolBase_OnPropertyRoi;
        }

        private void ToolBase_OnLoadImage(object sender, RoutedEventArgs e)
        {
            toolBase.cbxImage.SelectedIndex = 0;
            oldSelect = 0;
        }

        public void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            OnBtnRunClicked?.Invoke(sender, e);
        }

        private void BtnMaskOn_Click(object sender, RoutedEventArgs e)
        {
            //// 1. Tạo hộp thoại lưu file
            //OpenFileDialog openFileDialog = new OpenFileDialog
            //{
            //    Filter = "Bitmap Image|*.bmp|PNG Image|*.png|JPEG Image|*.jpg",
            //    Title = "Load an Image File",
            //    Multiselect = false,
            //};
            //// 2. Kiểm tra nếu người dùng đã chọn đường dẫn
            //if (openFileDialog.ShowDialog() == true)
            //{
            //    toolBase.imgView.Source = new BitmapImage(new Uri(openFileDialog.FileName));
            //}
        }

        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "Template Match";
            toolBase.cbxImage.Items.Add("[Template Match] Input Image");
            toolBase.cbxImage.Items.Add("[Template Match] Train Image");
            toolBase.cbxImage.Items.Add("[Template Match] Output Image");
            toolBase.cbxImage.SelectedIndex = 0;
            toolBase.cbxImage.Focusable = false;

            Grid parent = toolBase.gridBase.Parent as Grid;
            parent.Children.Add(this);
            parent.Children.Remove(toolBase.gridBase);
            try
            {
                CreatRect(200, 200, 200, 200, 0, colorRectStroke, colorRectFill, "Train", trainEle);

                TabControl tabControl = this.Children.OfType<TabControl>().FirstOrDefault();
                contextMenu = tabControl.FindResource("cmRegion") as ContextMenu;

            }
            catch (Exception ex)
            {
                logger.Create("Display Init Error: " + ex.Message, ex);
            }
        }

        public void InitOutProperty()
        {
            //lastRunSuccess = false;
            Score = 0.0;
            Result = new Point3d(0, 0, 0);
            ResultCP = new Point3d(0, 0, 0);
            ResultBox = null;
            failResultBox = null;
            m_result = new Point3d(0, 0, 0);

            if (FoundImage != null) FoundImage.Dispose();
            FoundImage = null;

            listResultBox = new List<FoundResult>();
            m_tempScaleXOld = 0;
            m_tempScaleYOld = 0;
            if (IsUseEdge)
                MatchMethod = TemplateMatchModes.CCoeffNormed;
            else
                MatchMethod = TemplateMatchModes.CCoeffNormed;
            m_SearchScale = 0;
            //GetOutParams();
        }

        private void ImgView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            shapeEditor?.ReleaseElement();
        }

        private void ImgView_MouseLeave(object sender, MouseEventArgs e)
        {
            Point mouse = e.GetPosition(ImgView);
            if (mouse.X < 0 || mouse.X > ImgView.ActualWidth || mouse.Y < 0 || mouse.Y > ImgView.ActualHeight)
            {
                shapeEditor.ReleaseElement();
                shapeEditor.KeyDown -= ShapeEditor_KeyDown;
                shapeEditor.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
            }
        }

        private void CreatRect(float left, float top, float width, float height, float angle, Brush stroke, Brush Fill, string name, List<UIElement> eleLst)
        {
            try
            {
                var rect = new System.Windows.Shapes.Rectangle()
                {
                    Width = width,
                    Height = height,
                    Stroke = stroke,
                    Fill = Fill,
                    Name = name,
                    StrokeThickness = UiManager.appSettings.Property.StrokeThickness,
                    RenderTransformOrigin = new System.Windows.Point(0.5, 0.5),
                    RenderTransform = new RotateTransform(angle),
                };
                rect.MouseLeftButtonDown += Rect_MouseLeftButtonDown;
                Canvas.SetLeft(rect, left);
                Canvas.SetTop(rect, top);
                eleLst.Add(rect);
                if (oldSelect == 1) { CanvasImg.Children.Add(rect); }
            }
            catch (Exception ex)
            {
                logger.Create("Create Rect Error: " + ex.Message, ex);
            }
        }

        private void ToolBase_OnPropertyRoi(object sender, RoutedEventArgs e)
        {
            var Point = Mouse.GetPosition(toolBase);
            new RegionProperty().DoConfirmMatrix(new System.Windows.Point(Point.X, Point.Y - 200));
            UpdateProperty();
        }
        private void UpdateProperty()
        {
            shapeEditor.ReleaseElement();
            shapeEditor.KeyDown -= ShapeEditor_KeyDown;
            shapeEditor.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
            while (CanvasImg.Children.Count > 1)
            {
                CanvasImg.Children.RemoveAt(1);
            }
            if (rect == null)
                return;
            rect.Name = "Train";
            var converter = new BrushConverter();
            RotateTransform rotTrans = rect.RenderTransform as RotateTransform ?? new RotateTransform(0);
            CreatRect((float)Canvas.GetLeft(rect), (float)Canvas.GetTop(rect), (float)rect.Width, (float)rect.Height, (float)rotTrans.Angle, colorRectStroke, colorRectFill, rect.Name, trainEle);
        }
        public Mat GetROIRegion(Mat image)
        {
            try
            {
                if (image == null || image.IsDisposed) return null;
                if (oldSelect != 1) return null;
                Rectangle roiRectangle = CanvasImg.Children
                                                    .OfType<Rectangle>()
                                                    .FirstOrDefault(r => r.Name == "Train");
                if (roiRectangle == null)
                {
                    if (shapeEditor == null) return null;
                    this.rect = shapeEditor.rCover;
                    this.rect.RenderTransform = shapeEditor.RenderTransform;
                }
                var rotTrans = (RotateTransform)this.rect.RenderTransform;
                double rotAngle = (rotTrans != null) ? rotTrans.Angle : 0d;

                // Tính tọa độ 4 góc trước khi xoay
                Point pLT = new Point(Canvas.GetLeft(roiRectangle), Canvas.GetTop(roiRectangle));
                Point centerPoint = new Point(pLT.X + this.rect.ActualWidth / 2, pLT.Y + this.rect.ActualHeight / 2);
                Point pRB = new Point(centerPoint.X + (this.rect.ActualWidth / 2), centerPoint.Y + (this.rect.ActualHeight / 2));
                Point pLB = new Point(pLT.X, pRB.Y);
                Point pRT = new Point(pRB.X, pLT.Y);
                // Tính tọa độ 4 góc sau khi xoay
                Point pLTr = RotatePoint(pLT, centerPoint, rotAngle);
                Point pRBr = RotatePoint(pRB, centerPoint, rotAngle);
                Point pLBr = RotatePoint(pLB, centerPoint, rotAngle);
                Point pRTr = RotatePoint(pRT, centerPoint, rotAngle);

                Point2d leftTop = new Point2d(pLTr.X, pLTr.Y);
                Point2d rightTop = new Point2d(pRTr.X, pRTr.Y);
                Point2d leftBottom = new Point2d(pLBr.X, pLBr.Y);
                Point2f center = new Point2f((float)pLTr.X, (float)pLTr.Y);

                double T = Math.Atan2(rightTop.Y - leftTop.Y, rightTop.X - leftTop.X);
                double H = Point2d.Distance(leftTop, rightTop);
                double V = Point2d.Distance(leftTop, leftBottom);

                Mat Rmat = Cv2.GetRotationMatrix2D(center, T * 180 / Math.PI, 1);
                Rmat.Set<double>(0, 2, Rmat.At<double>(0, 2) - center.X);
                Rmat.Set<double>(1, 2, Rmat.At<double>(1, 2) - center.Y);

                OpenCvSharp.Rect rect = new OpenCvSharp.Rect((int)center.X, (int)center.Y, (int)H, (int)V);

                Mat templateImg = new Mat(rect.Size, image.Type());
                //Func.ShowTestImg(mask, 0.25);
                Cv2.WarpAffine(image, templateImg, Rmat, rect.Size);
                if (templateImg.Channels() > 3)
                {
                    Cv2.CvtColor(templateImg, templateImg, ColorConversionCodes.BGR2RGB);
                    Cv2.CvtColor(templateImg, templateImg, ColorConversionCodes.RGB2BGR);
                }
                //Cv2.Threshold(templateImg, templateImg, this.model.Threshol, 255, ThresholdTypes.Binary);
                Rmat.Dispose();

                if (templateImg.Cols == 0 || templateImg.Rows == 0)
                {
                    templateImg.Dispose();
                    return null;
                }
                //templateImg.SaveImage("roiTestHung.png");
                return templateImg;
            }
            catch (Exception ex)
            {
                logger.Create("Get ROI Error: " + ex.Message, ex);
                return null;
            }
        }
        private Point RotatePoint(Point pointToRot, Point centerPoint, double angleInDeg)
        {
            try
            {
                double angleInRad = angleInDeg * (Math.PI / 180.0d);
                double cosTheta = Math.Cos(angleInRad);
                double sinTheta = Math.Sin(angleInRad);
                //Tính góc xoay của điểm
                double X = cosTheta * (pointToRot.X - centerPoint.X) - sinTheta * (pointToRot.Y - centerPoint.Y) + centerPoint.X;
                double Y = sinTheta * (pointToRot.X - centerPoint.X) + cosTheta * (pointToRot.Y - centerPoint.Y) + centerPoint.Y;
                return new Point(X, Y);
            }
            catch (Exception ex)
            {
                logger.Create("Rotate Point Error: " + ex.Message, ex);
                return new Point();
            }
        }

        private void Rect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle senderRect = sender as Rectangle;
            this.shapeEditor = new ShapeEditor(UiManager.appSettings.Property.rectSize.Width, UiManager.appSettings.Property.labelFontSize)
            {
                //rectSize = (double)UiManager.appSettings.Property.rectSize.Width,
                //rectSize = 40,
                Name = "ShETrain",
                Focusable = true,
                IsMulSelect = false,
            };
            //Clear ShapeEditor cũ cùng tên
            foreach (var element in CanvasImg.Children)
            {
                ShapeEditor a = element as ShapeEditor;
                if (a != null && a.Name == "ShETrain")
                {
                    CanvasImg.Children.Remove(a);
                    break;
                }
            }
            rect = senderRect;
            CanvasImg.Children.Add(this.shapeEditor);
            this.shapeEditor.KeyDown += ShapeEditor_KeyDown;
            this.shapeEditor.LostKeyboardFocus += ShapeEditor_LostKeyboardFocus; ;
            this.shapeEditor.CaptureElement(senderRect, e);
            this.shapeEditor.Focus();
        }

        private void ShapeEditor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            //Xác định đang có ít nhất 1 shapeEdtor được tác động và không có cửa sổ ContextMenu cmRegion được mở
            if (!contextMenu.IsOpen)
            {
                this.shapeEditor.Focus();
            }
        }

        private void ShapeEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down)
            {
                MoveRoiByKb(e);
            }
        }
        private void CbxImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (toolBase.cbxImage.SelectedIndex == 0)
                {
                    switch (oldSelect)
                    {
                        case 1:
                            trainEle.RemoveRange(0, trainEle.Count);
                            for (int i = 1; i < CanvasImg.Children.Count; i++)
                            {
                                trainEle.Add(CanvasImg.Children[i]);
                            }
                            break;
                        case 2:
                            outEle.RemoveRange(0, outEle.Count);
                            for (int i = 1; i < CanvasImg.Children.Count; i++)
                            {
                                outEle.Add(CanvasImg.Children[i]);
                            }
                            break;
                    }
                    CanvasImg.Children.RemoveRange(1, CanvasImg.Children.Count - 1);
                    oldSelect = 0;
                }
                else if (toolBase.cbxImage.SelectedIndex == 1)
                {
                    switch (oldSelect)
                    {
                        case 2:
                            outEle.RemoveRange(0, outEle.Count);
                            for (int i = 1; i < CanvasImg.Children.Count; i++)
                            {
                                outEle.Add(CanvasImg.Children[i]);
                            }
                            CanvasImg.Children.RemoveRange(1, CanvasImg.Children.Count - 1);
                            break;
                    }
                    foreach (var ele in trainEle)
                    {
                        CanvasImg.Children.Add(ele);
                    }
                    oldSelect = 1;
                }
                else if (toolBase.cbxImage.SelectedIndex == 2)
                {
                    switch (oldSelect)
                    {
                        case 1:
                            trainEle.RemoveRange(0, trainEle.Count);
                            for (int i = 1; i < CanvasImg.Children.Count; i++)
                            {
                                trainEle.Add(CanvasImg.Children[i]);
                            }
                            CanvasImg.Children.RemoveRange(1, CanvasImg.Children.Count - 1);
                            break;
                    }
                    foreach (var ele in outEle)
                    {
                        CanvasImg.Children.Add(ele);
                    }
                    oldSelect = 2;
                }
            }
            catch (Exception ex)
            {
                logger.Create("Cbx Image Error: " + ex.Message, ex);
            }
        }

        public void FitMasterImage()
        {
            try
            {
                if (masterImg.Source == null) return;

                double canvasWidth = masterCanvas.Width;
                double canvasHeight = masterCanvas.Height;
                double imageWidth = masterImg.Source.Width;
                double imageHeight = masterImg.Source.Height;

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
                masterImg.RenderTransform = transformGroup;
            }
            catch (Exception ex)
            {
                logger.Create("Fit Master Error: " + ex.Message, ex);
            }
        }
        private void MoveRoiByKb(KeyEventArgs e)
        {
            try
            {
                //Kiểm tra xem có đang chọn vào ROI nào không
                if (toolBase.cbxImage.SelectedIndex == 1)
                {
                    switch (e.Key)
                    {
                        case Key.Left:
                            Canvas.SetLeft(shapeEditor, Canvas.GetLeft(shapeEditor) - 2);
                            break;
                        case Key.Right:
                            Canvas.SetLeft(shapeEditor, Canvas.GetLeft(shapeEditor) + 2);
                            break;
                        case Key.Up:
                            Canvas.SetTop(shapeEditor, Canvas.GetTop(shapeEditor) - 2);
                            break;
                        case Key.Down:
                            Canvas.SetTop(shapeEditor, Canvas.GetTop(shapeEditor) + 2);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Move ROI Error: " + ex.Message, ex);
            }
        }

        private void BtnGrabMaster_Click(object sender, RoutedEventArgs e)
        {
            shapeEditor.ReleaseElement();
            shapeEditor.KeyDown -= ShapeEditor_KeyDown;
            shapeEditor.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;

            try
            {
                BitmapSource bmpImage = toolBase.imgView.Source as BitmapSource;
                Mat templateImg = GetROIRegion(bmpImage.ToMat());
                if (templateImg == null)
                {
                    //MessageBox.Show("선택된 이미지 영역이 없습니다.");
                    System.Windows.MessageBox.Show("There does not select image.");
                    return;
                }
                if (this.rect == null)
                {

                    if (shapeEditor == null) return;
                    this.rect = shapeEditor.rCover;
                    this.rect.RenderTransform = shapeEditor.RenderTransform;
                }
                RotateTransform rotTrans = rect.RenderTransform as RotateTransform ?? new RotateTransform(0);
                double angleRad = (rotTrans.Angle * Math.PI) / 180;

                Point cpPM = rect.TransformToAncestor(CanvasImg).Transform(new Point(templateImg.Width / 2, templateImg.Height / 2));
                cpPattern = new Point3d(cpPM.X, cpPM.Y, angleRad);

                SvPoint cpTempImg = new SvPoint(templateImg.Width / 2, templateImg.Height / 2, angleRad);
                //Save
                PatternData patterndata = new PatternData(new SvImage(templateImg), null, cpTempImg);
                mPatternDataList.Clear();
                mPatternDataList.Insert(0, patterndata);

                masterImg.Source = templateImg.ToBitmapSource();
                FitMasterImage();
                ShapeEditor roi = CanvasImg.Children.OfType<ShapeEditor>().FirstOrDefault();
                if (roi != null) { roi.ReleaseElement(); }
            }
            catch (Exception ex)
            {
                logger.Create("Button Grab Master Error: " + ex.Message, ex);
            }
        }

        public void SavePattentImage(Mat img, string fileName)
        {
            try
            {
                //var fileName = String.Format("{0}Template.png", this.cbxCameraCh.SelectedValue.ToString());
                var folder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image\\Template");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                var filePath = System.IO.Path.Combine(folder, fileName);
                img.SaveImage(filePath);
            }
            catch (Exception ex)
            {
                logger.Create("Save Pattern Image Error: " + ex.Message, ex);
            }
        }
        public Mat LoadPatternImage(string fileName)
        {
            try
            {
                var folder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image\\Template");
                if (!Directory.Exists(folder))
                {
                    return new Mat();
                }
                var filePath = System.IO.Path.Combine(folder, fileName);
                if (!File.Exists(filePath))
                {
                    return new Mat();
                }
                return Cv2.ImRead(filePath, ImreadModes.Color);
            }
            catch (Exception ex)
            {
                logger.Create("Load Pattern Image Error: " + ex.Message, ex);
                return new Mat(0,0, new MatType());
            }
            
        }
        private Line CreateLineThroughCenter(Vector dir, double cx, double cy, double width, double height)
        {
            try
            {
                List<Point> intersections = new List<Point>
                {
                    // Các biên canvas
                    // Left Edge (x=0)
                    IntersectWithVerLine(0, dir, new Point(cx, cy)),
                    // Right Edge (x=width)
                    IntersectWithVerLine(width, dir, new Point(cx, cy)),
                    // Top Edge (y=0)
                    IntersectWithHorLine(0, dir, new Point(cx, cy)),
                    // Bottom Edge (y=height)
                    IntersectWithHorLine(height, dir, new Point(cx, cy))
                };
                // Lọc ra các điểm thực sự nằm trong canvas
                intersections = intersections.FindAll(p => p.X >= 0 && p.X <= width && p.Y >= 0 && p.Y <= height);

                // Chỉ cần 2 điểm hợp lệ
                if (intersections.Count >= 2)
                {
                    return new Line
                    {
                        X1 = intersections[0].X,
                        Y1 = intersections[0].Y,
                        X2 = intersections[1].X,
                        Y2 = intersections[1].Y,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection() { 4, 2 }, // 4px nét, 2px hở
                    };
                }
                else
                    return new Line();
            }
            catch (Exception ex)
            {
                logger.Create("Create Line ThroughCenter Error: " + ex.Message, ex);
                return new Line();
            }
        }
        private static Point IntersectWithVerLine(double x, Vector dir, Point center)
        {
            // dir.X * t + center.X = x
            double t = (x - center.X) / dir.X;
            double y = center.Y + dir.Y * t;
            return new Point(x, y);
        }

        private static Point IntersectWithHorLine(double y, Vector dir, Point center)
        {
            // dir.Y * t + center.Y = y
            double t = (y - center.Y) / dir.Y;
            double x = center.X + dir.X * t;
            return new Point(x, y);
        }

        [NonSerialized]
        Mat m_sclSrc;
        [NonSerialized]
        Mat m_sclTemp;
        [NonSerialized]
        Mat m_scaledMask;
        [NonSerialized]
        List<Mat> m_listC = new List<Mat>();
        double m_tempScaleX = 0;
        double m_tempScaleY = 0;
        double m_tempScaleXOld = 0;
        double m_tempScaleYOld = 0;

        [OutputParam]
        public Point3d Result
        {
            get { return m_result; }
            set
            {
                m_result = value;
            }
        }

        [OutputParam]
        public SvPoint SvPoint
        {
            get
            {
                if (m_result == null) return null;
                SvPoint result = new SvPoint(m_result.X, m_result.Y)
                {
                    ThetaRad = m_result.Z
                };
                return result;
            }
            set
            {

            }
        }
        [NonSerialized]
        Point3d m_result = new Point3d(0, 0, 0);
        //List<Point3d> listResultCP;

        [OutputParam]
        public Point3d ResultCP
        {
            get { return resultCP; }
            set
            {
                resultCP = value;
            }
        }
        [NonSerialized]
        Point3d resultCP = new Point3d(0, 0, 0);

        [NonSerialized]
        double m_score = 0;
        [OutputParam]
        public double Score
        {
            get { return m_score; }
            set
            {
                m_score = value;
            }
        }

        [NonSerialized]
        Point2d[] m_resultBox;
        [NonSerialized]
        List<FoundResult> listResultBox;

        public FoundResult failResultBox;
        SvImage m_foundImage;
        [OutputParam]
        public SvImage FoundImage
        {
            get { return m_foundImage; }
            set
            {
                if (value == null) return;
                m_foundImage?.Dispose();
                m_foundImage = value.Clone(true);
            }
        }
        [OutputParam]
        public Point2d[] ResultBox
        {
            get { return m_resultBox; }
            set { m_resultBox = value; }
        }
        [NonSerialized]
        double m_SearchScale = 0;
        [NonSerialized]
        int m_iteration;
        [NonSerialized]
        Mat m_desROI;
        Rect m_ROIrect;
        byte[] lut;
        SvMask mask;
        SvImage tempImg;
        [OutputParam]
        public SvImage TemplateImage
        {
            get
            {
                return tempImg;
            }
            set
            {
                if (value == null) return;

                // 이미지 형식은 1채널로 통일
                SvImage temp = new SvImage(new Mat(value.Mat.Size(), MatType.CV_8UC1));
                if (value.Mat.Type() == MatType.CV_8UC3)
                    Cv2.CvtColor(value.Mat, temp.Mat, ColorConversionCodes.RGB2GRAY);
                else if (value.Mat.Type() == MatType.CV_8UC1)
                    temp = value.Clone(true);
                else
                    return;

                if (tempImg != null)
                    tempImg.Dispose();

                tempImg = temp;
                mBorderColor = ((double)tempImg.Mat.Mean() + 255 / 2) % 255;
            }
        }
        public SvMask Mask { get { return mask; } set { mask = value; } }
        double mBorderColor;
        double BorderColor { get { if (IsUseEdge) return 0; else return mBorderColor; } }


        OpenCvSharp.TemplateMatchModes MatchMethod = TemplateMatchModes.CCoeffNormed;

        public double RotTempMatching(Mat _src, Mat _temp, Rect _roi, double _degStart, double _degRange, double _degStep,
            double _searchscale, out Point3d _resultcp, out Point2d[] _resultbox, double _margin = 1.5, bool _maskedscore = false)
        {
            _resultcp = new Point3d();
            _resultbox = new Point2d[4];
            _degStep = Math.Round(_degStep, 10);

            try
            {
                if (_searchscale == 0) return -1;
                if (m_tempScaleX == 0 || m_tempScaleY == 0) return -1;

                // 이조건은 왜있는거임... ㅡ,.ㅡ A.S.H 삭제 17.10.10
                //if (src.Width < templROI.Right + 1 || src.Height < templROI.Bottom + 1) return -1;
                if (_roi.Width < 0 || _roi.Height < 0) return -1;
                if (double.IsNaN(_degStart) || double.IsNaN(_degRange) || double.IsNaN(_degStep)) return -1;

                // Original Rect, Size
                Point2d ROICP = (_roi.TopLeft + _roi.BottomRight) * 0.5;
                Point2d ScROICP = ROICP * _searchscale;

                Point3d matchingPoint = new Point3d(0.0, 0.0, -_degStart);
                double matchingScore = -1.0f;
                double ellipsS = 0.4;

                Rect ScSrcRotRect;
                Mat ScSrcRot;


                // Template Image Scaling
                if (m_tempScaleX != m_tempScaleXOld || m_tempScaleY != m_tempScaleYOld || _searchscale != m_SearchScale)
                {
                    m_tempScaleXOld = m_tempScaleX;
                    m_tempScaleYOld = m_tempScaleY;
                    int StempW = (int)Math.Round(_temp.Width * _searchscale * m_tempScaleX);
                    int StempH = (int)Math.Round(_temp.Height * _searchscale * m_tempScaleY);
                    if (StempH < 0 || StempW < 0) return -1;

                    OpenCvSharp.Size tempsize = new OpenCvSharp.Size(StempW, StempH);
                    m_sclTemp?.Dispose();
                    m_sclTemp = new Mat(_temp.Size(), MatType.CV_8UC4);

                    Cv2.Resize(_temp, m_sclTemp, tempsize, 0, 0, InterpolationFlags.Linear);

                    if (IsUseEdge)
                    {
                        m_scaledMask?.Dispose();
                        m_scaledMask = new Mat();

                        // EdgeDetect 적용
                        // 정밀도에 있어서는 잘 모르겠으나, 속도향상을 위해 여기에 구현
                        int minBlobsize = Math.Min(m_sclTemp.Width, m_sclTemp.Height);
                        GetEdge(m_sclTemp, minBlobsize, out m_sclTemp, _searchscale);

                        /////////////////// Mask 적용(Edge 일때만)
                        if (mask.MaskMat != null && Mask.MaskMat.Width != 0 && Mask.MaskMat.Height != 0 && !mask.MaskMat.IsDisposed)
                        {
                            Mat maskedtmp = new Mat(m_sclTemp.Size(), m_sclTemp.Type(), new Scalar(0));
                            Cv2.Resize(mask.MaskMat, m_scaledMask, m_sclTemp.Size());
                            m_sclTemp.CopyTo(maskedtmp, m_scaledMask);
                            m_sclTemp?.Dispose();
                            m_sclTemp = maskedtmp;//.Clone();
                                                  //SvFunc.ShowTestImg(scaledtemp, 1);
                        }
                    }
                }

                // Source Image Scaling
                if (m_SearchScale != _searchscale)
                {
                    m_SearchScale = _searchscale;
                    m_sclSrc?.Dispose();
                    m_sclSrc = new Mat();

                    Cv2.Resize(_src, m_sclSrc, SvFunc.ResizeSize(_src.Size(), _searchscale), 0, 0, InterpolationFlags.Linear);

                    if (IsUseEdge)
                    {
                        // EdgeDetect 적용
                        // 정밀도에 있어서는 잘 모르겠으나, 속도향상을 위해 여기에 구현
                        int minBlobsize = Math.Min(m_sclTemp.Width, m_sclTemp.Height);
                        GetEdge(m_sclSrc, minBlobsize, out m_sclSrc, _searchscale);
                        //masktmp.Dispose();
                    }
                }
                //int scaledSrcWidth = Cv.Round(sclK * Math.Max(Cv.Round(src_ROI.Width * scale), Cv.Round(src_ROI.Height * scale)));
                double Trad = _degRange / 2 * Math.PI / 180;
                Point2d righttop = new Point2d(Math.Max(_roi.Width / 2, _temp.Width * m_tempScaleX / 2), Math.Max(_roi.Height / 2, _temp.Height * m_tempScaleY / 2));
                double scaledSrcW = SvFunc.Rotate(righttop, -Trad).X * 2 * _searchscale * _margin;
                double scaledSrcH = SvFunc.Rotate(righttop, Trad).Y * 2 * _searchscale * _margin;

                ScSrcRotRect = new Rect
                {
                    X = (int)Math.Round(ScROICP.X - scaledSrcW / 2),
                    Y = (int)Math.Round(ScROICP.Y - scaledSrcH / 2),
                    Size = new OpenCvSharp.Size(scaledSrcW, scaledSrcH)
                };
                ScSrcRot = new Mat(ScSrcRotRect.Size, _src.Type());

                //Mat matTrans = new Mat(2, 3, MatType.CV_64F, new double[] { 1, 0, -ScSrcRotRect.X, 0, 1, -ScSrcRotRect.Y });
                Mat matTransScale = new Mat(2, 3, MatType.CV_64F, new double[]
                {
                _searchscale, 0, -ScSrcRotRect.X,
                0, _searchscale, -ScSrcRotRect.Y
                });

                if (_degStep <= 0)
                {
                    _degRange = 0;
                    _degStep = 1;
                }
                //Nếu _degStep > 0, thì chỉnh _degRange sao cho nó chia hết cho _degStep.
                else
                    _degRange -= 2 * ((_degRange / 2) % _degStep);

                int maxAngleIdx = 0;


                bool retry = true;
                // MatchTemplate
                double min = 0, max = 0;
                OpenCvSharp.Point minPoint = new OpenCvSharp.Point();
                OpenCvSharp.Point maxPoint = new OpenCvSharp.Point();

                List<Point2d> listAngleToScore = new List<Point2d>();
                List<Point2d> PrePoints = new List<Point2d>();

                listResultBox.ForEach(x => PrePoints.Add(new Point2d(x.CP.X * _searchscale, x.CP.Y * _searchscale)));

                Mat RotateMat = new Mat();
                //Mat TransScaleMat = new Mat(3, 3, MatType.CV_64F, new double[] 
                //{ 
                //    matTransScale.At<double>(0, 0), matTransScale.At<double>(0, 1), matTransScale.At<double>(0, 2),
                //    matTransScale.At<double>(1, 0), matTransScale.At<double>(1, 1), matTransScale.At<double>(1, 2),
                //    0, 0, 1
                //});

                //// ScSrcRot를 회전시키면서 가장 높은 Score의 위치, 각도를 찾는다.
                //// Image는 Pattern 결과값과 반대이므로 desStart는 음수가 맞다...
                Mat C = null;
                Mat Ctemp = null;

                int anglecount = 0;

                double startA = Math.Round(-_degRange / 2 - _degStart, 10);
                double endA = Math.Round(_degRange / 2 - _degStart, 10);

                for (double angle = startA; angle <= endA; angle += _degStep, anglecount++)
                {
                    if (double.IsInfinity(angle) || double.IsNaN(angle))
                        return 0;
                    RotateMat = Cv2.GetRotationMatrix2D(new Point2f((float)ScROICP.X, (float)ScROICP.Y), -angle, 1);
                    RotateMat.Set<double>(0, 2, RotateMat.At<double>(0, 2) - ScSrcRotRect.X);
                    RotateMat.Set<double>(1, 2, RotateMat.At<double>(1, 2) - ScSrcRotRect.Y);

                    Cv2.WarpAffine(m_sclSrc, ScSrcRot, RotateMat, ScSrcRot.Size(), InterpolationFlags.Linear, BorderTypes.Constant, BorderColor);
                    OpenCvSharp.Point ellipsP;

                    if (m_iteration == 0) // 전체 찾기할 경우
                    {
                        if (m_listC.Count - 1 < anglecount) // 한번도 찾지 않았을 경우, 궂이 count로 측정하는 이유는 이전에 찾을때 score가 높아서 도중에 탈출하면 m_listC가 다 없을수도 있어서...
                        {
                            C = new Mat(ScSrcRot.Size(), MatType.CV_8UC1);
                            SvFunc.StartWatch("TemplateMatch Cycle Time: ");
                            //Cv2.MatchTemplate(ScSrcRot, m_sclTemp, C, MatchMethod);
                            C = ScSrcRot.MatchTemplate(m_sclTemp, MatchMethod);

                            SvFunc.StopWatch();
                            for (int i = 0; i < PrePoints.Count; i++)
                            {
                                Point3d cp3d = SvFunc.MattoP(RotateMat/* * TransScaleMat*/ * SvFunc.PtoMat(new Point3d(PrePoints[i].X, PrePoints[i].Y, 1)), true);
                                ellipsP = new OpenCvSharp.Point(cp3d.X, cp3d.Y);
                                C?.Ellipse(ellipsP - new OpenCvSharp.Point(m_sclTemp.Width * 0.5, m_sclTemp.Height * 0.5), new OpenCvSharp.Size(m_sclTemp.Width * ellipsS, m_sclTemp.Height * ellipsS), 0, -180, 180, 0, -1);
                            }

                            Mat maskedC = ApplyROIMask(C, matTransScale, 1, out Mat mask);
                            //Cv2.MinMaxLoc(maskedC, out min, out max, out minPoint, out maxPoint, mask);
                            maskedC.MinMaxLoc(out min, out max, out minPoint, out maxPoint);
                            if (max > 1) { continue; }
                            m_listC.Add(maskedC);
                            C?.Dispose();
                            mask?.Dispose();
                        }
                        else // 한번이라도 찾아봤던거면 m_listC를 새로 만들지 않고 기존에 있는거에 Masking 해서 다음것 검색
                        {
                            C = m_listC[anglecount];

                            SvFunc.StartWatch("Mask에 원그리기");

                            if (PrePoints.Count > 0)
                            {
                                int i = PrePoints.Count - 1;

                                Point3d cp3d = SvFunc.MattoP(RotateMat/* * TransScaleMat*/ * SvFunc.PtoMat(new Point3d(PrePoints[i].X, PrePoints[i].Y, 1)), true);
                                ellipsP = new OpenCvSharp.Point(cp3d.X, cp3d.Y);
                                C.Ellipse(ellipsP - new OpenCvSharp.Point(m_sclTemp.Width * 0.5, m_sclTemp.Height * 0.5), new OpenCvSharp.Size(m_sclTemp.Width * ellipsS, m_sclTemp.Height * ellipsS), 0, -180, 180, -1, -1);
                            }
                            //Cv2.MinMaxLoc(C, out min, out max, out minPoint, out maxPoint);
                            C.MinMaxLoc(out min, out max, out minPoint, out maxPoint);
                        }
                        //SvFunc.ShowTestImg(C, 2);
                        SvFunc.StopWatch();
                    }
                    else // 전체 찾기 아닐경우는 걍 무식하게...
                    {
                        Ctemp = new Mat(ScSrcRot.Size(), MatType.CV_32F);

                        SvFunc.StartWatch("TemplateMatch 부분찾기");
                        //Cv2.MatchTemplate(ScSrcRot, m_sclTemp, Ctemp, MatchMethod); 
                        C = ScSrcRot.MatchTemplate(m_sclTemp, MatchMethod);
                        SvFunc.StopWatch();
                        for (int i = 0; i < PrePoints.Count; i++)
                        {
                            Point3d cp3d = SvFunc.MattoP(RotateMat/* * TransScaleMat*/ * SvFunc.PtoMat(new Point3d(PrePoints[i].X, PrePoints[i].Y, 1)), true);
                            ellipsP = new OpenCvSharp.Point(cp3d.X, cp3d.Y);
                            Ctemp.Ellipse(ellipsP - new OpenCvSharp.Point(m_sclTemp.Width * 0.5, m_sclTemp.Height * 0.5), new OpenCvSharp.Size(m_sclTemp.Width * ellipsS, m_sclTemp.Height * ellipsS), 0, -180, 180, -1, -1);
                        }

                        // ROI rotation 생략
                        //Cv2.MinMaxLoc(Ctemp, out min, out max, out minPoint, out maxPoint);
                        Ctemp.MinMaxLoc(out min, out max, out minPoint, out maxPoint);
                    }
                    Ctemp?.Dispose();

                    double creteria = 0.9;

                    listAngleToScore.Add(new Point2d(angle, max));

                    ////// Debugging용 Code 삭제 금지 ////////
                    //for (int i = 0; i < PrePoints.Count; i++)
                    //{
                    //    Point3d cp3d = SvFunc.MattoP(RotateMat/* * TransScaleMat*/ * SvFunc.PtoMat(new Point3d(PrePoints[i].X, PrePoints[i].Y, 1)), true);
                    //    ellipsP = new Point(cp3d.X, cp3d.Y);
                    //    if (C != null)
                    //        C.Ellipse(ellipsP - new Point(m_sclTemp.Width * 0.5, m_sclTemp.Height * 0.5), new Size(m_sclTemp.Width * ellipsS, m_sclTemp.Height * ellipsS), 0, -180, 180, 0.5, -1);
                    //}
                    //ScSrcRot.Circle(maxPoint, 40, 40, 10);
                    //ScSrcRot.Circle(maxPoint, 10, 40, 10);
                    //Scalar color = 125;
                    //if (max > matchingScore)
                    //    color = 200;
                    //ScSrcRot.PutText("Score Max:" + matchingScore.ToString("F3") + " now:" + max.ToString("F3"), new Point(0, 10), FontFace.Vector0, 0.3, color);
                    //ScSrcRot.PutText("Angle Max:" + matchingPoint.Z.ToString("F3") + " now:" + angle.ToString("F3"), new Point(0, 20), FontFace.Vector0, 0.3, color);
                    //SvFunc.ShowTestImg(m_sclTemp, 2, "Template");
                    //SvFunc.ShowTestImg(ScSrcRot, 2);
                    //if (C != null)
                    //    SvFunc.ShowTestImg(C, 2);
                    ///////// Debugging용 Code 끝 ///////////
                    if (max > 1) { continue; }
                    if (max > matchingScore)
                    {
                        matchingScore = max;
                        maxAngleIdx = listAngleToScore.Count - 1;
                        matchingPoint.X = maxPoint.X;
                        matchingPoint.Y = maxPoint.Y;
                        matchingPoint.Z = angle;

                        if (matchingScore > creteria)// && !(m_iteration == 0 && m_iFindCount == 0))
                            retry = false;
                    }
                    else if (!retry)
                    {
                        break;
                    }
                }

                ////// SubAngle 사용하여 다시 MaxPoint, MaxScore를 찾는다.

                if (listAngleToScore.Count > 1)
                {
                    // Score 최대값이 listAngleToScore 처음에 있을때(최대값을 앞에서 더 찾아야 함)
                    double angle = startA;
                    int count = 0;
                    while (maxAngleIdx == 0 && count < 5)
                    {
                        angle -= _degStep;
                        RotateMat = Cv2.GetRotationMatrix2D(new Point2f(ScSrcRotRect.X + ScSrcRotRect.Width / 2, ScSrcRotRect.Y + ScSrcRotRect.Height / 2), -angle, 1);
                        RotateMat.Set<double>(0, 2, RotateMat.At<double>(0, 2) - ScSrcRotRect.X);
                        RotateMat.Set<double>(1, 2, RotateMat.At<double>(1, 2) - ScSrcRotRect.Y);

                        Cv2.WarpAffine(m_sclSrc, ScSrcRot, RotateMat, ScSrcRot.Size(), InterpolationFlags.Linear, BorderTypes.Constant, BorderColor);
                        Ctemp = new Mat(ScSrcRot.Size(), MatType.CV_32F);

                        //Cv2.MatchTemplate(ScSrcRot, m_sclTemp, Ctemp, MatchMethod);
                        Ctemp = ScSrcRot.MatchTemplate(m_sclTemp, MatchMethod);
                        for (int i = 0; i < PrePoints.Count; i++)
                        {
                            Point3d cp3d = SvFunc.MattoP(RotateMat/* * TransScaleMat*/ * SvFunc.PtoMat(new Point3d(PrePoints[i].X, PrePoints[i].Y, 1)), true);
                            OpenCvSharp.Point ellipsP = new OpenCvSharp.Point(cp3d.X, cp3d.Y);
                            Ctemp.Ellipse(ellipsP - new OpenCvSharp.Point(m_sclTemp.Width * 0.5, m_sclTemp.Height * 0.5), new OpenCvSharp.Size(m_sclTemp.Width * ellipsS, m_sclTemp.Height * ellipsS), 0, -180, 180, -1, -1);
                        }

                        // ROI rotation
                        Mat maskedMat = ApplyROIMask(Ctemp, matTransScale, 1, out Mat mask);
                        //Cv2.MinMaxLoc(Ctemp, out min, out max, out minPoint, out maxPoint, mask);
                        Ctemp.MinMaxLoc(out min, out max, out minPoint, out maxPoint);
                        mask?.Dispose();
                        maskedMat?.Dispose();

                        listAngleToScore.Insert(0, new Point2d(angle, max));
                        maxAngleIdx++;

                        if (max > 1) { continue; }
                        if (max > matchingScore)
                        {
                            matchingScore = max;
                            maxAngleIdx = 0;
                            matchingPoint.X = maxPoint.X;
                            matchingPoint.Y = maxPoint.Y;
                            matchingPoint.Z = angle;
                            maxAngleIdx = 0;
                        }
                        Ctemp?.Dispose();
                        count++;
                    }

                    // Score 최대값이 listAngleToScore 마지막에 있을때(최대값을 뒤에서 더 찾아야 함)
                    angle = endA;
                    count = 0;
                    while (maxAngleIdx == listAngleToScore.Count - 1 && count < 5)
                    {
                        angle += _degStep;
                        RotateMat = Cv2.GetRotationMatrix2D(new Point2f(ScSrcRotRect.X + ScSrcRotRect.Width / 2, ScSrcRotRect.Y + ScSrcRotRect.Height / 2), -angle, 1);
                        RotateMat.Set<double>(0, 2, RotateMat.At<double>(0, 2) - ScSrcRotRect.X);
                        RotateMat.Set<double>(1, 2, RotateMat.At<double>(1, 2) - ScSrcRotRect.Y);

                        Cv2.WarpAffine(m_sclSrc, ScSrcRot, RotateMat, ScSrcRot.Size(), InterpolationFlags.Linear, BorderTypes.Constant, BorderColor);
                        Ctemp = new Mat(ScSrcRot.Size(), MatType.CV_32F);
                        //Cv2.MatchTemplate(ScSrcRot, m_sclTemp, Ctemp, MatchMethod);
                        Ctemp = ScSrcRot.MatchTemplate(m_sclTemp, MatchMethod);
                        for (int i = 0; i < PrePoints.Count; i++)
                        {
                            Point3d cp3d = SvFunc.MattoP(RotateMat/* * TransScaleMat*/ * SvFunc.PtoMat(new Point3d(PrePoints[i].X, PrePoints[i].Y, 1)), true);
                            OpenCvSharp.Point ellipsP = new OpenCvSharp.Point(cp3d.X, cp3d.Y);
                            Ctemp.Ellipse(ellipsP - new OpenCvSharp.Point(m_sclTemp.Width * 0.5, m_sclTemp.Height * 0.5), new OpenCvSharp.Size(m_sclTemp.Width * ellipsS, m_sclTemp.Height * ellipsS), 0, -180, 180, -1, -1);
                        }

                        // ROI rotation
                        Mat maskedMat = ApplyROIMask(Ctemp, matTransScale, 1, out Mat mask);
                        //Cv2.MinMaxLoc(Ctemp, out min, out max, out minPoint, out maxPoint, mask);
                        Ctemp.MinMaxLoc(out min, out max, out minPoint, out maxPoint);
                        mask?.Dispose();
                        maskedMat?.Dispose();

                        listAngleToScore.Add(new Point2d(angle, max));
                        Ctemp?.Dispose();
                        count++;
                        if (max > 1) { continue; }
                        if (max > matchingScore)
                        {
                            matchingScore = max;
                            maxAngleIdx = listAngleToScore.Count - 1;
                            matchingPoint.X = maxPoint.X;
                            matchingPoint.Y = maxPoint.Y;
                            matchingPoint.Z = angle;
                        }
                    }

                    double anglediff = double.MaxValue;
                    max = double.MaxValue;
                    Point2d subA = new Point2d();
                    List<Mat> listSubC = new List<Mat>();
                    Mat SubC = null;
                    count = 0;
                    while (anglediff > _degStep / 5.0 && count < 5)
                    {
                        double oldangle = listAngleToScore[maxAngleIdx].X;
                        subA = CalSubValue(listAngleToScore, maxAngleIdx);
                        anglediff = Math.Abs(oldangle - subA.X);
                        RotateMat = Cv2.GetRotationMatrix2D(new Point2f(ScSrcRotRect.X + ScSrcRotRect.Width / 2, ScSrcRotRect.Y + ScSrcRotRect.Height / 2), -subA.X, 1.0);
                        RotateMat.Set<double>(0, 2, RotateMat.At<double>(0, 2) - ScSrcRotRect.X);
                        RotateMat.Set<double>(1, 2, RotateMat.At<double>(1, 2) - ScSrcRotRect.Y);

                        Cv2.WarpAffine(m_sclSrc, ScSrcRot, RotateMat, ScSrcRot.Size(), InterpolationFlags.Linear, BorderTypes.Constant, BorderColor);

                        SubC = new Mat(ScSrcRot.Size(), MatType.CV_32F);
                        listSubC.Add(SubC);
                        //Cv2.MatchTemplate(ScSrcRot, m_sclTemp, SubC, MatchMethod);
                        SubC = ScSrcRot.MatchTemplate(m_sclTemp, MatchMethod);
                        for (int i = 0; i < PrePoints.Count; i++)
                        {
                            Point3d cp3d = SvFunc.MattoP(RotateMat/* * TransScaleMat*/ * SvFunc.PtoMat(new Point3d(PrePoints[i].X, PrePoints[i].Y, 1)), true);
                            OpenCvSharp.Point ellipsP = new OpenCvSharp.Point(cp3d.X, cp3d.Y);
                            SubC.Ellipse(ellipsP - new OpenCvSharp.Point(m_sclTemp.Width * 0.5, m_sclTemp.Height * 0.5), new OpenCvSharp.Size(m_sclTemp.Width * ellipsS, m_sclTemp.Height * ellipsS), 0, -180, 180, 0, -1);
                        }

                        // ROI rotation
                        Mat maskedMat = ApplyROIMask(SubC, matTransScale, 1, out Mat mask);
                        //Cv2.MinMaxLoc(SubC, out min, out max, out minPoint, out maxPoint, mask);
                        SubC.MinMaxLoc(out min, out max, out minPoint, out maxPoint);
                        mask?.Dispose();
                        maskedMat?.Dispose();

                        // 만약 각도가 어느정도 정확하다고 판단(각도에 따라 찾은 대상이 동일하다는 뜻)되면, SubAngle을 믿는다.

                        if (max > 1) { count++; continue; }
                        if (max > matchingScore)
                        {
                            matchingScore = max;
                            listAngleToScore.Add(new Point2d(subA.X, max));
                            listAngleToScore.Sort((a, b) => (a.X < b.X) ? -1 : 1);
                            maxAngleIdx = listAngleToScore.FindIndex(a => a.Y == max);
                            matchingPoint.X = maxPoint.X;
                            matchingPoint.Y = maxPoint.Y;
                            matchingPoint.Z = subA.X;

                            ////// Debugging용 Code 삭제 금지 ////////
                            //ScSrcRot.Circle(maxPoint, 40, 40, 10);
                            //ScSrcRot.Circle(maxPoint, 10, 40, 10);
                            //ScSrcRot.PutText(max.ToString("F3") + "  " + subA.X.ToString("F3"), new Point(0, 20), FontFace.HersheyComplex, 0.5, 125);
                            //if (SubC != null)
                            //    SvFunc.ShowTestImg(SubC, 2);
                            //SvFunc.ShowTestImg(ScSrcRot, 2);
                            ///////// Debugging용 Code 끝 ///////////
                            if (m_iteration == 0)
                                break;

                            count++;
                            continue;
                        }
                        break;

                    }

                    Point2d subpixel = CalSubpixel3(SubC, new OpenCvSharp.Point(matchingPoint.X, matchingPoint.Y));
                    matchingPoint.X = subpixel.X;
                    matchingPoint.Y = subpixel.Y;
                    matchingPoint.Z = subA.X;
                    matchingScore = max;

                    foreach (Mat m in listSubC)
                    {
                        m?.Dispose();
                    }
                }

                if (IsUseEdge && _maskedscore)
                {
                    DateTime dt2 = DateTime.Now;
                    if (_maskedscore)
                        ApplyMask(maxPoint, ScSrcRot, m_sclTemp, m_scaledMask, max, out matchingScore);

                    SvFunc.TimeWatch(dt2, "CalSubpixel3");
                }


                //////// Debugging용 Code 삭제 금지 ////////
                //for (int i = 0; i < PrePoints.Count; i++)
                //{
                //    Point3d cp3d = SvFunc.MattoP(RotateMat/* * TransScaleMat*/ * SvFunc.PtoMat(new Point3d(PrePoints[i].X, PrePoints[i].Y, 1)), true);
                //    Point ellipsP = new Point(cp3d.X, cp3d.Y);
                //    C.Ellipse(ellipsP - new Point(m_sclTemp.Width * 0.5, m_sclTemp.Height * 0.5), new Size(m_sclTemp.Width * ellipsS, m_sclTemp.Height * ellipsS), 0, -180, 180, 0.5, -1);
                //}
                //ScSrcRot.Circle(maxPoint, 50, 255, 10);
                //ScSrcRot.Circle(maxPoint, 10, 255, 10);
                //SvFunc.ShowTestImg(m_sclTemp, 1);
                //SvFunc.ShowTestImg(ScSrcRot, 1);
                ////SvFunc.ShowTestImg(Cmat, 1);
                //foreach (Mat m in m_listC)
                //{
                //    SvFunc.ShowTestImg(m, 1);
                //}
                //////////// Debugging용 Code 끝 ///////////


                ////////////////////////////////////////////////////     Result Warping     ///////////////////////////////////////////////////////////////////
                Point2d[] source = new Point2d[4];
                Point2d[] target = new Point2d[4];

                source[0] = new Point2d(0, 0);
                source[1] = new Point2d(1, 0);
                source[2] = new Point2d(0, 1);
                source[3] = new Point2d(1, 1);

                double T = matchingPoint.Z * Math.PI / 180;

                target[0] = new Point2d(ScSrcRot.Width / 2, ScSrcRot.Height / 2) + new Point2d(-ScROICP.X * Math.Cos(T) + ScROICP.Y * Math.Sin(T), -ScROICP.X * Math.Sin(T) - ScROICP.Y * Math.Cos(T));
                target[1] = target[0] + new Point2d(Math.Cos(T), Math.Sin(T));
                target[2] = target[0] + new Point2d(Math.Cos(T + Math.PI / 2), Math.Sin(T + Math.PI / 2));
                target[3] = target[0] + new Point2d(Math.Cos(T) + Math.Cos(T + Math.PI / 2), Math.Sin(T) + Math.Sin(T + Math.PI / 2));

                Mat warpMat = Cv2.FindHomography(target, source);
                Mat newSM = warpMat * new Mat(3, 1, MatType.CV_64FC1, new double[] { matchingPoint.X, matchingPoint.Y, 1 });
                warpMat.Dispose();

                _resultcp = new Point3d(newSM.At<double>(0, 0) / newSM.At<double>(2, 0) / _searchscale, newSM.At<double>(1, 0) / newSM.At<double>(2, 0) / _searchscale, -T);
                if (_resultcp.Z < 0)
                {
                    _resultcp.Z += 2 * Math.PI;
                }
                _resultbox = OrigintoBox(_resultcp, _temp.Width * m_tempScaleX, _temp.Height * m_tempScaleY);
                ScSrcRot.Dispose();

                _resultcp = (_resultcp + new Point3d(_resultbox[2].X, _resultbox[2].Y, _resultcp.Z)) * 0.5;
                double scaleFactX = (m_tempScaleX < 1) ? Math.Sqrt(m_tempScaleX) : 1;
                double scaleFactY = (m_tempScaleY < 1) ? Math.Sqrt(m_tempScaleY) : 1;

                return matchingScore * scaleFactX * scaleFactY;
            }
            catch(Exception ex)
            {
                logger.Create("Rot TempMatching Error: " + ex.Message, ex);
                return 0d;
            }
            
        }
        SvImage GetFoundImage(SvImage image)
        {
            if (image == null || image.Mat == null || image.Mat.IsDisposed) return null;

            Point2f center = SvFunc.FixtureToImage2F(new Point2f((float)ResultCP.X, (float)resultCP.Y), image.TransformMat);

            double T = ResultCP.Z;
            double H = tempImg.Mat.Width;
            int V = tempImg.Mat.Height;

            Mat Rmat = Cv2.GetRotationMatrix2D(center, T * 180 / Math.PI, 1);
            Rmat.Set<double>(0, 2, Rmat.At<double>(0, 2) - center.X + H / 2.0);
            Rmat.Set<double>(1, 2, Rmat.At<double>(1, 2) - center.Y + V / 2.0);

            Rect rect = new Rect((int)center.X, (int)center.Y, (int)H, (int)V);

            SvImage templateImg = new SvImage();
            //Func.ShowTestImg(mask, 0.25);

            Cv2.WarpAffine(image.Mat, templateImg.Mat, Rmat, rect.Size, InterpolationFlags.Linear, BorderTypes.Constant, BorderColor);

            Rmat.Dispose();

            return templateImg;
        }
        public void GetEdge(Mat SrcImg, int minSize, out Mat Edge, double scale, int backcolor = 0, ThresholdTypes THtype = ThresholdTypes.Binary)
        {
            Edge = new Mat();
            SvFunc.StartWatch("GetEdge");
            if (SrcImg == null || SrcImg.Width == 0 || SrcImg.Height == 0)
            {
                Edge = null;
                return;
            }
            try
            {
                Mat srcGray = SrcImg.Clone();
                if (SrcImg.Channels() > 1)
                {
                    srcGray = srcGray.CvtColor(ColorConversionCodes.BGR2GRAY);
                }
                if (lut == null)
                {
                    lut = new byte[256];
                    for (int i = 0; i < lut.Length; i++)
                    {
                        if (i < 250)
                            lut[i] = 0;
                        else
                            lut[i] = (byte)i;

                    }
                }

                SrcImg.Blur(new OpenCvSharp.Size(3, 3));

                Edge = srcGray.AdaptiveThreshold(255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 5, 10);
                Edge.LUT(lut);
                SvFunc.StopWatch();
            }
            catch (Exception ex)
            {
                logger.Create("Get Edge Error: " + ex.Message, ex);
            }
        }
        private Mat ApplyROIMask(Mat _C, Mat _transferMat, double _scale, out Mat _mask)
        {
            _mask = null;

            if (_C == null || _transferMat == null) return null;

            //Mat transferMat = new Mat(2, 3, MatType.CV_64FC1, new double[] { 1, 0, _transferMat.Get<double>(0, 2), 0, 1, _transferMat.Get<double>(1, 2) });
            //_rotateMat.Set(0, 0, 1);
            //_rotateMat.Set(0, 1, 0);
            //_rotateMat.Set(1, 0, 0);
            //_rotateMat.Set(1, 1, 1);
            try
            {
                _mask = new Mat(_C.Size(), MatType.CV_8UC1, 0);

                Point2d[] roibox = new Point2d[4];
                roibox[0] = new Point2d(m_ROIrect.Left, m_ROIrect.Top) * _scale;
                roibox[1] = new Point2d(m_ROIrect.Right, m_ROIrect.Top) * _scale;
                roibox[2] = new Point2d(m_ROIrect.Right, m_ROIrect.Bottom) * _scale;
                roibox[3] = new Point2d(m_ROIrect.Left, m_ROIrect.Bottom) * _scale;

                // ROI rotation
                OpenCvSharp.Point[] rotROIpoints = new OpenCvSharp.Point[4];
                for (int i = 0; i < 4; i++)
                {
                    Mat result = _transferMat * new Mat(3, 1, MatType.CV_64FC1, new double[3] { roibox[i].X, roibox[i].Y, 1 });
                    rotROIpoints[i] = new OpenCvSharp.Point(result.At<double>(0, 0), result.At<double>(0, 1));
                }
                Cv2.FillPoly(_mask, new OpenCvSharp.Point[][] { rotROIpoints }, new Scalar(100));

                Mat maskedC = new Mat(_C.Size(), _C.Type(), 0);
                //SvFunc.ShowTestImg(_mask, 2);
                _C.CopyTo(maskedC, _mask);
                //SvFunc.ShowTestImg(maskedC, 2);

                return maskedC;
            }
            catch (Exception ex)
            {
                logger.Create("Apply ROI Mask Error: " + ex.Message, ex);
                return new Mat(0, 0, new MatType());
            } 
            
        }
        Point2d CalSubpixel3(Mat CoeffMat, OpenCvSharp.Point maxPoint)
        {
            //////////////////// subpixel 적용
            //  ax^3 + bx^2 + cx + d = f(x)
            //  3ax^2 + 2bx + c = f'(x)
            //  f'(1) = 3a + 2b + c,  f'(-1) = 3a - 2b + c
            //  f'(0) = c,
            //  f'(1) - f'(-1) = 4b,
            //  f'(1) + f'(-1) - 2f'(0) = 6a
            //  f'(x) = 0, x = (-b + sqrt(b^2 - 3ac)) / 3a 
            ////////////////////
            //Mat subPixel = C[maxPoint.Y - 2, maxPoint.Y + 2, maxPoint.X - 2, maxPoint.X + 2];
            //Mat kernel = new Mat(3, 3, MatType.CV_8UC1, new double[] { 0, -0.5, 0, -0.5, 0, 0.5, 0});
            //src_ROI.Filter2D(-1, kernel, new Point(-1, -1));
            const int cp = 4;
            if (maxPoint.X - cp < 0 || maxPoint.Y - cp < 0 || maxPoint.X + cp > CoeffMat.Width - 1 || maxPoint.Y + cp > CoeffMat.Height - 1)
                return new Point2d(maxPoint.X, maxPoint.Y);

            Mat C = CoeffMat[maxPoint.Y - cp, maxPoint.Y + cp, maxPoint.X - cp, maxPoint.X + cp].Clone();
            C = C.GaussianBlur(new OpenCvSharp.Size(3, 3), 1);
            float[] dfX = new float[3];
            float[] dfY = new float[3];
            for (int i = -1; i < 2; i++)
            {
                dfX[1 + i] = (C.At<float>(cp, cp + i + 1) - C.At<float>(cp, cp + i - 1)) / 2;
                dfY[1 + i] = (C.At<float>(cp + i + 1, cp) - C.At<float>(cp + i - 1, cp)) / 2;
            }
            double a, b, c, dX, dY;
            c = dfX[1];
            b = (dfX[2] - dfX[0]) / 4;
            a = (dfX[2] + dfX[0] - 2 * c) / 6;
            if (a == 0)
                dX = 0;
            else
                dX = (b < 0) ? (-b - Math.Sqrt(b * b - 3 * a * c)) / 3 / a : (-b + Math.Sqrt(b * b - 3 * a * c)) / 3 / a;
            c = dfY[1];
            b = (dfY[2] - dfY[0]) / 4;
            a = (dfY[2] + dfY[0] - 2 * c) / 6;
            if (a == 0)
                dY = 0;
            else
                dY = (b < 0) ? (-b - Math.Sqrt(b * b - 3 * a * c)) / 3 / a : (-b + Math.Sqrt(b * b - 3 * a * c)) / 3 / a;

            if (Math.Abs(dX) > 1 || double.IsNaN(dX)) dX = 0;
            if (Math.Abs(dY) > 1 || double.IsNaN(dY)) dY = 0;

            //SvLogger.Log.Debug(string.Format("dfX[0]: {0:0.000}, dfX[1]: {1:0.000}, dfX[2]: {2:0.000})", dfX[0], dfX[1], dfX[2]));
            //SvLogger.Log.Debug(string.Format("dfY[0]: {0:0.000}, dfY[1]: {1:0.000}, dfY[2]: {2:0.000})", dfY[0], dfY[1], dfY[2]));
            //SvLogger.Log.Debug(string.Format("Third : ({0:0.000}, {1:0.000})", dX, dY));
            //System.Diagnostics.Debug.WriteLine(string.Format("dfX[0]: {0:0.000}, dfX[1]: {1:0.000}, dfX[2]: {2:0.000})", dfX[0], dfX[1], dfX[2]));
            //System.Diagnostics.Debug.WriteLine(string.Format("dfY[0]: {0:0.000}, dfY[1]: {1:0.000}, dfY[2]: {2:0.000})", dfY[0], dfY[1], dfY[2]));
            //System.Diagnostics.Debug.WriteLine(string.Format("Third : ({0:0.000}, {1:0.000})", dX, dY));
            C.Dispose();
            return new Point2d(maxPoint.X + dX, maxPoint.Y + dY);
        }
        Point2d[] OrigintoBox(Point3d OriginP, double W, double H)
        {
            Point2d[] Box = new Point2d[4];
            Box[0] = new Point2d(OriginP.X, OriginP.Y);
            Box[1] = Box[0] + new Point2d(Math.Cos(OriginP.Z), Math.Sin(OriginP.Z)) * W;
            Box[3] = Box[0] + new Point2d(Math.Cos(OriginP.Z + Math.PI / 2), Math.Sin(OriginP.Z + Math.PI / 2)) * H;
            Box[2] = Box[1] + Box[3] - Box[0];
            return Box;
        }
        OpenCvSharp.Point ApplyMask(OpenCvSharp.Point maxPoint, Mat scaledSrcRot, Mat scaledtemp, Mat scaledMask, double maxV, out double score)
        {
            DateTime dt = DateTime.Now;
            score = maxV;

            if (scaledMask == null || scaledMask.Width == 0 || scaledMask.Height == 0)
                return maxPoint;
            if (scaledtemp == null || scaledtemp.Width != scaledMask.Width || scaledtemp.Height != scaledMask.Height)
                return maxPoint;
            int spare = 0;
            OpenCvSharp.Point newMaxPoint = maxPoint;

            //using ()
            {
                Mat C = new Mat(scaledtemp.Size(), MatType.CV_32F);
                for (int r = -spare; r <= spare; r++)
                {
                    for (int c = -spare; c <= spare; c++)
                    {
                        Mat srctmp;
                        Mat scaledMasktmp;

                        int X = maxPoint.X + c; int Y = maxPoint.Y + r;

                        Rect srcROI = new Rect(X, Y, scaledtemp.Width, scaledtemp.Height);
                        srcROI = SvFunc.GetRect(scaledSrcRot, srcROI);
                        Rect maskROI = new Rect(0, 0, scaledtemp.Width, scaledtemp.Height);

                        //if (X < 0)
                        //{
                        //    maskROI.X = -X;
                        //    srcROI.Width = maskROI.Width += X;
                        //    srcROI.X = 0;
                        //}
                        //if (Y < 0)
                        //{
                        //    maskROI.Y = -Y;
                        //    srcROI.Height = maskROI.Height += Y;
                        //    srcROI.Y = 0;
                        //}
                        //if (X + scaledtemp.Width > scaledSrcRot.Width)
                        //{
                        //    maskROI.Width = srcROI.Width = scaledtemp.Width - X;
                        //}
                        //if (Y + scaledtemp.Height > scaledSrcRot.Height)
                        //{
                        //    maskROI.Height = srcROI.Height = scaledtemp.Height - Y;
                        //}

                        srctmp = scaledSrcRot[srcROI];
                        scaledMasktmp = scaledMask[maskROI];

                        Mat maskedtmp = new Mat();
                        srctmp.CopyTo(maskedtmp, scaledtemp);

                        //Cv2.MatchTemplate(maskedtmp, scaledtemp, C, MatchMethod);
                        C = maskedtmp.MatchTemplate(scaledtemp, MatchMethod);

                        maskedtmp.Dispose();
                        //Cv2.MinMaxLoc(C, out double min, out double max, out OpenCvSharp.Point minP, out OpenCvSharp.Point maxP);
                        C.MinMaxLoc(out double min, out double max, out OpenCvSharp.Point minP, out OpenCvSharp.Point maxP);

                        // max 와 maxPoint를 Masking 후 최대값으로 교체
                        if (max > score)
                        {
                            score = max;
                            newMaxPoint = new OpenCvSharp.Point(X, Y);
                        }
                    }
                }
            }

            //SvFunc.TimeWatch(dt, "한번더 찾는데 걸리는 시간");
            return newMaxPoint;
        }
        Point2d CalSubValue(List<Point2d> listScore, int maxIdx)
        {
            //////////////////// subpixel 적용
            //  ax^2 + bx + c = f(x)
            //  2ax + b = f'(x)
            //  |x0^2 x0 1||a|   |y0|
            //  |x1^2 x1 1||b| = |y1|
            //  |x2^2 x2 1||c|   |y2|
            ////////////////////
            //if (listScore.Count < 3) return listScore[maxIdx];
            //if (maxIdx == listScore.Count - 1)
            //    maxIdx = listScore.Count - 2;
            //if (maxIdx == 0)
            //    maxIdx = 1;//
            if (maxIdx == listScore.Count - 1 || maxIdx == 0)
                return listScore[maxIdx];

            //Point2d maxScore = listScore[maxIdx];
            double[] X = new double[3] { listScore[maxIdx - 1].X, listScore[maxIdx].X, listScore[maxIdx + 1].X };
            double[] Y = new double[3] { listScore[maxIdx - 1].Y, listScore[maxIdx].Y, listScore[maxIdx + 1].Y };

            Mat KMat = new Mat(3, 3, MatType.CV_64FC1, new double[] { X[0] * X[0], X[0], 1, X[1] * X[1], X[1], 1, X[2] * X[2], X[2], 1 });
            Mat YMat = new Mat(3, 1, MatType.CV_64FC1, Y);
            if (KMat.Determinant() == 0)
            {
                return listScore[maxIdx];
            }
            Mat XMat = KMat.Inv() * YMat;

            double solX = -1 * XMat.At<double>(1) / XMat.At<double>(0) / 2;
            double solY = XMat.At<double>(0) * solX * solX + XMat.At<double>(1) * solX + XMat.At<double>(2);

            if (double.IsInfinity(solX) || double.IsInfinity(solY))
            {
                return listScore[maxIdx];
            }
            return new Point2d(solX, solY);
        }
        private Polygon DrawArrowHead(Point prev, Point end)
        {

            Polygon arrowHead = new Polygon
            {
                Fill = Brushes.Red
            };
            double arrowSize = 10;
            try
            {
                Vector direction = new Vector(end.X - prev.X, end.Y - prev.Y);
                direction.Normalize();
                Vector perpendicular = new Vector(-direction.Y, direction.X);

                Point arrowPoint1 = end - direction * arrowSize;
                Point arrowPoint2 = arrowPoint1 + perpendicular * (arrowSize / 2);
                Point arrowPoint3 = arrowPoint1 - perpendicular * (arrowSize / 2);

                arrowHead.Points.Clear();
                arrowHead.Points.Add(end);
                arrowHead.Points.Add(arrowPoint2);
                arrowHead.Points.Add(arrowPoint3);
            }
            catch (Exception ex)
            {
                logger.Create("Draw Arrow Head Error: " + ex.Message, ex);
            }
            return arrowHead;
        }
        private void DrawCross(Brush color, Canvas cnvBackGround, Point2d centerP, double _width, double _height, double thetaRad)
        {
            // size 조절
            try
            {
                Point2d fixtureC = SvFunc.FixtureToImage2D(centerP, mTransform2D);
                Point2d point_X = SvFunc.FixtureToImage2D(centerP + new Point2d(_width, 0), mTransform2D);
                Point2d point_Y = SvFunc.FixtureToImage2D(centerP + new Point2d(0, _height), mTransform2D);

                double newWidth = fixtureC.DistanceTo(point_X);
                double newHeight = fixtureC.DistanceTo(point_Y);

                double Wscale = _width / newWidth;
                double Hscale = _height / newHeight;
                double scale = Math.Max(Wscale, Hscale);

                Point2d CvP1_0 = (centerP + SvFunc.Rotate(new Point2d(-_width * scale / 2, 0), thetaRad));
                Point2d CvP1_1 = (centerP + SvFunc.Rotate(new Point2d(+_width * scale / 2, 0), thetaRad));
                Point2d CvP1_2 = (centerP + SvFunc.Rotate(new Point2d(0, -_height * scale / 2), thetaRad));
                Point2d CvP1_3 = (centerP + SvFunc.Rotate(new Point2d(0, +_height * scale / 2), thetaRad));

                Line lineX = new Line()
                {
                    Stroke = color,
                    StrokeThickness = 2,
                    X1 = CvP1_0.X,
                    X2 = CvP1_1.X,
                    Y1 = CvP1_0.Y,
                    Y2 = CvP1_1.Y
                };
                Line lineY = new Line()
                {
                    Stroke = color,
                    StrokeThickness = 2,
                    X1 = CvP1_2.X,
                    X2 = CvP1_3.X,
                    Y1 = CvP1_2.Y,
                    Y2 = CvP1_3.Y
                };
                cnvBackGround.Children.Add(lineX);
                cnvBackGround.Children.Add(lineY);
            }
            catch (Exception ex)
            {
                logger.Create("Draw Cross Error: " + ex.Message, ex);
            }
        }
        public void DisplayResult()
        {
            try
            {
                if (!meaRunTime.IsRunning) return;
                if (runImage == null || runImage.Mat == null || runImage.Width <= 0 || runImage.Height <= 0)
                {
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "InputImage is null or error!");
                    return;
                }
                if (Score == 0) return;

                toolBase.cbxImage.SelectedIndex = 2;
                Thread.Sleep(1);
                CanvasImg.Children.RemoveRange(1, CanvasImg.Children.Count - 1);
                outEle.RemoveRange(0, outEle.Count);

                Style StyleLb = new Style(typeof(Label));
                StyleLb.Setters.Add(new Setter(Label.FontSizeProperty, 15.0));
                StyleLb.Setters.Add(new Setter(Label.ForegroundProperty, Brushes.Red));
                StyleLb.Setters.Add(new Setter(Label.BackgroundProperty, Brushes.Transparent));

                //Draw Pattern Master Coordinate
                Point PO = new Point(cpPattern.X, cpPattern.Y);
                // Vector hướng
                Vector dir1 = new Vector(Math.Cos(cpPattern.Z), Math.Sin(cpPattern.Z));      // Line X
                Vector dir2 = new Vector(-Math.Sin(cpPattern.Z), Math.Cos(cpPattern.Z));     // Line Y
                Line linePMX = CreateLineThroughCenter(dir1, PO.X, PO.Y, runImage.Width, runImage.Height);
                Line linePMY = CreateLineThroughCenter(dir2, PO.X, PO.Y, runImage.Width, runImage.Height);
                string contentLbPM = String.Format("Template Image Point = ({0:F3}, {1:F3}, {2:F3})", cpPattern.X, cpPattern.Y, (cpPattern.Z * 180) / Math.PI);
                Label lbPM = new Label()
                {
                    Style = StyleLb,
                    Foreground = Brushes.White,
                    Content = contentLbPM,
                };
                Canvas.SetLeft(lbPM, 10);
                Canvas.SetTop(lbPM, 10);

                outEle.Add(linePMX);
                outEle.Add(linePMY);
                outEle.Add(lbPM);

                string tmp = null;
                if (Score < PriorityCreteria || listResultBox.Count == 0)
                {
                    if (failResultBox == null)
                    {
                        meaRunTime.Stop();
                        toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Can not find any result box!");
                        return;
                    }

                    //Set In/Out
                    OutTranslateX = failResultBox.CP.X;
                    OutTranslateY = failResultBox.CP.Y;
                    OutRotation = failResultBox.CP.Z * 180f / Math.PI;
                    OutScore = Score;

                    if (isEditMode)
                    {
                        Polygon failResult = new Polygon()
                        {
                            Stroke = Brushes.Red,
                            StrokeThickness = 2,
                            Fill = Brushes.Transparent,
                        };
                        foreach (var point in failResultBox.Box)
                        {
                            Point p = new Point(point.X, point.Y);
                            failResult.Points.Add(p);
                        }
                        outEle.Add(failResult);

                        tmp = string.Format("P : ({0:F3}, {1:F3})\n\rT : {2:F3}deg\n\rScore : {3:F3}", failResultBox.CP.X, failResultBox.CP.Y, failResultBox.CP.Z * 180f / Math.PI, Score);
                        Label lbFail = new Label()
                        {
                            Style = StyleLb,
                            Content = tmp,
                        };
                        Canvas.SetLeft(lbFail, failResultBox.CP.X + 10);
                        Canvas.SetTop(lbFail, failResultBox.CP.Y + 10);
                        outEle.Add(lbFail);
                    }
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Fail result box!");
                    return;
                }
                if (isEditMode)
                {
                    Point2d P0 = new Point2d(m_result.X, m_result.Y);
                    Point2d P1 = P0 + SvFunc.Rotate(new Point2d(runImage.Width / 15, 0), m_result.Z);
                    Point2d P2 = P0 + SvFunc.Rotate(new Point2d(0, runImage.Width / 15), m_result.Z);
                    Line lineX = new Line()
                    {
                        Stroke = Brushes.Red,
                        StrokeThickness = 2,
                        X1 = P0.X,
                        X2 = P1.X,
                        Y1 = P0.Y,
                        Y2 = P1.Y
                    };
                    Line lineY = new Line()
                    {
                        Stroke = Brushes.Red,
                        StrokeThickness = 2,
                        X1 = P0.X,
                        X2 = P2.X,
                        Y1 = P0.Y,
                        Y2 = P2.Y
                    };
                    outEle.Add(lineX);
                    outEle.Add(lineY);
                    outEle.Add(DrawArrowHead(new Point(P0.X, P0.Y), new Point(P1.X, P1.Y)));
                    outEle.Add(DrawArrowHead(new Point(P0.X, P0.Y), new Point(P2.X, P2.Y)));

                    //Hiển thị tất cả result cả tốt cả xấu
                    //foreach (var resultBox in listResultBox)
                    //{
                    //    Polygon polyResults = new Polygon()
                    //    {
                    //        Stroke = Brushes.Gray,
                    //        StrokeThickness = 1,
                    //        Fill = Brushes.Transparent,
                    //    };
                    //    foreach (var point in resultBox.Box)
                    //    {
                    //        Point p = new Point(point.X, point.Y);
                    //        polyResults.Points.Add(p);
                    //    }
                    //    outEle.Add(polyResults);
                    //}

                    //Hiển thị cho result đẹp nhất
                    Polygon polyResult = new Polygon()
                    {
                        Stroke = Brushes.YellowGreen,
                        StrokeThickness = 2,
                        Fill = Brushes.Transparent,
                    };
                    foreach (var point in listResultBox[0].Box)
                    {
                        Point p = new Point(point.X, point.Y);
                        polyResult.Points.Add(p);
                    }
                    outEle.Add(polyResult);

                    tmp = string.Format("P : ({0:F3}, {1:F3})\n\rT : {2:F3}deg\n\rScore : {3:F3}", m_result.X, m_result.Y, m_result.Z * 180f / Math.PI, Score);

                    Label lbData1 = new Label()
                    {
                        Style = StyleLb,
                        Content = tmp,
                    };
                    Canvas.SetLeft(lbData1, P0.X + 10);
                    Canvas.SetTop(lbData1, P0.Y + 10);
                    outEle.Add(lbData1);
                    tmp = string.Format("Finding Count : {0}\n\rAdjust Scale : {1:F3}, {2:F3}", listResultBox.Count, m_tempScaleX, m_tempScaleY);
                    Label lbData2 = new Label() { Style = StyleLb, Content = tmp };
                    Canvas.SetLeft(lbData2, runImage.Width * 0.5f);
                    Canvas.SetTop(lbData2, 5);
                    outEle.Add(lbData2);
                    foreach (var ele in outEle)
                        CanvasImg.Children.Add(ele);
                    DrawCross(Brushes.Green, CanvasImg, new Point2d(m_result.X, m_result.Y), 100, 100, m_result.Z);
                }

                //Set In/Out
                OutTranslateX = m_result.X;
                OutTranslateY = m_result.Y;
                OutRotation = m_result.Z * 180f / Math.PI;
                OutScore = Score;
            }
            catch (Exception ex)
            {
                logger.Create("Display Result Error: " + ex.Message, ex);
            }
            
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
                        runImage.Mat = (ImgView.Source as BitmapSource).ToMat();
                        runImage.RegionRect.Rect = new Rect(0, 0, (int)ImgView.Source.Width, (int)ImgView.Source.Height);
                        if (runImage.Mat.Channels() > 3)
                        {
                            runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.BGR2RGB);
                            runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.RGB2BGR);
                        }
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
                    if (runImage.Mat.Channels() > 3)
                    {
                        runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.BGR2RGB);
                        runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.RGB2BGR);
                    }
                }
                else if (InputImage.Mat != null && InputImage.Mat.Width > 0 && InputImage.Mat.Height > 0 && !toolBase.isImgPath)
                {
                    runImage = this.InputImage.Clone(true);
                }

                if (mPatternDataList.Count <= 0)
                {
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Can not find any Pattern!");
                    return;
                }
                IsUseEdge = false;

                tempImg = mPatternDataList[0].PatternImage;
                if (IsAutoMatchPara)
                {
                }
                if (tempImg == null)
                {
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Can not find any Pattern Image!");
                    return;
                }
                if (tempImg.Mat.Channels() != runImage.Mat.Channels())
                {
                    if (tempImg.Mat.Channels() > runImage.Mat.Channels())
                    {
                        tempImg.Mat = tempImg.Mat.CvtColor(ColorConversionCodes.BGR2GRAY);
                    }
                    else
                    {
                        tempImg.Mat = tempImg.Mat.CvtColor(ColorConversionCodes.GRAY2BGR);
                    }
                }

                OpenCvSharp.Rect m_searchROI = runImage.RegionRect.Rect;
                if (m_searchROI == null)
                    m_searchROI = new Rect(new OpenCvSharp.Point(0, 0), runImage.Mat.Size());
                if (m_searchROI.Width == 0 || m_searchROI.Height == 0) m_searchROI = new Rect(new OpenCvSharp.Point(), runImage.Mat.Size());
                m_searchROI.Location = m_searchROI.Location;


                if (runImage.Width < tempImg.Width || runImage.Height < tempImg.Height)
                {
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "InputImage size is smaller than Pattern Image!");
                    return;
                }
                //Point3d resultCenter = new Point3d();
                Mat des = runImage.Mat.Clone();
                if (m_searchROI.Width < tempImg.Width || m_searchROI.Height < tempImg.Height)
                {
                    m_searchROI = new Rect(0, 0, tempImg.Width, tempImg.Height);
                }
                m_ROIrect = new Rect(m_searchROI.X, m_searchROI.Y, m_searchROI.Width - tempImg.Width, m_searchROI.Height - tempImg.Height);

                //Point2d[] resultbox = new Point2d[4];
                OpenCvSharp.Rect ROI = new OpenCvSharp.Rect();

                double degStep = 0.0;
                double degRange = 0.0;
                double startT = 0.0;
                double S = 0.0;
                double maxScore = 0.0d;
                Point3d maxCenterPoint = new Point3d();
                Point2d[] maxResultBox = new Point2d[4];
                //double score = 0.0;
                double margin = 1.1;
                double templW, templH;
                //List<FoundResult> adjustobserver = new List<FoundResult>();
                listResultBox.Clear();
                if (IsAutoMatchPara)
                {
                    double templimitsize = 50;
                    ScaleFirst = 10;

                    if (tempImg.Width < templimitsize || tempImg.Height < templimitsize)
                        ScaleFirst = 1;

                    ScaleLast = Math.Max(ScaleFirst / 8, 1);

                    FirstStep = 5;
                    Precision = 0.02;
                    DegMin = DegMax = 20;
                }

                Point2d[] box = new Point2d[4];
                m_listC = new List<Mat>();

                DateTime dt;
                m_tempScaleX = m_tempScaleY = Math.Round((TempScaleMin + TempScaleMax) / 2);

                for (int ifindCount = 0; ifindCount < MaxCount; ifindCount++)
                {
                    dt = DateTime.Now;
                    // Adjust scale
                    List<Point2d> list_scaletoscore = new List<Point2d>();
                    double adjustgoodscore = -1;
                    double adjustgoodscale = -1;
                    int adjustgoodstep = 0;
                    Point3d adjustgoolcenter = new Point3d();
                    Point2d[] adjustgoodbox = new Point2d[4];
                    double adjustgoodrange = 0;

                    double adjustscalestep = Math.Min(0.05, (TempScaleMax - TempScaleMin) / 2);
                    int adjuststep = 0;
                    for (double adjustscale = TempScaleMin; adjustscale <= TempScaleMax; adjustscale += adjustscalestep, adjuststep += 1)
                    {
                        if (ifindCount == 0)
                        {
                            templW = Math.Round(tempImg.Width * adjustscale);
                            templH = Math.Round(tempImg.Height * adjustscale);
                            m_tempScaleX = adjustscale;
                            m_tempScaleY = adjustscale;
                        }
                        else // 두번째 부터는 처음에 찾은 m_tempScale을 적용한다. 
                        {
                            templW = Math.Round(tempImg.Width * ((m_tempScaleX + m_tempScaleY) / 2));
                            templH = Math.Round(tempImg.Height * ((m_tempScaleX + m_tempScaleY) / 2));
                        }

                        S = 1.0 / ScaleFirst;
                        degRange = DegMin + DegMax;
                        degStep = Math.Max(FirstStep, Precision);

                        startT = (DegMax - DegMin) / 2;
                        ROI = new OpenCvSharp.Rect(m_searchROI.Location, m_searchROI.Size);// new Rect(new Point(0, 0), desROI.Size());
                        margin = 1.1;

                        // 1. Iteration
                        m_iteration = 0;

                        double oldstartT;
                        do
                        {
                            oldstartT = startT;
                            double tempScore = RotTempMatching(des, tempImg.Mat, ROI, startT, degRange, degStep, S, out Point3d tempResultCenter, out Point2d[] tempResultbox, margin);
                            adjustgoodscore = Math.Max(adjustgoodscore, tempScore);
                            if (adjustgoodscore == tempScore)
                            {
                                adjustgoolcenter = tempResultCenter;
                                adjustgoodbox = tempResultbox;
                                adjustgoodrange = degRange;
                                adjustgoodscale = adjustscale;
                            }

                            ROI = new Rect((int)Math.Round(tempResultCenter.X - templW / 2), (int)Math.Round(tempResultCenter.Y - templH / 2), (int)Math.Round(templW), (int)Math.Round(templH));
                            m_iteration += 1;

                            // 처음의 경우 degStep으로 판단해야 해서...
                            degRange = Math.Min(degStep, degRange / 2);
                            degStep = Math.Max(degRange / 2, Precision);
                            startT = tempResultCenter.Z * 180.0 / Math.PI;
                            if (Math.Abs(oldstartT - startT) < Precision && m_iteration > 1)
                            {
                                degRange = Precision * 2;
                                break;
                            }
                            margin = 1.1;
                            S *= 2;
                            if (S < 0.2) S = 0.2;
                            if (S > 1 / ScaleLast) S = 1 / ScaleLast;
                            if (m_iteration == 3) break;
                        }
                        while (degStep > Precision);

                        // 찾은 결과는 List에 저장
                        list_scaletoscore.Add(new Point2d(adjustscale, maxScore));
                        //adjustobserver.Add(new FoundResult(score, resultCenter, resultbox));
                        if (adjustgoodscore > maxScore)
                        {
                            //adjustgoodscore = maxScore;
                            //adjustgoolcenter = resultCenter;
                            adjustgoodscale = adjustscale;
                            adjustgoodstep = adjuststep;
                            adjustgoodrange = degRange;

                            maxScore = adjustgoodscore;
                            maxCenterPoint = adjustgoolcenter;
                            maxResultBox = adjustgoodbox;
                        }

                        // 이건 훗날 메모리 vs 속도 향상을 좀 고민해야함...
                        // 속도 향상을 위해서는 이게 RotTemplateMatch 안에 들어가 있어야될 듯
                        // 
                        if (ifindCount == 0 && adjustscalestep > 0) // scale 옵션이 없으면 그냥 건너뛰자. 그래야 m_listC가 남아있다.
                        {
                            foreach (var m in m_listC)
                                m?.Dispose();
                            m_listC.Clear();
                        }
                        else
                            break;
                    }
                    //////////////////////////////////// Adjust Scale
                    if (ifindCount == 0 && adjustscalestep > 0)
                    {
                        double oldscale = adjustgoodscale;
                        //Point3d maxcenter = maxCenterPoint;
                        //double maxscore = maxScore;
                        //double maxscale = adjustgoodscale;

                        double tempDegRange = DegMin + DegMax;
                        degStep = Math.Max(FirstStep, Precision);
                        startT = (DegMax - DegMin) / 2;

                        for (int i = 0; i < 4; i++)
                        {
                            foreach (var c in m_listC)
                            {
                                c?.Dispose();
                            }
                            m_listC.Clear();

                            adjustgoodstep = list_scaletoscore.FindIndex(a => a.Y == maxScore);
                            Point2d adjustresult = CalSubValue(list_scaletoscore, adjustgoodstep);
                            double tempScale = adjustresult.X;

                            templW = Math.Round(tempImg.Width * tempScale);
                            templH = Math.Round(tempImg.Height * tempScale);
                            ROI = new Rect((int)Math.Round(maxCenterPoint.X - templW / 2), (int)Math.Round(maxCenterPoint.Y - templH / 2), (int)Math.Round(templW), (int)Math.Round(templH));
                            degStep = Math.Max(degRange / 2, Precision);

                            m_tempScaleX = m_tempScaleY = tempScale;
                            double tempScore = RotTempMatching(des, tempImg.Mat, ROI, startT, degRange, degStep, S, out Point3d tempResultCenter, out Point2d[] tempResultbox, margin);

                            maxScore = Math.Max(maxScore, tempScore);
                            if (maxScore == tempScore)
                            {
                                adjustgoodscale = tempScale;
                                maxCenterPoint = tempResultCenter;
                                maxResultBox = tempResultbox;
                                adjustgoodrange = degRange;
                            }
                            else
                            {
                                m_tempScaleX = m_tempScaleY = list_scaletoscore[adjustgoodstep].X;
                            }

                            list_scaletoscore.Add(new Point2d(adjustgoodscale, tempScore));
                            list_scaletoscore.Sort((x1, x2) => x1.X < x2.X ? -1 : 1);
                            oldscale = adjustgoodscale;
                            // 더이상 score 변화가 없으면 탈출~
                            if (Math.Abs(oldscale - adjustgoodscale) < 0.005) break;
                        }
                    }

                    listResultBox.Add(new FoundResult(maxScore, maxCenterPoint, maxResultBox));

                    //resultCenter = adjustgoolcenter;
                    //degRange = adjustgoodrange;
                    double tempScore1 = 0.0f;
                    Point3d tempCenterPoint = new Point3d();
                    Point2d[] tempResultBox = new Point2d[4];
                    if (IsUseEdge && maxScore > PriorityCreteria / 2)
                    {
                        double oldscore = adjustgoodscore;
                        templW = Math.Round(tempImg.Width * TempScaleMax);
                        templH = Math.Round(tempImg.Height * TempScaleMax);
                        ROI = new Rect((int)Math.Round(maxCenterPoint.X - templW / 2), (int)Math.Round(maxCenterPoint.Y - templH / 2), (int)Math.Round(templW), (int)Math.Round(templH));
                        margin = 1.1;
                        tempScore1 = RotTempMatching(des, tempImg.Mat, ROI, startT, 0, degStep, S, out tempCenterPoint, out tempResultBox, margin, true);
                        //score = (oldscore + score) / 2;
                    }

                    // Priority
                    if (tempScore1 > PriorityCreteria)
                    {
                        box = new Point2d[4];
                        for (int ii = 0; ii < 4; ii++)
                        {
                            box[ii] = tempResultBox[ii];
                        }
                        listResultBox.Add(new FoundResult(tempScore1, tempCenterPoint, box));
                        // float[,] ff = SvFunc.DisplayMatF(InputImage.TransformMat);
                        //SvFunc.ShowTestImg(desROI, 0.2);
                    }
                    else { break; }
                }
                m_listC.Clear();

                //double firstscore = 0;
                if (listResultBox.Count > 0)
                {
                    switch (SelectedPriority)
                    {
                        //Sắp xếp theo điểm số (Score) từ cao đến thấp.
                        case Priority.None:
                            listResultBox.Sort((a, b) => ((a.Score > b.Score) ? -1 : 1));
                            break;
                        //Sắp xếp theo giá trị X (từ trái sang phải).
                        case Priority.Left:
                            listResultBox.Sort((a, b) => ((a.CP.X < b.CP.X) ? -1 : 1));
                            break;
                        //Sắp xếp theo giá trị X (từ phải sang trái).
                        case Priority.Right:
                            listResultBox.Sort((a, b) => ((a.CP.X > b.CP.X) ? -1 : 1));
                            break;
                        //Sắp xếp theo giá trị Y (từ trên xuống dưới).
                        case Priority.Top:
                            listResultBox.Sort((a, b) => ((a.CP.Y < b.CP.Y) ? -1 : 1));
                            break;
                        //Sắp xếp theo giá trị Y (từ dưới lên trên).
                        case Priority.Bottom:
                            listResultBox.Sort((a, b) => ((a.CP.Y > b.CP.Y) ? -1 : 1));
                            break;
                    }
                    // Priority Mask에서 listResultBox[0]을 사용하면 안되기 때문에 0번은 삭제후 나중에 추가
                    //firstscore = listResultBox[0].Score;
                    //resultCenter = listResultBox[0].CP; // Final Finding을 위해 대입
                    //25032025 NVH
                    //listResultBox.RemoveAt(0);
                }
                else
                {
                    box = new Point2d[4];
                    for (int ii = 0; ii < 4; ii++)
                    {
                        box[ii] = SvFunc.ImageToFixture2D(maxResultBox[ii], runImage.TransformMat);
                    }
                    //Tạo một failResultBox để biểu thị rằng không tìm thấy kết quả hợp lệ
                    failResultBox = new FoundResult(maxScore, SvFunc.ImageToFixture3D(maxCenterPoint, runImage.TransformMat), box);
                    maxCenterPoint = failResultBox.CP;
                    //for (int ii = 0; ii < resultbox.Length; ii++)
                    //{
                    //    failResultBox.Box[ii] += new Point2d(m_searchROI.X, m_searchROI.Y);
                    //}
                    //failResultBox.CP += new Point3d(m_searchROI.X, m_searchROI.Y, 0);
                    //m_score = score;
                    //return;
                }

                ///////////////////// Final Finding
                dt = DateTime.Now;
                startT = maxCenterPoint.Z * 180.0 / Math.PI;
                //degRange = m_precision * 2;
                degStep = degRange / 2;
                S = 1 / ScaleLast;

                //location = new Point(Math.Max(0, Cv.Round(resultCenter.X - templW / 2)), Math.Max(0, Cv.Round(resultCenter.Y - templH / 2)));
                //sz = new Size(Math.Min(resultCenter.X + templW / 2, m_searchROI.Width) - location.X, Math.Min(resultCenter.Y + templH / 2, m_searchROI.Height) - location.Y);

                //ROI = new Rect(location, sz);
                templW = Math.Round(tempImg.Width * m_tempScaleX);
                templH = Math.Round(tempImg.Height * m_tempScaleY);
                ROI = new Rect((int)Math.Round(maxCenterPoint.X - templW / 2), (int)Math.Round(maxCenterPoint.Y - templH / 2), (int)Math.Round(templW), (int)Math.Round(templH));

                margin = 1.1;


                double lastScore = RotTempMatching(des, tempImg.Mat, ROI, startT, degRange, degStep, S, out Point3d lastCenterPoint, out Point2d[] lastResultBox, margin);
                if (IsUseEdge)
                {
                    //double oldscore = score;
                    lastScore = RotTempMatching(des, tempImg.Mat, ROI, startT, 0, degStep, S, out lastCenterPoint, out lastResultBox, margin, true);
                    //score = (oldscore + score) / 2;
                }
                //25032025 NVH
                //listResultBox.Insert(0, new FoundResult(score, resultCenter, resultbox));
                listResultBox.Add(new FoundResult(lastScore, lastCenterPoint, lastResultBox));
                listResultBox = listResultBox.OrderByDescending(x => x.Score).ToList();
                // ROI 보상
                //for (int ii = 0; ii < listResultBox.Count; ii++)
                //{
                //    for (int jj = 0; jj < resultbox.Length; jj++)
                //    {
                //        listResultBox[ii].Box[jj] += new Point2d(m_searchROI.X, m_searchROI.Y);
                //    }
                //    listResultBox[ii].CP += new Point3d(m_searchROI.X, m_searchROI.Y, 0);
                //}

                // Calculate Ref. Point
                //25032025 NVH
                //Mat matRot = Cv2.GetRotationMatrix2D(new Point2f((float)resultbox[0].X, (float)resultbox[0].Y), -resultCenter.Z * 180 / Math.PI, 1);
                Mat matRot = Cv2.GetRotationMatrix2D(new Point2f((float)listResultBox[0].Box[0].X, (float)listResultBox[0].Box[0].Y), -listResultBox[0].CP.Z * 180 / Math.PI, 1);

                //Mat resultRefMat = new Mat(3, 1, MatType.CV_64FC1, new double[] { m_tempScaleX * mPatternDataList[0].RefPoint.X + resultbox[0].X, m_tempScaleY * mPatternDataList[0].RefPoint.Y + resultbox[0].Y, 1 });
                Mat resultRefMat = new Mat(3, 1, MatType.CV_64FC1, new double[] { m_tempScaleX * mPatternDataList[0].RefPoint.X + listResultBox[0].Box[0].X, m_tempScaleY * mPatternDataList[0].RefPoint.Y + listResultBox[0].Box[0].Y, 1 });
                resultRefMat = matRot * resultRefMat;
                //m_result = new Point3d(resultRefMat.At<double>(0, 0), resultRefMat.At<double>(1, 0), resultCenter.Z);
                m_result = new Point3d(resultRefMat.At<double>(0, 0), resultRefMat.At<double>(1, 0), listResultBox[0].CP.Z);

                // Fixture 보상
                m_result = SvFunc.ImageToFixture3D(m_result, runImage.TransformMat);
                for (int ii = 0; ii < listResultBox.Count; ii++)
                {
                    for (int jj = 0; jj < 4; jj++)
                        maxResultBox[jj] = SvFunc.ImageToFixture2D(maxResultBox[jj], runImage.TransformMat);
                    listResultBox[ii].CP = SvFunc.ImageToFixture3D(listResultBox[ii].CP, runImage.TransformMat);
                }

                maxCenterPoint = listResultBox[0].CP;
                ResultBox = listResultBox[0].Box;

                // Mat Dispose
                if (des != null) des.Dispose();
                if (matRot != null) matRot.Dispose();
                if (resultRefMat != null) resultRefMat.Dispose();
                if (m_desROI != null) m_desROI.Dispose();
                foreach (var c in m_listC)
                    c?.Dispose();
                if (FoundImage != null) FoundImage.Dispose();

                if (maxResultBox == null || maxResultBox.Length == 0) return;
                /////////////////////

                ResultCP = maxCenterPoint;
                //FoundImage = GetFoundImage(runImage);

                if (listResultBox.Count > 0)
                    Score = listResultBox[0].Score;

                //if (m_score > PriorityCreteria)
                //    lastRunSuccess = true;
            }
            catch (Exception ex)
            {
                logger.Create("Run Error: " + ex.Message, ex);
            }
        }

        public void Run1()
        {
            if (this.InputImage == null || this.InputImage.Mat == null || this.InputImage.Mat.Width <= 0 || this.InputImage.Mat.Height <= 0)
            {
                if (toolBase.isImgPath)
                {
                    runImage.Mat = (ImgView.Source as BitmapSource).ToMat();
                    if (runImage.Mat.Channels() > 3)
                    {
                        runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.BGR2RGB);
                        runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.RGB2BGR);
                    }
                }
                else
                {
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "InputImage is null or error!");
                    return;
                }
            }
            else if (InputImage.Mat != null && toolBase.isImgPath)
            {
                runImage.Mat = (ImgView.Source as BitmapSource).ToMat();
                if (runImage.Mat.Channels() > 3)
                {
                    runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.BGR2RGB);
                    runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.RGB2BGR);
                }
            }
            else if (InputImage.Mat != null && InputImage.Mat.Width > 0 && InputImage.Mat.Height > 0 && !toolBase.isImgPath)
            {
                runImage = this.InputImage.Clone(true);
            }

            if (mPatternDataList.Count <= 0)
            {
                meaRunTime.Stop();
                toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Can not find any Pattern!");
                return;
            }
            tempImg = mPatternDataList[0].PatternImage;
            if (tempImg == null)
            {
                meaRunTime.Stop();
                toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Can not find any Pattern Image!");
                return;
            }

            // 1. Khởi tạo SIFT
            var sift = SIFT.Create();

            // 2. Detect + Compute descriptor
            KeyPoint[] keypointsInput, keypointsTemplate;
            Mat descriptorsInput = new Mat();
            Mat descriptorsTemplate = new Mat();


            sift.DetectAndCompute(runImage.Mat, null, out keypointsInput, descriptorsInput);
            sift.DetectAndCompute(tempImg.Mat, null, out keypointsTemplate, descriptorsTemplate);

            // 3. Match descriptors
            var matcher = new BFMatcher(NormTypes.L2);
            var knnMatches = matcher.KnnMatch(descriptorsTemplate, descriptorsInput, k: 2);

            // 4. Áp dụng Lowe's ratio test
            List<DMatch> goodMatches = new List<DMatch>();
            foreach (var match in knnMatches)
            {
                if (match.Length >= 2)
                {
                    DMatch m1 = match[0];
                    DMatch m2 = match[1];
                    if (m1.Distance < 0.8 * m2.Distance)
                    {
                        goodMatches.Add(m1);
                    }
                }
            }

            if (goodMatches.Count < 4)
            {
                Console.WriteLine("Không đủ matches để tìm vị trí!");
                return;
            }

            // 5. Lấy các điểm keypoint match
            Point2f[] templatePts = goodMatches.Select(m => keypointsTemplate[m.QueryIdx].Pt).ToArray();
            Point2f[] inputPts = goodMatches.Select(m => keypointsInput[m.TrainIdx].Pt).ToArray();

            // 6. Tính Homography
            IEnumerable<Point2d> dstPoints = templatePts.Select(p => new Point2d(p.X, p.Y));
            IEnumerable<Point2d> srcPoints = inputPts.Select(p => new Point2d(p.X, p.Y));


            var H = Cv2.FindHomography(dstPoints, srcPoints, HomographyMethods.Ransac, 3.0);
            if (H.Width < 3 || H.Height < 3) return;
            //var H = Mat.Eye(3, 3, MatType.CV_64FC1);

            // 7. Biến đổi góc của template để tìm vị trí trong input
            var templateCorners = new[]
            {
                new Point2f(0, 0),
                new Point2f(tempImg.Mat.Cols, 0),
                new Point2f(tempImg.Mat.Cols, tempImg.Mat.Rows),
                new Point2f(0, tempImg.Mat.Rows)
            };
            var transformedCorners = Cv2.PerspectiveTransform(templateCorners, H);


            //Cv2.PutText(runImage.Mat, "AngleDegree: " + angle.ToString("F3"), new OpenCvSharp.Point(20, 20), HersheyFonts.Italic, 2, Scalar.Lime, 3);

            // 8. Vẽ kết quả
            for (int i = 0; i < 4; i++)
            {
                Cv2.Line(runImage.Mat, new OpenCvSharp.Point(transformedCorners[i].X, transformedCorners[i].Y), new OpenCvSharp.Point(transformedCorners[(i + 1) % 4].X, transformedCorners[(i + 1) % 4].Y), Scalar.Red, 3);
            }

            // Hiển thị ảnh
            Mat display = new Mat();
            Cv2.Resize(runImage.Mat, display, new OpenCvSharp.Size(runImage.Mat.Width / 4, runImage.Mat.Height / 4));
            Cv2.ImShow("Detected Template", display);
            Cv2.WaitKey(0);
        }
    }
    [Serializable]
    public class FoundResult
    {
        public double Score;
        public Point3d CP;
        public Point2d[] Box;

        public FoundResult(double S, Point3d cp, Point2d[] box)
        {
            Score = S;
            CP = cp;
            Box = box;
        }
    }
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class PatternData : IDisposable
    {
        [JsonIgnore]
        public SvImage PatternImage { get; set; }
        [JsonProperty]
        public SvMask MaskImage { get; set; }
        [JsonProperty]
        public SvPoint RefPoint { get; set; }
        [JsonIgnore]
        public Mat MaskedPattern { get; set; }

        public PatternData()
        {
            PatternImage = new SvImage();
            MaskImage = new SvMask(PatternImage.Width, PatternImage.Height);
            RefPoint = new SvPoint();
        }

        public PatternData(SvImage _pattern, SvMask _mask = null, SvPoint _point = null)
        {
            if (_pattern == null)
                return;
            else
                PatternImage = _pattern.Clone(true);

            if (_mask == null || _mask.MaskMat.Width != PatternImage.Width || _mask.MaskMat.Height != PatternImage.Height)
                MaskImage = new SvMask(_pattern.Width, _pattern.Height);
            else
                MaskImage = _mask.Clone();

            if (_point == null)
                RefPoint = new SvPoint(_pattern.Width / 2, _pattern.Height / 2);
            else
                RefPoint = new SvPoint(_point.Point3d);
        }

        public void MakeMaskedPattern()
        {
            if (PatternImage.Mat != null && MaskImage.MaskMat != null)
            {
                //MaskedPattern = PatternImage.Mat.Clone();
                if (MaskedPattern != null)
                    MaskedPattern.Dispose();
                MaskedPattern = new Mat();
                PatternImage.Mat.CopyTo(MaskedPattern, MaskImage.MaskMat);
                double mean = (double)MaskedPattern.Mean(MaskImage.MaskMat);
                Cv2.ScaleAdd(new Scalar(255) - MaskImage.MaskMat, mean / 255.0, MaskedPattern, MaskedPattern);
                //SvFunc.ShowTestImg(PatternImage.Mat, 1);
                //SvFunc.ShowTestImg(MaskImage.MaskMat, 1);
                //SvFunc.ShowTestImg(new Scalar(255) - MaskImage.MaskMat, 1);
                //SvFunc.ShowTestImg(MaskedPattern, 1);
            }
        }

        public bool SavePattern(string _path)
        {
            //SvSerializer serialize = new SvSerializer();
            //return serialize.Serializing(_path, this);
            return false;
        }

        public void Dispose()
        {
            if (PatternImage != null) PatternImage.Dispose();
            if (MaskImage != null) MaskImage.Dispose();
        }
    }
}
