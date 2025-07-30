using Development;
using nrt;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Point = System.Windows.Point;
using Rect = OpenCvSharp.Rect;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for TempMatchZeroEdit.xaml
    /// </summary>
    public partial class TempMatchZeroEdit : GridBase, INotifyPropertyChanged
    {
        //UI Element
        private MyLogger logger = new MyLogger("TempMatchZero Edit");
        ContextMenu contextMenu = new ContextMenu();

        //Variable
        public ShapeEditor shEditTrain = new ShapeEditor(UiManager.appSettings.Property.rectSize.Width, UiManager.appSettings.Property.labelFontSize, false);
        public ShapeEditor shEditSearch = new ShapeEditor(UiManager.appSettings.Property.rectSize.Width, UiManager.appSettings.Property.labelFontSize, false);
        public Rectangle rectTrain = new Rectangle();
        public Rectangle rectSearch = new Rectangle();
        public Rect rectTrainCv = new Rect();
        public Rect rectSearchCv = new Rect();

        private Brush colorTrainFill = (Brush)new BrushConverter().ConvertFromString("#40DC143C");
        private Brush colorTrainStroke = (Brush)new BrushConverter().ConvertFromString("#DC143C");
        private Brush colorSearchStroke = (Brush)new BrushConverter().ConvertFromString("#36de1b");
        private Brush colorSearchFill = (Brush)new BrushConverter().ConvertFromString("#2036de1b");
        public Point3d cpPattern = new Point3d();
        public List<UIElement> trainEle = new List<UIElement>();
        private List<UIElement> outEle = new List<UIElement>();
        public List<UIElement> inEle = new List<UIElement>();
        public PatternData patternData = new PatternData();
        public List<PatternData> patternLst = new List<PatternData>();
        public int numPattern = 0;
        private FoundResult resultBox = new FoundResult(0, new Point3d(0, 0, 0), new Point2d[4]);
        private Point3d m_result = new Point3d(0, 0, 0);
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
        public SvImage OutputImage { get; set; } = new SvImage();

        public double OutScore = 0;
        public double OutTranslateX = 0;
        public double OutTranslateY = 0;
        public double OutRotation = 0;

        public double OutOffsetX = 0d;
        public double OutOffsetY = 0d;
        //Binding
        public event PropertyChangedEventHandler PropertyChanged;
        private double _priorityCreteria = 0.75;
        private int _maxCount = 1;
        private string _txtScore = string.Empty;
        private string _txtOffsetX = "", _txtOffsetY = "", _txtRotate = "";
        private bool _isUseROI = true, _isUseROIEnable = true;
        public double PriorityCreteria { get => _priorityCreteria; set { _priorityCreteria = value; OnPropertyChanged(nameof(PriorityCreteria)); } }
        public int MaxCount { get => _maxCount; set { _maxCount = value; OnPropertyChanged(nameof(MaxCount)); } }
        public string TxtScore { get => _txtScore; set { _txtScore = value; OnPropertyChanged(nameof(TxtScore)); } }
        public string TxtOffsetX { get => _txtOffsetX; set { _txtOffsetX = value; OnPropertyChanged(nameof(TxtOffsetX)); } }
        public string TxtOffsetY { get => _txtOffsetY; set { _txtOffsetY = value; OnPropertyChanged(nameof(TxtOffsetY)); } }
        public string TxtRotate { get => _txtRotate; set { _txtRotate = value; OnPropertyChanged(nameof(TxtRotate)); } }
        public bool IsUseROI
        {
            get => _isUseROI; set
            {
                _isUseROI = value;
                if (value == true)
                {
                    CanvasImg.Children.RemoveRange(1, CanvasImg.Children.Count - 1);
                    inEle.ForEach(l => CanvasImg.Children.Add(l));
                }
                else
                {
                    inEle.Clear();
                    for (int i = 1; i < CanvasImg.Children.Count; i++)
                    {
                        inEle.Add(CanvasImg.Children[i]);
                    }
                    CanvasImg.Children.RemoveRange(1, CanvasImg.Children.Count - 1);
                }
                OnPropertyChanged(nameof(IsUseROI));
            }
        }
        public bool IsUseROIEnable { get => _isUseROIEnable; set { _isUseROIEnable = value; OnPropertyChanged(nameof(IsUseROIEnable)); } }

        public bool IsSendData2PLC
        {
            get => (bool)ckbxIsSend2PLC.IsChecked; set
            {
                ckbxIsSend2PLC.IsChecked = value;
                stAddr.IsEnabled = value;
                stVal.IsEnabled = value;
            }
        }

        private Mat mTransform2D = Mat.Eye(3, 3, MatType.CV_64FC1);
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
        public TempMatchZeroEdit()
        {
            InitializeComponent();
            DisplayInit();
            RegisterEvent();
            toolBase.DataContext = this;
        }
        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "Template Match";
            toolBase.cbxImage.Items.Add("[Template Match] Input Image");
            toolBase.cbxImage.Items.Add("[Template Match] Train Image");
            toolBase.cbxImage.Items.Add("[Template Match] Output Image");
            toolBase.cbxImage.SelectedIndex = 0;

            try
            {
                Grid parent = toolBase.gridBase.Parent as Grid;
                parent.Children.Add(this);
                parent.Children.Remove(toolBase.gridBase);

                toolBase.cbxImage.Focusable = false;
                CreatRect(10, 10, 200, 200, 0, colorTrainStroke, colorTrainFill, "T", trainEle);
                rectSearchCv = new Rect(10, 10, 200, 200);
                CreatRect(10, 10, 200, 200, 0, colorSearchStroke, colorSearchFill, "S", inEle);
                rectTrainCv = new Rect(10, 10, 200, 200);

                TabControl tabControl = this.Children.OfType<TabControl>().FirstOrDefault();
                contextMenu = tabControl.FindResource("cmRegion") as ContextMenu;
            }
            catch (Exception ex)
            {
                logger.Create("Display Init Error: " + ex.Message, ex);
            }
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

        public void CreatRect(float left, float top, float width, float height, float angle, Brush stroke, Brush Fill, string name, List<UIElement> eleLst)
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
                if (eleLst.Count > 0) { eleLst.Clear(); }
                eleLst.Add(rect);
                if (CanvasImg.Children.Count > 1)
                    CanvasImg.Children.RemoveRange(1, CanvasImg.Children.Count - 1);
                if (rect.Name == "T")
                {
                    rectTrain = rect;
                    if (oldSelect == 1)
                        CanvasImg.Children.Add(rect);
                }
                else if (rect.Name == "S")
                {
                    rectSearch = rect;
                    if (oldSelect == 0)
                        CanvasImg.Children.Add(rect);
                }
            }
            catch (Exception ex)
            {
                logger.Create("Create Rect Error: " + ex.Message, ex);
            }
        }

        private void Rect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Rectangle senderRect = sender as Rectangle;
                ShapeEditor shEdit = (oldSelect == 0) ? shEditSearch : shEditTrain;
                Rectangle rect = (oldSelect == 0) ? rectSearch : rectTrain;

                shEdit = new ShapeEditor(UiManager.appSettings.Property.rectSize.Width, UiManager.appSettings.Property.labelFontSize, false)
                {
                    //rectSize = (double)UiManager.appSettings.Property.rectSize.Width,
                    //rectSize = 40,
                    Name = (oldSelect == 0) ? "SheS" : "SheT",
                    Focusable = true,
                    IsMulSelect = false,
                };
                //Clear ShapeEditor cũ cùng tên
                foreach (var element in CanvasImg.Children)
                {
                    ShapeEditor a = element as ShapeEditor;
                    if (a != null && a.Name == "SheS")
                    {
                        CanvasImg.Children.Remove(a);
                        break;
                    }
                    else if (a != null && a.Name == "SheT")
                    {
                        CanvasImg.Children.Remove(a);
                        break;
                    }
                }
                rect = senderRect;
                CanvasImg.Children.Add(shEdit);
                shEdit.KeyDown += ShEditTrain_KeyDown;
                shEdit.LostKeyboardFocus += ShEditTrain_LostKeyboardFocus;
                shEdit.CaptureElement(senderRect, e);
                shEdit.Focus();

                switch (oldSelect)
                {
                    case 0:
                        shEditSearch = shEdit;
                        rectSearch = rect;
                        break;
                    case 1:
                        shEditTrain = shEdit;
                        rectTrain = rect;
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Create("Select Rect Error: " + ex.Message, ex);
            }

        }

        private void ShEditTrain_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            //Xác định đang có ít nhất 1 shapeEdtor được tác động và không có cửa sổ ContextMenu cmRegion được mở
            if (!contextMenu.IsOpen)
            {
                ShapeEditor shEdit = (oldSelect == 0) ? shEditSearch : shEditTrain;
                shEdit.Focus();
            }
        }

        private void ShEditTrain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down)
            {
                MoveRoiByKb(e);
            }
        }
        private void MoveRoiByKb(KeyEventArgs e)
        {
            try
            {
                //Kiểm tra xem có đang chọn vào ROI nào không
                if (oldSelect == 0 || oldSelect == 1)
                {
                    ShapeEditor shEdit = (oldSelect == 0) ? shEditSearch : shEditTrain;
                    switch (e.Key)
                    {
                        case Key.Left:
                            Canvas.SetLeft(shEdit, Canvas.GetLeft(shEdit) - 2);
                            break;
                        case Key.Right:
                            Canvas.SetLeft(shEdit, Canvas.GetLeft(shEditTrain) + 2);
                            break;
                        case Key.Up:
                            Canvas.SetTop(shEdit, Canvas.GetTop(shEdit) - 2);
                            break;
                        case Key.Down:
                            Canvas.SetTop(shEdit, Canvas.GetTop(shEdit) + 2);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Move ROI Error: " + ex.Message, ex);
            }
        }
        private void ToolBase_OnPropertyRoi(object sender, RoutedEventArgs e)
        {
            var Point = Mouse.GetPosition(this);
            new RegionProperty().DoConfirmMatrix(new System.Windows.Point(Point.X, Point.Y - 200));
            UpdateProperty();
        }
        private void UpdateProperty()
        {
            try
            {
                ShapeEditor shEdit = (oldSelect == 0) ? shEditSearch : shEditTrain;
                Rectangle rect = (oldSelect == 0) ? rectSearch : rectTrain;

                shEdit.ReleaseElement();
                shEdit.KeyDown -= ShEditTrain_KeyDown;
                shEdit.LostKeyboardFocus -= ShEditTrain_LostKeyboardFocus;
                while (CanvasImg.Children.Count > 1)
                {
                    CanvasImg.Children.RemoveAt(1);
                }
                if (rect == null)
                    return;
                rect.Name = (oldSelect == 0) ? "S" : "T";
                var converter = new BrushConverter();
                RotateTransform rotTrans = rect.RenderTransform as RotateTransform ?? new RotateTransform(0);
                CreatRect((float)Canvas.GetLeft(rect), (float)Canvas.GetTop(rect), (float)rect.Width, (float)rect.Height, (float)rotTrans.Angle, (oldSelect == 0) ? colorSearchStroke : colorTrainStroke, (oldSelect == 0) ? colorSearchFill : colorTrainFill, rect.Name, (oldSelect == 0) ? inEle : trainEle);
                if (oldSelect == 0) { rectSearchCv = new Rect((int)Canvas.GetLeft(rect), (int)Canvas.GetTop(rect), (int)rect.Width, (int)rect.Height); }
                else if (oldSelect == 1) { rectTrainCv = new Rect((int)Canvas.GetLeft(rect), (int)Canvas.GetTop(rect), (int)rect.Width, (int)rect.Height); }
            }
            catch (Exception ex)
            {
                logger.Create("Update Property Error: " + ex.Message, ex);
            }
        }

        private void ToolBase_OnLoadImage(object sender, RoutedEventArgs e)
        {
            toolBase.cbxImage.SelectedIndex = 0;
            oldSelect = 0;
            CanvasImg.Children.RemoveRange(1, CanvasImg.Children.Count - 1);
            outEle.RemoveRange(0, outEle.Count);
        }

        public void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            OnBtnRunClicked?.Invoke(sender, e);
        }

        private void BtnGrabMaster_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (oldSelect == 1)
                {
                    shEditTrain.ReleaseElement();
                    shEditTrain.KeyDown -= ShEditTrain_KeyDown;
                    shEditTrain.LostKeyboardFocus -= ShEditTrain_LostKeyboardFocus;

                    BitmapSource bmpImage = toolBase.imgView.Source as BitmapSource;
                    Mat templateImg = GetROIRegion(bmpImage.ToMat());
                    if (templateImg == null)
                    {
                        System.Windows.MessageBox.Show("There does not select image.");
                        return;
                    }
                    if (this.rectTrain == null)
                    {

                        if (shEditTrain == null) return;
                        this.rectTrain = shEditTrain.rCover;
                        this.rectTrain.RenderTransform = shEditTrain.RenderTransform;
                    }
                    RotateTransform rotTrans = rectTrain.RenderTransform as RotateTransform ?? new RotateTransform(0);
                    double angleRad = (rotTrans.Angle * Math.PI) / 180;

                    cpPattern = new Point3d(Canvas.GetLeft(rectTrain) + templateImg.Width / 2, Canvas.GetTop(rectTrain) + templateImg.Height / 2, angleRad);

                    SvPoint cpTempImg = new SvPoint(templateImg.Width / 2, templateImg.Height / 2, angleRad);
                    //Save
                    patternData = new PatternData(new SvImage(templateImg), null, cpTempImg);
                    imgMaster.Source = templateImg.ToBitmapSource();
                    canvasMaster.Children.RemoveRange(1, canvasMaster.Children.Count - 1);
                    //Vẽ thông tin lên ảnh Master
                    DrawCoordinateMaster();

                    FitMasterImage();
                    ShapeEditor roi = CanvasImg.Children.OfType<ShapeEditor>().FirstOrDefault();
                    roi?.ReleaseElement();
                }
            }
            catch (Exception ex)
            {
                logger.Create("Button Grab Master Error: " + ex.Message, ex);
            }
        }
        public void DrawCoordinateMaster()
        {
            try
            {
                Point2d P0 = new Point2d(canvasMaster.Width / 2, canvasMaster.Height / 2);
                Point2d P1 = new Point2d(P0.X + canvasMaster.Height / 4, P0.Y);
                Point2d P2 = new Point2d(P0.X, P0.Y + canvasMaster.Height / 4);
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
                canvasMaster.Children.Add(lineX);
                canvasMaster.Children.Add(lineY);
                canvasMaster.Children.Add(DrawArrowHead(new Point(P0.X, P0.Y), new Point(P1.X, P1.Y), Brushes.Red));
                canvasMaster.Children.Add(DrawArrowHead(new Point(P0.X, P0.Y), new Point(P2.X, P2.Y), Brushes.Red));
                DrawCross(canvasMaster, P0, canvasMaster.Height / 15, canvasMaster.Height / 15, 0, Brushes.Green);
                Label lbMaster = new Label
                {
                    Foreground = Brushes.Red,
                    Background = Brushes.Transparent,
                    Content = String.Format("({0:F3}, {1:F3}, {2:F3}deg)", cpPattern.X, cpPattern.Y, cpPattern.Z),
                    FontSize = 8,
                };
                Canvas.SetLeft(lbMaster, P0.X + 10d);
                Canvas.SetTop(lbMaster, P0.Y + 10d);
                canvasMaster.Children.Add(lbMaster);
            }
            catch (Exception ex)
            {
                logger.Create("Draw Coordinate Master Error: " + ex.Message, ex);
            }
        }

        public Mat GetROIRegion(Mat image)
        {
            try
            {
                if (image == null || image.IsDisposed) return null;
                if (oldSelect != 1) return null;
                Rectangle roiRectangle = CanvasImg.Children
                                                    .OfType<Rectangle>()
                                                    .FirstOrDefault(r => r.Name == "T");
                if (roiRectangle == null)
                {
                    if (shEditTrain == null) return null;
                    this.rectTrain = shEditTrain.rCover;
                    this.rectTrain.RenderTransform = shEditTrain.RenderTransform;
                }
                var rotTrans = (RotateTransform)this.rectTrain.RenderTransform;
                double rotAngle = (rotTrans != null) ? rotTrans.Angle : 0d;

                // Tính tọa độ 4 góc trước khi xoay
                Point pLT = new Point(Canvas.GetLeft(roiRectangle), Canvas.GetTop(roiRectangle));
                Point centerPoint = new Point(pLT.X + this.rectTrain.ActualWidth / 2, pLT.Y + this.rectTrain.ActualHeight / 2);
                Point pRB = new Point(centerPoint.X + (this.rectTrain.ActualWidth / 2), centerPoint.Y + (this.rectTrain.ActualHeight / 2));
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
                return new Mat(0, 0, new MatType());
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
                logger.Create("Save Pattent Image Error: " + ex.Message, ex);
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
                return new Mat(0, 0, new MatType());
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
                return new Point(0, 0);
            }
        }
        public void FitMasterImage()
        {
            try
            {
                if (imgMaster.Source == null) return;

                double canvasWidth = canvasMaster.Width;
                double canvasHeight = canvasMaster.Height;
                double imageWidth = imgMaster.Source.Width;
                double imageHeight = imgMaster.Source.Height;

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
                imgMaster.RenderTransform = transformGroup;
            }
            catch (Exception ex)
            {
                logger.Create("Fit Image Error: " + ex.Message, ex);
            }
        }
        private void ImgView_MouseLeave(object sender, MouseEventArgs e)
        {
            Point mouse = e.GetPosition(ImgView);
            if (mouse.X < 0 || mouse.X > ImgView.ActualWidth || mouse.Y < 0 || mouse.Y > ImgView.ActualHeight)
            {
                ShapeEditor shEdit = (oldSelect == 0) ? shEditSearch : shEditTrain;
                shEdit.ReleaseElement();
                shEdit.KeyDown -= ShEditTrain_KeyDown;
                shEdit.LostKeyboardFocus -= ShEditTrain_LostKeyboardFocus;
            }
        }
        private void ImgView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShapeEditor shEdit = (oldSelect == 0) ? shEditSearch : shEditTrain;
            shEdit?.ReleaseElement();
        }
        private void CbxImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (toolBase.cbxImage.SelectedIndex == 0)
                {
                    IsUseROIEnable = true;
                    switch (oldSelect)
                    {
                        case 1:
                            trainEle.RemoveRange(0, trainEle.Count);
                            for (int i = 1; i < CanvasImg.Children.Count; i++)
                            {
                                trainEle.Add(CanvasImg.Children[i]);
                            }
                            Rectangle rect = CanvasImg.Children.OfType<Rectangle>().FirstOrDefault();
                            rectTrainCv = new Rect((int)Canvas.GetLeft(rect), (int)Canvas.GetTop(rect), (int)rect.Width, (int)rect.Height);
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
                    if (IsUseROI)
                    {
                        foreach (var ele in inEle)
                        {
                            CanvasImg.Children.Add(ele);
                        }
                    }
                    oldSelect = 0;
                }
                else if (toolBase.cbxImage.SelectedIndex == 1)
                {
                    IsUseROIEnable = false;
                    switch (oldSelect)
                    {
                        case 0:
                            if (IsUseROI)
                            {
                                inEle.RemoveRange(0, inEle.Count);
                                for (int i = 1; i < CanvasImg.Children.Count; i++)
                                {
                                    inEle.Add(CanvasImg.Children[i]);
                                }
                                Rectangle rect = CanvasImg.Children.OfType<Rectangle>().FirstOrDefault();
                                rectSearchCv = new Rect((int)Canvas.GetLeft(rect), (int)Canvas.GetTop(rect), (int)rect.Width, (int)rect.Height);
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
                    foreach (var ele in trainEle)
                    {
                        CanvasImg.Children.Add(ele);
                    }
                    oldSelect = 1;
                }
                else if (toolBase.cbxImage.SelectedIndex == 2)
                {
                    IsUseROIEnable = false;
                    switch (oldSelect)
                    {
                        case 0:
                            if (IsUseROI)
                            {
                                inEle.RemoveRange(0, inEle.Count);
                                for (int i = 1; i < CanvasImg.Children.Count; i++)
                                {
                                    inEle.Add(CanvasImg.Children[i]);
                                }
                                Rectangle rect1 = CanvasImg.Children.OfType<Rectangle>().FirstOrDefault();
                                rectSearchCv = new Rect((int)Canvas.GetLeft(rect1), (int)Canvas.GetTop(rect1), (int)rect1.Width, (int)rect1.Height);
                            }
                            break;
                        case 1:
                            trainEle.RemoveRange(0, trainEle.Count);
                            for (int i = 1; i < CanvasImg.Children.Count; i++)
                            {
                                trainEle.Add(CanvasImg.Children[i]);
                            }
                            Rectangle rect2 = CanvasImg.Children.OfType<Rectangle>().FirstOrDefault();
                            rectTrainCv = new Rect((int)Canvas.GetLeft(rect2), (int)Canvas.GetTop(rect2), (int)rect2.Width, (int)rect2.Height);
                            break;
                    }
                    CanvasImg.Children.RemoveRange(1, CanvasImg.Children.Count - 1);
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
                        //StrokeDashArray = new DoubleCollection() { 4, 2 }, // 4px nét, 2px hở
                    };
                }
                else
                    return new Line();
            }
            catch (Exception ex)
            {
                logger.Create("Create Line Through Center Error: " + ex.Message, ex);
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
        private Polygon DrawArrowHead(Point prev, Point end, Brush colorFill, double arrowSize = 10)
        {
            Polygon arrowHead = new Polygon
            {
                Fill = colorFill
            };
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
        private void DrawCross(Canvas cnvBackGround, Point2d centerP, double _width, double _height, double thetaRad, Brush color)
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
        private bool CheckDeviceDSyntax(string _address)
        {
            return Regex.IsMatch(_address, @"^D[1-9]\d*$");
        }
        private bool CheckDeviceMSyntax(string _address)
        {
            return Regex.IsMatch(_address, @"^M[1-9]\d*$");
        }
        private bool String2Enum(string strDev, out DeviceCode _devType, out string _strDevNo)
        {
            bool isDefined = false;
            string letters = "";
            _devType = DeviceCode.M;
            _strDevNo = "";
            try
            {
                foreach (char synx in strDev)
                {
                    if (char.IsLetter(synx)) { letters += synx; }
                    else if (char.IsDigit(synx)) { _strDevNo += synx; }
                }

                isDefined = Enum.IsDefined(typeof(DeviceCode), letters);
                if (isDefined)
                {
                    _devType = (DeviceCode)Enum.Parse(typeof(DeviceCode), letters);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                logger.Create("Convert syntax error: " + ex.Message);
            }
            return isDefined;
        }
        private void SendDataOffset(string address, int dataOffset)
        {
            try
            {
                String2Enum(address, out DeviceCode _devType, out string _strDevNo);
                UiManager.PLC1.device.WriteDoubleWord(_devType, int.Parse(_strDevNo), dataOffset);
            }
            catch (Exception ex)
            {
                logger.Create($"SendDataOffset to {address} Error: " + ex.Message);
            }
        }
        private void SendBitComplete(string address, bool value)
        {
            try
            {
                String2Enum(address, out DeviceCode _devType, out string _strDevNo);
                UiManager.PLC1.device.WriteBit(_devType, int.Parse(_strDevNo), value);
            }
            catch (Exception ex)
            {
                logger.Create($"SendBitComplete to {address} Error: " + ex.Message);
            }
        }
        public Mat RenderCanvasToMat(Canvas canvas)
        {
            canvas.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            canvas.Arrange(new System.Windows.Rect(canvas.DesiredSize));
            canvas.UpdateLayout();
            int width = runImage.Mat.Width;
            int height = runImage.Mat.Height;

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                width,
                height,
                96, 96, // dpiX, dpiY
                PixelFormats.Pbgra32);

            rtb.Render(canvas);
            BmpBitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Position = 0;
                using (System.Drawing.Bitmap bitmapImg = new System.Drawing.Bitmap(stream))
                {
                    return bitmapImg.ToMat();
                }
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
                if (OutScore == 0) return;
                if (OutScore < PriorityCreteria) return;
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
                Polygon arrHeadX = DrawArrowHead(new Point(linePMX.X1, linePMX.Y1), new Point(linePMX.X2, linePMX.Y2), Brushes.Gray, 20);
                Polygon arrHeadY = DrawArrowHead(new Point(linePMY.X1, linePMY.Y1), new Point(linePMY.X2, linePMY.Y2), Brushes.Gray, 20);

                outEle.Add(linePMX);
                outEle.Add(linePMY);
                outEle.Add(arrHeadX);
                outEle.Add(arrHeadY);

                //if (isEditMode)
                {
                    Point2d P0 = new Point2d(resultBox.CP.X, resultBox.CP.Y);
                    Point2d P1 = P0 + SvFunc.Rotate(new Point2d(runImage.Width / 15, 0), 0);
                    Point2d P2 = P0 + SvFunc.Rotate(new Point2d(0, runImage.Width / 15), 0);

                    //Hiển thị cho result đẹp nhất
                    Polygon polyResult = new Polygon()
                    {
                        Stroke = Brushes.YellowGreen,
                        StrokeThickness = 2,
                        Fill = Brushes.Transparent,
                    };
                    foreach (var point in resultBox.Box)
                    {
                        Point p = new Point(point.X, point.Y);
                        polyResult.Points.Add(p);
                    }
                    outEle.Add(polyResult);

                    //Vẽ đường gióng từ trục tọa độ Master đến điểm thực tế tìm được
                    Line lineViewX = new Line
                    {
                        X1 = resultBox.CP.X,
                        Y1 = linePMX.Y1,
                        X2 = resultBox.CP.X,
                        Y2 = resultBox.CP.Y,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection() { 4, 2 }, // 4px nét, 2px hở
                    };
                    Line lineViewY = new Line
                    {
                        X1 = linePMY.X1,
                        Y1 = resultBox.CP.Y,
                        X2 = resultBox.CP.X,
                        Y2 = resultBox.CP.Y,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection() { 4, 2 }, // 4px nét, 2px hở
                    };
                    outEle.Add(lineViewX);
                    outEle.Add(lineViewY);

                    //Vẽ điểm gióng
                    Ellipse elipX = new Ellipse
                    {
                        Height = 5,
                        Width = 5,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Fill = Brushes.Gray,
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        RenderTransform = new TranslateTransform() { X = -2.5, Y = -2.5 }
                    };
                    Canvas.SetLeft(elipX, lineViewX.X1);
                    Canvas.SetTop(elipX, lineViewX.Y1);
                    Ellipse elipY = new Ellipse
                    {
                        Height = 5,
                        Width = 5,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Fill = Brushes.Gray,
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        RenderTransform = new TranslateTransform() { X = -2.5, Y = -2.5 }
                    };
                    Canvas.SetLeft(elipY, lineViewY.X1);
                    Canvas.SetTop(elipY, lineViewY.Y1);
                    outEle.Add(elipX);
                    outEle.Add(elipY);

                    //Vẽ thông tin Point/Offset & Score
                    //string tmp = string.Format("Offset : ({0:F3}, {1:F3}, {2:F3}deg)\n\rScore : {3:F3}", resultBox.CP.X, resultBox.CP.Y, resultBox.CP.Z, resultBox.Score);
                    string tmp = string.Format("Offset : ({0}, {1}, {2}deg)\n\rScore : {3:F3}", TxtOffsetX, TxtOffsetY, TxtRotate, resultBox.Score);
                    Label lbData1 = new Label()
                    {
                        Style = StyleLb,
                        Content = tmp,
                    };
                    Canvas.SetLeft(lbData1, P0.X + 10);
                    Canvas.SetTop(lbData1, P0.Y + 10);
                    outEle.Add(lbData1);
                    foreach (var ele in outEle)
                        CanvasImg.Children.Add(ele);
                    DrawCross(CanvasImg, new Point2d(resultBox.CP.X, resultBox.CP.Y), 20, 20, resultBox.CP.Z, Brushes.Green);

                    CanvasImg.InvalidateVisual();
                    if (runImage.Mat.Channels() < 3)
                    {
                        runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.GRAY2RGB);
                    }
                    OutputImage.Mat = runImage.Mat.Clone();
                    Cv2.Line(OutputImage.Mat, new OpenCvSharp.Point(0, (int)cpPattern.Y), new OpenCvSharp.Point(OutputImage.Mat.Width, (int)cpPattern.Y), Scalar.LightGreen, 2);
                    Cv2.Line(OutputImage.Mat, new OpenCvSharp.Point((int)cpPattern.X, 0), new OpenCvSharp.Point(cpPattern.X, OutputImage.Mat.Height), Scalar.LightGreen, 2);
                    Cv2.Line(OutputImage.Mat, new OpenCvSharp.Point((int)resultBox.CP.X - 12, (int)resultBox.CP.Y), new OpenCvSharp.Point((int)resultBox.CP.X + 12, (int)resultBox.CP.Y), Scalar.Red, 2);
                    Cv2.Line(OutputImage.Mat, new OpenCvSharp.Point((int)resultBox.CP.X, (int)resultBox.CP.Y - 12), new OpenCvSharp.Point((int)resultBox.CP.X, (int)resultBox.CP.Y + 12), Scalar.Red, 2);
                }
            }
            catch (Exception ex)
            {
                logger.Create("Display Result Error: " + ex.Message, ex);
            }
        }

        private SvImage runImage = new SvImage();
        public override void Run()
        {
            ShapeEditor shEdit = (oldSelect == 0) ? shEditSearch : shEditTrain;
            shEdit.ReleaseElement();
            shEdit.KeyDown -= ShEditTrain_KeyDown;
            shEdit.LostKeyboardFocus -= ShEditTrain_LostKeyboardFocus;

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

                if (patternData.PatternImage == null || patternData.PatternImage.Mat == null || patternData.PatternImage.Mat.Width <= 0 || patternData.PatternImage.Mat.Height <= 0)
                {
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Can not find any Pattern Image!");
                    return;
                }
                Mat imageSearch = new Mat();
                if (IsUseROI)
                {
                    if (rectSearch.Width < rectTrain.Width || rectSearch.Height < rectTrain.Height)
                    {
                        meaRunTime.Stop();
                        toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "ROI Search Size must be larger than ROI Train Size!");
                        return;
                    }
                    Rect rectRoi = new Rect((int)Canvas.GetLeft(rectSearch), (int)Canvas.GetTop(rectSearch), (int)rectSearch.Width, (int)rectSearch.Height);
                    if (rectRoi.X < 0 || rectRoi.Y < 0 || rectRoi.Right > runImage.Mat.Width || rectRoi.Bottom > runImage.Mat.Height)
                    {
                        meaRunTime.Stop();
                        toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "ROI Search is out of Image!");
                        return;
                    }
                    imageSearch = new Mat(runImage.Mat, rectRoi);
                }
                else
                {
                    imageSearch = runImage.Mat.Clone();
                }


                double maxVal = 0, maxValLast = 0;
                Point2d[] boundBox = new Point2d[4];
                OpenCvSharp.Point maxPoint = new OpenCvSharp.Point();
                OpenCvSharp.Point maxPointLast = new OpenCvSharp.Point();
                Mat result1 = new Mat(), result2 = new Mat();
                Mat tempImage = patternData.PatternImage.Mat.Clone();
                if (tempImage.Channels() != runImage.Mat.Channels())
                {
                    if (tempImage.Channels() > runImage.Mat.Channels())
                    {
                        tempImage = tempImage.CvtColor(ColorConversionCodes.BGR2GRAY);
                    }
                    else
                    {
                        tempImage = tempImage.CvtColor(ColorConversionCodes.GRAY2BGR);
                    }
                }
                for (int i = 0; i < MaxCount; i++)
                {
                    result1 = imageSearch.MatchTemplate(tempImage, TemplateMatchModes.CCoeffNormed);
                    //Cv2.Flip(tempImage, tempImage, FlipMode.X);
                    //Cv2.Flip(tempImage, tempImage, FlipMode.Y);
                    //result2 = runImage.Mat.MatchTemplate(tempImage, TemplateMatchModes.CCoeffNormed);
                    result1.MinMaxLoc(out double minVal1, out double maxVal1, out OpenCvSharp.Point minPoint1, out OpenCvSharp.Point maxPoint1);
                    //result2.MinMaxLoc(out double minVal2, out double maxVal2, out OpenCvSharp.Point minPoint2, out OpenCvSharp.Point maxPoint2);
                    //maxVal = Math.Max(maxVal1, maxVal2);
                    //maxPoint = (maxVal1 > maxVal2) ? maxPoint1 : maxPoint2;

                    maxValLast = Math.Max(maxValLast, maxVal1);
                    if (maxValLast == maxVal1)
                    {
                        maxPointLast = maxPoint1;
                    }
                }
                maxValLast = Math.Round(maxValLast, 3);
                OutScore = maxValLast;
                TxtScore = OutScore.ToString();

                if (IsUseROI)
                {
                    //Tạo matrix fixture từ imageSearch về Image gốc
                    Mat translateMat = new Mat(3, 3, MatType.CV_64FC1, new double[] {   1, 0, Canvas.GetLeft(rectSearch),
                                                                                0, 1, Canvas.GetTop(rectSearch),
                                                                                0, 0, 1});
                    maxPointLast = SvFunc.ImageToFixture(maxPointLast, translateMat);
                }
                if (maxValLast >= PriorityCreteria)
                {
                    boundBox[0] = new Point2d(maxPointLast.X, maxPointLast.Y);
                    boundBox[1] = new Point2d(maxPointLast.X + patternData.PatternImage.Width, maxPointLast.Y);
                    boundBox[2] = new Point2d(maxPointLast.X + patternData.PatternImage.Width, maxPointLast.Y + patternData.PatternImage.Height);
                    boundBox[3] = new Point2d(maxPointLast.X, maxPointLast.Y + patternData.PatternImage.Height);

                    Point2d cp = new Point2d((boundBox[0].X + boundBox[2].X) / 2, (boundBox[0].Y + boundBox[2].Y) / 2);

                    resultBox = new FoundResult(maxValLast, new Point3d(cp.X, cp.Y, 0), boundBox);

                    //Đọc hiểu lại
                    Mat matRot = Cv2.GetRotationMatrix2D(new Point2f((float)resultBox.Box[0].X, (float)resultBox.Box[0].Y), -resultBox.CP.Z * 180 / Math.PI, 1);

                    Mat resultRefMat = new Mat(3, 1, MatType.CV_64FC1, new double[] { patternData.RefPoint.X + resultBox.Box[0].X, patternData.RefPoint.Y + resultBox.Box[0].Y, 1 });
                    resultRefMat = matRot * resultRefMat;
                    m_result = new Point3d(resultRefMat.At<double>(0, 0), resultRefMat.At<double>(1, 0), resultBox.CP.Z);

                    //CHuyển sang hệ tọa độ Fixture
                    m_result = SvFunc.ImageToFixture3D(m_result, runImage.TransformMat);
                    for (int j = 0; j < resultBox.Box.Length; j++)
                        resultBox.Box[j] = SvFunc.ImageToFixture2D(resultBox.Box[j], runImage.TransformMat);

                    resultBox.CP = SvFunc.ImageToFixture3D(resultBox.CP, runImage.TransformMat);

                    //OutTranslateX = resultBox.CP.X - cpPattern.X;
                    //OutTranslateY = resultBox.CP.Y - cpPattern.Y;
                    //OutRotation = (resultBox.CP.Z - cpPattern.Z) * 180f / Math.PI;

                    //TxtOffsetX = (OutTranslateX).ToString("F3");
                    //TxtOffsetY = (OutTranslateY).ToString("F3");
                    //TxtRotate = (OutRotation).ToString("F3");

                    OutTranslateX = m_result.X;
                    OutTranslateY = m_result.Y;
                    OutRotation = m_result.Z * 180f / Math.PI;

                    TxtOffsetX = (OutTranslateX - cpPattern.X).ToString("F3");
                    TxtOffsetY = (OutTranslateY - cpPattern.Y).ToString("F3");
                    TxtRotate = (OutRotation - cpPattern.Z).ToString("F3");
                    OutOffsetX = OutTranslateX - cpPattern.X;
                    OutOffsetY = OutTranslateY - cpPattern.Y;

                    if ((bool)ckbxIsUsePx2mm.IsChecked)
                    {
                        OutOffsetX *= (double)numUDPx2mm.Value;
                        OutOffsetY *= (double)numUDPx2mm.Value;
                    }
                    OutOffsetX = Math.Round(OutOffsetX, 3);
                    OutOffsetY = Math.Round(OutOffsetY, 3);
                    txtValX.Text = OutOffsetX.ToString();
                    txtValY.Text = OutOffsetY.ToString();

                    if ((bool)ckbxIsSend2PLC.IsChecked)
                    {
                        if (!String.IsNullOrEmpty(txtAddrX.Text) && CheckDeviceDSyntax(txtAddrX.Text))
                        {
                            SendDataOffset(txtAddrX.Text, (int)(OutOffsetX * 1000));
                        }
                        else
                        {
                            MessageBox.Show("Address PLC Empty or Error Syntax!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            meaRunTime.Stop();
                            toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Address PLC Empty or Error Syntax!");
                            return;
                        }
                        if (!String.IsNullOrEmpty(txtAddrY.Text) && CheckDeviceDSyntax(txtAddrY.Text))
                        {
                            SendDataOffset(txtAddrY.Text, (int)(OutOffsetY * 1000));
                        }
                        else
                        {
                            MessageBox.Show("Address PLC Empty or Error Syntax!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            meaRunTime.Stop();
                            toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Address PLC Empty or Error Syntax!");
                            return;
                        }
                        if (!String.IsNullOrEmpty(txtAddrCpl.Text) && CheckDeviceMSyntax(txtAddrCpl.Text))
                        {
                            if (txtValCpl.Text.ToLower() == "true" || txtValCpl.Text.ToLower() == "false")
                            {
                                SendBitComplete(txtAddrCpl.Text, (txtValCpl.Text.ToLower() == "true" ? true : false));
                            }
                            else
                            {
                                MessageBox.Show("Value Bit Complete Error Syntax!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                meaRunTime.Stop();
                                toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Value Bit Complete Error Syntax!");
                                return;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Address PLC Empty or Error Syntax!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            meaRunTime.Stop();
                            toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Address PLC Empty or Error Syntax!");
                            return;
                        }
                    }
                }
                else
                {
                    OutTranslateX = 0d;
                    OutTranslateY = 0d;
                    OutRotation = 0d;

                    TxtOffsetX = "0.00";
                    TxtOffsetY = "0.00";
                    TxtRotate = "0.00";

                    txtValX.Text = "0.00";
                    txtValY.Text = "0.00";
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Score < Priority Creteria!");
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.Create("Run Error: " + ex.Message, ex);
            }
        }

        //Run so sánh với nhiều Pattern
        public void Run1()
        {
            if (this.InputImage == null || this.InputImage.Mat == null || this.InputImage.Mat.Width <= 0 || this.InputImage.Mat.Height <= 0)
            {
                if (toolBase.isImgPath)
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
            else if (InputImage.Mat != null && toolBase.isImgPath)
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
            foreach (var pattern in patternLst)
            {
                if (pattern.PatternImage == null || pattern.PatternImage.Mat == null || pattern.PatternImage.Mat.Width <= 0 || pattern.PatternImage.Mat.Height <= 0)
                {
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Can not find any Pattern Image!");
                    return;
                }
            }

            numPattern = patternLst.Count;
            double maxValLast = 0;
            Point2d[] boundBox = new Point2d[4];
            OpenCvSharp.Point maxPointLast = new OpenCvSharp.Point();
            PatternData patternLast = new PatternData();
            for (int j = 0; j < patternLst.Count; j++)
            {
                double maxVal = 0;
                OpenCvSharp.Point maxPoint = new OpenCvSharp.Point();
                Mat result1 = new Mat();
                Mat tempImage = patternLst[j].PatternImage.Mat.Clone();
                if (tempImage.Channels() != runImage.Mat.Channels())
                {
                    if (tempImage.Channels() > runImage.Mat.Channels())
                    {
                        tempImage = tempImage.CvtColor(ColorConversionCodes.BGR2GRAY);
                    }
                    else
                    {
                        tempImage = tempImage.CvtColor(ColorConversionCodes.GRAY2BGR);
                    }
                }
                for (int i = 0; i < MaxCount; i++)
                {
                    result1 = runImage.Mat.MatchTemplate(tempImage, TemplateMatchModes.CCoeffNormed);
                    result1.MinMaxLoc(out double minVal1, out double maxVal1, out OpenCvSharp.Point minPoint1, out OpenCvSharp.Point maxPoint1);

                    maxVal = Math.Max(maxVal, maxVal1);
                    if (maxVal == maxVal1)
                    {
                        maxPoint = maxPoint1;
                    }
                }

                maxValLast = Math.Max(maxValLast, maxVal);
                if (maxValLast == maxVal)
                {
                    maxPointLast = maxPoint;
                    patternLast = patternLst[j];
                }
            }

            maxValLast = Math.Round(maxValLast, 3);
            if (maxValLast >= PriorityCreteria)
            {
                boundBox[0] = new Point2d(maxPointLast.X, maxPointLast.Y);
                boundBox[1] = new Point2d(maxPointLast.X + patternLast.PatternImage.Width, maxPointLast.Y);
                boundBox[2] = new Point2d(maxPointLast.X + patternLast.PatternImage.Width, maxPointLast.Y + patternLast.PatternImage.Height);
                boundBox[3] = new Point2d(maxPointLast.X, maxPointLast.Y + patternLast.PatternImage.Height);

                Point2d cp = new Point2d((boundBox[0].X + boundBox[2].X) / 2, (boundBox[0].Y + boundBox[2].Y) / 2);

                resultBox = new FoundResult(maxValLast, new Point3d(cp.X, cp.Y, 0), boundBox);
                OutScore = maxValLast;
                TxtScore = OutScore.ToString();

                Mat matRot = Cv2.GetRotationMatrix2D(new Point2f((float)resultBox.Box[0].X, (float)resultBox.Box[0].Y), -resultBox.CP.Z * 180 / Math.PI, 1);

                Mat resultRefMat = new Mat(3, 1, MatType.CV_64FC1, new double[] { patternLast.RefPoint.X + resultBox.Box[0].X, patternLast.RefPoint.Y + resultBox.Box[0].Y, 1 });
                resultRefMat = matRot * resultRefMat;
                m_result = new Point3d(resultRefMat.At<double>(0, 0), resultRefMat.At<double>(1, 0), resultBox.CP.Z);

                // Fixture
                m_result = SvFunc.ImageToFixture3D(m_result, runImage.TransformMat);
                for (int j = 0; j < resultBox.Box.Length; j++)
                    resultBox.Box[j] = SvFunc.ImageToFixture2D(resultBox.Box[j], runImage.TransformMat);

                resultBox.CP = SvFunc.ImageToFixture3D(resultBox.CP, runImage.TransformMat);

                OutTranslateX = m_result.X;
                OutTranslateY = m_result.Y;
                OutRotation = m_result.Z * 180f / Math.PI;
            }
        }
    }
}
