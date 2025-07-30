using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using VisionInspection;
using Xceed.Wpf.AvalonDock.Controls;
using Point = System.Windows.Point;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for EditRegionEdit.xaml
    /// </summary>
    public partial class EditRegionEdit : GridBase, INotifyPropertyChanged
    {
        //UI Element
        private MyLogger logger = new MyLogger("EditRegion Edit");
        private ContextMenu contextMenu = new ContextMenu();

        //Variables
        public List<Tuple<int, SvImage, double>> imgLst = new List<Tuple<int, SvImage, double>>();
        public List<Tuple<int, Point[], double>> rectPointLst = new List<Tuple<int, Point[], double>>();
        List<ShapeEditor> ShapeEditorControls = new List<ShapeEditor>();
        public List<Rectangle> rectCurLst = new List<Rectangle>();
        public List<Rectangle> rectCoppyLst = new List<Rectangle>();
        public List<double> angleCurLst = new List<double>();
        public List<double> angleCopyLst = new List<double>();
        public List<System.Windows.Shapes.Rectangle> RectLst = new List<System.Windows.Shapes.Rectangle>();
        public List<Label> LabelLst = new List<Label>();
        private bool isCoppy = false;
        private Brush colorRectFill = (Brush)new BrushConverter().ConvertFromString("#40DC143C");
        private Brush colorRectStroke = (Brush)new BrushConverter().ConvertFromString("#DC143C");
        private Canvas canvasCoppy = new Canvas();
        public List<UIElement> outEle = new List<UIElement>();
        public List<UIElement> inEle = new List<UIElement>();
        List<Point> centPointRects = new List<Point>();
        public event RoutedEventHandler OnBtnRunClicked;
        public event RoutedEventHandler OnBtnRefreshClicked;
        ObservableCollection<TableRow> tableData = new ObservableCollection<TableRow>();
        //public List<Tuple<int, int, bool>> allCheckedStates = new List<Tuple<int, int, bool>>();
        public List<List<Tuple<int, bool>>> resultCkbList = new List<List<Tuple<int, bool>>>();
        // Danh sách chứa các danh sách ảnh mới
        public List<List<Tuple<int, SvImage, double>>> imageSubList = new List<List<Tuple<int, SvImage, double>>>();
        //public Point3d coordTrans = new Point3d();
        public List<Mat> transform2Ds = new List<Mat>();
        public List<Point> transPoints = new List<Point>();
        public Point3d centInputImg = new Point3d(0,0,0);

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
        public List<Tuple<int, SvImage, double>> OutImageList { get => imgLst; set => imgLst = value; }

        //Binding
        public event PropertyChangedEventHandler PropertyChanged;
        private int _regionIndex, _regionCount;
        public int RegionIndex { get => _regionIndex; set { _regionIndex = value; OnPropertyChanged(nameof(RegionIndex)); } }
        public int RegionCount { get => _regionCount; set { _regionCount = value; OnPropertyChanged(nameof(RegionCount)); } }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public EditRegionEdit()
        {
            InitializeComponent();
            DisplayInit();
            RegisterEvent();
            RoiShowCheck();
            toolBase.DataContext = this;
        }
        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "Edit Region";
            toolBase.cbxImage.Items.Add("[Edit Region] Input Image");
            toolBase.cbxImage.Items.Add("[Edit Region] Output Image");
            toolBase.cbxImage.SelectedIndex = 0;

            try
            {
                Grid parent = toolBase.gridBase.Parent as Grid;
                parent.Children.Add(this);
                parent.Children.Remove(toolBase.gridBase);

                TabControl tabControl = this.Children.OfType<TabControl>().FirstOrDefault();
                contextMenu = tabControl.FindResource("cmRegion") as ContextMenu;
                GenerateTable(1, imgLst);
            }
            catch (Exception ex)
            {
                logger.Create("Display Init Error: " + ex.Message, ex);
            }
        }
        protected override void RegisterEvent()
        {
            this.KeyDown += EditRegionEdit_KeyDown;
            ImgView.MouseLeftButtonDown += ImgView_MouseLeftButtonDown;
            ImgView.MouseLeave += ImgView_MouseLeave;
            ImgView.MouseRightButtonDown += ImgView_MouseRightButtonDown;
            toolBase.btnRun.Click += BtnRun_Click;
            toolBase.cbxImage.SelectionChanged += CbxImage_SelectionChanged;
            toolBase.OnDeleteRoi += ToolBase_OnDeleteRoi;
            toolBase.OnMatrixRoi += ToolBase_OnMatrixRoi;
            toolBase.OnPropertyRoi += ToolBase_OnPropertyRoi;
            dataGrid.MouseLeave += DataGrid_MouseLeave;
        }

        private void DataGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            btnRefresh.Focusable = true;
            btnRefresh.Focus();
        }

        //public void UpdateTransformMat1()
        //{
        //    coordTrans = SvFunc.FixtureToImage3D(new Point3d(0, 0, 0), runImage.TransformMat);
        //    Point3d[] rectTransVal = new Point3d[RectLst.Count];
        //    transform2Ds.Clear();
        //    for (int i = 0; i < RectLst.Count; i++)
        //    {
        //        Point LT = new Point(Canvas.GetLeft(RectLst[i]), Canvas.GetTop(RectLst[i]));
        //        Point centerPoint = new Point(LT.X + RectLst[i].Width / 2, LT.Y + RectLst[i].Height / 2);
        //        //Point centerPoint = RectLst[i].TransformToAncestor(CanvasImg).Transform(new Point(RectLst[i].Width / 2, RectLst[i].Height / 2));
        //        rectTransVal[i].X = centerPoint.X - coordTrans.X;
        //        rectTransVal[i].Y = centerPoint.Y - coordTrans.Y;

        //        //Lấy góc xoay của ROI
        //        //RotateTransform thisRot = rectLst[i].RenderTransform as RotateTransform
        //        //double angleDeg = (thisRot != null) ? thisRot.Angle : 0d;
        //        //double angleRad = (angleDeg / 180) * Math.PI;
        //        Mat translate = new Mat(3, 3, MatType.CV_64FC1, new double[] { 1, 0, rectTransVal[i].X, 0, 1, rectTransVal[i].Y, 0, 0, 1 });
        //        //Mat rotate = new Mat(3, 3, MatType.CV_64FC1, new double[] { Math.Cos(coordTrans.Z + angleRad), -Math.Sin(coordTrans.Z + angleRad), 0, Math.Sin(coordTrans.Z + angleRad), Math.Cos(coordTrans.Z + angleRad), 0, 0, 0, 1 });
        //        //Mat rotate = new Mat(3, 3, MatType.CV_64FC1, new double[] { Math.Cos(0 + angleRad), -Math.Sin(0 + angleRad), 0, Math.Sin(0 + angleRad), Math.Cos(0 + angleRad), 0, 0, 0, 1 });
        //        //Mat transform2D = translate * rotate;
        //        Mat transform2D = translate;
        //        transform2D = transform2D.Inv();
        //        transform2Ds.Add(transform2D);
        //    }
        //}
        public void UpdateTransformMat()
        {
            try
            {
                //coordTrans = SvFunc.FixtureToImage3D(new Point3d(0, 0, 0), runImage.TransformMat);
                Point3d[] rectTransVal = new Point3d[RectLst.Count];
                transform2Ds.Clear();
                for (int i = 0; i < RectLst.Count; i++)
                {
                    Point LT = new Point(Canvas.GetLeft(RectLst[i]), Canvas.GetTop(RectLst[i]));
                    Point centerPoint = new Point(LT.X + RectLst[i].Width / 2, LT.Y + RectLst[i].Height / 2);
                    //Point centerPoint = RectLst[i].TransformToAncestor(CanvasImg).Transform(new Point(RectLst[i].Width / 2, RectLst[i].Height / 2));

                    //Trong trường hợp load Tool từ appSetting thì không chạy dòng trong if
                    if (InputImage != null && InputImage.Mat != null && InputImage.Mat.Width > 0 && InputImage.Mat.Height > 0)
                    {
                        centInputImg = new Point3d(InputImage.CenterPoint.X, InputImage.CenterPoint.Y, InputImage.CenterPoint.Z);
                    }
                    rectTransVal[i].X = centerPoint.X - centInputImg.X;
                    rectTransVal[i].Y = centerPoint.Y - centInputImg.Y;

                    //Lấy góc xoay của ROI
                    //RotateTransform thisRot = rectLst[i].RenderTransform as RotateTransform
                    //double angleDeg = (thisRot != null) ? thisRot.Angle : 0d;
                    //double angleRad = (angleDeg / 180) * Math.PI;
                    Mat translate = new Mat(3, 3, MatType.CV_64FC1, new double[] { 1, 0, rectTransVal[i].X, 0, 1, rectTransVal[i].Y, 0, 0, 1 });
                    //Mat rotate = new Mat(3, 3, MatType.CV_64FC1, new double[] { Math.Cos(coordTrans.Z + angleRad), -Math.Sin(coordTrans.Z + angleRad), 0, Math.Sin(coordTrans.Z + angleRad), Math.Cos(coordTrans.Z + angleRad), 0, 0, 0, 1 });
                    //Mat rotate = new Mat(3, 3, MatType.CV_64FC1, new double[] { Math.Cos(0 + angleRad), -Math.Sin(0 + angleRad), 0, Math.Sin(0 + angleRad), Math.Cos(0 + angleRad), 0, 0, 0, 1 });
                    //Mat transform2D = translate * rotate;
                    Mat transform2D = translate;
                    transform2D = transform2D.Inv();
                    transform2Ds.Add(transform2D);
                }
            }
            catch (Exception ex)
            {
                logger.Create("Update TransformMat Error: " + ex.Message, ex);
            }
        }
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            //allCheckedStates.Clear();
            //for (int row = 0; row < tableData.Count; row++)
            //{
            //    for (int col = 0; col < tableData[row].ImageChecks.Count; col++)
            //    {
            //        bool isChecked = tableData[row].ImageChecks[col];
            //        allCheckedStates.Add(new Tuple<int, int, bool>(row, col, isChecked));
            //    }
            //}
            OnBtnRefreshClicked?.Invoke(sender, e);
        }

        public void UpdateResultCheckBox()
        {
            try
            {
                resultCkbList.Clear(); // Xóa dữ liệu cũ
                                       // Duyệt qua từng hàng trong bảng
                for (int row = 0; row < tableData.Count; row++)
                {
                    List<Tuple<int, bool>> imageRowData = new List<Tuple<int, bool>>();

                    // Duyệt qua từng checkbox của hàng đó
                    for (int col = 0; col < tableData[row].ImageChecks.Count; col++)
                    {
                        bool isChecked = tableData[row].ImageChecks[col];
                        imageRowData.Add(new Tuple<int, bool>(col + 1, isChecked));
                    }

                    // Thêm hàng vào danh sách
                    resultCkbList.Add(imageRowData);
                }
            }
            catch (Exception ex)
            {
                logger.Create("Update Result CheckBox Error: " + ex.Message, ex);
            }
        }
        public void UpdateImageList(List<Tuple<int, SvImage, double>> imgLst)
        {
            try
            {
                UpdateResultCheckBox();
                imageSubList.Clear();
                // Duyệt qua từng hàng trong imageSubList
                foreach (var resultCkbRow in resultCkbList)
                {
                    List<Tuple<int, SvImage, double>> newList = new List<Tuple<int, SvImage, double>>();

                    // Duyệt qua từng phần tử trong hàng, không lấu phần tử cuối (ALL)
                    for (int i = 0; i < resultCkbRow.Count - 1; i++)
                    {
                        if (resultCkbRow[i].Item2) // Chỉ lấy những phần tử có giá trị true
                        {
                            // Tìm ảnh trong imgLst có cùng chỉ số index
                            var matchedImage = imgLst.FirstOrDefault(img => img.Item1 == resultCkbRow[i].Item1);

                            if (matchedImage != null)
                            {
                                newList.Add(matchedImage);
                            }
                        }
                    }

                    // Thêm danh sách mới vào kết quả tổng
                    imageSubList.Add(newList);
                }
            }
            catch (Exception ex)
            {
                logger.Create("Update Image List Error: " + ex.Message, ex);
            }
        }

        public void GenerateTable(int rows, List<Tuple<int, SvImage, double>> imgLst)
        {
            try
            {
                tableData.Clear();
                dataGrid.Columns.Clear();
                // Cột đầu tiên là "Sub"
                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    FontSize = 15,
                    Header = "Sub",
                    Binding = new Binding("Sub"),
                    IsReadOnly = true // Không chỉnh sửa nội dung
                });

                // Tạo các cột Image1, Image2...
                for (int i = 1; i < imgLst.Count; i++)
                {
                    dataGrid.Columns.Add(new DataGridCheckBoxColumn
                    {
                        Header = $"Image {imgLst[i].Item1}",
                        Binding = new Binding($"ImageChecks[{imgLst[i].Item1 - 1}]")
                    });
                }

                // Thêm cột "All" để chọn tất cả các checkbox trên hàng đó
                dataGrid.Columns.Add(new DataGridCheckBoxColumn
                {
                    Header = "All",
                    Width = 30,
                    Binding = new Binding("AllChecked")
                });

                // Tạo dữ liệu
                for (int i = 1; i <= rows; i++)
                {
                    tableData.Add(new TableRow { Sub = $"Sub {i}", ImageChecks = new List<bool>(new bool[imgLst.Count]) });
                }

                dataGrid.ItemsSource = tableData;
            }
            catch (Exception ex)
            {
                logger.Create("Generate Table Error: " + ex.Message, ex);
            }
        }
        public void GenerateTable(int rows, List<List<Tuple<int, bool>>> resultCkbList)
        {
            try
            {
                if (resultCkbList.Count <= 0 || resultCkbList.Count < rows)
                    return;
                List<Tuple<int, bool>> resultCkbs = resultCkbList[0];
                //Trường hợp không sử dụng Tạo bảng chia ảnh xuống SubProgram
                if (resultCkbs.Count <= 0) return;
                tableData.Clear();
                dataGrid.Columns.Clear();
                // Cột đầu tiên là "Sub"
                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    FontSize = 15,
                    Header = "Sub",
                    Binding = new Binding("Sub"),
                    IsReadOnly = true // Không chỉnh sửa nội dung
                });

                // Tạo các cột Image1, Image2...
                for (int i = 0; i < resultCkbs.Count - 1; i++)
                {
                    dataGrid.Columns.Add(new DataGridCheckBoxColumn
                    {
                        Header = $"Image {resultCkbs[i].Item1}",
                        Binding = new Binding($"ImageChecks[{resultCkbs[i].Item1 - 1}]")
                    });
                }
                // Thêm cột "All" để chọn tất cả các checkbox trên hàng đó
                dataGrid.Columns.Add(new DataGridCheckBoxColumn
                {
                    Header = "All",
                    Width = 30,
                    Binding = new Binding("AllChecked")
                });

                // Tạo dữ liệu
                for (int i = 0; i < rows; i++)
                {
                    List<bool> resultCkbRow = resultCkbList[i].OrderBy(t => t.Item1).Select(t => t.Item2).ToList();
                    TableRow tableRow = new TableRow { Sub = $"Sub {i}", ImageChecks = resultCkbRow };
                    //Kiểm tra có checked All không
                    if (resultCkbRow[resultCkbRow.Count - 1]) { tableRow.AllChecked = true; }
                    tableData.Add(tableRow);
                }
                dataGrid.ItemsSource = tableData;
            }
            catch (Exception ex)
            {
                logger.Create("Generate Table Error: " + ex.Message, ex);
            }
        }

        private void ToolBase_OnPropertyRoi(object sender, RoutedEventArgs e)
        {
            var Point = Mouse.GetPosition(this);
            new RegionProperty().DoConfirmMatrix(new System.Windows.Point(Point.X, Point.Y - 200));
            UpdateProperty();
        }
        private void ToolBase_OnMatrixRoi(object sender, RoutedEventArgs e)
        {
            try
            {
                var Point = Mouse.GetPosition(this);
                RegionCreatMatrix.MatrixData matrix = new RegionCreatMatrix().DoConfirmMatrix(new System.Windows.Point(Point.X, Point.Y - 200));
                foreach (var shapeEditer in ShapeEditorControls)
                {
                    shapeEditer.ReleaseElement();
                }
                string Name = "";


                for (int i = 0; i < matrix.Row; i++)
                {
                    for (int j = 0; j < matrix.Colum; j++)
                    {
                        if (!(i == 0 && j == 0))
                        {
                            if (RectLst.Count >= 0)
                            {
                                Name = String.Format("R{0}", RectLst.Count + 1);
                            }
                            if (rectCurLst.Count > 0)
                            {
                                foreach (Rectangle rectCur in rectCurLst)
                                {
                                    RotateTransform rotTrans = rectCur.RenderTransform as RotateTransform ?? new RotateTransform(0);
                                    CreatRect((float)(Canvas.GetLeft(rectCur) + j * matrix.ColumPitch), (float)Canvas.GetTop(rectCur) + i * matrix.RowPitch, (float)rectCur.ActualWidth, (float)rectCur.ActualHeight, (float)rotTrans.Angle, rectCur.Stroke, rectCur.Fill, Name);
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Create Matrix Error: " + ex.Message, ex);
            }
        }
        private void ToolBase_OnDeleteRoi(object sender, RoutedEventArgs e)
        {
            DeleteRegion();
        }

        private void UpdateProperty()
        {
            try
            {
                List<System.Windows.Shapes.Rectangle> RectLstCoppy = new List<System.Windows.Shapes.Rectangle>();
                foreach (var shapeEditer in ShapeEditorControls)
                {
                    shapeEditer.ReleaseElement();
                    shapeEditer.KeyDown -= ShapeEditor_KeyDown;
                    shapeEditer.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
                }
                while (CanvasImg.Children.Count > 1)
                {
                    CanvasImg.Children.RemoveAt(1);
                }
                if (RectLst == null)
                    return;
                for (int i = 0; i < RectLst.Count; i++)
                {
                    RectLstCoppy.Add(RectLst[i]);
                }
                RectLst.Clear();
                for (int i = 0; i < RectLstCoppy.Count; i++)
                {
                    Name = String.Format("R{0}", i + 1);
                    var converter = new BrushConverter();
                    RotateTransform rotTrans = RectLstCoppy[i].RenderTransform as RotateTransform ?? new RotateTransform(0);
                    CreatRect((float)Canvas.GetLeft(RectLstCoppy[i]), (float)Canvas.GetTop(RectLstCoppy[i]), (float)RectLstCoppy[i].ActualWidth, (float)RectLstCoppy[i].ActualHeight, (float)rotTrans.Angle, colorRectStroke, colorRectFill, Name);
                }
            }
            catch (Exception ex)
            {
                logger.Create("Update Property Error: " + ex.Message, ex);
            }
        }
        private void CbxImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (toolBase.cbxImage.SelectedIndex == 0)
                {
                    foreach (var shapeEditor in ShapeEditorControls)
                    {
                        shapeEditor.ReleaseElement();
                        shapeEditor.KeyDown -= ShapeEditor_KeyDown;
                        shapeEditor.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
                    }
                    outEle.RemoveRange(0, outEle.Count);
                    for (int i = 1; i < CanvasImg.Children.Count; i++)
                    {
                        outEle.Add(CanvasImg.Children[i]);
                    }
                    CanvasImg.Children.RemoveRange(1, CanvasImg.Children.Count - 1);
                    foreach (var ele in inEle)
                    {
                        CanvasImg.Children.Add(ele);
                    }
                    oldSelect = 0;
                }
                else if (toolBase.cbxImage.SelectedIndex == 1)
                {
                    inEle.RemoveRange(0, inEle.Count);
                    for (int i = 1; i < CanvasImg.Children.Count; i++)
                    {
                        inEle.Add(CanvasImg.Children[i]);
                    }
                    CanvasImg.Children.RemoveRange(1, CanvasImg.Children.Count - 1);
                    foreach (var ele in outEle)
                    {
                        CanvasImg.Children.Add(ele);
                    }
                    oldSelect = 1;
                }
                RoiShowCheck();
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

        public void ImgView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            foreach (var shapeEditer in ShapeEditorControls)
            {
                shapeEditer.ReleaseElement();
                shapeEditer.KeyDown -= ShapeEditor_KeyDown;
                shapeEditer.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
            }
            ShapeEditorControls.Clear();
            rectCurLst.Clear();
            angleCurLst.Clear();
        }
        private void ImgView_MouseLeave(object sender, MouseEventArgs e)
        {
            Point mouse = e.GetPosition(ImgView);
            if (mouse.X < 0 || mouse.X > ImgView.ActualWidth || mouse.Y < 0 || mouse.Y > ImgView.ActualHeight)
            {
                foreach (var shapeEditer in ShapeEditorControls)
                {
                    shapeEditer.ReleaseElement();
                    shapeEditer.KeyDown -= ShapeEditor_KeyDown;
                    shapeEditer.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
                }
                ShapeEditorControls.Clear();
                rectCurLst.Clear();
                angleCurLst.Clear();
                if (RectLst.Count > 0 && countEvent > 0)
                {
                    UpdateTransformMat();
                    countEvent = 0;
                }
            }
        }
        private void ImgView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (toolBase.cbxImage.SelectedIndex == 0)
            {
                if (contextMenu != null)
                {
                    contextMenu.PlacementTarget = sender as UIElement; // Đặt đúng kiểu dữ liệu
                    contextMenu.IsOpen = true; // Mở ContextMenu
                }
            }
        }
        private void RoiShowCheck()
        {
            //this.btnCreateROI.IsEnabled = IsEditROI;
            //this.btnDeleteAllROI.IsEnabled = IsEditROI;
            //this.btnMoveROILeft.IsEnabled = IsEditROI;
            //this.btnMoveROIRight.IsEnabled = IsEditROI;
            //this.btnMoveROIUp.IsEnabled = IsEditROI;
            //this.btnMoveROIDown.IsEnabled = IsEditROI;
            bool isEdit = toolBase.cbxImage.SelectedIndex == 0;
            this.btnCreateROI.IsEnabled = isEdit;
            this.btnDeleteAllROI.IsEnabled = isEdit;
            this.btnMoveROILeft.IsEnabled = isEdit;
            this.btnMoveROIRight.IsEnabled = isEdit;
            this.btnMoveROIUp.IsEnabled = isEdit;
            this.btnMoveROIDown.IsEnabled = isEdit;
        }

        private void DeleteRegion()
        {
            try
            {
                do
                {
                    foreach (var shapeEditer in ShapeEditorControls)
                    {
                        shapeEditer.ReleaseElement();
                        shapeEditer.KeyDown -= EditRegionEdit_KeyDown;
                        shapeEditer.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
                    }
                    // Tìm các Rectangle có cùng Name
                    List<Rectangle> comRectLst = RectLst.Where(r1 => rectCurLst.Any(r2 => r2.Name == r1.Name)).ToList();
                    if (comRectLst.Count < 0) { break; }
                    foreach (var comRect in comRectLst)
                    {
                        for (int i = 0; i < CanvasImg.Children.Count; i++)
                        {
                            Label a = CanvasImg.Children[i] as Label;
                            {
                                if (a != null)
                                {
                                    if ((string)a.Name == comRect.Name)
                                    {
                                        CanvasImg.Children.RemoveAt(i);
                                        LabelLst.Remove(a);
                                        RectLst.Remove(comRect);
                                    }
                                }
                            }

                        }
                        int b = 1;
                        for (int i = 0; i < RectLst.Count; i++)
                        {
                            int temp = Convert.ToInt32(RectLst[i].Name.Replace("R", String.Empty));
                            if (b - temp < 0)
                            {
                                RegionIndex = b + 1;
                            }
                            else
                            {
                                b = temp;
                            }
                        }

                        CanvasImg.Children.Remove(comRect);
                        LabelLst.Remove(LabelLst.FirstOrDefault(lbl => lbl.Name == comRect.Name));
                        RectLst.Remove(comRect);
                    }
                }
                while (false);
                ShapeEditorControls.Clear();
                rectCurLst.Clear();
                angleCurLst.Clear();
                RegionCount = RectLst.Count;
                inEle.Clear();
            }
            catch (Exception ex)
            {
                logger.Create("Delete Region Error: " + ex.Message, ex);
            }
        }
        private void EditRegionEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (rectCurLst.Count < 0)
                return;
            if (e.Key == Key.Delete)
            {
                DeleteRegion();
            }

            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                try
                {

                    isCoppy = true;
                    //Copy rectangle
                    rectCoppyLst.Clear();
                    angleCopyLst.Clear();
                    for (int i = 0; i < rectCurLst.Count; i++)
                    {
                        var rectCopy = rectCurLst[i];
                        rectCoppyLst.Add(rectCopy);
                        var angleCopy = angleCurLst[i];
                        angleCopyLst.Add(angleCopy);
                    }
                }
                catch (Exception ex)
                {
                    logger.Create("Copy ROI Error: " + ex.Message, ex);
                }
            }

            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (isCoppy == true)
                {
                    try
                    {
                        //Danh sách chứa các nameRect sẽ được sử dụng để tạo Rectangle mới
                        List<string> rectNames = new List<string>();
                        //Tìm chỉ số của Rectangle lớn nhất
                        int maxIdxRect = RectLst
                            .Where(r => r.Name.StartsWith("R") && int.TryParse(r.Name.Substring(1), out _)) // Lọc các Name hợp lệ
                            .Select(r => int.Parse(r.Name.Substring(1))) // Chuyển thành số
                            .DefaultIfEmpty(0) // Tránh lỗi khi danh sách trống
                            .Max(); // Tìm max
                        if (maxIdxRect > RectLst.Count)
                        {
                            //Tạo 1 danh sách Rect Name đầy đủ (bao gồm cả tên các Rectangle khuyết)
                            List<string> allNamesRect = Enumerable.Range(1, maxIdxRect).Select(i => $"R{i}").ToList();
                            // Lấy danh sách các tên có trong RectLst
                            HashSet<string> existNamesInRect = new HashSet<string>(RectLst.Select(r => r.Name));
                            // Tìm các tên bị thiếu
                            rectNames = allNamesRect.Where(name => !existNamesInRect.Contains(name)).ToList();
                        }
                        while (rectNames.Count < rectCoppyLst.Count)
                        {
                            maxIdxRect++;
                            rectNames.Add(String.Format("R{0}", maxIdxRect));
                        }


                        for (int i = 0; (i < rectCoppyLst.Count); i++)
                        {
                            CreatRect((float)Canvas.GetLeft(rectCoppyLst[i]) + 100,
                                (float)Canvas.GetTop(rectCoppyLst[i]), (float)rectCoppyLst[i].ActualWidth, (float)rectCoppyLst[i].ActualHeight, (float)angleCopyLst[i], rectCoppyLst[i].Stroke, rectCoppyLst[i].Fill, rectNames[i]);

                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("Paste ROI Error: " + ex.Message, ex);
                    }
                }
            }
        }

        public void CreatRect(float left, float top, float width, float height, float angle, Brush stroke, Brush Fill, string name)
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
                rect.MouseRightButtonDown += Rect_MouseRightButtonDown;
                inEle.Add(rect);
                CanvasImg.Children.Add(rect);
                Canvas.SetLeft(rect, left);
                Canvas.SetTop(rect, top);

                int index = Convert.ToInt32(name.Replace("R", string.Empty));
                if (index > RectLst.Count)
                {
                    RectLst.Add(rect);
                }
                else
                {
                    RectLst.Insert(index - 1, rect);
                }

                RegionIndex = Convert.ToInt32(name.Replace("R", String.Empty)) + 1;

                Label lb = new Label();
                lb.Name = name;
                lb.Content = name.Replace("R", String.Empty);
                lb.FontSize = UiManager.appSettings.Property.labelFontSize;
                lb.Foreground = (Brush)new BrushConverter().ConvertFromString("#FFFF00");
                Canvas.SetLeft(lb, left + width / 2.0 - 20.0);
                Canvas.SetTop(lb, top + width / 2.0 - 20.0);
                lb.RenderTransform = new RotateTransform(angle);
                lb.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                LabelLst.Add(lb);
                inEle.Add(lb);
                CanvasImg.Children.Add(lb);
                RegionCount = RectLst.Count;
            }
            catch (Exception ex)
            {
                logger.Create("Create Rect Error: " + ex.Message, ex);
            }
        }


        public void DeleteAllRegion()
        {
            try
            {
                foreach (var shapeEditer in ShapeEditorControls)
                {
                    shapeEditer.ReleaseElement();
                }
                CanvasImg.Children.RemoveRange(1, CanvasImg.Children.Count);
                inEle.Clear();
                RectLst.Clear();
                RegionCount = RectLst.Count;
            }
            catch (Exception ex)
            {
                logger.Create("Delete All Region Error: " + ex.Message, ex);
            }
        }

        private void Rect_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            foreach (var shapeEditer in ShapeEditorControls)
            {
                shapeEditer.ReleaseElement();
                shapeEditer.KeyDown -= ShapeEditor_KeyDown;
                shapeEditer.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
            }

            contextMenu.PlacementTarget = sender as ContextMenu;
            contextMenu.IsOpen = true;
        }
        private void Rect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Rectangle senderRect = sender as Rectangle;
                ShapeEditor shapeEditor = new ShapeEditor(UiManager.appSettings.Property.rectSize.Width, UiManager.appSettings.Property.labelFontSize)
                {
                    rectSize = (double)UiManager.appSettings.Property.rectSize.Width,
                    Name = "SE" + Convert.ToInt32(senderRect.Name.Replace("R", String.Empty)).ToString("00"),
                    Focusable = true,
                };
                //if (IsEditROI == false)
                if (toolBase.cbxImage.SelectedIndex != 0)
                    return;

                if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                {
                    if (ShapeEditorControls.Count > 0)
                    {
                        foreach (var shE in ShapeEditorControls)
                        {
                            shE.ReleaseElement();
                            shE.KeyDown -= ShapeEditor_KeyDown;
                            shE.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
                            shE.IsMulSelect = false;
                        }
                        ShapeEditorControls.Clear();
                        rectCurLst.Clear();
                        angleCurLst.Clear();
                    }
                }
                //Clear ShapeEditor cũ cùng tên
                foreach (var element in CanvasImg.Children)
                {
                    ShapeEditor a = element as ShapeEditor;
                    if (a != null && a.Name == "SE" + Convert.ToInt32(senderRect.Name.Replace("R", String.Empty)).ToString("00"))
                    {
                        CanvasImg.Children.Remove(a);
                        break;
                    }
                }
                CanvasImg.Children.Add(shapeEditor);
                shapeEditor.KeyDown += ShapeEditor_KeyDown;
                shapeEditor.LostKeyboardFocus += ShapeEditor_LostKeyboardFocus;
                shapeEditor.OnRectMove += ShapeEditor_OnRectMove;
                shapeEditor.OnRectResize += ShapeEditor_OnRectResize;
                shapeEditor.OnRectRotate += ShapeEditor_OnRectRotate;
                shapeEditor.CaptureElement(senderRect, e);
                rectCurLst.Add(senderRect);
                RotateTransform rot = shapeEditor.RenderTransform as RotateTransform ?? new RotateTransform(0);
                angleCurLst.Add(rot.Angle);
                ShapeEditorControls.Add(shapeEditor);
                if (ShapeEditorControls.Count > 1)
                {
                    foreach (var shE in ShapeEditorControls)
                    {
                        shE.IsMulSelect = true;
                    }
                }
                shapeEditor.Focus();
            }
            catch(Exception ex)
            {
                logger.Create("Select ROI Error: " + ex.Message, ex);
            } 
        }

        private int countEvent = 0;
        private void ShapeEditor_OnRectMove(object sender, MouseEventArgs e)
        {
            countEvent++;
        }

        private void ShapeEditor_OnRectResize(object sender, MouseEventArgs e)
        {
            countEvent++;
        }

        private void ShapeEditor_OnRectRotate(object sender, MouseEventArgs e)
        {
            countEvent++;
        }

        private void BtnCreateROI_Click(object sender, RoutedEventArgs e)
        {
            //if (IsEditROI == false)
            if (toolBase.cbxImage.SelectedIndex != 0)
                return;

            String Name = "";
            if (RectLst.Count == 0)
            {

                Name = "R1";

            }
            else if (RectLst.Count > 0)
            {
                int b = 1;
                for (int i = 0; i < RectLst.Count; i++)
                {
                    int temp = Convert.ToInt32(RectLst[i].Name.Replace("R", String.Empty));
                    if (b - temp < 0)
                    {
                        Name = String.Format("R{0}", b);
                        break;
                    }
                    else
                    {
                        b++;
                    }

                }
                if (b - 1 == Convert.ToInt32(RectLst[RectLst.Count - 1].Name.Replace("R", String.Empty)))
                {
                    var recName = RectLst[RectLst.Count - 1].Name.Replace("R", String.Empty);
                    Name = String.Format("R{0}", Convert.ToInt32(recName) + 1);
                }
            }
            //CreatRect(10, 10, 200, 200, new SolidColorBrush(Colors.Red), (Brush)converter.ConvertFromString("#40DC143C"), Name);
            CreatRect(210f, 210f, 200f, 200f, 0f, colorRectStroke, colorRectFill, Name);
        }

        private void ShapeEditor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            contextMenu.PlacementTarget = sender as UIElement;
            //Xác định đang có ít nhất 1 shapeEdtor được tác động và không có cửa sổ ContextMenu cmRegion được mở
            if (ShapeEditorControls.Count > 0 && !contextMenu.IsOpen)
            {
                ShapeEditorControls[0].Focus();
            }
        }

        private void ShapeEditor_KeyDown(object sender, KeyEventArgs e)
        {
            //if (IsEditROI == false)
            if (toolBase.cbxImage.SelectedIndex != 0)
                return;
            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down)
            {
                MoveRoiByKb(e);
            }
            if (e.Key == Key.A)
            {
                if (ShapeEditorControls.Count > 0 && Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    try
                    {
                        foreach (var shapeEditer in ShapeEditorControls)
                        {
                            shapeEditer.ReleaseElement();
                            shapeEditer.KeyDown -= ShapeEditor_KeyDown;
                            shapeEditer.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
                        }
                        rectCurLst.Clear();
                        ShapeEditorControls.Clear();
                        angleCurLst.Clear();
                        foreach (Rectangle rect in RectLst)
                        {
                            ShapeEditor shapeEditor = new ShapeEditor(UiManager.appSettings.Property.rectSize.Width, UiManager.appSettings.Property.labelFontSize)
                            {
                                rectSize = (double)UiManager.appSettings.Property.rectSize.Width,
                                Name = "SE" + Convert.ToInt32(rect.Name.Replace("R", String.Empty)).ToString("00"),
                                Focusable = true,
                                IsMulSelect = true,
                            };
                            shapeEditor.IsMulSelect = true;
                            Rectangle rectRemoved = new Rectangle();
                            //Clear ShapeEditor cũ cùng tên
                            foreach (var element in CanvasImg.Children)
                            {
                                ShapeEditor a = element as ShapeEditor;
                                if (a != null && a.Name == "SE" + Convert.ToInt32(rect.Name.Replace("R", String.Empty)).ToString("00"))
                                {
                                    CanvasImg.Children.Remove(a);
                                    break;
                                }
                            }
                            bool b = shapeEditor.Focus();
                            CanvasImg.Children.Add(shapeEditor);
                            shapeEditor.KeyDown += ShapeEditor_KeyDown;
                            shapeEditor.LostKeyboardFocus += ShapeEditor_LostKeyboardFocus;
                            shapeEditor.CaptureElement(rect, null);
                            rectCurLst.Add(rect);
                            RotateTransform rot = shapeEditor.RenderTransform as RotateTransform ?? new RotateTransform(0);
                            angleCurLst.Add(rot.Angle);
                            ShapeEditorControls.Add(shapeEditor);
                            shapeEditor.Focus();
                        }
                    }
                    catch(Exception ex)
                    {
                        logger.Create("Select All ROI Error: " + ex.Message, ex);
                    } 
                }
            }
        }
        private void MoveRoiByKb(KeyEventArgs e)
        {
            try
            {
                //Kiểm tra xem có đang chọn vào ROI nào không
                if (rectCurLst.Count > 0)
                {
                    switch (e.Key)
                    {
                        case Key.Left:
                            foreach (var shapeEditer in ShapeEditorControls)
                                Canvas.SetLeft(shapeEditer, Canvas.GetLeft(shapeEditer) - 2);
                            break;
                        case Key.Right:
                            foreach (var shapeEditer in ShapeEditorControls)
                                Canvas.SetLeft(shapeEditer, Canvas.GetLeft(shapeEditer) + 2);
                            break;
                        case Key.Up:
                            foreach (var shapeEditer in ShapeEditorControls)
                                Canvas.SetTop(shapeEditer, Canvas.GetTop(shapeEditer) - 2);
                            break;
                        case Key.Down:
                            foreach (var shapeEditer in ShapeEditorControls)
                                Canvas.SetTop(shapeEditer, Canvas.GetTop(shapeEditer) + 2);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Move ROI Error: " + ex.Message, ex);
            }
        }
        Point[] Get4PointsRect(Rectangle rect, FrameworkElement parent)
        {
            Point[] points = new Point[4];
            //Lấy góc xoay của ROI
            RotateTransform thisRot = rect.RenderTransform as RotateTransform ?? new RotateTransform(0);
            double rotAngle = (thisRot != null) ? thisRot.Angle : 0d;
            Point LT = new Point(Canvas.GetLeft(rect), Canvas.GetTop(rect));
            Point centerPoint = new Point(LT.X + rect.Width / 2, LT.Y + rect.Height / 2);
            Point3d CPInv = SvFunc.FixtureToImage3D(new Point3d(centerPoint.X, centerPoint.Y, 0.0), runImage.TransformMat);
            centerPoint = new Point(CPInv.X, CPInv.Y);
            // Tính tọa độ 4 góc trước khi xoay
            Point pLT = new Point(centerPoint.X - (rect.Width / 2), centerPoint.Y - (rect.Height / 2));
            Point pRB = new Point(centerPoint.X + (rect.Width / 2), centerPoint.Y + (rect.Height / 2));
            Point pLB = new Point(pLT.X, pRB.Y);
            Point pRT = new Point(pRB.X, pLT.Y);
            //Tính tọa độ 4 góc sau khi xoay
            points[0] = RotatePoint(pLT, centerPoint, rotAngle);
            points[1] = RotatePoint(pRT, centerPoint, rotAngle);
            points[2] = RotatePoint(pRB, centerPoint, rotAngle);
            points[3] = RotatePoint(pLB, centerPoint, rotAngle);

            //points[0] = RotatePoint(pLT, centerPoint, CPInv.Z);
            //points[1] = RotatePoint(pRT, centerPoint, CPInv.Z);
            //points[2] = RotatePoint(pRB, centerPoint, CPInv.Z);
            //points[3] = RotatePoint(pLB, centerPoint, CPInv.Z);

            return points;
        }
        //Point[] Get4PointsRect1(Rectangle rect, Mat transformMat, FrameworkElement parent)
        //{
        //    Point[] points = new Point[4];
        //    //Lấy góc xoay của ROI
        //    RotateTransform rot = rect.RenderTransform as RotateTransform ?? new RotateTransform(0);
        //    double rotAngle = (rot != null) ? rot.Angle : 0d;
        //    Point3d coordTransRT = SvFunc.FixtureToImage3D(new Point3d(0, 0, 0), runImage.TransformMat);
        //    Point2d coordTransRT2D = new Point2d(coordTransRT.X, coordTransRT.Y);
        //    Point2d centerPoint2D = SvFunc.FixtureToImage2D(coordTransRT2D, transformMat);
        //    Point centerPoint = new Point(centerPoint2D.X, centerPoint2D.Y);
        //    //Tính góc lệch khi transform
        //    double angleDeg = ((coordTransRT.Z - coordTrans.Z) / Math.PI) * 180;
        //    //centerPoint = RotatePoint(centerPoint, new Point(coordTransRT2D.X, coordTransRT2D.Y), angleDeg);

        //    // Tính tọa độ 4 góc trước khi xoay
        //    Point pLT = new Point(centerPoint.X - (rect.Width / 2), centerPoint.Y - (rect.Height / 2));
        //    Point pRB = new Point(centerPoint.X + (rect.Width / 2), centerPoint.Y + (rect.Height / 2));
        //    Point pLB = new Point(pLT.X, pRB.Y);
        //    Point pRT = new Point(pRB.X, pLT.Y);
        //    //Tính tọa độ 4 góc sau khi xoay
        //    points[0] = RotatePoint(pLT, centerPoint, angleDeg + rotAngle);
        //    points[1] = RotatePoint(pRT, centerPoint, angleDeg + rotAngle);
        //    points[2] = RotatePoint(pRB, centerPoint, angleDeg + rotAngle);
        //    points[3] = RotatePoint(pLB, centerPoint, angleDeg + rotAngle);
        //    return points;
        //}
        Point[] Get4PointsRect(Rectangle rect, Mat transformMat, FrameworkElement parent)
        {
            Point[] points = new Point[4];
            try
            {
                //Lấy góc xoay của ROI
                RotateTransform rot = rect.RenderTransform as RotateTransform ?? new RotateTransform(0);
                double rotAngle = (rot != null) ? rot.Angle : 0d;
                //Point3d coordTransRT = SvFunc.FixtureToImage3D(new Point3d(0, 0, 0), runImage.TransformMat);
                Point2d coordTransRT2D = new Point2d(InputImage.CenterPoint.X, InputImage.CenterPoint.Y);
                Point2d centerPoint2D = SvFunc.FixtureToImage2D(coordTransRT2D, transformMat);
                Point centerPoint = new Point(centerPoint2D.X, centerPoint2D.Y);
                //Tính góc lệch khi transform
                //double angleDeg = ((coordTransRT.Z - coordTrans.Z) / Math.PI) * 180;
                double angleDeg = (InputImage.CenterPoint.Z / Math.PI) * 180;
                //centerPoint = RotatePoint(centerPoint, new Point(coordTransRT2D.X, coordTransRT2D.Y), angleDeg);

                // Tính tọa độ 4 góc trước khi xoay
                Point pLT = new Point(centerPoint.X - (rect.Width / 2), centerPoint.Y - (rect.Height / 2));
                Point pRB = new Point(centerPoint.X + (rect.Width / 2), centerPoint.Y + (rect.Height / 2));
                Point pLB = new Point(pLT.X, pRB.Y);
                Point pRT = new Point(pRB.X, pLT.Y);
                //Tính tọa độ 4 góc sau khi xoay
                points[0] = RotatePoint(pLT, centerPoint, angleDeg + rotAngle);
                points[1] = RotatePoint(pRT, centerPoint, angleDeg + rotAngle);
                points[2] = RotatePoint(pRB, centerPoint, angleDeg + rotAngle);
                points[3] = RotatePoint(pLB, centerPoint, angleDeg + rotAngle);
            }
            catch (Exception ex)
            {
                logger.Create("Get 4 Point of ROI Error: " + ex.Message, ex);
            }
            return points;
        }
        private Point RotatePoint(Point pointToRot, Point centerPoint, double angleInDeg)
        {
            double angleInRad = angleInDeg * (Math.PI / 180.0d);
            double cosTheta = Math.Cos(angleInRad);
            double sinTheta = Math.Sin(angleInRad);
            //Tính góc xoay của điểm
            double X = cosTheta * (pointToRot.X - centerPoint.X) - sinTheta * (pointToRot.Y - centerPoint.Y) + centerPoint.X;
            double Y = sinTheta * (pointToRot.X - centerPoint.X) + cosTheta * (pointToRot.Y - centerPoint.Y) + centerPoint.Y;
            return new Point(X, Y);
        }
        private bool CropImage(OpenCvSharp.Mat src, out List<Tuple<int, SvImage, double>> imgLst, out List<Tuple<int, Point[], double>> rectPLst)
        {
            imgLst = new List<Tuple<int, SvImage, double>>();
            rectPLst = new List<Tuple<int, Point[], double>>();
            try
            {
                if (src == null || src.IsDisposed)
                {
                    return false;
                }
                Point[] pointsImgView = new Point[] { new Point(0, 0), new Point(ImgView.Source.Width, 0), new Point(toolBase.imgView.Source.Width, toolBase.imgView.Source.Height), new Point(0, toolBase.imgView.Source.Height) };
                rectPLst.Insert(0, new Tuple<int, Point[], double>(0, pointsImgView, 0d));

                for (int i = 0; i < RectLst.Count; i++)
                {
                    int index = Convert.ToInt32(RectLst[i].Name.Replace("R", string.Empty));
                    Point[] points = new Point[4];
                    if (transform2Ds.Count <= 0)
                        UpdateTransformMat();
                    points = Get4PointsRect(RectLst[i], transform2Ds[i], CanvasImg);
                    RotateTransform rotTrans = (RotateTransform)RectLst[i].RenderTransform;
                    double rotAngle = (rotTrans != null) ? rotTrans.Angle : 0d;
                    rectPLst.Add(new Tuple<int, Point[], double>(index, points, rotAngle));
                }

                foreach (var rectPoint in rectPLst)
                {
                    Point2d leftTop = new Point2d(rectPoint.Item2[0].X, rectPoint.Item2[0].Y);
                    Point2d rightTop = new Point2d(rectPoint.Item2[1].X, rectPoint.Item2[1].Y);
                    Point2d leftBottom = new Point2d(rectPoint.Item2[3].X, rectPoint.Item2[3].Y);
                    Point2f center = new Point2f((float)rectPoint.Item2[0].X, (float)rectPoint.Item2[0].Y);

                    double T = Math.Atan2(rightTop.Y - leftTop.Y, rightTop.X - leftTop.X);
                    double H = Point2d.Distance(leftTop, rightTop);
                    double V = Point2d.Distance(leftTop, leftBottom);

                    Mat Rmat = Cv2.GetRotationMatrix2D(center, T * 180 / Math.PI, 1);
                    Rmat.Set<double>(0, 2, Rmat.At<double>(0, 2) - center.X);
                    Rmat.Set<double>(1, 2, Rmat.At<double>(1, 2) - center.Y);

                    OpenCvSharp.Rect rect = new OpenCvSharp.Rect((int)center.X, (int)center.Y, (int)H, (int)V);

                    SvImage tempImg = new SvImage();
                    tempImg.Mat = new Mat(rect.Size, src.Type());
                    Cv2.WarpAffine(src, tempImg.Mat, Rmat, rect.Size);
                    Rmat.Dispose();
                    if (tempImg.Mat.Cols == 0 || tempImg.Mat.Rows == 0)
                    {
                        tempImg.Dispose();
                        return false;
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        tempImg.RectPoint[i].X = rectPoint.Item2[i].X;
                        tempImg.RectPoint[i].Y = rectPoint.Item2[i].Y;
                    }
                    tempImg.CenterPoint = new Point3d((rectPoint.Item2[0].X + rectPoint.Item2[2].X) / 2, (rectPoint.Item2[0].Y + rectPoint.Item2[2].Y) / 2, rectPoint.Item3 * Math.PI / 180d);
                    tempImg.AnglgeRoiRad = (rectPoint.Item3 * Math.PI) / 180d;


                    imgLst.Add(new Tuple<int, SvImage, double>(rectPoint.Item1, tempImg, rectPoint.Item3));
                }
            }
            catch (Exception ex)
            {
                logger.Create("Crop Image Error: " + ex.Message, ex);
            }
            return (imgLst.Count > 0);
        }
        public SvImage runImage = new SvImage();
        public override void Run()
        {
            try
            {
                foreach (var shapeEditer in ShapeEditorControls)
                {
                    shapeEditer.ReleaseElement();
                    shapeEditer.KeyDown -= ShapeEditor_KeyDown;
                    shapeEditer.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
                }
                ShapeEditorControls.Clear();
                rectCurLst.Clear();
                angleCurLst.Clear();
                if (RectLst.Count > 0 && countEvent > 0)
                {
                    UpdateTransformMat();
                    countEvent = 0;
                }
                if (this.InputImage == null || this.InputImage.Mat == null || this.InputImage.Mat.Width <= 0 || this.InputImage.Mat.Height <= 0)
                {
                    if (toolBase.isImgPath && isEditMode)
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
                else if (InputImage.Mat != null && toolBase.isImgPath && isEditMode)
                {
                    runImage.Mat = (ImgView.Source as BitmapSource).ToMat();
                }
                else if (InputImage.Mat != null && InputImage.Mat.Width > 0 && InputImage.Mat.Height > 0 && !toolBase.isImgPath)
                {
                    runImage = this.InputImage.Clone(true);
                }
                if (oldSelect != 0)
                {
                    toolBase.cbxImage.SelectedIndex = 0;
                }
                if (stImgLogMain.Children.Count > 0)
                {
                    Border bd0 = stImgLogMain.Children.OfType<Border>().First();
                    SelectBorder(bd0);
                }
                imgLst.Clear();
                rectPointLst.Clear();
                if (!CropImage(runImage.Mat, out imgLst, out rectPointLst))
                {
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Error when get region image");
                }
                UpdateImageList(imgLst);
                if (isEditMode) { CreateImgLog(imgLst, rectPointLst); }
            }
            catch (Exception ex)
            {
                logger.Create("Run Error: " + ex.Message, ex);
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

                toolBase.cbxImage.SelectedIndex = 1;
                Thread.Sleep(1);
                CanvasImg.Children.RemoveRange(1, CanvasImg.Children.Count - 1);
                outEle.RemoveRange(0, outEle.Count);

                for (int i = 1; i < rectPointLst.Count; i++)
                {
                    Polygon polyResults = new Polygon()
                    {
                        Stroke = Brushes.Aqua,
                        StrokeThickness = 2,
                        Fill = Brushes.Transparent,
                    };
                    foreach (var point in rectPointLst[i].Item2)
                    {
                        Point p = new Point(point.X, point.Y);
                        polyResults.Points.Add(p);
                    }
                    outEle.Add(polyResults);
                }

                outEle.ForEach(ele => CanvasImg.Children.Add(ele));
            }
            catch (Exception ex)
            {
                logger.Create("Display Image Error: " + ex.Message, ex);
            }
        }

        private void CreateImgLog(List<Tuple<int, SvImage, double>> imgLst, List<Tuple<int, Point[], double>> rectPointLst)
        {
            try
            {
                stImgLogMain.Children.Clear();
                ImgView.Source = runImage.Mat.ToBitmapSource();
                var imgLstTemp = imgLst.OrderBy(t => t.Item1).ToList();
                var rectPointLstTemp = rectPointLst.OrderBy(t => t.Item1).ToList();
                for (int i = 0; i < imgLstTemp.Count; i++)
                {
                    Border brdr = new Border() { Name = $"Bd{i}", BorderThickness = new Thickness(2), BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC"), };
                    brdr.MouseMove += (sender, e) =>
                    {
                        Border bdSelected = sender as Border;
                        StackPanel stMain = bdSelected.Parent as StackPanel;
                        List<Border> bdLst = stMain.Children.OfType<Border>().ToList();
                        foreach (var bd in bdLst)
                        {
                            if (bd.BorderBrush == Brushes.Red)
                                continue;
                            bd.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC");
                        }
                        if (bdSelected.BorderBrush != Brushes.Red)
                            bdSelected.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF489E37");
                    };
                    brdr.MouseLeftButtonDown += (sender, e) =>
                    {
                        Border bdSelected = sender as Border;
                        SelectBorder(bdSelected);
                    };
                    StackPanel stImgLog = new StackPanel();

                    Label lbHeader = new Label
                    {
                        Background = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC"),
                        Foreground = (Brush)new BrushConverter().ConvertFromString("#FF489E37"),
                        Padding = new Thickness(10, 5, 0, 5),
                        Content = String.Format("{0}. Image Region {0}", imgLstTemp[i].Item1),
                        FontStyle = FontStyles.Italic,
                        FontWeight = FontWeights.Bold,
                    };
                    StackPanel st0 = new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Background = Brushes.Cornsilk,
                    };
                    Canvas cnvImg = new Canvas()
                    {
                        Background = (Brush)new BrushConverter().ConvertFromString("#FF999999"),
                        Width = 250,
                        Height = 150,
                        Margin = new Thickness(10, 0, 10, 0)
                    };
                    Image imgLog = new Image()
                    {
                        Source = imgLstTemp[i].Item2.Mat.ToBitmapSource()
                    };

                    cnvImg.Children.Add(imgLog);
                    FitImage(imgLog, cnvImg);

                    StackPanel st1 = new StackPanel();
                    Style StyleLb = new Style(typeof(Label));
                    StyleLb.Setters.Add(new Setter(Label.PaddingProperty, new Thickness(1)));
                    StyleLb.Setters.Add(new Setter(Label.ForegroundProperty, (Brush)new BrushConverter().ConvertFromString("#FF489E37")));
                    StyleLb.Setters.Add(new Setter(Label.FontStyleProperty, FontStyles.Italic));
                    Label lbIndex = new Label() { Content = $"Index : {imgLstTemp[i].Item1}", Style = StyleLb, Padding = new Thickness(0, 5, 0, 1) };
                    Label lbTime = new Label() { Content = $"Time : {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss:fff")}", Style = StyleLb, };
                    Label lbLocate = new Label() { Content = $"Locate : [X, Y] = [{rectPointLstTemp[i].Item2[0].X.ToString("F2")}, {rectPointLstTemp[i].Item2[0].Y.ToString("F2")}]", Style = StyleLb, };
                    Label lbSize = new Label() { Content = $"Width x Height : {imgLstTemp[i].Item2.Width} x {imgLstTemp[i].Item2.Height}", Style = StyleLb, };
                    Label lbAngle = new Label() { Content = $"Angle : {imgLstTemp[i].Item3.ToString("F2")}°", Style = StyleLb, };
                    st1.Children.Add(lbIndex);
                    st1.Children.Add(lbTime);
                    st1.Children.Add(lbLocate);
                    st1.Children.Add(lbSize);
                    st1.Children.Add(lbAngle);
                    st0.Children.Add(cnvImg);
                    st0.Children.Add(st1);
                    stImgLog.Children.Add(lbHeader);
                    stImgLog.Children.Add(st0);
                    brdr.Child = stImgLog;
                    stImgLogMain.Children.Add(brdr);
                }
            }
            catch (Exception ex)
            {
                logger.Create("Create Image Log Error: " + ex.Message, ex);
            }
        }
        private void CreateImgLog1(List<Tuple<int, SvImage, double>> imgLst, List<Tuple<int, Point[], double>> rectPointLst)
        {
            stImgLogMain.Children.Clear();
            ImgView.Source = runImage.Mat.ToBitmapSource();
            var imgLstTemp = imgLst.OrderBy(t => t.Item1).ToList();
            var rectPointLstTemp = rectPointLst.OrderBy(t => t.Item1).ToList();
            for (int i = 0; i < imgLstTemp.Count; i++)
            {
                Border brdr = new Border() { Name = $"Bd{i}", BorderThickness = new Thickness(2), BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC"), };
                brdr.MouseMove += (sender, e) =>
                {
                    Border bdSelected = sender as Border;
                    StackPanel stMain = bdSelected.Parent as StackPanel;
                    List<Border> bdLst = stMain.Children.OfType<Border>().ToList();
                    foreach (var bd in bdLst)
                    {
                        if (bd.BorderBrush == Brushes.Red)
                            continue;
                        bd.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC");
                    }
                    if (bdSelected.BorderBrush != Brushes.Red)
                        bdSelected.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF489E37");
                };
                brdr.MouseLeftButtonDown += (sender, e) =>
                {
                    Border bdSelected = sender as Border;
                    SelectBorder(bdSelected);
                };
                TreeView trVwImgLog = new TreeView
                {
                    Margin = new Thickness(-5, 0, 0, 0),
                    Background = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC"),
                };
                // Tạo TreeViewItem
                TreeViewItem trVwIt = new TreeViewItem();

                Label lbHeader = new Label
                {
                    Background = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC"),
                    Foreground = (Brush)new BrushConverter().ConvertFromString("#FF489E37"),
                    Padding = new Thickness(0, 5, 0, 5),
                    Content = String.Format("{0}. Image Region {0}", imgLstTemp[i].Item1),
                    FontStyle = FontStyles.Italic,
                    FontWeight = FontWeights.Bold,
                };
                trVwIt.Header = lbHeader;

                StackPanel st0 = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Background = Brushes.Cornsilk,
                    Margin = new Thickness(-15, 0, 0, 1),
                };
                Canvas cnvImg = new Canvas()
                {
                    Background = (Brush)new BrushConverter().ConvertFromString("#FF999999"),
                    Width = 250,
                    Height = 150,
                    Margin = new Thickness(10, 0, 10, 0)
                };
                Image imgLog = new Image()
                {
                    Source = imgLstTemp[i].Item2.Mat.ToBitmapSource()
                };

                cnvImg.Children.Add(imgLog);
                FitImage(imgLog, cnvImg);

                StackPanel st1 = new StackPanel()
                {
                    Width = 180,
                    Margin = new Thickness(10, 0, 0, 0),
                };
                Style StyleLb = new Style(typeof(Label));
                StyleLb.Setters.Add(new Setter(Label.PaddingProperty, new Thickness(1)));
                StyleLb.Setters.Add(new Setter(Label.ForegroundProperty, (Brush)new BrushConverter().ConvertFromString("#FF489E37")));
                StyleLb.Setters.Add(new Setter(Label.FontStyleProperty, FontStyles.Italic));
                Label lbIndex = new Label() { Content = $"Index : {imgLstTemp[i].Item1}", Style = StyleLb, Padding = new Thickness(0, 5, 0, 1) };
                Label lbTime = new Label() { Content = $"Time : {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss:fff")}", Style = StyleLb, };
                Label lbLocate = new Label() { Content = $"Locate : [X, Y] = [{rectPointLstTemp[i].Item2[0].X.ToString("F2")}, {rectPointLstTemp[i].Item2[0].Y.ToString("F2")}]", Style = StyleLb, };
                Label lbSize = new Label() { Content = $"Width x Height : {imgLstTemp[i].Item2.Width} x {imgLstTemp[i].Item2.Height}", Style = StyleLb, };
                Label lbAngle = new Label() { Content = $"Angle : {imgLstTemp[i].Item3.ToString("F2")}°", Style = StyleLb, };
                st1.Children.Add(lbIndex);
                st1.Children.Add(lbTime);
                st1.Children.Add(lbLocate);
                st1.Children.Add(lbSize);
                st1.Children.Add(lbAngle);
                st0.Children.Add(cnvImg);
                st0.Children.Add(st1);
                trVwIt.Items.Add(st0);
                trVwImgLog.Items.Add(trVwIt);
                brdr.Child = trVwImgLog;
                stImgLogMain.Children.Add(brdr);
            }
        }
        private void SelectBorder(Border bdSelected)
        {
            try
            {
                StackPanel stMain = bdSelected.Parent as StackPanel;
                List<Border> bdLst = stMain.Children.OfType<Border>().ToList();
                bdLst.ForEach(border => border.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC"));
                bdSelected.BorderBrush = Brushes.Red;
                if (bdSelected != bdLst[0])
                {
                    var elements = CanvasImg.Children.Cast<UIElement>().ToList();
                    foreach (UIElement element in elements)
                    {
                        if (element is Image)
                            continue;
                        CanvasImg.Children.Remove(element);
                        canvasCoppy.Children.Add(element);
                    }
                }
                else
                {
                    var elements = canvasCoppy.Children.Cast<UIElement>().ToList();
                    foreach (UIElement element in elements)
                    {
                        canvasCoppy.Children.Remove(element);
                        CanvasImg.Children.Add(element);
                    }
                }
                Canvas canvasImg = (bdSelected.Child as StackPanel).Children.OfType<StackPanel>().FirstOrDefault().Children.OfType<Canvas>().FirstOrDefault();
                //Canvas canvasImg = (bdSelected.Child as TreeView).Items.OfType<TreeViewItem>().FirstOrDefault().Items.OfType<StackPanel>().FirstOrDefault().Children.OfType<Canvas>().FirstOrDefault();
                toolBase.imgView.Source = canvasImg.Children.OfType<Image>().FirstOrDefault().Source;
                toolBase.FitImage();
            }
            catch (Exception ex)
            {
                logger.Create("Select Image Log Error: " + ex.Message, ex);
            }
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
        public Polygon ClonePolygon(Polygon original)
        {
            Polygon clone = new Polygon();

            // Copy các điểm
            foreach (var point in original.Points)
            {
                clone.Points.Add(point);
            }

            // Copy các thuộc tính hình ảnh
            clone.Stroke = original.Stroke;
            clone.Fill = original.Fill;
            clone.StrokeThickness = original.StrokeThickness;

            Canvas.SetLeft(clone, Canvas.GetLeft(original));
            Canvas.SetTop(clone, Canvas.GetTop(original));

            return clone;
        }
        public Rectangle CloneRectangle(Rectangle original)
        {
            if (original == null)
                return null;
            Rectangle rect = new Rectangle
            {
                Name = original.Name,
                Width = original.Width,
                Height = original.Height,
                Fill = original.Fill,
                Stroke = original.Stroke,
                StrokeThickness = original.StrokeThickness,
                RenderTransform = original.RenderTransform?.Clone(),
                RenderTransformOrigin = original.RenderTransformOrigin
            };
            rect.MouseLeftButtonDown += Rect_MouseLeftButtonDown;
            rect.MouseRightButtonDown += Rect_MouseRightButtonDown;

            // Sao chép Canvas.Left và Canvas.Top nếu có
            double left = Canvas.GetLeft(original);
            double top = Canvas.GetTop(original);

            if (!double.IsNaN(left)) Canvas.SetLeft(rect, left);
            if (!double.IsNaN(top)) Canvas.SetTop(rect, top);

            return rect;
        }
        public Label CloneLabel(Label original)
        {
            if (original == null)
                return null;
            Label lb = new Label
            {
                Name = original.Name,
                Content = original.Name.Replace("R", String.Empty),
                FontSize = UiManager.appSettings.Property.labelFontSize,
                Foreground = (Brush)new BrushConverter().ConvertFromString("#FFFF00"),
                RenderTransform = original.RenderTransform?.Clone(),
                RenderTransformOrigin = original.RenderTransformOrigin
            };
            // Sao chép Canvas.Left và Canvas.Top nếu có
            double left = Canvas.GetLeft(original);
            double top = Canvas.GetTop(original);

            if (!double.IsNaN(left)) Canvas.SetLeft(lb, left);
            if (!double.IsNaN(top)) Canvas.SetTop(lb, top);
            return lb;
        }

        private void BtnDeleteAllROI_Click(object sender, RoutedEventArgs e)
        {
            DeleteAllRegion();
        }

        private void BtnMoveROILeft_Click(object sender, RoutedEventArgs e)
        {
            foreach (var shapeEditor in ShapeEditorControls)
            {
                shapeEditor.ReleaseElement();
                shapeEditor.KeyDown -= ShapeEditor_KeyDown;
                shapeEditor.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
            }
            ShapeEditorControls.Clear();
            rectCurLst.Clear();
            int index = CanvasImg.Children.Count;
            for (int i = 1; i < index; i++)
            {
                Canvas.SetLeft(CanvasImg.Children[i], Canvas.GetLeft(CanvasImg.Children[i]) - 2);
            }
        }

        private void BtnMoveROIRight_Click(object sender, RoutedEventArgs e)
        {
            foreach (var shapeEditor in ShapeEditorControls)
            {
                shapeEditor.ReleaseElement();
                shapeEditor.KeyDown -= ShapeEditor_KeyDown;
                shapeEditor.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
            }
            ShapeEditorControls.Clear();
            rectCurLst.Clear();
            int index = CanvasImg.Children.Count;
            for (int i = 1; i < index; i++)
            {
                Canvas.SetLeft(CanvasImg.Children[i], Canvas.GetLeft(CanvasImg.Children[i]) + 2);
            }
        }

        private void BtnMoveROIUp_Click(object sender, RoutedEventArgs e)
        {
            foreach (var shapeEditor in ShapeEditorControls)
            {
                shapeEditor.ReleaseElement();
                shapeEditor.KeyDown -= ShapeEditor_KeyDown;
                shapeEditor.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
            }
            ShapeEditorControls.Clear();
            rectCurLst.Clear();
            int index = CanvasImg.Children.Count;
            for (int i = 1; i < index; i++)
            {
                Canvas.SetTop(CanvasImg.Children[i], Canvas.GetTop(CanvasImg.Children[i]) - 2);
            }
        }

        private void BtnMoveROIDown_Click(object sender, RoutedEventArgs e)
        {
            foreach (var shapeEditor in ShapeEditorControls)
            {
                shapeEditor.ReleaseElement();
                shapeEditor.KeyDown -= ShapeEditor_KeyDown;
                shapeEditor.LostKeyboardFocus -= ShapeEditor_LostKeyboardFocus;
            }
            ShapeEditorControls.Clear();
            rectCurLst.Clear();
            int index = CanvasImg.Children.Count;
            for (int i = 1; i < index; i++)
            {
                Canvas.SetTop(CanvasImg.Children[i], Canvas.GetTop(CanvasImg.Children[i]) + 2);
            }
        }

    }
    public class TableRow : INotifyPropertyChanged
    {
        public string Sub { get; set; }

        private List<bool> _imageChecks;
        private bool _allChecked;
        public List<bool> ImageChecks { 
            get
            {
                return _imageChecks;
            } 
            set { _imageChecks = value;   OnPropertyChanged(nameof(ImageChecks)); }}
        public bool AllChecked
        {
            get => _allChecked;
            set
            {
                if (_allChecked != value)
                {
                    _allChecked = value;
                    OnPropertyChanged();

                    // Nếu checkbox "All" được tích, tất cả các checkbox trong hàng cũng được tích
                    if (_allChecked)
                    {
                        for (int i = 0; i < ImageChecks.Count; i++)
                        {
                            ImageChecks[i] = _allChecked;
                        }
                        OnPropertyChanged(nameof(ImageChecks));
                    } 
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected void OnPropertyChanged()
        {

        }
    }
}
