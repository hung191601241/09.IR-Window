using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Development;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using VisionInspection;
using Xceed.Wpf.AvalonDock.Themes;
using static VisionTools.ToolEdit.BlobEdit;
using Window = System.Windows.Window;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for WndOutResultEdit.xaml
    /// </summary>
    public partial class OutBlobResEdit : GridBase, INotifyPropertyChanged
    {
        //Variable
        MyLogger logger = new MyLogger("OutBlobRes Edit");
        public bool resultOut = true; 
        public List<int> indexImgs = new List<int>();
        public List<string> addrOKLst = new List<string>();
        public List<string> addrNGLst = new List<string>();
        private DataTable dataTable = new DataTable();
        private DataView dataView = new DataView();
        public event RoutedEventHandler OnBtnRunClicked;
        private List<Tuple<int, SvImage, double>> _imgLstSub = new List<Tuple<int, SvImage, double>>();
        public List<Tuple<int, SvImage, double>> ImgLstSub
        {
            get => _imgLstSub;
            set
            {
                _imgLstSub = value;
            }
        }

        //InOut
        private List<BlobObject> blobs1 = new List<BlobObject>();
        private List<BlobObject> blobs2 = new List<BlobObject>();
        public List<BlobObject> Blobs1
        {
            get
            {
                if (blobs1 == null)
                    return new List<BlobObject>();
                return blobs1;
            }
            set => blobs1 = value;
        }
        public List<BlobObject> Blobs2
        {
            get
            {
                if (blobs2 == null)
                    return new List<BlobObject>();
                return blobs2;
            }
            set => blobs2 = value;
        }
        public SvImage OriginImage { get; set; } = new SvImage();
        public SvImage InputImage { get; set; } = new SvImage();
        public SvImage OutputImage { get; set; } = new SvImage();

        //[*********************************** BINDING ***********************************]
        public event PropertyChangedEventHandler PropertyChanged;
        public Array DeviceCodes => Enum.GetValues(typeof(DeviceCode));

        private DeviceCode _selectDevOutOK = DeviceCode.M, _selectDevOutNG = DeviceCode.M;
        public DeviceCode SelectDevOutOK { get => _selectDevOutOK; set => _selectDevOutOK = value; }
        public DeviceCode SelectDevOutNG { get => _selectDevOutNG; set => _selectDevOutNG = value; }
        private double _distSet = 0d, _distReal = 0d;
        private List<BlobObject> blobs = new List<BlobObject>();
        public List<BlobObject> Blobs
        {
            get
            {
                if (blobs == null)
                    return new List<BlobObject>();
                return blobs;
            }
            set { blobs = value; OnPropertyChanged(nameof(Blobs)); }
        }
        public double DistSet { get => _distSet; set { _distSet = value; OnPropertyChanged(nameof(DistSet)); } }
        public double DistReal { get => _distReal; set { _distReal = value; OnPropertyChanged(nameof(DistReal)); } }
        public DataView DataView { get => dataView; set { dataView = value; OnPropertyChanged(nameof(DataView)); } }
        protected void OnPropertyChanged(string pptName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pptName));
        }
        public OutBlobResEdit()
        {
            InitializeComponent();
            DisplayInit();
            RegisterEvent();
            toolBase.DataContext = this;
            DataView = dataTable.DefaultView;
        }

        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "OutBlobResult";
            toolBase.cbxImage.Items.Add("[OutBlobResult] Input Image");
            toolBase.cbxImage.Items.Add("[OutBlobResult] Output Image");
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
            this.Loaded += OutBlobResultEdit_Loaded;
            this.Unloaded += OutBlobResultEdit_Unloaded;
            txtAddrOutOK.KeyDown += TxtAddrOut_KeyDown;
            txtAddrOutNG.KeyDown += TxtAddrOut_KeyDown;
        }

        private void OutBlobResultEdit_Unloaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtAddrOutOK.Text) || string.IsNullOrEmpty(txtAddrOutNG.Text))
            {
                MessageBox.Show("Address PLC can not empty!");
                return;
            }
            try
            {
                for (int col = 1; col < dataTable.Columns.Count; col++)
                {
                    // Lấy và parse giá trị từ DataTable
                    if (string.IsNullOrEmpty(dataTable.Rows[0][col].ToString()) || string.IsNullOrEmpty(dataTable.Rows[1][col].ToString()))
                    {
                        MessageBox.Show("Address PLC can not empty!");
                        return;
                    }
                    else
                    {
                        addrOKLst[col - 1] = dataTable.Rows[0][col]?.ToString();
                        addrNGLst[col - 1] = dataTable.Rows[1][col]?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Add PLC Address Error: " + ex.Message, ex);
            }
        }

        private void TxtAddrOut_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!string.IsNullOrEmpty(txtAddrOutOK.Text))
                {
                    if (!CheckIntSyntax(txtAddrOutOK.Text))
                    {
                        MessageBox.Show("Error PLC Address Syntax!");
                        return;
                    }
                }
                if (!string.IsNullOrEmpty(txtAddrOutNG.Text))
                {
                    if (!CheckIntSyntax(txtAddrOutNG.Text))
                    {
                        MessageBox.Show("Error PLC Address Syntax!");
                        return;
                    }
                }
                if (ImgLstSub.Count <= 0)
                {
                    MessageBox.Show("Have no Image in Sub Program!");
                    return;
                }
                try
                {
                    addrOKLst.Clear();
                    addrNGLst.Clear();
                    for (int i = 0; i < ImgLstSub.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(txtAddrOutOK.Text))
                            addrOKLst.Add(String.Format($"{SelectDevOutOK.ToString()}{int.Parse(txtAddrOutOK.Text) + i}"));
                        if (!string.IsNullOrEmpty(txtAddrOutNG.Text))
                            addrNGLst.Add(String.Format($"{SelectDevOutNG.ToString()}{int.Parse(txtAddrOutNG.Text) + i}"));
                    }
                }
                catch (Exception ex)
                {
                    logger.Create("Add PLC Address Error: " + ex.Message, ex);
                }
                UpdateDataGrid();
            }
        }
        private void UpdateDataGrid()
        {
            try
            {
                dataTable.Columns.Clear();
                dataTable.Rows.Clear();
                indexImgs.Clear();
                // Tạo cột đầu tiên là row header
                dataTable.Columns.Add("ResultType");
                ImgLstSub.ForEach(x => indexImgs.Add(x.Item1));
                // Tạo cột tương ứng với các phần tử trong img
                foreach (var item in ImgLstSub)
                {
                    string columnHeader = $"Image{item.Item1}";
                    dataTable.Columns.Add(columnHeader);
                }

                // Thêm dòng OK
                if (!string.IsNullOrEmpty(txtAddrOutOK.Text))
                {
                    DataRow rowOK = dataTable.NewRow();
                    rowOK["ResultType"] = "OK";
                    for (int i = 0; i < ImgLstSub.Count; i++)
                    {
                        rowOK[$"Image{ImgLstSub[i].Item1}"] = addrOKLst[i];
                    }
                    dataTable.Rows.Add(rowOK);
                }


                // Thêm dòng NG
                if (!string.IsNullOrEmpty(txtAddrOutNG.Text))
                {
                    DataRow rowNG = dataTable.NewRow();
                    rowNG["ResultType"] = "NG";
                    for (int i = 0; i < ImgLstSub.Count; i++)
                    {
                        rowNG[$"Image{ImgLstSub[i].Item1}"] = addrNGLst[i];
                    }
                    dataTable.Rows.Add(rowNG);
                }

                // Gán vào DataGrid
                DataView = null;
                DataView = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                logger.Create("Update PLC Address Error: " + ex.Message, ex);
            }
        }
        public void UpdateDataGrid(int addrCount)
        {
            try
            {
                dataTable.Columns.Clear();
                dataTable.Rows.Clear();
                // Tạo cột đầu tiên là row header
                dataTable.Columns.Add("ResultType");

                // Tạo cột tương ứng với các phần tử trong img
                foreach (var item in indexImgs)
                {
                    string columnHeader = $"Image{item}";
                    dataTable.Columns.Add(columnHeader);
                }

                // Thêm dòng OK
                if (!string.IsNullOrEmpty(txtAddrOutOK.Text))
                {
                    DataRow rowOK = dataTable.NewRow();
                    rowOK["ResultType"] = "OK";
                    for (int i = 0; i < addrCount; i++)
                    {
                        rowOK[$"Image{indexImgs[i]}"] = addrOKLst[i];
                    }
                    dataTable.Rows.Add(rowOK);
                }
                // Thêm dòng NG
                if (!string.IsNullOrEmpty(txtAddrOutNG.Text))
                {
                    DataRow rowNG = dataTable.NewRow();
                    rowNG["ResultType"] = "NG";
                    for (int i = 0; i < addrCount; i++)
                    {
                        rowNG[$"Image{indexImgs[i]}"] = addrNGLst[i];
                    }
                    dataTable.Rows.Add(rowNG);
                }

                // Gán vào DataGrid
                DataView = null;
                DataView = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                logger.Create("Display Init Error: " + ex.Message, ex);
            }
        }
        private bool CheckIntSyntax(string num)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(num, @"^[1-9]\d*$");
        }
        private void OutBlobResultEdit_Loaded(object sender, RoutedEventArgs e)
        {
            //AddBlobsInput();
        }
        private void AddBlobsInput()
        {
            Blobs.Clear();
            if (Blobs1.Count > 0)
            {
                Blobs1.ForEach(b => Blobs.Add(b));
            }
            if (Blobs2.Count > 0)
            {
                Blobs2.ForEach(b => Blobs.Add(b));
            }
        }
        private bool SetResultOK(DeviceCode devOK, string addrOK, bool value)
        {
            try
            {
                return UiManager.PLC1.device.WriteBit(devOK, int.Parse(addrOK), value);
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("WRITE_RESULT_OK: " + ex.Message));
                return false;
            }
        }
        private bool SetResultNG(DeviceCode devNG, string addrNG, bool value)
        {
            try
            {
                return UiManager.PLC1.device.WriteBit(devNG, int.Parse(addrNG), value);
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("WRITE_RESULT_NG: " + ex.Message));
                return false;
            }
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
        public void SendResultToPLC(string addrOK, string addrNG, bool result)
        {
            String2Enum(addrOK, out DeviceCode devCodeOK, out string devNoOK);
            SetResultOK(devCodeOK, devNoOK, result);
            String2Enum(addrNG, out DeviceCode devCodeNG, out string devNoNG);
            SetResultNG(devCodeNG, devNoNG, !result);
        }
        private void CbxImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (toolBase.cbxImage.SelectedIndex == 0)
                {
                    if (InputImage.Mat.Height > 0 && InputImage.Mat.Width > 0)
                    {
                        toolBase.imgView.Source = InputImage.Mat.ToBitmapSource();
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

        private SvImage runImage = new SvImage();
        public Point2d centPtBlob1 = new Point2d(0, 0);
        public Point2d centPtBlob2 = new Point2d(0, 0);
        public override void Run()
        {
            if (InputImage == null || InputImage.Mat == null || InputImage.Mat.Height <= 0 || InputImage.Mat.Width <= 0)
            {
                meaRunTime.Stop();
                toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "InputImage is null or error!");
                resultOut = false;
                return;
            }
            if (OriginImage == null || OriginImage.Mat == null || OriginImage.Mat.Height <= 0 || OriginImage.Mat.Width <= 0)
            {
                meaRunTime.Stop();
                toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "OriginImage is null or error!");
                resultOut = false;
                return;
            }
            //Thêm các Blob đầu vào vào Blobs
            AddBlobsInput();
            //Nếu số lượng Blob không đạt
            if (Blobs1.Count <= 0 || Blobs2.Count <= 0)
            {
                resultOut = false;
                //SendResultToPLC(addrOKLst[0], addrNGLst[0], resultOut);
            }
            //Nếu khoảng cách Blob không đạt
            else if (Math.Sqrt(Math.Pow(Blobs1[0].CenterMassX - Blobs2[0].CenterMassX, 2) + Math.Pow(Blobs1[0].CenterMassY - Blobs2[0].CenterMassY, 2)) > DistSet)
            {
                resultOut = false;
                //SendResultToPLC(addrOKLst[0], addrNGLst[0], resultOut);
            }

            //Nếu không rơi vào 2 TH trên thì OK
            //SendResultToPLC(addrOKLst[0], addrNGLst[0], resultOut);

            if (OriginImage.Mat.Channels() == 1)
            {
                OriginImage.Mat = OriginImage.Mat.CvtColor(ColorConversionCodes.GRAY2RGB);
            }
            //Tâm ROI hiện tại (chưa xoay, chưa tịnh tiến)
            Point2d CP = new Point2d(InputImage.Mat.Width / 2, InputImage.Mat.Height / 2);
            // 1. Dịch ngược tâm Roi ban đầu về gốc (đưa tâm về (0,0))
            Mat toOrigin = new Mat(3, 3, MatType.CV_64FC1, new double[] {1, 0, -CP.X,
                                                                         0, 1, -CP.Y,
                                                                         0, 0, 1    });
            // 2. Xoay quanh gốc (tức là quanh tâm ban đầu sau khi đưa về gốc)
            Mat rotate = new Mat(3, 3, MatType.CV_64FC1, new double[] {
                Math.Cos(InputImage.AnglgeRoiRad), -Math.Sin(InputImage.AnglgeRoiRad), 0,
                Math.Sin(InputImage.AnglgeRoiRad),  Math.Cos(InputImage.AnglgeRoiRad), 0,
                0,                                  0,                                 1});
            // 3. Dịch đến vị trí Roi sau khi đã tịnh tiến và xoay
            Mat goToCenter = new Mat(3, 3, MatType.CV_64FC1, new double[] {   1, 0, InputImage.CenterPoint.X,
                                                                                0, 1, InputImage.CenterPoint.Y,
                                                                                0, 0, 1});
            // Tổng hợp ma trận: M = goToCenter × rotate × toOrigin
            Mat transform2D = goToCenter * (rotate * toOrigin);

            try
            {
                runImage = InputImage.Clone(true);
                if (runImage.Mat.Channels() == 1)
                {
                    runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.GRAY2RGB);
                }
                if (Blobs1.Count > 0)
                {
                    //Draw Blob1 in OutputImage
                    Cv2.Line(runImage.Mat, new OpenCvSharp.Point(Blobs1[0].CenterMassX - 2, Blobs1[0].CenterMassY), new OpenCvSharp.Point(Blobs1[0].CenterMassX + 2, Blobs1[0].CenterMassY), Scalar.Yellow);
                    Cv2.Line(runImage.Mat, new OpenCvSharp.Point(Blobs1[0].CenterMassX, Blobs1[0].CenterMassY - 2), new OpenCvSharp.Point(Blobs1[0].CenterMassX, Blobs1[0].CenterMassY + 2), Scalar.Yellow);
                    OpenCvSharp.Rect rectBlob1 = new OpenCvSharp.Rect((int)Blobs1[0].CenterMassX - Blobs1[0].RectWidth / 2, (int)Blobs1[0].CenterMassY - Blobs1[0].RectHeight / 2, Blobs1[0].RectWidth, Blobs1[0].RectHeight);
                    Cv2.Rectangle(runImage.Mat, rectBlob1, resultOut ? Scalar.LightGreen : Scalar.Red);
                    //Draw Blob1 in OriginImage
                    centPtBlob1 = SvFunc.ImageToFixture2D(new Point2d(Blobs1[0].CenterMassX, Blobs1[0].CenterMassY), transform2D);
                    Cv2.Line(OriginImage.Mat, new OpenCvSharp.Point(centPtBlob1.X - 4, centPtBlob1.Y), new OpenCvSharp.Point(centPtBlob1.X + 4, centPtBlob1.Y), Scalar.Yellow, 1);
                    Cv2.Line(OriginImage.Mat, new OpenCvSharp.Point(centPtBlob1.X, centPtBlob1.Y - 4), new OpenCvSharp.Point(centPtBlob1.X, centPtBlob1.Y + 4), Scalar.Yellow, 1);
                    OpenCvSharp.Rect rectBlob11 = new OpenCvSharp.Rect((int)centPtBlob1.X - Blobs1[0].RectWidth / 2, (int)centPtBlob1.Y - Blobs1[0].RectHeight / 2, Blobs1[0].RectWidth, Blobs1[0].RectHeight);
                    Cv2.Rectangle(OriginImage.Mat, rectBlob11, resultOut ? Scalar.LightGreen : Scalar.Red, 2);
                }
                if (Blobs2.Count > 0)
                {
                    //Draw Blob2 in OutputImage
                    Cv2.Line(runImage.Mat, new OpenCvSharp.Point(Blobs2[0].CenterMassX - 2, Blobs2[0].CenterMassY), new OpenCvSharp.Point(Blobs2[0].CenterMassX + 2, Blobs2[0].CenterMassY), Scalar.Yellow);
                    Cv2.Line(runImage.Mat, new OpenCvSharp.Point(Blobs2[0].CenterMassX, Blobs2[0].CenterMassY - 2), new OpenCvSharp.Point(Blobs2[0].CenterMassX, Blobs2[0].CenterMassY + 2), Scalar.Yellow);
                    OpenCvSharp.Rect rectBlob2 = new OpenCvSharp.Rect((int)Blobs2[0].CenterMassX - Blobs2[0].RectWidth / 2, (int)Blobs2[0].CenterMassY - Blobs2[0].RectHeight / 2, Blobs2[0].RectWidth, Blobs2[0].RectHeight);
                    Cv2.Rectangle(runImage.Mat, rectBlob2, resultOut ? Scalar.LightGreen : Scalar.Red);
                    //Draw Blob2 in OriginImage
                    centPtBlob2 = SvFunc.ImageToFixture2D(new Point2d(Blobs2[0].CenterMassX, Blobs2[0].CenterMassY), transform2D);
                    Cv2.Line(OriginImage.Mat, new OpenCvSharp.Point(centPtBlob2.X - 4, centPtBlob2.Y), new OpenCvSharp.Point(centPtBlob2.X + 4, centPtBlob2.Y), Scalar.Yellow, 1);
                    Cv2.Line(OriginImage.Mat, new OpenCvSharp.Point(centPtBlob2.X, centPtBlob2.Y - 4), new OpenCvSharp.Point(centPtBlob2.X, centPtBlob2.Y + 4), Scalar.Yellow, 1);
                    OpenCvSharp.Rect rectBlob11 = new OpenCvSharp.Rect((int)centPtBlob2.X - Blobs2[0].RectWidth / 2, (int)centPtBlob2.Y - Blobs2[0].RectHeight / 2, Blobs2[0].RectWidth, Blobs2[0].RectHeight);
                    Cv2.Rectangle(OriginImage.Mat, rectBlob11, resultOut? Scalar.LightGreen : Scalar.Red, 2);
                }
                if (Blobs1.Count > 0 && Blobs2.Count > 0)
                {
                    //Draw in OutputImage
                    Cv2.Line(runImage.Mat, new OpenCvSharp.Point(Blobs1[0].CenterMassX, Blobs1[0].CenterMassY), new OpenCvSharp.Point(Blobs2[0].CenterMassX, Blobs2[0].CenterMassY), resultOut ? Scalar.Green : Scalar.Red);
                    DistReal = Math.Sqrt(Math.Pow(Blobs1[0].CenterMassX - Blobs2[0].CenterMassX, 2) + Math.Pow(Blobs1[0].CenterMassY - Blobs2[0].CenterMassY, 2));
                    DistReal = Math.Round(DistReal, 2);
                    Cv2.PutText(runImage.Mat, $"Distance = {DistReal}", new OpenCvSharp.Point(10, 20), HersheyFonts.Italic, 0.5d, resultOut ? Scalar.Green : Scalar.Red, thickness: 1);

                    //Draw in OriginImage
                    Cv2.Line(OriginImage.Mat, new OpenCvSharp.Point(centPtBlob1.X, centPtBlob1.Y), new OpenCvSharp.Point(centPtBlob2.X, centPtBlob2.Y), resultOut ? Scalar.Green : Scalar.Red);
                    Cv2.PutText(OriginImage.Mat, $"Distance = {DistReal}", new OpenCvSharp.Point(10, 40), HersheyFonts.Italic, 1d, resultOut ? Scalar.Green : Scalar.Red, thickness: 2);
                }
                Cv2.PutText(runImage.Mat, resultOut ? "OK" : "NG", new OpenCvSharp.Point(10, 60), HersheyFonts.Italic, 1d, resultOut ? Scalar.Green : Scalar.Red, thickness: 5);
                //Draw in OriginImage
                Cv2.PutText(OriginImage.Mat, resultOut ? "OK" : "NG", new OpenCvSharp.Point(10, 120), HersheyFonts.Italic, 3d, resultOut ? Scalar.Green : Scalar.Red, thickness: 15);

                OutputImage = runImage.Clone(true);
                toolBase.imgView.Source = OutputImage.Mat.ToBitmapSource();
            }
            catch (Exception ex)
            {
                logger.Create("Run Error: " + ex.Message, ex);
                meaRunTime.Stop();
                toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, $"OutputBlobResultTool: {ex.Message}");
                return;
            }
        }
    }
}
