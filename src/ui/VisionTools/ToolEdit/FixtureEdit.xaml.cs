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
using Xceed.Wpf.AvalonDock.Controls;
using Point = System.Windows.Point;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for FixtureEdit.xaml
    /// </summary>
    public partial class FixtureEdit : GridBase, INotifyPropertyChanged
    {
        private MyLogger logger = new MyLogger("Fixture Edit");

        //InOut
        private SvImage _inputImage = new SvImage();
        public SvImage InputImage
        {
            get => _inputImage; 
            set
            {
                _inputImage = value;
                if (value == null) return;
                if (_inputImage.Mat.Height > 0 && _inputImage.Mat.Width > 0)
                {
                    toolBase.imgView.Source = _inputImage.Mat.ToBitmapSource();
                }
            }
        }
        public double InScaleX { get; set; } = 1;
        public double InScaleY { get; set; } = 1;
        public SvImage OutputImage { get; set; } = new SvImage();

        //Variable
        List<UIElement> outEle = new List<UIElement>();
        public event RoutedEventHandler OnBtnRunClicked;

        //Binding
        double _translateX, _translateY, _rotation;
        public event PropertyChangedEventHandler PropertyChanged;
        public double InTranslateX { get => _translateX; set { _translateX = value; OnPropertyChanged(nameof(InTranslateX)); } }
        public double InTranslateY { get => _translateY; set { _translateY = value; OnPropertyChanged(nameof(InTranslateY)); } }
        public double InScale { get { return (InScaleX + InScaleY) / 2; } set { InScaleX = value; InScaleY = value; OnPropertyChanged(nameof(InScale)); } }
        public double InRotation 
        {
            get
            {
                double value = _rotation;

                if (btnConvertDegRad.Content.ToString() == "Deg")
                    value = (double)(value * Math.PI / 180);
                return value;
            } 
            set 
            {
                double val = value;
                if (btnConvertDegRad.Content.ToString() == "Deg")
                    val = (double)(val / Math.PI * 180);
                _rotation = val;
                OnPropertyChanged(nameof(InRotation)); 
            } 
        }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public FixtureEdit()
        {
            InitializeComponent();
            DisplayInit();
            RegisterEvent();
            toolBase.DataContext = this; // Set the DataContext
        }
        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "Fixture";
            toolBase.cbxImage.Items.Add("[Fixture] Input Image");
            toolBase.cbxImage.Items.Add("[Fixture] Output Image");

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
                    foreach (var ele in outEle)
                    {
                        CanvasImg.Children.Add(ele);
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

        private void BtnConvertDegRad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                double value = InRotation;
                if (btn.Content.ToString() == "Deg")
                {
                    btn.Content = "Rad";
                    InRotation = (double)(value * Math.PI / 180);
                }
                else
                {
                    btn.Content = "Deg";
                    InRotation = (double)(value / Math.PI * 180);
                }
            }
            catch (Exception ex)
            {
                logger.Create("Conver Between Deg & Rad Error: " + ex.Message, ex);
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            InTranslateX = 0;
            InTranslateY = 0;
            InScale = 1;
            InRotation = 0;
        }

        #region InputValue
        Mat translate
        {
            get
            {
                if (double.IsNaN(InTranslateX) || double.IsNaN(InTranslateY))
                    return Mat.Eye(3, 3, MatType.CV_64FC1);
                return new Mat(3, 3, MatType.CV_64FC1, new double[] { 1, 0, InTranslateX, 0, 1, InTranslateY, 0, 0, 1 });
            }
        }
        Mat RotationMat
        {
            get
            {
                if (double.IsNaN(InRotation))
                    return Mat.Eye(3, 3, MatType.CV_64FC1);
                if(btnConvertDegRad.Content.ToString() == "Deg")
                {
                    double angleRad = (InRotation/ 180) * Math.PI;
                    return new Mat(3, 3, MatType.CV_64FC1, new double[] { Math.Cos(angleRad), -Math.Sin(angleRad), 0, Math.Sin(angleRad), Math.Cos(angleRad), 0, 0, 0, 1 });
                }
                else
                    return new Mat(3, 3, MatType.CV_64FC1, new double[] { Math.Cos(InRotation), -Math.Sin(InRotation), 0, Math.Sin(InRotation), Math.Cos(InRotation), 0, 0, 0, 1 });
            }
        }
        Mat scaleMat
        {
            get
            {
                if (InScaleX == 0 || InScaleY == 0) return Mat.Eye(3, 3, MatType.CV_64FC1);
                return new Mat(3, 3, MatType.CV_64FC1, new double[] { InScaleX, 0, 0, 0, InScaleY, 0, 0, 0, 1 });
            }
        }

        Mat transform2D = Mat.Eye(3, 3, MatType.CV_64FC1);
        public Mat Transform2D
        {
            get
            {
                transform2D = (translate * (RotationMat * scaleMat));
                //PrintMat(translate);
                //PrintMat(RotationMat);
                //PrintMat(scaleMat);
                //PrintMat(transform2D);

                transform2D = transform2D.Inv();
                //PrintMat(transform2D);
                return transform2D;
            }
        }

        //[InputParam]
        //public float[] CalibrationMatF
        //{
        //    get
        //    {
        //        if (transform2D == null) return null;
        //        float[] calMatF = new float[9];
        //        for (int i = 0; i < 3; i++)
        //        {
        //            for (int j = 0; j < 3; j++)
        //            {
        //                calMatF[i * 3 + j] = (float)transform2D.At<double>(i, j);
        //            }
        //        }
        //        return calMatF;
        //    }
        //    set
        //    {
        //        if (value == null) return;
        //        Mat temp = new Mat(3, 3, MatType.CV_32FC1, value);
        //        if (transform2D != null) transform2D.Dispose();

        //        // Fixture transform Matrix는 FixturetoImage Matrix 이므로 역행렬을 구해야 동일 변환(방향)임
        //        if (temp.Determinant() == 0)
        //            transform2D = Mat.Eye(3, 3, MatType.CV_32FC1);
        //        else
        //            transform2D = temp.Clone();
        //    }
        //}

        #endregion
        private Polygon DrawArrowHead(Point prev, Point end)
        {
            Polygon arrowHead = new Polygon
            {
                Fill = Brushes.Aqua,
            };
            try
            {
                double arrowSize = 20;
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
                logger.Create("Draw ArrowHead Error: " + ex.Message, ex);
            }
            return arrowHead;
        }
        void PrintMat(Mat mat)
        {
            for (int i = 0; i < mat.Rows; i++)
            {
                for (int j = 0; j < mat.Cols; j++)
                {
                    Console.Write(mat.At<double>(i, j).ToString("F6") + "\t");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
        public void DisplayResult()
        {
            try
            {
                if (!meaRunTime.IsRunning) return;
                //if (!isEditMode) return;

                if (OutputImage == null || OutputImage.Mat == null || OutputImage.Width <= 0 || OutputImage.Height <= 0)
                {
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "OutputImage is null or error!");
                    return;
                }
                if (Transform2D == null)
                {
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Transform Matrix is null!");
                    return;
                }
                if (Transform2D.Determinant() == 0)
                {
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Transform Matrix can not determine!");
                    return;
                }

                toolBase.cbxImage.SelectedIndex = 1;
                oldSelect = 1;
                CanvasImg.Children.RemoveRange(1, CanvasImg.Children.Count - 1);
                outEle.RemoveRange(0, outEle.Count);

                Style StyleLb = new Style(typeof(Label));
                StyleLb.Setters.Add(new Setter(Label.FontSizeProperty, 20.0));
                StyleLb.Setters.Add(new Setter(Label.ForegroundProperty, Brushes.Aqua));
                StyleLb.Setters.Add(new Setter(Label.BackgroundProperty, Brushes.Transparent));

                //Point2d P0 = new Point2d(0, 0);
                //Point2d PX, PY;
                //P0 = new Point2d(InTranslateX, InTranslateY);
                //PX = P0 + SvFunc.Rotate(new Point2d(OutputImage.Width / 20, 0), InRotation);
                //PY = P0 + SvFunc.Rotate(new Point2d(0, OutputImage.Width / 20), InRotation);

                double size = 1;

                Point2d P0 = new Point2d(0f, 0f);
                Point2d PX = new Point2d(size, 0);
                Point2d PY = new Point2d(0, size);
                //PrintMat(transform2D);
                Point2d e_x = (SvFunc.FixtureToImage2D(PX, transform2D) - SvFunc.FixtureToImage2D(P0, transform2D));
                Point2d e_y = (SvFunc.FixtureToImage2D(PY, transform2D) - SvFunc.FixtureToImage2D(P0, transform2D));
                while (e_x.DistanceTo(new Point2d()) < 100)
                {
                    e_x *= 10; e_y *= 10; size *= 10;
                }
                while (e_x.DistanceTo(new Point2d()) > 1000)
                {
                    e_x *= 0.1; e_y *= 0.1; size *= 0.1;
                }

                PX = PX * size;
                PY = PY * size;
                P0 = SvFunc.FixtureToImage2D(P0, Transform2D);
                PX = SvFunc.FixtureToImage2D(PX, Transform2D);
                PY = SvFunc.FixtureToImage2D(PY, Transform2D);
                Line lineX = new Line()
                {
                    Stroke = Brushes.Aqua,
                    StrokeThickness = 4,
                    X1 = P0.X,
                    X2 = PX.X,
                    Y1 = P0.Y,
                    Y2 = PX.Y
                };
                Line lineY = new Line()
                {
                    Stroke = Brushes.Aqua,
                    StrokeThickness = 4,
                    X1 = P0.X,
                    X2 = PY.X,
                    Y1 = P0.Y,
                    Y2 = PY.Y
                };
                //Vẽ mũi tên
                outEle.Add(lineX);
                outEle.Add(lineY);
                outEle.Add(DrawArrowHead(new Point(P0.X, P0.Y), new Point(PX.X, PX.Y)));
                outEle.Add(DrawArrowHead(new Point(P0.X, P0.Y), new Point(PY.X, PY.Y)));

                Label lbX = new Label() { Style = StyleLb, Content = "X" };
                Canvas.SetLeft(lbX, PX.X + 20);
                Canvas.SetTop(lbX, PX.Y + 20);
                Label lbY = new Label() { Style = StyleLb, Content = "Y" };
                Canvas.SetLeft(lbY, PY.X + 20);
                Canvas.SetTop(lbY, PY.Y + 20);
                outEle.Add(lbX);
                outEle.Add(lbY);
                outEle.ForEach(ele => CanvasImg.Children.Add(ele));
            }
            catch (Exception ex)
            {
                logger.Create("Display Image Error: " + ex.Message, ex);
            }
        }
        
        private SvImage runImage = new SvImage();
        public override void Run()
        {
            try
            {
                if (this.InputImage == null || this.InputImage.Mat == null || this.InputImage.Mat.Width <= 0 || this.InputImage.Mat.Height <= 0)
                {
                    if (toolBase.isImgPath)
                    {
                        runImage.Mat = (ImgView.Source as BitmapSource).ToMat();
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
                }
                else if (InputImage.Mat != null && InputImage.Mat.Width > 0 && InputImage.Mat.Height > 0 && !toolBase.isImgPath)
                {
                    runImage = this.InputImage.Clone(true);
                }
                OutputImage?.Dispose();
                OutputImage = InputImage.Clone(true);
                OutputImage.TransformMat = Transform2D;
                OutputImage.CenterPoint = new Point3d(InTranslateX, InTranslateY, InRotation);
                //PrintMat(OutputImage.TransformMat);
            }
            catch (Exception ex)
            {
                logger.Create("Run Error: " + ex.Message, ex);
            }
        }
    }
}
