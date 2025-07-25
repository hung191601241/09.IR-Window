using Development;
using DeviceSource;
using ITM_Semiconductor;
using MvCamCtrl.NET;
using MVSDK_Net;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Flann;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using ViDi2;
using VisionTools.ToolDesign;
using VisionTools.ToolEdit;
using Xceed.Wpf.Toolkit.Primitives;
using static MVSDK_Net.IMVDefine;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using IMVCamera = MVSDK_Net.MyCamera;
using MVSCamera = MvCamCtrl.NET.MyCamera;
using Page = System.Windows.Controls.Page;
using Point = System.Windows.Point;

namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for PgCamera.xaml
    /// </summary>
    public partial class PgCamera : Page
    {
        //private static PgCamera instance = new PgCamera();
        //public static PgCamera Instance
        //{
        //    get
        //    {
        //        if (instance == null)
        //        {
        //            instance = new PgCamera();
        //        }
        //        return instance;
        //    }
        //}

        private Object lockMousemov = new Object();

        private bool autoScrollMode = true;
        private ConnectionSettings connectionSettings = UiManager.appSettings.connection;
        private static MyLogger logger = new MyLogger("Camera Page");
        private HikCam searchCam = new HikCam();
        CameraOperator m_pOperator = new CameraOperator();
        CameraOperatorHandle cameraHandle = new CameraOperatorHandle();
        private System.Timers.Timer clock;
        private System.Timers.Timer cycleTimer;
        private Mat Image;


        //Canvas
        //private Brush colorRectFill = (Brush)new BrushConverter().ConvertFromString("#3000b189");
        //private Brush colorRectStroke = (Brush)new BrushConverter().ConvertFromString("#00b189");
        private Brush colorRectFill = (Brush)new BrushConverter().ConvertFromString("#40DC143C");
        private Brush colorRectStroke = (Brush)new BrushConverter().ConvertFromString("#DC143C");
        private Brush colorSearchStroke = (Brush)new BrushConverter().ConvertFromString("#36de1b");
        private Brush colorSearchFill = (Brush)new BrushConverter().ConvertFromString("#4036de1b");

        //Trasnform Image
        protected bool isDragging;
        ScaleTransform scaleTrans = new ScaleTransform();
        TranslateTransform translateTrans = new TranslateTransform();
        private System.Windows.Point lastMousePosition;
        public PgCamera()
        {
            InitializeComponent();
            TransFormCoordinate();
            this.clock = new System.Timers.Timer(500);
            this.cycleTimer = new System.Timers.Timer(500);
            this.cycleTimer.AutoReset = true;
            this.cycleTimer.Elapsed += CycleTimer_Elapsed;
            this.clock.AutoReset = true;

            this.menuPylonView.Click += menuPylonView_Clicked;
            this.menuMVSView.Click += menuMVSView_Clicked;
            this.menuHikCamView.Click += menuHikCamView_CLicked;

            //Camera
            //AddeviceCam();
            //showCamDevice();

            //Canvas
            //Image Source Update Event
            var prop = DependencyPropertyDescriptor.FromProperty(System.Windows.Controls.Image.SourceProperty, typeof(System.Windows.Controls.Image));
            prop.AddValueChanged(this.imgView, SourceChangedHandler);

            //Tabar
            this.btnCamCenterLine.Click += btnCamCenterLine_Clicked;
            this.btnCameraZoomOut.Click += BtnCameraZoomOut_Click;
            this.btnCameraZoomIn.Click += BtnCameraZoomIn_Click;


            this.btnCamSaveSetting.Click += this.btnCamSave_Clicked;
            this.Loaded += this.PgCamera_Load;
            this.Unloaded += PgCamera_Unloaded;

            Canvas.SetLeft(myCanvas, 100);
            Canvas.SetTop(myCanvas, 100);

            //load Tab Vision Tool Setting
            this.cbxDisplayResult.SelectionChanged += CbxDisplayResult_SelectionChanged;
            this.lbVisionProgram.MouseRightButtonDown += LbVisionProgram_MouseRightButtonDown;
            this.cbxJob.SelectionChanged += CbxJob_SelectionChanged;
            JobSet();
            LoadJob();
        }
        private void btnCamSave_Clicked(object sender, RoutedEventArgs e)
        {
            SaveData();
        }
        #region Event tab
        private void CycleTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // ShowDataMatrix();
            if (UiManager.appSettings.caseShowDataMatrixRT)
            {
                UiManager.appSettings.caseShowDataMatrixRT = false;
                UiManager.SaveAppSettings();
            }
            return;
        }

        

        private void PgCamera_Unloaded(object sender, RoutedEventArgs e)
        {
        }
        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (e.Source.GetType().Equals(typeof(ScrollViewer)))
                {
                    ScrollViewer sv = (ScrollViewer)e.Source;
                    if (sv != null)
                    {
                        // User scroll event : set or unset autoscroll mode
                        if (e.ExtentHeightChange == 0)
                        {   // Content unchanged : user scroll event
                            if (sv.VerticalOffset == sv.ScrollableHeight)
                            {   // Scroll bar is in bottom -> Set autoscroll mode
                                autoScrollMode = true;
                            }
                            else
                            {   // Scroll bar isn't in bottom -> Unset autoscroll mode
                                autoScrollMode = false;
                            }
                        }

                        // Content scroll event : autoscroll eventually
                        if (autoScrollMode && e.ExtentHeightChange != 0)
                        {   // Content changed and autoscroll mode set -> Autoscroll
                            sv.ScrollToVerticalOffset(sv.ExtentHeight);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Write("ScrollChanged error:" + ex.Message);
            }
        }
        private void PgCamera_Load(object sender, RoutedEventArgs e)
        {
            showTabar();
            cycleTimer.Start();
            try
            {
                Mat srcDisplay1 = Cv2.ImRead("temp1.bmp", ImreadModes.Color);
            }
            catch (Exception ex)
            {
                logger.Create("ReadTemp1 Image Err" + ex.Message);
            }

            enableImage(imgView, @"Images\OK.bmp");

            //this.JobSet();
            //this.DeleteAllVP();
            //this.LoadJob();

            //DeleteAcqTool();
            //LoadAcqTool();
            cbxDisplayResult.ItemsSource = outResToolNames;
        }

        #endregion

        #region Camera
        //Boolean AddeviceCam()
        //{
        //    //hikCamera.DeviceListAcq();

        //    MyCamera.MV_CC_DEVICE_INFO_LIST m_pDeviceList = UiManager.hikCamera.m_pDeviceList;
        //    for (int i = 0; i < m_pDeviceList.nDeviceNum; i++)
        //    {
        //        MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
        //        if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
        //        {
        //            IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
        //            MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
        //            if (gigeInfo.chUserDefinedName != "")
        //            {
        //                string Caminfo = (String.Format("GigE: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")"));
        //                ConectCamera(device);

        //            }
        //            else
        //            {
        //                string Caminfo = String.Format(("GigE: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")"));
        //                ConectCamera(device);
        //            }
        //        }
        //        else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
        //        {
        //            IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stUsb3VInfo, 0);
        //            MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));

        //            if (usbInfo.chUserDefinedName != "")
        //            {
        //                string Caminfo = String.Format(("USB: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")"));
        //                ConectCamera(device);
        //            }
        //            else
        //            {
        //                string Caminfo = String.Format(("USB: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")"));
        //                ConectCamera(device);
        //            }
        //        }
        //    }
        //    return true;

        //}
        //void showCamDevice()
        //{
        //    MyCamera.MV_CC_DEVICE_INFO device = connectionSettings.camera1.device;
        //    MyCamera.MV_CC_DEVICE_INFO device2 = connectionSettings.camera2.device;

        //    //Cam1
        //    if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
        //    {
        //        IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
        //        MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
        //        if (gigeInfo.chUserDefinedName != "")
        //        {
        //            string Caminfo = (String.Format("GigE: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")"));
        //            ConectCamera(device);
        //        }
        //        else
        //        {
        //            string Caminfo = String.Format(("GigE: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")"));
        //            ConectCamera(device);
        //        }
        //    }
        //    else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
        //    {
        //        IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stUsb3VInfo, 0);
        //        MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));

        //        if (usbInfo.chUserDefinedName != "")
        //        {
        //            string Caminfo = String.Format(("USB: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")"));
        //            ConectCamera(device);
        //        }
        //        else
        //        {
        //            string Caminfo = String.Format(("USB: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")"));
        //            ConectCamera(device);
        //        }
        //    }
        //}

        //public void ConectCamera(MyCamera.MV_CC_DEVICE_INFO device)
        //{
        //    int ret = UiManager.Cam1.Open(device, HikCam.AquisMode.AcquisitionMode);
        //    Thread.Sleep(2);
        //    if (ret == MyCamera.MV_OK)
        //    {
        //        Mat img = UiManager.Cam1.CaptureImage();
        //        //return true;
        //    }
        //    else
        //    {
        //        //return false;
        //    }


        //}
        #endregion


        #region memnuItem Event



        void menuPylonView_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(@"C:\Program Files\Basler\pylon 5\Applications\x64\PylonViewerApp.exe");
            }
            catch (Exception ex)
            {
                logger.Create("Start Process Pylon View Err.." + ex.ToString());
            }

        }
        void menuMVSView_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(@"C:\Program Files (x86)\MVS\Applications\Win64\MVS.exe");
            }
            catch (Exception ex)
            {
                logger.Create("Start Process MVS View Err.." + ex.ToString());
            }

        }
        void menuHikCamView_CLicked(object sender, RoutedEventArgs e)
        {
            //Form1 frm = new Form1();
            //frm.ShowDialog();
        }
        #endregion

        #region CameraFuntion

        private void CommonCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        #endregion

        #region Tabar
        private void BtnCameraZoomOut_Click(object sender, RoutedEventArgs e)
        {
            scaleTrans.ScaleX *= 1.1d;
            scaleTrans.ScaleY *= 1.1d;
        }
        private void BtnCameraZoomIn_Click(object sender, RoutedEventArgs e)
        {
            scaleTrans.ScaleX /= 1.1d;
            scaleTrans.ScaleY /= 1.1d;
        }
        void showTabar()
        {
            enableImage(cameraGrab, @"Images\play.png");
            enableImage(cameraCenterLine, @"Images\center.png");
            enableImage(cameraGridLine, @"Images\grid.png");
            enableImage(cameraZoomIn, @"Images\zoomin.png");
            enableImage(cameraZoomOut, @"Images\zoomout.png");
            enableImage(cameraFrameSave, @"Images\saveFolder.png");
        }

        void enableImage(System.Windows.Controls.Image img, String path)
        {
            try
            {
                this.Image = Cv2.ImRead(path, ImreadModes.Color);
            }
            catch (Exception e)
            {
                logger.Create("Load Image Err" + e.Message);
            }

            var folder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(folder);
            bitmap.EndInit();
            img.Source = bitmap;
        }

        void btnCamCenterLine_Clicked(object sender, RoutedEventArgs e)
        {
            this.cameraHandle.CrossCenter = !cameraHandle.CrossCenter;
            if (cameraHandle.CrossCenter)
            {
                LineCreossX.Visibility = Visibility.Visible;
                LineCreossY.Visibility = Visibility.Visible;
            }
            else
            {
                LineCreossX.Visibility = Visibility.Hidden;
                LineCreossY.Visibility = Visibility.Hidden;
            }
        }

        #endregion

        #region Config Canvas
        void SourceChangedHandler(object sender, EventArgs e)
        {
            System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
            myCanvas.Width = imgView.Source.Width;
            myCanvas.Height = imgView.Source.Height;
            //Cross X
            LineCreossX.X1 = 0;
            LineCreossX.Y1 = this.imgView.Source.Height / 2;
            LineCreossX.X2 = this.imgView.Source.Width;
            LineCreossX.Y2 = this.LineCreossX.Y1;
            //Cross Y
            LineCreossY.X1 = this.imgView.Source.Width / 2;
            LineCreossY.Y1 = 0;
            LineCreossY.X2 = this.LineCreossY.X1;
            LineCreossY.Y2 = this.imgView.Source.Height;

            rect.Width = 100;
            rect.Height = 100;
            Canvas.SetLeft(rect, myCanvas.Width / 2 - rect.Width / 2);
            Canvas.SetTop(rect, myCanvas.Height / 2 - rect.Height / 2);

        }
        private void Container_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Point mousePosition = e.GetPosition(myGrid);
                double zoomFactor = e.Delta > 0 ? 1.1 : (1 / 1.1);

                // Cập nhật giá trị ScaleX và ScaleY
                scaleTrans.ScaleX *= zoomFactor;
                scaleTrans.ScaleY *= zoomFactor;

                // Tính toán lại vị trí của ảnh dựa trên vị trí chuột
                translateTrans.X = (1 - zoomFactor) * (mousePosition.X) + zoomFactor * translateTrans.X;
                translateTrans.Y = (1 - zoomFactor) * (mousePosition.Y) + zoomFactor * translateTrans.Y;
            }
        }
        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            lastMousePosition = e.GetPosition(myGrid);
            myCanvas.CaptureMouse();
        }

        private void MainCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            myCanvas.ReleaseMouseCapture();
        }

        private void MainCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2 && e.RightButton == MouseButtonState.Pressed)
            {
                FitImage();
            }
        }
        public void FitImage()
        {
            if (imgView == null || imgView.Source == null) return;

            double canvasWidth = myGrid.ActualWidth;
            double canvasHeight = myGrid.ActualHeight;
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
        }
        private void TransFormCoordinate()
        {
            // Tạo một TransformGroup để kết hợp phóng to/thu nhỏ và di chuyển
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(scaleTrans);
            transformGroup.Children.Add(translateTrans);
            myCanvas.RenderTransform = transformGroup;
        }
        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            //Check object Canvas
            if (isDragging && Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Point currentPosition = e.GetPosition(myGrid);
                double offsetX = currentPosition.X - lastMousePosition.X;
                double offsetY = currentPosition.Y - lastMousePosition.Y;

                translateTrans.X += offsetX;
                translateTrans.Y += offsetY;

                lastMousePosition = currentPosition;
            }
            Point mousePosCanvas = e.GetPosition(myCanvas);
            ShowToolTip(e);
        }

        private void ShowToolTip(MouseEventArgs e)
        {
            lock (lockMousemov)
            {
                System.Windows.Point currentPos = e.GetPosition(myCanvas);
                System.Windows.Point currentPos2 = e.GetPosition((FrameworkElement)myCanvas.Parent);
                myToolTip.RenderTransform = new TranslateTransform(currentPos.X + 20, currentPos.Y);
                int X = 0;
                int Y = 0;
                //myToolTip.Text = "X=" + currentPos.X + ";Y=" + currentPos.Y + "\n";
                try
                {
                    X = Convert.ToInt32(Math.Round(currentPos.X, 0));
                    Y = Convert.ToInt32(Math.Round(currentPos.Y, 0));
                }
                catch
                {

                }
                this.canVasPos.Content = String.Format("Position: {0}, {1}", X, Y);
                try
                {
                    //var pixel = this.Image.Get<Vec3b>(Y, X);
                    //this.CanImageRGB.Content = String.Format("R: {0}, G: {1}, B: {2}", pixel.Item0, pixel.Item1, pixel.Item2);
                }
                catch
                {

                }


                this.CanResolution.Content = String.Format("Image: {0} x {1}", this.Image.Width, this.Image.Height);
            }
            //myToolTip.Text += "Cursor position from Parent : X=" + currentPos2.X + ";Y=" + currentPos2.Y + "\n";
            //myToolTip.Text += "OffsetXY of MainCanvas: X=" + myCanvas.RenderTransform.Value.OffsetX + ";Y=" + myCanvas.RenderTransform.Value.OffsetY + "\n";
            //myToolTip.Text += "Size of MainCanvas : Width=" + myCanvas.ActualWidth + ";Height=" + myCanvas.ActualWidth + "\n";
            //myToolTip.Text += "Size of Parent: Width=" + ((FrameworkElement)myCanvas.Parent).ActualWidth + ";Height=" + ((FrameworkElement)myCanvas.Parent).ActualHeight;
        }
        #endregion
        #region Show Matrix Real Time

        public void SaveData()
        {
            //Save Vision Program
            SaveJob();
            UiManager.SaveAppSettings();
            MessageBox.Show("Saving Success...");
            if (this.Image == null)
                return;
        }
        #endregion
        #region Tab Vision Tool Setting
        private Label lbVP = new Label();
        private Label lbMainSub = new Label();
        private ToolList toolList = new ToolList();
        public List<ToolAreaGroup> toolAreaGrs = new List<ToolAreaGroup>();
        private List<string> outResToolNames = new List<string>();
        public VisionProgram curVsProgram = new VisionProgram();
        private int oldIdxJob = 1;
        public Array DeviceCodes => Enum.GetValues(typeof(DeviceCode));
        public List<KeyValuePair<string, bool>> resultOK = new List<KeyValuePair<string, bool>>();
        public List<KeyValuePair<string, bool>> resultNG = new List<KeyValuePair<string, bool>>();
        private void CbxDisplayResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbxDisplayResult.SelectedItem == null) return;
            //Truy vấn đến Tool
            VisionTool outResTool = QueryOutBlobResTool(cbxDisplayResult.SelectedItem as String);
            if (outResTool == null) return;
            switch(outResTool.ToolType)
            {
                case VisionToolType.OUTBLOBRES:
                    var outBlobEdit = (outResTool as OutBlobResTool).toolEdit;
                    if (outBlobEdit.InputImage == null || outBlobEdit.InputImage.Mat == null || outBlobEdit.InputImage.Mat.Height <= 0 || outBlobEdit.InputImage.Mat.Width <= 0)
                    {
                        return;
                    }
                    imgView.Source = outBlobEdit.OriginImage.Mat.ToBitmapSource();
                    break;
                case VisionToolType.OUTACQUISRES:
                    var outAcqEdit = (outResTool as OutAcquisResTool).toolEdit;
                    if (outAcqEdit.InputImage == null || outAcqEdit.InputImage.Mat == null || outAcqEdit.InputImage.Mat.Height <= 0 || outAcqEdit.InputImage.Mat.Width <= 0)
                    {
                        return;
                    }
                    imgView.Source = outAcqEdit.InputImage.Mat.ToBitmapSource();
                    break;
                case VisionToolType.OUTSEGNEURORES:
                    var outSegEdit = (outResTool as OutSegNeuroResTool).toolEdit;
                    if (outSegEdit.InputImage == null || outSegEdit.InputImage.Mat == null || outSegEdit.InputImage.Mat.Height <= 0 || outSegEdit.InputImage.Mat.Width <= 0)
                    {
                        return;
                    }
                    imgView.Source = outSegEdit.InputImage.Mat.ToBitmapSource();
                    break;
            }    
            //imgView.Source = outResTool.toolEdit.OutputImage.Mat.ToBitmapSource();
            
        }
        public VisionTool QueryOutBlobResTool(string textQuery)
        {
            string[] itemPartName = textQuery.Split('.');
            int idxVP = int.Parse(itemPartName[0].Substring("VP".Length));
            VisionTool outResTool = null;
            if (itemPartName[1].Contains("Main"))
            {
                outResTool = toolAreaGrs[idxVP].ToolAreaMain.Children.OfType<VisionTool>().FirstOrDefault(ed => ed.Name == itemPartName[2]);
            }
            else if (itemPartName[1].Contains("Sub"))
            {
                int idxSub = int.Parse(itemPartName[1].Substring(itemPartName[1].IndexOf("Sub") + "Sub".Length));
                outResTool = toolAreaGrs[idxVP].ToolAreaSubs[idxSub].Children.OfType<VisionTool>().FirstOrDefault(ed => ed.Name == itemPartName[2]);
            }
            if (outResTool == null)
            {
                MessageBox.Show("Can not find any Edit Tool");
                return null;
            }
            return outResTool;
        }
        private void UpdateDisplayOutTool()
        {
            outResToolNames.Clear();
            foreach (var toolAreaGr in toolAreaGrs)
            {
                if(toolAreaGr.ToolAreaMain.IsOutTool)
                {
                    foreach (var ele in toolAreaGr.ToolAreaMain.Children)
                    {
                        if (ele is VisionTool tool)
                        {
                            string outResToolInputName = string.Empty;
                            switch (tool.ToolType)
                            {
                                case VisionToolType.OUTBLOBRES:
                                case VisionToolType.OUTACQUISRES:
                                case VisionToolType.OUTSEGNEURORES:
                                case VisionToolType.OUTVIDICOGRES:
                                    outResToolInputName = String.Format($"VP{toolAreaGr.index}.{toolAreaGr.ToolAreaMain.Name}.{tool.Name}.OutputImage");
                                    outResToolNames.Add(outResToolInputName);
                                    break;
                            }
                            if (!string.IsNullOrEmpty(outResToolInputName))
                                break;
                        }

                    }
                }
                if(toolAreaGr.ToolAreaSubs.Any(s => s.IsOutTool))
                {
                    foreach (var toolAreaSub in toolAreaGr.ToolAreaSubs)
                    {
                        foreach (var ele in toolAreaSub.Children)
                        {
                            if (ele is VisionTool tool)
                            {
                                string outResToolInputName = string.Empty;
                                switch (tool.ToolType)
                                {
                                    case VisionToolType.OUTBLOBRES:
                                    case VisionToolType.OUTACQUISRES:
                                    case VisionToolType.OUTSEGNEURORES:
                                    case VisionToolType.OUTVIDICOGRES:
                                        outResToolInputName = String.Format($"VP{toolAreaGr.index}.{toolAreaSub.Name}.{tool.Name}.OutputImage");
                                        outResToolNames.Add(outResToolInputName);
                                        break;
                                }
                                if (!string.IsNullOrEmpty(outResToolInputName))
                                    break;
                            }

                        }
                    }
                }  
            }
            cbxDisplayResult.ItemsSource = null;
            cbxDisplayResult.ItemsSource = outResToolNames;
            if (outResToolNames.Count < cbxDisplayResult.SelectedIndex)
            {
                cbxDisplayResult.SelectedIndex = 0;
            }
        }
        private void CbxJob_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbxJob.SelectedValue == null)
                return;
            MessageBoxResult result = MessageBox.Show($"Do you want to Save Data For Job{oldIdxJob}?", "Confirmation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes) { SaveData(); }
            this.DeleteAllVP();
            this.JobSet();
            this.LoadJob();
            oldIdxJob = cbxJob.SelectedIndex + 1;
        }
        
        private void ToolArea_OnChildrenChanged(object sender, RoutedEventArgs e)
        {
            UpdateDisplayOutTool();
        }
        private void ToolArea_OnToolDrop(object sender, DragEventArgs e)
        {
            ToolArea toolArea = (ToolArea)sender;
            if(toolArea.IsToolMain)
            {
                foreach (var toolAreaGr in toolAreaGrs)
                {
                    if (toolAreaGr.ToolAreaMain == toolArea)
                    {
                        if (toolAreaGr.ToolAreaMain.IsOutTool)
                        {
                            foreach (var toolAreaSub in toolAreaGr.ToolAreaSubs)
                            {
                                toolAreaSub.IsBlockOut = true;
                            }
                        }
                        else
                        {
                            foreach (var toolAreaSub in toolAreaGr.ToolAreaSubs)
                            {
                                if (toolAreaSub.IsOutTool)
                                {
                                    toolAreaGr.ToolAreaMain.IsBlockOut = true;
                                    break;
                                }    
                            }    
                        }    
                        break;
                    }
                }
            }    
            else
            {
                foreach (var toolAreaGr in toolAreaGrs)
                {
                    foreach (var toolAreaSub in toolAreaGr.ToolAreaSubs)
                    {
                        if(toolAreaSub == toolArea)
                        {
                            if (toolAreaSub.IsOutTool)
                            {
                                toolAreaGr.ToolAreaMain.IsBlockOut = true;
                            }
                            else
                            {
                                if (toolAreaGr.ToolAreaMain.IsOutTool)
                                {
                                    foreach (var tASub in toolAreaGr.ToolAreaSubs)
                                    {
                                        tASub.IsBlockOut = true;
                                    }
                                }
                            }
                            break;
                        }
                    }    
                }    
            }    
        }
        private void ToolArea_OnToolDeleted(object sender, RoutedEventArgs e)
        {
            ToolArea toolArea = (ToolArea)sender;
            if (toolArea.IsToolMain)
            {
                foreach (var toolAreaGr in toolAreaGrs)
                {
                    if (toolAreaGr.ToolAreaMain == toolArea)
                    {
                        if (!toolAreaGr.ToolAreaMain.IsOutTool)
                        {
                            foreach (var toolAreaSub in toolAreaGr.ToolAreaSubs)
                            {
                                toolAreaSub.IsBlockOut = false;
                            }
                        }
                        break;
                    }
                }
            }
            else
            {
                foreach (var toolAreaGr in toolAreaGrs)
                {
                    if (toolAreaGr.ToolAreaSubs.All(s => !s.IsOutTool))
                    {
                        toolAreaGr.ToolAreaMain.IsBlockOut = false;
                        break;
                    }
                }
            }
        }
        private void LbVisionProgram_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (tabVisionTool.FindResource("cmVP") is ContextMenu cm)
            {
                cm.PlacementTarget = sender as UIElement; // Đặt đúng kiểu dữ liệu
                cm.IsOpen = true; // Mở ContextMenu
            }
        }
        private void LbVP_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (tabVisionTool.FindResource("cmMainVP") is ContextMenu cm)
            {
                cm.PlacementTarget = sender as UIElement; // Đặt đúng kiểu dữ liệu
                lbVP = sender as Label;
                lbVP.ContextMenu = cm;
                cm.IsOpen = true; // Mở ContextMenu
            }
        }
        private void LbSub_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (tabVisionTool.FindResource("cmSub") is ContextMenu cm)
            {
                cm.PlacementTarget = sender as UIElement; // Đặt đúng kiểu dữ liệu
                lbMainSub = sender as Label;
                lbMainSub.ContextMenu = cm;
                cm.IsOpen = true; // Mở ContextMenu
            }
        }

        private void MnItOpenToolList_Click(object sender, RoutedEventArgs e)
        {
            Grid grdVsToolSetting = (stVisionProgram.Parent as ScrollViewer).Parent as Grid;
            List<ToolList> tlOld = new List<ToolList>();
            tlOld = grdVsToolSetting.Children.OfType<ToolList>().ToList();
            if (tlOld.Count > 0)
            {
                foreach (var old in tlOld)
                {
                    grdVsToolSetting.Children.Remove(old);
                    GC.Collect();
                }
            }

            toolList = new ToolList();
            Grid.SetColumn(toolList, 1);
            grdVsToolSetting.Children.Add(toolList);
        }
        private void MnItAddNewVP_Click(object sender, RoutedEventArgs e)
        {
            //Skip qua EditComm và Label Content
            stVisionProgram.Children.Add(CreateVisionProgram(stVisionProgram.Children.Count - 2));
        }
        private void MnItRenameVP_Click(object sender, RoutedEventArgs e)
        {
            TextBox txtEdit = new TextBox()
            {
                Text = lbVisionProgram.Content.ToString(),
                FontWeight = FontWeights.Bold,
                FontSize = 16.0,
                Foreground = Brushes.Black,
                Padding = new Thickness(5,3,5,3),
                Background = Brushes.White,
            };
            txtEdit.SelectAll(); // Bôi đen toàn bộ text
            txtEdit.Focusable = true;
            var b = txtEdit.Focus();
            var a = Keyboard.FocusedElement;

            if (lbVisionProgram.Parent is not DockPanel dp) return;
            dp.Children.Remove(lbVisionProgram);
            dp.Children.Insert(0, txtEdit);

            txtEdit.KeyDown += (s1, e1) =>
            {
                if (e1.Key == Key.Enter)
                {
                    lbVisionProgram.Content = txtEdit.Text;
                    dp.Children.Remove(txtEdit);
                    dp.Children.Insert(0, lbVisionProgram);
                    Keyboard.ClearFocus(); // Bỏ focus khỏi TextBox
                    GC.Collect();
                }
            };
        }
        private void MnItRename_Click(object sender, RoutedEventArgs e)
        {
            TextBox tbxEdit = new TextBox()
            {
                Text = lbVP.Content.ToString(),
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(3, 5, 0, 5),
                FontSize = 15.0,
                Foreground = Brushes.Black,
                Padding = new Thickness(5),
                Background = Brushes.White,
            };
            tbxEdit.SelectAll(); // Bôi đen toàn bộ text
            DockPanel dkPnVp = lbVP.Parent as DockPanel;
            Expander expdVP = dkPnVp.Parent as Expander;
            expdVP.Header = tbxEdit;
            tbxEdit.KeyDown += (sender1, e1) =>
            {
                if (e1.Key == Key.Enter)
                {
                    lbVP.Content = tbxEdit.Text;
                    expdVP.Header = dkPnVp;
                    Keyboard.ClearFocus(); // Bỏ focus khỏi TextBox
                    GC.Collect();
                }
            };

            tbxEdit.Focusable = true;
            var b = tbxEdit.Focus();
            var a = Keyboard.FocusedElement;
        }
        private void MnItAddNewSub_Click(object sender, RoutedEventArgs e)
        {
            if (!((lbVP.Parent as DockPanel).Parent is Expander expdVP)) return;
            if (!(expdVP.Content is StackPanel stVP)) return;
            //Skip VP Main & Input Trigger
            CreateSubProgram(0, stVP.Children.Count - 1, stVP);
        }
        private void MnItDeleteVP_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = (sender as MenuItem).Parent as ContextMenu;
            if (cm.PlacementTarget is Label lbVsProgram)
            {
                if (!((lbVsProgram.Parent as DockPanel).Parent is Expander expdVPn)) return;
                lbVsProgram.MouseRightButtonDown -= LbVP_MouseRightButtonDown;
                //Skip 2 Children is Edit Job and Label
                int idxVP = stVisionProgram.Children.IndexOf(expdVPn) - 2;
                stVisionProgram.Children.Remove(expdVPn);
                toolAreaGrs.RemoveAt(idxVP);
                UpdateDisplayOutTool();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        private void MnItRenameSubVP_Click(object sender, RoutedEventArgs e)
        {
            TextBox tbxEdit = new TextBox()
            {
                Text = lbMainSub.Content.ToString(),
                FontSize = 15.0,
                Foreground = Brushes.Black,
                Padding = new Thickness(0, 3, 0, 0),
                Background = Brushes.White,
            };
            tbxEdit.Focus();
            tbxEdit.SelectAll(); // Bôi đen toàn bộ text
            DockPanel dkPnSub = lbMainSub.Parent as DockPanel;
            Expander expdSub = dkPnSub.Parent as Expander;
            expdSub.Header = tbxEdit;
            tbxEdit.KeyDown += (sender1, e1) =>
            {
                if (e1.Key == Key.Enter)
                {
                    lbMainSub.Content = tbxEdit.Text;
                    expdSub.Header = dkPnSub;
                    Keyboard.ClearFocus(); // Bỏ focus khỏi TextBox
                    GC.Collect();
                }
            };
        }
        private void MnItDeleteSubVP_Click(object sender, RoutedEventArgs e)
        {
            if (!((lbMainSub.Parent as DockPanel).Parent is Expander expdSub)) return;
            if (!(expdSub.Parent is StackPanel stVP)) return;
            lbMainSub.MouseRightButtonDown -= LbSub_MouseRightButtonDown;
            //Remove Label Sub
            stVP.Children.Remove(expdSub);
            GC.Collect();
        }

        private void BtnEditCom_Click(object sender, RoutedEventArgs e)
        {
            WndCommEdit wndCommEdit = new WndCommEdit();
            wndCommEdit.Show();
        }

        private void CreateSubProgram(int index, int indexSub, StackPanel stVP, bool isLoading = false)
        {
            int iOfStVP = int.Parse(stVP.Name.Substring(4));
            ToolAreaGroup toolAreaGr = toolAreaGrs.Find(t => t.index == iOfStVP);
            //Image Run Button
            BitmapImage imgRunSource = new BitmapImage(new Uri("pack://application:,,,/src/ui/Images/RunToolButton.png"));
            /**************************************SUB PROGRAM****************************************/
            // Tạo TreeViewItem cho Sub Program
            Expander expdSub = new Expander();
            // Tạo ScrollViewer cho Sub Program
            ScrollViewer scrVwSub = new ScrollViewer
            {
                Height = 500,
                Margin = new Thickness(0, 5, 0, 0),
                CanContentScroll = true,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            ToolArea toolAreaSub = new ToolArea
            {
                Name = $"ToolAreaSub{indexSub}",
                Height = 4000,
                Background = Brushes.White,
                AllowDrop = true,
                IsToolMain = false,
            };


            toolAreaSub.OnChildrenChanged += ToolArea_OnChildrenChanged;
            toolAreaSub.OnToolDrop += ToolArea_OnToolDrop;
            toolAreaSub.OnToolDeleted += ToolArea_OnToolDeleted;
            scrVwSub.Content = toolAreaSub;
            expdSub.Content =  scrVwSub;
            toolAreaGr.ToolAreaSubs.Add(toolAreaSub);

            DockPanel dkPnSub = new DockPanel() { Height = 30};
            Label lbSub = new Label
            {
                Content = $"Sub Program {stVP.Children.Count}",
                Padding = new Thickness(5, 3, 0, 0),
            };
            if (isLoading)
            {
                lbSub.Content = this.curVsProgram.vsProgramNs[index].vsSubs[indexSub].ContentSub;
            }
            else
            {
                lbSub.Content = $"Sub Program {indexSub}";
            }
            lbSub.MouseRightButtonDown += LbSub_MouseRightButtonDown;
            Button btnRunSub = new Button
            {
                Name = $"btnRunSub{stVP.Children.Count}",
                Content = new Image() { Source = imgRunSource },
            };
            btnRunSub.Click += (senderBtnSub, eBtnSub) =>
            {
                RunTreeToolSub(toolAreaSub);
            };
            dkPnSub.Children.Add(btnRunSub);
            dkPnSub.Children.Add(lbSub);
            expdSub.Header = dkPnSub;

            // Thêm các mục vào Vision Program
            stVP.Children.Add(expdSub);
            //Thay thế toolAreaGr sau khi chỉnh sửa vào List toolAreaGrs
            toolAreaGrs[iOfStVP] = toolAreaGr;

            //Load ToolArea Sub
            var outImageSubTool = new OutputImageSubTool();
            if (isLoading)
            {
                //Setup Tool OutputImageSub
                outImageSubTool.Name = this.curVsProgram.vsProgramNs[index].vsSubs[indexSub].nameTools[0];
                toolAreaSub.InitLabelTool(outImageSubTool);
                toolAreaSub.LoadTool(outImageSubTool, 0);
                LoadToolAreaSub(index, indexSub, toolAreaSub);
            }
            else
            {
                //Setup Tool OutputImageSub
                toolAreaSub.CreateNewTool(outImageSubTool);
                var timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();     // Dừng timer sau khi gọi action
                    toolAreaSub.heightToolLst.Clear();
                    toolAreaSub.heightToolLst = new List<double> { 0d, 70d };
                    toolAreaSub.heightAllTool = 70d;
                };
                timer.Start();
            }
        }
        private Expander CreateVisionProgram(int idxVPn, bool isLoading = false)
        {
            //Image Run Button
            BitmapImage imgRunSource = new BitmapImage(new Uri("pack://application:,,,/src/ui/Images/RunToolButton.png"));
            BitmapImage imgRunAllSource = new BitmapImage(new Uri("pack://application:,,,/src/ui/Images/RunRed.png"));
            //Tạo ToolAreaGroup cho buttonRunVP
            ToolAreaGroup toolAreaGr = new ToolAreaGroup() { index = idxVPn };

            /**************************************VISION PROGRAM****************************************/
            // Tạo TreeViewItem cho Vision Program 1
            Expander expdVP = new Expander { Name = $"expdVP{idxVPn}", Background = (Brush)new BrushConverter().ConvertFromString("#404040") };
            DockPanel dkPnVP = new DockPanel() { Height = 30 };
            Label lbVP = new Label
            {
                Padding = new Thickness(5, 3, 0, 0),
            };
            if (isLoading)
            {
                lbVP.Content = this.curVsProgram.vsProgramNs[idxVPn].ContentVP;
            }
            else
            {
                lbVP.Content = "Vision Program " + (idxVPn + 1).ToString();
            }
            lbVP.MouseRightButtonDown += LbVP_MouseRightButtonDown;
            Button btnRunVP = new Button
            {
                Name = $"btnRunVP{idxVPn}",
                Content = new Image() { Source = imgRunAllSource },
            };
            btnRunVP.Click += (senderBtnVP, eBtnVP) =>
            {
                RunTreeToolVP(toolAreaGrs[idxVPn]);
            };
            dkPnVP.Children.Add(btnRunVP);
            dkPnVP.Children.Add(lbVP);

            expdVP.Header = dkPnVP;

            //StackPanel chứa Input Trigger, Main, Sub
            StackPanel stVP = new StackPanel{ Name = $"stVP{idxVPn}", Margin = new Thickness(15,0,0,0)};
            expdVP.Content = stVP;

            /**************************************INPUT TRIGGER***************************************/
            // Tạo TreeViewItem cho Trigger
            Expander expdInTrigg = new Expander();
            //Header
            Label lbHeader = new Label
            {
                Content = "Input Trigger",
                Padding = new Thickness(0, 3, 0, 0),
                Height = 30
            };
            expdInTrigg.Header = lbHeader;
            StackPanel stInTrigg = new StackPanel
            { 
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = (Brush)new BrushConverter().ConvertFromString("#CCCCCC"),
                Margin = new Thickness(25,0,15,0),
                Width = 287,
            };
            Label lbTrigg = new Label 
            { 
                Content = "Trigger : ", 
                Padding = new Thickness(5,3,0,0), 
                Foreground = Brushes.Black, 
                Width = 70 
            };
            ComboBox cbxDevIn = new ComboBox
            {
                Width = 60,
                Margin = new Thickness(0,5,0,5),
                ItemsSource = DeviceCodes,
                SelectedValuePath = "Content",
                SelectedValue = "M",
                SelectedItem = isLoading ? this.curVsProgram.vsProgramNs[idxVPn].selectDevIn : DeviceCode.M,
            };
            TextBox txtAddrIn = new TextBox
            {
                Margin = new Thickness(0, 5, 0, 5),
                Width = 120,
                Text = isLoading ? this.curVsProgram.vsProgramNs[idxVPn].addrIn : "",
            };
            stInTrigg.Children.Add(lbTrigg);
            stInTrigg.Children.Add(cbxDevIn);
            stInTrigg.Children.Add(txtAddrIn);
            expdInTrigg.Content = stInTrigg;
            // Thêm các mục vào Vision Program
            stVP.Children.Insert(0, expdInTrigg);

            /**************************************MAIN PROGRAM****************************************/
            // Tạo TreeViewItem cho Main Program
            Expander expdMain = new Expander();
            // Tạo ScrollViewer cho Main Program
            ScrollViewer ScrVwMain = new ScrollViewer
            {
                Height = 400,
                Margin = new Thickness(0, 5, 0, 0),
                CanContentScroll = true,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            ToolArea toolAreaMain = new ToolArea
            {
                Height = 4000,
                Name = "ToolAreaMain",
                Background = Brushes.White,
                AllowDrop = true,
                IsToolMain = true
            };
            toolAreaGr.ToolAreaMain = toolAreaMain;
            toolAreaMain.OnChildrenChanged += ToolArea_OnChildrenChanged;
            toolAreaMain.OnToolDrop += ToolArea_OnToolDrop;
            toolAreaMain.OnToolDeleted += ToolArea_OnToolDeleted;
            ScrVwMain.Content = toolAreaMain;
            expdMain.Content = ScrVwMain;

            DockPanel dkPnMain = new DockPanel() { Height = 30};
            Label lbMain = new Label
            {
                Content = "Main Program",
                Padding = new Thickness(5, 3, 0, 0),
            };
            Button btnRunMain = new Button
            {
                Name = $"btnRunMain",
                Content = new Image() { Source = imgRunSource },
            };
            btnRunMain.Click += (senderBtnMain, eBtnMain) =>
            {
                RunTreeToolMain(toolAreaMain);
            };
            dkPnMain.Children.Add(btnRunMain);
            dkPnMain.Children.Add(lbMain);
            expdMain.Header = dkPnMain;
            // Thêm các mục vào Vision Program
            stVP.Children.Add(expdMain);

            /**************************************SUB PROGRAM****************************************/
            ToolArea toolAreaSub = new ToolArea();
            if (!isLoading)
            {
                // Tạo TreeViewItem cho Sub Program
                Expander expdSub = new Expander ();
                // Tạo ScrollViewer cho Sub Program
                ScrollViewer scrVwSub = new ScrollViewer
                {
                    Height = 500,
                    Margin = new Thickness(0, 5, 0, 0),
                    CanContentScroll = true,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                toolAreaSub = new ToolArea
                {
                    Name = "ToolAreaSub0",
                    Height = 4000,
                    Background = Brushes.White,
                    AllowDrop = true,
                    IsToolMain = false
                };
                //toolAreaSub.CreateNewToolTool(new OutputImageSubTool());
                toolAreaSub.OnChildrenChanged += ToolArea_OnChildrenChanged;
                toolAreaSub.OnToolDrop += ToolArea_OnToolDrop;
                toolAreaSub.OnToolDeleted += ToolArea_OnToolDeleted;

                scrVwSub.Content = toolAreaSub;
                expdSub.Content = scrVwSub;
                toolAreaGr.ToolAreaSubs.Add(toolAreaSub);

                DockPanel dkPnSub = new DockPanel() { Height = 30};
                //DockPanel dkPnSub = new DockPanel() { Height = 30, Width = 517 };
                //DockPanel dkPnSub = new DockPanel() { Height = 30, Width = 371 };
                Label lbSub = new Label
                {
                    Content = "Sub Program 1",
                    Padding = new Thickness(5, 3, 0, 0),
                };
                lbSub.MouseRightButtonDown += LbSub_MouseRightButtonDown;
                Button btnRunSub = new Button
                {
                    Name = "btnRunSub1",
                    Content = new Image() { Source = imgRunSource },
                };
                btnRunSub.Click += (senderBtnSub, eBtnSub) =>
                {
                    RunTreeToolSub(toolAreaSub);
                };
                dkPnSub.Children.Add(btnRunSub);
                dkPnSub.Children.Add(lbSub);
                expdSub.Header = dkPnSub;

                // Thêm các mục vào Vision Program
                stVP.Children.Add(expdSub);
            }
            // Thêm toolAreaGroup vào List toolAreaGroup
            toolAreaGrs.Add(toolAreaGr);

            //Load appSetting to ToolAreas
            if (isLoading)
            {
                LoadToolAreaMain(idxVPn, toolAreaMain);
            }
            else
            {
                CreateDefaultTools(toolAreaMain, toolAreaSub);
            }    
            return expdVP;
        }
        private void CreateDefaultTools(ToolArea toolAreaMain, ToolArea toolAreaSub)
        {
            //ToolArea Main
            toolAreaMain.heightAllTool = 310.0d;
            toolAreaMain.heightToolLst = new List<double> { 0.0d, 50.0d, 180.0d, 310.0d };
            AcquisitionTool acquisitionTool = new AcquisitionTool();
            TempMatchZeroTool tempMatchZeroTool = new TempMatchZeroTool();
            FixtureTool fixtureTool = new FixtureTool();
            EditRegionTool editRegionTool = new EditRegionTool();

            acquisitionTool.Name += "0";
            toolAreaMain.InitLabelTool(acquisitionTool);
            toolAreaMain.LoadTool(acquisitionTool, toolAreaMain.heightToolLst[0]);
            tempMatchZeroTool.Name += "0";
            toolAreaMain.InitLabelTool(tempMatchZeroTool);
            toolAreaMain.LoadTool(tempMatchZeroTool, toolAreaMain.heightToolLst[1]);
            fixtureTool.Name += "0";
            toolAreaMain.InitLabelTool(fixtureTool);
            toolAreaMain.LoadTool(fixtureTool, toolAreaMain.heightToolLst[2]);
            editRegionTool.Name += "0";
            toolAreaMain.InitLabelTool(editRegionTool);
            toolAreaMain.LoadTool(editRegionTool, toolAreaMain.heightToolLst[3]);

            toolAreaMain.CreateArrowConnect("AcquisitionTool0.lbOutputImage-TempMatchZeroTool0.lbInputImage", new Point(115.5, 40), new Point(103.1, 90));
            toolAreaMain.CreateArrowConnect("AcquisitionTool0.lbOutputImage-FixtureTool0.lbInputImage", new Point(115.5, 40), new Point(103.1, 220));
            toolAreaMain.CreateArrowConnect("TempMatchZeroTool0.lbTranslateX-FixtureTool0.lbTranslateX", new Point(94.29667, 130), new Point(94.29667, 240));
            toolAreaMain.CreateArrowConnect("TempMatchZeroTool0.lbTranslateY-FixtureTool0.lbTranslateY", new Point(93.74, 150), new Point(93.74, 260));
            toolAreaMain.CreateArrowConnect("TempMatchZeroTool0.lbRotation-FixtureTool0.lbRotation", new Point(83.03667, 170), new Point(83.03667, 280));
            toolAreaMain.CreateArrowConnect("FixtureTool0.lbOutputImage-EditRegionTool0.lbInputImage", new Point(115.5, 300), new Point(103.1, 350));

            //ToolArea Sub
            toolAreaSub.heightAllTool = 290.0d;
            toolAreaSub.heightToolLst = new List<double> { 0.0d, 70.0d, 180.0d, 290.0d };
            OutputImageSubTool outputImageSubTool = new OutputImageSubTool();
            BlobTool blobTool1 = new BlobTool();
            BlobTool blobTool2 = new BlobTool();
            OutBlobResTool outResultTool = new OutBlobResTool();

            outputImageSubTool.Name += "0";
            toolAreaSub.InitLabelTool(outputImageSubTool);
            toolAreaSub.LoadTool(outputImageSubTool, toolAreaSub.heightToolLst[0]);
            blobTool1.Name += "0";
            toolAreaSub.InitLabelTool(blobTool1);
            toolAreaSub.LoadTool(blobTool1, toolAreaSub.heightToolLst[1]);
            blobTool2.Name += "1";
            toolAreaSub.InitLabelTool(blobTool2);
            toolAreaSub.LoadTool(blobTool2, toolAreaSub.heightToolLst[2]);
            outResultTool.Name += "0";
            toolAreaSub.InitLabelTool(outResultTool);
            toolAreaSub.LoadTool(outResultTool, toolAreaSub.heightToolLst[3]);

            toolAreaSub.CreateArrowConnect("OutputImageSubTool0.lbOutputImage-BlobTool0.lbInputImage", new Point(115.5, 60), new Point(103.1, 110));
            toolAreaSub.CreateArrowConnect("OutputImageSubTool0.lbOutputImage-BlobTool1.lbInputImage", new Point(115.5, 60), new Point(103.1, 220));
            toolAreaSub.CreateArrowConnect("OutputImageSubTool0.lbOriginImage-OutBlobResTool0.lbOriginImage", new Point(109.33667, 40), new Point(109.33667, 330));
            toolAreaSub.CreateArrowConnect("OutputImageSubTool0.lbOutputImage-OutBlobResTool0.lbInputImage", new Point(115.5, 60), new Point(103.1, 350));
            toolAreaSub.CreateArrowConnect("BlobTool0.lbBlobs-OutBlobResTool0.lbBlobs1", new Point(63.20667, 170), new Point(71.2933, 370));
            toolAreaSub.CreateArrowConnect("BlobTool1.lbBlobs-OutBlobResTool0.lbBlobs2", new Point(63.20667, 280), new Point(71.2933, 390));
        }

        private void RunTreeToolMain(ToolArea toolArea)
        {
            List<VisionTool> tools = toolArea.Children.OfType<VisionTool>().ToList();
            if (tools == null || tools.Count == 0)
                return;
            //Reset bộ đếm và stt tool NG
            toolArea.resRunTools = 0;
            toolArea.sttRunTools = "";
            Dictionary<int, string[]> connectTags = toolArea.CreateConnectTags(toolArea.arrowCntLst);
            foreach (VisionTool tool in tools)
            {
                if (tool.ToolType == VisionToolType.ACQUISITION)
                {
                    AcquisitionTool Tool = (tool as AcquisitionTool);
                    bool resRun = Tool.RunToolInOut(toolArea.arrowCntLst, connectTags);
                    if (!resRun) { toolArea.resRunTools += 1; toolArea.sttRunTools += "AcquisitionTool Error | "; }
                }
                else
                    ToolRun(tool, toolArea, connectTags);   
            }

        }
        private void RunTreeToolSub(ToolArea toolArea)
        {
            List<VisionTool> tools = toolArea.Children.OfType<VisionTool>().ToList();
            if (tools == null || tools.Count == 0)
                return;

            Dictionary<int, string[]> connectTags = toolArea.CreateConnectTags(toolArea.arrowCntLst);
            for(int j = 0; j < toolArea.ImgLstSub.Count; j++)
            {
                for (int i = 0; i < tools.Count; i++)
                {
                    if (tools[i].ToolType == VisionToolType.OUTIMAGESUB)
                    {
                        OutputImageSubTool Tool = (tools[i] as OutputImageSubTool);
                        if (i == 0) 
                        { 
                            Tool.InputImage = toolArea.ImgLstSub[j].Item2 as SvImage; 
                            Tool.OriginImage = toolArea.OriginImage.Clone(true);
                        }
                        bool resRun = Tool.RunToolInOut(toolArea.arrowCntLst, connectTags);
                        if (!resRun) { toolArea.resRunTools += 1; toolArea.sttRunTools += "OutputImageSubTool Error | "; }
                    }
                    else if (tools[i].ToolType == VisionToolType.OUTBLOBRES)
                    {
                        OutBlobResTool Tool = (tools[i] as OutBlobResTool);
                        Tool.RunToolInOut(toolArea.arrowCntLst, connectTags);
                        CbxDisplayResult_SelectionChanged(null, null);
                        var outResEdit = Tool.toolEdit;
                        if(outResEdit.indexImgs.Count != toolArea.ImgLstSub.Count)
                        {
                            MessageBox.Show("Number of Image ROI and Number of Address OK/NG is not equal!");
                            return;
                        }    
                        if (outResEdit.indexImgs.Count > 0 && (outResEdit.addrOKLst.Count > 0 || outResEdit.addrNGLst.Count > 0))
                        {
                            resultOK.Add(new KeyValuePair<string, bool>(outResEdit.addrOKLst[j], outResEdit.resultOut));
                            resultNG.Add(new KeyValuePair<string, bool>(outResEdit.addrNGLst[j], !outResEdit.resultOut));
                        }
                    }
                    else
                        ToolRun(tools[i], toolArea, connectTags);
                }
            }

        }
        public void RunTreeToolVP(ToolAreaGroup toolAreaGr)
        {
            resultOK.Clear();
            resultNG.Clear();
            RunTreeToolMain(toolAreaGr.ToolAreaMain);
            foreach (var toolAreaSub in toolAreaGr.ToolAreaSubs)
            {
                RunTreeToolSub(toolAreaSub);
            }
        }
        private void ToolRun(VisionTool tool, ToolArea toolArea, Dictionary<int, string[]> connectTags)
        {
            bool runResult = tool.RunToolInOut(toolArea.arrowCntLst, connectTags);
            if(!runResult)
            {
                toolArea.resRunTools += 1;
                switch (tool.ToolType)
                {
                    case VisionToolType.TEMPLATEMATCH:
                         toolArea.sttRunTools += "TemplateMatchTool Error | ";
                        break;
                    case VisionToolType.TEMPMATCHZERO:
                        toolArea.sttRunTools += "TempMatchZeroTool Error | ";
                        break;
                    case VisionToolType.EDITREGION:
                        toolArea.sttRunTools += "EditRegionTool Error | ";
                        break;
                    case VisionToolType.FIXTURE:
                        toolArea.sttRunTools += "FixtureTool Error | ";
                        break;
                    case VisionToolType.IMAGEPROCESS:
                        toolArea.sttRunTools += "ImageProcessTool Error | ";
                        break;
                    case VisionToolType.CONTRASTnBRIGHTNESS:
                        toolArea.sttRunTools += "ContrastNBrightnessTool Error | ";
                        break;
                    case VisionToolType.BLOB:
                        toolArea.sttRunTools += "BlobTool Error | ";
                        break;
                    case VisionToolType.SAVEIMAGE:
                        toolArea.sttRunTools += "SaveImageTool Error | ";
                        break;
                    case VisionToolType.VISIONPRO:
                        toolArea.sttRunTools += "VisionProTool Error | ";
                        break;
                    case VisionToolType.SEGMENTNEURO:
                        toolArea.sttRunTools += "SegmentNeuroTool Error | ";
                        break;
                    case VisionToolType.VIDICOGNEX:
                        toolArea.sttRunTools += "VidiCognexTool Error | ";
                        break;
                    case VisionToolType.OUTACQUISRES:
                    case VisionToolType.OUTSEGNEURORES:
                        CbxDisplayResult_SelectionChanged(null, null);
                        break;
                }
            }    
            
        }

        private void DeleteAllVP()
        {
            stVisionProgram.Children.RemoveRange(2, stVisionProgram.Children.Count - 2);
        }
        public void JobSet()
        {
            try
            {
                if (UiManager.appSettings.vsPrograms.Length <= cbxJob.SelectedIndex)
                    return;
                this.curVsProgram = UiManager.appSettings.vsPrograms[cbxJob.SelectedIndex];
            }
            catch (Exception ex)
            {
                logger.Create("JobSet Error: " + ex.Message);
            }
        }
        public void LoadJob()
        {
            //VidiCognexTool là TH đặc biệt, cần Dispose Control trước khi load Job mới 
            foreach (var toolAreaGr in toolAreaGrs)
            {
                List<VidiCognexTool> vidiToolMains = toolAreaGr.ToolAreaMain.Children.OfType<VidiCognexTool>().ToList();
                if(vidiToolMains.Count > 0)
                {
                    foreach(var tool in vidiToolMains)
                    {
                        tool.toolEdit.Control?.Dispose();
                    }    
                }  
                for (int i = 0; i < toolAreaGr.ToolAreaSubs.Count; i++)
                {
                    List<VidiCognexTool> vidiToolSubs = toolAreaGr.ToolAreaSubs[i].Children.OfType<VidiCognexTool>().ToList();
                    if (vidiToolSubs.Count > 0)
                    {
                        foreach (var tool in vidiToolSubs)
                        {
                            tool.toolEdit.Control?.Dispose();
                        }
                    }
                }    
            }   
            
            toolAreaGrs.ForEach(t => t.ToolAreaSubs.Clear());
            toolAreaGrs.Clear();
            lbVisionProgram.Content = this.curVsProgram.NameDisp;
            stVisionProgram.Children.RemoveRange(2, stVisionProgram.Children.Count - 2);
            for (int i = 0; i < this.curVsProgram.vsProgramNs.Count; i++)
            {
                stVisionProgram.Children.Add(CreateVisionProgram(i, true));
                if (!(stVisionProgram.Children[i + 2] is Expander expdVP)) return;
                if (!(expdVP.Content is StackPanel stVP)) return;
                for (int j = 0; j < this.curVsProgram.vsProgramNs[i].vsSubs.Count; j++)
                {
                    CreateSubProgram(i, j, stVP, true);
                }
            }
            GC.Collect();
        }
        public void SaveJob()
        {
            //Save VisionProgram
            if (this.curVsProgram.vsProgramNs.Count != stVisionProgram.Children.Count - 2)
            {
                if (this.curVsProgram.vsProgramNs.Count > (stVisionProgram.Children.Count - 2))
                {
                    while (this.curVsProgram.vsProgramNs.Count > stVisionProgram.Children.Count - 2)
                        this.curVsProgram.vsProgramNs.RemoveAt(this.curVsProgram.vsProgramNs.Count - 1);
                }
                else
                {
                    while (this.curVsProgram.vsProgramNs.Count < stVisionProgram.Children.Count - 2)
                        this.curVsProgram.vsProgramNs.Add(new VisionProgramN());
                }
            }
            this.curVsProgram.NameDisp = lbVisionProgram.Content.ToString();
            for (int i = 2; i < stVisionProgram.Children.Count; i++)
            {
                int idxVPn = i - 2;
                if (!(stVisionProgram.Children[i] is Expander expdVP)) return;
                if (!(expdVP.Content is StackPanel stVP)) return;

                DockPanel dkPnVP = expdVP.Header as DockPanel;
                Label lbVP = dkPnVP.Children.OfType<Label>().FirstOrDefault();

                //Clear và tạo mới 1 vsProgramNs cũ tại index cụ thể
                this.curVsProgram.vsProgramNs.RemoveAt(idxVPn);
                this.curVsProgram.vsProgramNs.Insert(idxVPn, new VisionProgramN());
                this.curVsProgram.vsProgramNs[idxVPn].ContentVP = lbVP.Content.ToString();

                //Save Input Trigger Parameter
                Expander expdInTrigg = stVP.Children.OfType<Expander>().FirstOrDefault();
                StackPanel stTrigg = expdInTrigg.Content as StackPanel;
                ComboBox cbxDevIn = stTrigg.Children[1] as ComboBox;
                this.curVsProgram.vsProgramNs[idxVPn].selectDevIn = (DeviceCode)cbxDevIn.SelectedItem;
                TextBox txtAddrIn = stTrigg.Children[2] as TextBox;
                this.curVsProgram.vsProgramNs[idxVPn].addrIn = txtAddrIn.Text;

                //Skip TreeViewItem Input Trigger
                Expander expdMain = stVP.Children.OfType<Expander>().Skip(1).FirstOrDefault();
                ScrollViewer ScrVwMain = expdMain.Content as ScrollViewer;
                ToolArea toolAreaMain = ScrVwMain.Content as ToolArea;
                SaveToolAreaMain(idxVPn, toolAreaMain);
                //Bỏ qua VSMain và Input Trigger
                for (int j = 2; j < stVP.Children.Count; j++)
                {
                    Expander expdSub = stVP.Children[j] as Expander;
                    ScrollViewer ScrVwSub = expdSub.Content as ScrollViewer;
                    ToolArea toolAreaSub = ScrVwSub.Content as ToolArea;
                    SaveToolAreaSub(idxVPn, j - 2, toolAreaSub);

                    DockPanel dkPnSub = expdSub.Header as DockPanel;
                    Label lbSub = dkPnSub.Children.OfType<Label>().FirstOrDefault();
                    this.curVsProgram.vsProgramNs[idxVPn].vsSubs[j - 2].ContentSub = lbSub.Content.ToString();
                }
            }
        }

        public void LoadToolAreaMain(int index, ToolArea toolArea)
        {
            if (this.curVsProgram.vsProgramNs.Count <= 0)
                return;
            toolArea.heightAllTool = this.curVsProgram.vsProgramNs[index].vsMain.heightAllTool;
            toolArea.heightToolLst = this.curVsProgram.vsProgramNs[index].vsMain.heightToolLst;
            VisionMainSub vsMain = this.curVsProgram.vsProgramNs[index].vsMain;
            for (int i = 0; i < vsMain.nameTools.Count; i++)
            {
                LoadToolCommon(index, i, toolArea, vsMain);
            }
            //Arrow Connect
            foreach (var arrow in vsMain.arrowConnect)
            {
                toolArea.CreateArrowConnect(arrow.name, arrow.startPoint, arrow.endPoint);
            }
        }
        public void SaveToolAreaMain(int idxVP, ToolArea toolArea)
        {
            List<VisionTool> tools = toolArea.Children.OfType<VisionTool>().ToList();

            //ToolArea Main
            VisionMainSub vsMain = this.curVsProgram.vsProgramNs[idxVP].vsMain;
            //Tool
            SaveToolCommon(idxVP, tools, toolArea, vsMain);
        }
        public void LoadToolAreaSub(int idxVP, int indexSub, ToolArea toolAreaSub)
        {
            if (this.curVsProgram.vsProgramNs.Count <= idxVP)
                return;
            if (this.curVsProgram.vsProgramNs[idxVP].vsSubs.Count <= indexSub)
                return;
            VisionMainSub vsSub = this.curVsProgram.vsProgramNs[idxVP].vsSubs[indexSub];
            {
                toolAreaSub.heightAllTool = vsSub.heightAllTool;
                toolAreaSub.heightToolLst = vsSub.heightToolLst;
                //Skip OutImageSub Tool
                for (int i = 1; i < vsSub.nameTools.Count; i++)
                {
                    LoadToolCommon(idxVP, i, toolAreaSub, vsSub);
                }
                foreach (var arrow in vsSub.arrowConnect)
                {
                    toolAreaSub.CreateArrowConnect(arrow.name, arrow.startPoint, arrow.endPoint);
                }
            }

        }
        public void SaveToolAreaSub(int idxVP, int indexSub, ToolArea toolAreaSub)
        {
            List<VisionTool> tools = toolAreaSub.Children.OfType<VisionTool>().ToList();
            if (this.curVsProgram.vsProgramNs.Count <= 0)
                this.curVsProgram.vsProgramNs.Add(new VisionProgramN());
            while (this.curVsProgram.vsProgramNs[idxVP].vsSubs.Count <= indexSub)
                this.curVsProgram.vsProgramNs[idxVP].vsSubs.Add(new VisionMainSub());
            if (this.curVsProgram.vsProgramNs[idxVP].vsSubs[indexSub] == null)
                return;
            VisionMainSub vsSub = this.curVsProgram.vsProgramNs[idxVP].vsSubs[indexSub];

            //ToolArea
            SaveToolCommon(idxVP, tools, toolAreaSub, vsSub);
        }
        private void LoadToolCommon(int idxVP, int indexTools, ToolArea toolArea, VisionMainSub vsMnSb)
        {
            //2 Para quyết định ToolArea có bị chặn các tool Output không
            toolArea.IsOutTool = vsMnSb.isOutTool;
            toolArea.IsBlockOut = vsMnSb.isBlockOut;

            string nameTool = vsMnSb.nameTools[indexTools];
            MatchCollection matches = Regex.Matches(nameTool, @"\d+");
            string strNumber = "";
            if (matches.Count <= 0) return;
            foreach (Match match in matches)
            {
                strNumber += match.Value;
            }
            int idxToolType = 0;
            idxToolType = int.Parse(strNumber);
            if (nameTool.Contains("AcquisitionTool"))
            {
                if (vsMnSb.aquisitionSettings.Count == 0 || vsMnSb.aquisitionSettings[idxToolType] == null)
                    return;
                AcquisitionTool acqTool = new AcquisitionTool() { Name = nameTool };
                toolArea.InitLabelTool(acqTool);
                toolArea.LoadTool(acqTool, toolArea.heightToolLst[indexTools]);

                AcquisitionSetting toolSetting = vsMnSb.aquisitionSettings[idxToolType];
                AcquisitionEdit toolEdit = acqTool.toolEdit;

                toolEdit.isImageCam = toolSetting.isCmaera;
                toolEdit.isImageFolder = toolSetting.isFolder;
                toolEdit.ckbxVFlip.IsChecked = toolSetting.isVerticalFlip;
                toolEdit.ckbxHFlip.IsChecked = toolSetting.isHorizontalFlip;
                toolEdit.ckbxToGray.IsChecked = toolSetting.isGrayMode;
                toolEdit.SelectedRotateMode = toolSetting.rotateMode;
                toolEdit.SelectedGrayMode = toolSetting.grayMode;

                string gainStr = "";
                for(int i = 0; i < UiManager.CamList.Count; i++)
                {
                    if(toolSetting.eCamDevType == UiManager.CamList[i].eCamDevType && toolSetting.serialNumber == UiManager.CamList[i].SerialNumber)
                    {
                        switch (toolSetting.eCamDevType)
                        {
                            case ECamDevType.Hikrobot:
                                MVSCamera.MV_CC_DEVICE_INFO hikDev = Marshal.PtrToStructure<MVSCamera.MV_CC_DEVICE_INFO>(UiManager.CamDevList[i]);
                                toolEdit.Camera = new HikCam(hikDev);
                                gainStr = "Gain";
                                break;
                            case ECamDevType.Irayple:
                                IMVDefine.IMV_DeviceInfo irpDev = Marshal.PtrToStructure<IMV_DeviceInfo>(UiManager.CamDevList[i]);
                                toolEdit.Camera = new IraypleCam(irpDev);
                                gainStr = "GainRaw";
                                break;
                        }
                        toolEdit.deviceInfo = Marshal.PtrToStructure<IMV_DeviceInfo>(UiManager.DevList.devInfo[i]);
                        //Add and Select camera
                        toolEdit.ShowCamDevice(toolEdit.deviceInfo);
                        break;
                    }       
                }    
                if(toolEdit.Camera == null)
                {
                    toolEdit.Camera = new HikCam();
                    gainStr = "Gain";
                }    
                   
                toolEdit.Camera.GetDoubleMaxValue("ExposureTime", out double dTemp);
                toolEdit.ExposureValue = Math.Min(dTemp, toolSetting.ExposureTime);
                toolEdit.Camera.GetDoubleMaxValue(gainStr, out dTemp);
                toolEdit.GainValue = Math.Min(dTemp, toolSetting.Gain);

                toolEdit.Camera.GetIntMaxValue("Width", out long nTemp);
                toolEdit.WidthCam = Math.Min(nTemp, toolSetting.WidthCam);
                toolEdit.Camera.GetIntMaxValue("Height", out nTemp);
                toolEdit.HeightCam = Math.Min(nTemp, toolSetting.HeightCam);
            }
            else if (nameTool.Contains("SaveImageTool"))
            {
                if (vsMnSb.saveImageSettings.Count == 0 || vsMnSb.saveImageSettings[idxToolType] == null)
                    return;
                SaveImageTool saveImgTool = new SaveImageTool() { Name = nameTool };
                toolArea.InitLabelTool(saveImgTool);
                toolArea.LoadTool(saveImgTool, toolArea.heightToolLst[indexTools]);

                SaveImageSetting toolSetting = vsMnSb.saveImageSettings[idxToolType];
                SaveImageEdit toolEdit = saveImgTool.toolEdit;

                toolEdit.txtFileName.Text = toolSetting.fileName;
                toolEdit.txtFolderPath.Text = toolSetting.folderPath;
                toolEdit.ImageFormatSelected = toolSetting.imageFormat;
                toolEdit.IsAddDateTime = toolSetting.isAddDateTime;
                toolEdit.IsAddCounter = toolSetting.isAddCounter;
                toolEdit.NumUDCounter = toolSetting.counter;
                toolEdit.NumUDImageStorage = toolSetting.imageStorage;
            }
            else if (nameTool.Contains("TemplateMatchTool"))
            {
                if (vsMnSb.templateMatchSettings.Count == 0 || vsMnSb.templateMatchSettings[idxToolType] == null)
                    return;
                TemplateMatchTool tempTool = new TemplateMatchTool() { Name = nameTool };
                toolArea.InitLabelTool(tempTool);
                toolArea.LoadTool(tempTool, toolArea.heightToolLst[indexTools]);

                TemplateMatchSetting toolSetting = vsMnSb.templateMatchSettings[idxToolType];
                TemplateMatchEdit toolEdit = tempTool.toolEdit;

                toolEdit.IsUseEdge = toolSetting.isUseEdge;
                toolEdit.IsAutoMatchPara = toolSetting.isAutoMatchPara;
                toolEdit.ScaleFirst = toolSetting.scaleFirst;
                toolEdit.ScaleLast = toolSetting.scaleLast;
                toolEdit.DegMin = toolSetting.degMin;
                toolEdit.DegMax = toolSetting.degMax;
                toolEdit.FirstStep = toolSetting.firstStep;
                toolEdit.Precision = toolSetting.precision;
                toolEdit.SelectedPriority = toolSetting.priority;
                toolEdit.PriorityCreteria = toolSetting.priorityCreteria;
                toolEdit.TempScaleMin = toolSetting.tempScaleMin;
                toolEdit.TempScaleMax = toolSetting.tempScaleMax;
                toolEdit.MaxCount = toolSetting.maxCount;
                toolEdit.mPatternDataList.Clear();

                if (toolSetting.patternDataSetting.PatternImage == null)
                    toolSetting.patternDataSetting = new PatternData();
                SvImage patternImage = new SvImage()
                {
                    Mat = toolEdit.LoadPatternImage($"{this.curVsProgram.NameJob}.{toolArea.Name}.{idxVP}.{nameTool}_Mat.bmp"),
                    TransformMat = toolEdit.LoadPatternImage($"{this.curVsProgram.NameJob}.{toolArea.Name}.{idxVP}.{nameTool}_TransMat.bmp"),
                };
                SvMask maskImage = new SvMask(patternImage.Width, patternImage.Height)
                {
                    DrawMask = toolSetting.patternDataSetting.MaskImage.DrawMask,
                    OnMask = toolSetting.patternDataSetting.MaskImage.OnMask,
                    DisplayMask = toolSetting.patternDataSetting.MaskImage.DisplayMask,
                };
                SvPoint refPoint = new SvPoint() { Point3d = toolSetting.patternDataSetting.RefPoint.Point3d };
                PatternData patternData = new PatternData(patternImage, maskImage, refPoint);

                toolEdit.mPatternDataList.Insert(0, patternData);
                if (patternData.PatternImage.Mat != null && patternData.PatternImage.Mat.Width > 0 && patternData.PatternImage.Mat.Height > 0)
                {
                    toolEdit.cpPattern = toolSetting.cpPattern;
                    toolEdit.masterImg.Source = patternImage.Mat.ToBitmapSource();
                    toolEdit.FitMasterImage();
                }

            }
            else if (nameTool.Contains("TempMatchZeroTool"))
            {
                if (vsMnSb.tempMatchZeroSettings.Count == 0 || vsMnSb.tempMatchZeroSettings[idxToolType] == null)
                    return;
                TempMatchZeroTool tempTool = new TempMatchZeroTool() { Name = nameTool };
                toolArea.InitLabelTool(tempTool);
                toolArea.LoadTool(tempTool, toolArea.heightToolLst[indexTools]);

                TempMatchZeroEdit toolEdit = tempTool.toolEdit;
                TempMatchZeroSetting toolSetting = vsMnSb.tempMatchZeroSettings[idxToolType];

                toolEdit.PriorityCreteria = toolSetting.priorityCreteria;
                toolEdit.MaxCount = toolSetting.maxCount;
                toolEdit.rectTrainCv = toolSetting.rectTrain;
                toolEdit.rectSearchCv = toolSetting.rectSearch;
                //Rect Search
                toolEdit.CreatRect(toolSetting.rectSearch.X, toolSetting.rectSearch.Y, toolSetting.rectSearch.Width, toolSetting.rectSearch.Height, 0, colorSearchStroke, colorSearchFill, "S", toolEdit.inEle);
                //Rect Train
                toolEdit.CreatRect(toolSetting.rectTrain.X, toolSetting.rectTrain.Y, toolSetting.rectTrain.Width, toolSetting.rectTrain.Height, 0, colorRectStroke, colorRectFill, "T", toolEdit.trainEle);
                toolEdit.IsUseROI = toolSetting.isUseROI;

                if (toolSetting.patternDataSetting.PatternImage == null)
                    toolSetting.patternDataSetting = new PatternData();
                SvImage patternImage = new SvImage()
                {
                    Mat = toolEdit.LoadPatternImage($"{this.curVsProgram.NameJob}.{toolArea.Name}.{idxVP}.{nameTool}_Mat.bmp"),
                    TransformMat = toolEdit.LoadPatternImage($"{this.curVsProgram.NameJob}.{toolArea.Name}.{idxVP}.{nameTool}_TransMat.bmp"),
                };
                SvMask maskImage = new SvMask(patternImage.Width, patternImage.Height)
                {
                    DrawMask = toolSetting.patternDataSetting.MaskImage.DrawMask,
                    OnMask = toolSetting.patternDataSetting.MaskImage.OnMask,
                    DisplayMask = toolSetting.patternDataSetting.MaskImage.DisplayMask,
                };
                SvPoint refPoint = new SvPoint() { Point3d = toolSetting.patternDataSetting.RefPoint.Point3d };
                PatternData patternData = new PatternData(patternImage, maskImage, refPoint);

                toolEdit.patternData = patternData;
                if (patternData.PatternImage.Mat != null && patternData.PatternImage.Mat.Width > 0 && patternData.PatternImage.Mat.Height > 0)
                {
                    toolEdit.cpPattern = toolSetting.cpPattern;
                    toolEdit.imgMaster.Source = patternImage.Mat.ToBitmapSource();
                    toolEdit.DrawCoordinateMaster();
                    toolEdit.FitMasterImage();
                }
            }
            else if (nameTool.Contains("FixtureTool"))
            {
                if (vsMnSb.fixtureSettings.Count == 0 || vsMnSb.fixtureSettings[idxToolType] == null)
                    return;
                FixtureTool fixtureTool = new FixtureTool() { Name = nameTool };
                toolArea.InitLabelTool(fixtureTool);
                toolArea.LoadTool(fixtureTool, toolArea.heightToolLst[indexTools]);

                FixtureSetting toolSetting = vsMnSb.fixtureSettings[idxToolType];
                fixtureTool.toolEdit.InTranslateX = toolSetting.inTranslateX;
                fixtureTool.toolEdit.InTranslateY = toolSetting.inTranslateY;
                fixtureTool.toolEdit.InScale = toolSetting.inScale;
                fixtureTool.toolEdit.InRotation = toolSetting.inRotation;

            }
            else if (nameTool.Contains("EditRegionTool"))
            {
                if (vsMnSb.editRegonSettings.Count == 0 || vsMnSb.editRegonSettings[idxToolType] == null)
                    return;
                EditRegionTool editRegionTool = new EditRegionTool() { Name = nameTool };
                toolArea.InitLabelTool(editRegionTool);
                toolArea.LoadTool(editRegionTool, toolArea.heightToolLst[indexTools]);

                EditRegonSetting editRegonSetting = vsMnSb.editRegonSettings[idxToolType];
                for (int j = 0; j < vsMnSb.editRegonSettings[idxToolType].ROIList.Count; j++)
                {
                    RotatedRect rect = vsMnSb.editRegonSettings[idxToolType].ROIList[j];
                    if (rect.Center.X == 0 && rect.Center.Y == 0)
                        continue;
                    Name = String.Format("R{0}", j + 1);
                    float left = rect.Center.X - rect.Size.Width / 2f;
                    float top = rect.Center.Y - rect.Size.Height / 2f;
                    float width = rect.Size.Width;
                    float height = rect.Size.Height;
                    float angle = rect.Angle;
                    editRegionTool.toolEdit.CreatRect(left, top, width, height, angle, colorRectStroke, colorRectFill, Name);
                }
                editRegionTool.toolEdit.centInputImg = editRegonSetting.centInputImage;
                editRegionTool.toolEdit.UpdateTransformMat();

                editRegionTool.toolEdit.resultCkbList = editRegonSetting.resultCkbList;
                editRegionTool.toolEdit.GenerateTable(editRegonSetting.numberSub, editRegonSetting.resultCkbList);
            }
            else if (nameTool.Contains("ContrastNBrightnessTool"))
            {
                if (vsMnSb.contrastNBrightnessSettings.Count == 0 || vsMnSb.contrastNBrightnessSettings[idxToolType] == null)
                    return;
                ContrastNBrightnessTool contrastTool = new ContrastNBrightnessTool() { Name = nameTool }; 
                toolArea.InitLabelTool(contrastTool);
                toolArea.LoadTool(contrastTool, toolArea.heightToolLst[indexTools]);
                ContrastNBrightnessSetting contrastSetting = vsMnSb.contrastNBrightnessSettings[idxToolType];
                ContrastNBrightnessEdit contrastEdit = contrastTool.toolEdit;

                contrastEdit.GammaValue = contrastSetting.gammaValue;
                contrastEdit.AlphaValue = contrastSetting.alphaValue;
                contrastEdit.BetaValue = contrastSetting.betaValue;
            }
            else if (nameTool.Contains("ImageProcessTool"))
            {
                if (vsMnSb.imageProcessSettings.Count == 0 || vsMnSb.imageProcessSettings[idxToolType] == null)
                    return;
                ImageProcessTool imageProcessTool = new ImageProcessTool() { Name = nameTool };
                toolArea.InitLabelTool(imageProcessTool);
                toolArea.LoadTool(imageProcessTool, toolArea.heightToolLst[indexTools]);

                ImageProcessSetting imageProcessSetting = vsMnSb.imageProcessSettings[idxToolType];
                ImageProcessEdit toolEdit = imageProcessTool.toolEdit;

                toolEdit.SelectedImageProcessMode = imageProcessSetting.selectedImageProcessMode;
                toolEdit.SelectedThresholdMode = imageProcessSetting.selectedThresholdMode;
                toolEdit.ThresholdValue = imageProcessSetting.thresholdValue;
            }
            else if (nameTool.Contains("BlobTool"))
            {
                if (vsMnSb.blobSettings.Count == 0 || vsMnSb.blobSettings[idxToolType] == null)
                    return;
                BlobTool blobTool = new BlobTool() { Name = nameTool };
                toolArea.InitLabelTool(blobTool);
                toolArea.LoadTool(blobTool, toolArea.heightToolLst[indexTools]);

                blobTool.Name = nameTool;
                BlobSetting blobSetting = vsMnSb.blobSettings[idxToolType];
                BlobEdit toolEdit = blobTool.toolEdit;

                toolEdit.SelectBlobMode = blobSetting.selectBlobMode;
                toolEdit.SelectBlobType = blobSetting.selectBlobType;
                toolEdit.SelectBlobPolarity = blobSetting.selectBlobPolarity;
                toolEdit.SelectBlobBinary = blobSetting.selectBlobBinary;
                toolEdit.SelectBlobPriority = blobSetting.selectBlobPriority;
                toolEdit.SelectSort = blobSetting.selectSort;
                toolEdit.IsCalBlob = blobSetting.isCalBlob;
                toolEdit.IsExceptBound = blobSetting.isExceptBound;
                toolEdit.IsFillHole = blobSetting.isFillHole;
                toolEdit.IsAscend = blobSetting.isAscend;
                toolEdit.Range = blobSetting.range;
                toolEdit.LowRange = blobSetting.lowRange;
                toolEdit.HighRange = blobSetting.highRange;
                toolEdit.BlockSize = blobSetting.blockSize;
                toolEdit.Coeff = blobSetting.coeff;
                toolEdit.CoeffR = blobSetting.coeffR;
                toolEdit.ConstSub = blobSetting.constantSubtract;
                toolEdit.ConstMin = blobSetting.constantMin;
                toolEdit.BlobFilters = blobSetting.blobFilters;

                
            }
            else if (nameTool.Contains("SegmentNeuroTool"))
            {
                //Check Neurocle License
                if (!UiManager.CheckNeurocleLicense()) { nrt.Model model = new nrt.Model(); /*Tắt ứng dụng*/ }
                if (vsMnSb.segmentNeuroSettings[idxToolType] == null)
                    return;
                SegmentNeuroTool toolTool = new SegmentNeuroTool() { Name = nameTool };
                toolArea.InitLabelTool(toolTool);
                toolArea.LoadTool(toolTool, toolArea.heightToolLst[indexTools]);

                SegmentNeuroEdit toolEdit = toolTool.toolEdit;
                SegmentNeuroSetting toolSetting = vsMnSb.segmentNeuroSettings[idxToolType];
                List<NeuroModel> nrtModels = toolSetting.modelNeuroes;
                for (int i = 0; i < toolSetting.modelNeuroes.Count; i++)
                {
                    toolEdit.CreatNewModel(nrtModels[i].Name, nrtModels[i].Path, nrtModels[i].PathRuntime, nrtModels[i].Device, nrtModels[i].DeviceIdx, nrtModels[i].Score, i);
                }
            }
            else if (nameTool.Contains("VidiCognexTool"))
            {
                //Check Vidi Suite License
                if (!UiManager.CheckVidiLicense())
                { 
                    /*Tắt ứng dụng*/
                    Application.Current.Shutdown();  
                    return; 
                }
                if (vsMnSb.vidiCognexSettings[idxToolType] == null)
                    return;
                VidiCognexTool toolTool = new VidiCognexTool() { Name = nameTool };
                toolArea.InitLabelTool(toolTool);
                toolArea.LoadTool(toolTool, toolArea.heightToolLst[indexTools]);

                VidiCognexEdit toolEdit = toolTool.toolEdit;
                VidiCognexSetting toolSetting = vsMnSb.vidiCognexSettings[idxToolType];
                toolEdit.DeviceSelected = toolSetting.deviceName;
                toolEdit.DevCbxIdx = toolSetting.deviceIdx;
                List<VidiModel> vidiModels = toolSetting.vidiModels;
                for (int i = 0; i < toolSetting.vidiModels.Count; i++)
                {
                    toolEdit.CreatNewModel(vidiModels[i].Name, vidiModels[i].Path, vidiModels[i].Score, i);
                }
            }
            else if (nameTool.Contains("VisionProTool"))
            {
                //Check VisionPro License
                if (!UiManager.CheckVisionProLicense()) 
                { 
                    Application.Current.Shutdown();
                    return;
                }
                if (vsMnSb.visionProSettings[idxToolType] == null)
                    return;
                VisionProTool toolTool = new VisionProTool() { Name = nameTool };
                toolArea.InitLabelTool(toolTool);
                toolArea.LoadTool(toolTool, toolArea.heightToolLst[indexTools]);

                VisionProEdit toolEdit = toolTool.toolEdit;
                VisionProSetting toolSetting = vsMnSb.visionProSettings[idxToolType];
                toolEdit.TxtVppPath.Text = toolSetting.path;
                toolEdit.VppOutInforRaws = toolSetting.vppOutputInfos;
                if(!String.IsNullOrEmpty(toolEdit.TxtVppPath.Text))
                {
                    toolEdit.DecodeVppFile(toolEdit.TxtVppPath.Text);
                }    
            }
            else if (nameTool.Contains("OutBlobResTool"))
            {
                if (vsMnSb.outBlobResSettings.Count == 0 || vsMnSb.outBlobResSettings[idxToolType] == null)
                    return;
                OutBlobResTool outResultTool = new OutBlobResTool() { Name = nameTool };
                toolArea.InitLabelTool(outResultTool);
                toolArea.LoadTool(outResultTool, toolArea.heightToolLst[indexTools]);

                OutBlobResSetting toolSetting = vsMnSb.outBlobResSettings[idxToolType];
                OutBlobResEdit toolEdit = outResultTool.toolEdit;
                toolEdit.txtAddrOutOK.Text = toolSetting.addrOutOK;
                toolEdit.txtAddrOutNG.Text = toolSetting.addrOutNG;
                toolEdit.SelectDevOutOK = toolSetting.selectDevOutOK;
                toolEdit.SelectDevOutNG = toolSetting.selectDevOutNG;
                toolEdit.addrOKLst = toolSetting.addrOKLst;
                toolEdit.addrNGLst = toolSetting.addrNGLst;
                toolEdit.indexImgs = toolSetting.indexImgs;
                toolEdit.DistSet = toolSetting.distanceSet;
                if((toolEdit.addrOKLst.Count > 0 && toolEdit.indexImgs.Count== toolEdit.addrOKLst.Count) || (toolEdit.addrNGLst.Count > 0 && toolEdit.indexImgs.Count == toolEdit.addrNGLst.Count))
                {
                    toolEdit.UpdateDataGrid(toolEdit.indexImgs.Count);
                }    
            }
            else if (nameTool.Contains("OutAcquisResTool"))
            {
                if (vsMnSb.outAcquisResSettings.Count == 0 || vsMnSb.outAcquisResSettings[idxToolType] == null)
                    return;
                OutAcquisResTool outResultTool = new OutAcquisResTool() { Name = nameTool };
                toolArea.InitLabelTool(outResultTool);
                toolArea.LoadTool(outResultTool, toolArea.heightToolLst[indexTools]);

                OutAcquisResSetting toolSetting = vsMnSb.outAcquisResSettings[idxToolType];
                OutAcquisResEdit toolEdit = outResultTool.toolEdit;
                toolEdit.txtAddrOutOK.Text = toolSetting.addrOutOK;
                toolEdit.txtAddrOutNG.Text = toolSetting.addrOutNG;
                toolEdit.SelectDevOutOK = toolSetting.selectDevOutOK;
                toolEdit.SelectDevOutNG = toolSetting.selectDevOutNG;
            }
            else if(nameTool.Contains("OutSegNeuroResTool"))
            {
                if (vsMnSb.outSegNeuroResSettings.Count == 0 || vsMnSb.outSegNeuroResSettings[idxToolType] == null)
                    return;
                OutSegNeuroResTool outResultTool = new OutSegNeuroResTool() { Name = nameTool };
                toolArea.InitLabelTool(outResultTool);
                toolArea.LoadTool(outResultTool, toolArea.heightToolLst[indexTools]);

                OutSegNeuroResSetting toolSetting = vsMnSb.outSegNeuroResSettings[idxToolType];
                OutSegNeuroResEdit toolEdit = outResultTool.toolEdit;
                toolEdit.TxtAddrOut = toolSetting.addrOut;
                toolEdit.SelectDevOut = toolSetting.selectDevOut;
                toolEdit.NumberPos = toolSetting.numberPos;
                toolEdit.addrLst = toolSetting.addrLst;
                toolEdit.UpdateDataGrid();
            }
            else if (nameTool.Contains("OutVidiCogResTool"))
            {
                if (vsMnSb.outVidiCogResSettings.Count == 0 || vsMnSb.outVidiCogResSettings[idxToolType] == null)
                    return;
                OutVidiCogResTool outResultTool = new OutVidiCogResTool() { Name = nameTool };
                toolArea.InitLabelTool(outResultTool);
                toolArea.LoadTool(outResultTool, toolArea.heightToolLst[indexTools]);

                OutVidiCogResSetting toolSetting = vsMnSb.outVidiCogResSettings[idxToolType];
                OutVidiCogResEdit toolEdit = outResultTool.toolEdit;
                toolEdit.TxtAddrOut = toolSetting.addrOut;
                toolEdit.SelectDevOut = toolSetting.selectDevOut;
                toolEdit.arrAddr = toolSetting.arrAddr;
                toolEdit.UpdateDataGrid();
            }
        }
        private void SaveToolCommon(int indexVP, List<VisionTool> tools, ToolArea toolArea, VisionMainSub vsMnSb)
        {
            //2 Para quyết định ToolArea có bị chặn các tool Output không
            vsMnSb.isOutTool = toolArea.IsOutTool;
            vsMnSb.isBlockOut = toolArea.IsBlockOut;
            //2 Para quy định tọa độ từng tool trên ToolArea
            vsMnSb.heightToolLst = toolArea.heightToolLst;
            vsMnSb.heightAllTool = toolArea.heightAllTool;
            //ToolArea
            foreach (var tool in tools)
            {
                vsMnSb.nameTools.Add(tool.Name);
                if (tool.ToolType == VisionToolType.ACQUISITION)
                {
                    AcquisitionTool Tool = (tool as AcquisitionTool);
                    if(Tool == null) { continue; }
                    AcquisitionEdit toolEdit = Tool.toolEdit;
                    AcquisitionSetting toolSetting = new AcquisitionSetting()
                    {
                        isCmaera = toolEdit.isImageCam,
                        isFolder = toolEdit.isImageFolder,
                        isVerticalFlip = (bool)toolEdit.ckbxVFlip.IsChecked,
                        isHorizontalFlip = (bool)toolEdit.ckbxHFlip.IsChecked,
                        isGrayMode = (bool)toolEdit.ckbxToGray.IsChecked,
                        rotateMode = toolEdit.SelectedRotateMode,
                        grayMode = toolEdit.SelectedGrayMode,
                    };
                    if (toolEdit.cbxCamDevice.SelectedValue != null && toolEdit.cbxCamDevice.SelectedIndex != 0)
                    {
                        try
                        {
                            //47
                            //toolSetting.deviceInfo = UiManager.DeviceInfo;
                            toolSetting.eCamDevType = toolEdit.Camera.eCamDevType;
                            toolSetting.serialNumber = toolEdit.Camera.SerialNumber;
                            toolSetting.WidthCam = toolEdit.WidthCam;
                            toolSetting.HeightCam = toolEdit.HeightCam;
                            toolSetting.ExposureTime = toolEdit.ExposureValue;
                            toolSetting.Gain = toolEdit.GainValue;

                            //if (toolEdit.Camera.GetserialNumber() != UiManager.IrpCamera.GetserialNumber(toolSetting.device))
                            if (toolEdit.Camera.GetserialNumber() != UiManager.IrpCamera.GetserialNumber())
                            {
                                //toolSetting.deviceInfo = UiManager.DeviceInfo;
                                toolSetting.eCamDevType = toolEdit.Camera.eCamDevType;
                                toolSetting.serialNumber = toolEdit.Camera.SerialNumber;
                                toolEdit.Camera.Close();
                                toolEdit.Camera.DisPose();
                                toolEdit.ConectCamera();
                                //logger.Create("Change Camera1 Setting" + connectionSettings.camera1.device.SpecialInfo.stCamLInfo.ToString());
                            }

                        }
                        catch (Exception ex)
                        {
                            //logger.Create("Ptr Device Camera1 Err" + ex.ToString() + UiManager.Cam1.GetserialNumber() + " " + UiManager.hikCamera.GetserialNumber(device1));
                        }
                    }
                    vsMnSb.aquisitionSettings.Add(toolSetting);
                }
                else if (tool.ToolType == VisionToolType.SAVEIMAGE)
                {
                    SaveImageTool Tool = (tool as SaveImageTool);
                    if (Tool == null) { continue; }
                    SaveImageEdit toolEdit = Tool.toolEdit;
                    SaveImageSetting toolSetting = new SaveImageSetting()
                    {
                        fileName = toolEdit.txtFileName.Text,
                        folderPath = toolEdit.txtFolderPath.Text,
                        imageFormat = toolEdit.ImageFormatSelected,
                        isAddDateTime = toolEdit.IsAddDateTime,
                        isAddCounter = toolEdit.IsAddCounter,
                        counter = toolEdit.NumUDCounter,
                        imageStorage = toolEdit.NumUDImageStorage,
                    };
                    vsMnSb.saveImageSettings.Add(toolSetting);
                }
                else if (tool.ToolType == VisionToolType.TEMPLATEMATCH)
                {
                    TemplateMatchTool Tool = (tool as TemplateMatchTool);
                    if (Tool == null) { continue; }
                    TemplateMatchEdit toolEdit = Tool.toolEdit;
                    TemplateMatchSetting toolSetting = new TemplateMatchSetting()
                    {
                        isUseEdge = toolEdit.IsUseEdge,
                        isAutoMatchPara = toolEdit.IsAutoMatchPara,
                        scaleFirst = toolEdit.ScaleFirst,
                        scaleLast = toolEdit.ScaleLast,
                        degMin = toolEdit.DegMin,
                        degMax = toolEdit.DegMax,
                        firstStep = toolEdit.FirstStep,
                        precision = toolEdit.Precision,
                        priorityCreteria = toolEdit.PriorityCreteria,
                        tempScaleMin = toolEdit.TempScaleMin,
                        tempScaleMax = toolEdit.TempScaleMax,
                        priority = toolEdit.SelectedPriority,
                        maxCount = toolEdit.MaxCount,

                    };
                    if (toolEdit.mPatternDataList.Count <= 0)
                    {
                        toolEdit.mPatternDataList.Add(new PatternData());
                    }
                    //Save patternData
                    if (toolEdit.mPatternDataList[0].PatternImage != null)
                    {
                        toolSetting.cpPattern = toolEdit.cpPattern;
                        toolEdit.SavePattentImage(toolEdit.mPatternDataList[0].PatternImage.Mat, $"{this.curVsProgram.NameJob}.{toolArea.Name}.{indexVP}.{Tool.Name}_Mat.bmp");
                        toolEdit.SavePattentImage(toolEdit.mPatternDataList[0].PatternImage.TransformMat, $"{this.curVsProgram.NameJob}.{toolArea.Name}.{indexVP}.{Tool.Name}_TransMat.bmp");

                        toolSetting.patternDataSetting.MaskImage.DrawMask = toolEdit.mPatternDataList[0].MaskImage.DrawMask;
                        toolSetting.patternDataSetting.MaskImage.OnMask = toolEdit.mPatternDataList[0].MaskImage.OnMask;
                        toolSetting.patternDataSetting.MaskImage.DisplayMask = toolEdit.mPatternDataList[0].MaskImage.DisplayMask;

                        toolSetting.patternDataSetting.RefPoint.Point3d = toolEdit.mPatternDataList[0].RefPoint.Point3d;
                    }
                    vsMnSb.templateMatchSettings.Add(toolSetting);
                }
                else if (tool.ToolType == VisionToolType.TEMPMATCHZERO)
                {
                    TempMatchZeroTool Tool = (tool as TempMatchZeroTool);
                    if (Tool == null) { continue; }
                    var toolEdit = Tool.toolEdit;
                    TempMatchZeroSetting toolSetting = new TempMatchZeroSetting()
                    {
                        priorityCreteria = toolEdit.PriorityCreteria,
                        maxCount = toolEdit.MaxCount,
                        isUseROI = toolEdit.IsUseROI,
                        //rectSearch = new OpenCvSharp.Rect((int)Canvas.GetLeft(toolEdit.rectSearch), (int)Canvas.GetTop(toolEdit.rectSearch), (int)toolEdit.rectSearch.Width, (int)toolEdit.rectSearch.Height),
                        //rectTrain = new OpenCvSharp.Rect((int)Canvas.GetLeft(toolEdit.rectTrain), (int)Canvas.GetTop(toolEdit.rectTrain), (int)toolEdit.rectTrain.Width, (int)toolEdit.rectTrain.Height)
                        rectSearch = toolEdit.rectSearchCv,
                        rectTrain = toolEdit.rectTrainCv,
                    };
                    //Save patternData
                    if (toolEdit.patternData.PatternImage != null)
                    {
                        toolSetting.cpPattern = toolEdit.cpPattern;
                        toolEdit.SavePattentImage(toolEdit.patternData.PatternImage.Mat, $"{this.curVsProgram.NameJob}.{toolArea.Name}.{indexVP}.{Tool.Name}_Mat.bmp");
                        toolEdit.SavePattentImage(toolEdit.patternData.PatternImage.TransformMat, $"{this.curVsProgram.NameJob}.{toolArea.Name}.{indexVP}.{Tool.Name}_TransMat.bmp");

                        toolSetting.patternDataSetting.MaskImage.DrawMask = toolEdit.patternData.MaskImage.DrawMask;
                        toolSetting.patternDataSetting.MaskImage.OnMask = toolEdit.patternData.MaskImage.OnMask;
                        toolSetting.patternDataSetting.MaskImage.DisplayMask = toolEdit.patternData.MaskImage.DisplayMask;

                        toolSetting.patternDataSetting.RefPoint.Point3d = toolEdit.patternData.RefPoint.Point3d;
                    }
                    vsMnSb.tempMatchZeroSettings.Add(toolSetting);
                }
                else if (tool.ToolType == VisionToolType.FIXTURE)
                {
                    FixtureTool Tool = (tool as FixtureTool);
                    if (Tool == null) { continue; }
                    FixtureSetting toolSetting = new FixtureSetting()
                    {
                        inTranslateX = Tool.toolEdit.InTranslateX,
                        inTranslateY = Tool.toolEdit.InTranslateY,
                        inScale = Tool.toolEdit.InScale,
                        inRotation = Tool.toolEdit.InRotation,
                    };
                    vsMnSb.fixtureSettings.Add(toolSetting);
                }
                else if (tool.ToolType == VisionToolType.EDITREGION)
                {
                    EditRegionTool Tool = (tool as EditRegionTool);
                    if (Tool == null) { continue; }
                    EditRegonSetting toolSetting = new EditRegonSetting();
                    for (int i = 0; i < Tool.toolEdit.RectLst.Count; i++)
                    {
                        double x = Canvas.GetLeft(Tool.toolEdit.RectLst[i]) + Tool.toolEdit.RectLst[i].Width / 2.0;
                        double y = Canvas.GetTop(Tool.toolEdit.RectLst[i]) + Tool.toolEdit.RectLst[i].Height / 2.0;
                        RotateTransform rotTrans = Tool.toolEdit.RectLst[i].RenderTransform as RotateTransform ?? new RotateTransform(0);
                        OpenCvSharp.RotatedRect rec = new OpenCvSharp.RotatedRect(new Point2f((float)x, (float)y), new Size2f((float)Tool.toolEdit.RectLst[i].Width, (float)Tool.toolEdit.RectLst[i].Height), (float)rotTrans.Angle);
                        if (rec.Center.X == 0 && rec.Center.Y == 0)
                            continue;
                        toolSetting.ROIList.Add(rec);
                    }
                    toolSetting.centInputImage = Tool.toolEdit.centInputImg;
                    toolSetting.numberSub = Tool.QueryNumberSubProgram(out List<ToolArea> toolAreaSubs);
                    //Update kết quả checkbox chọn ảnh chia về các Sub
                    Tool.toolEdit.UpdateResultCheckBox();
                    toolSetting.resultCkbList = Tool.toolEdit.resultCkbList;
                    vsMnSb.editRegonSettings.Add(toolSetting);
                }
                else if (tool.ToolType == VisionToolType.CONTRASTnBRIGHTNESS)
                {
                    ContrastNBrightnessTool Tool = (tool as ContrastNBrightnessTool);
                    if (Tool == null) { continue; }
                    ContrastNBrightnessSetting toolSetting = new ContrastNBrightnessSetting()
                    {
                        gammaValue = Tool.toolEdit.GammaValue,
                        alphaValue = Tool.toolEdit.AlphaValue,
                        betaValue = Tool.toolEdit.BetaValue,
                    };
                    vsMnSb.contrastNBrightnessSettings.Add(toolSetting);
                }
                else if (tool.ToolType == VisionToolType.IMAGEPROCESS)
                {
                    ImageProcessTool Tool = (tool as ImageProcessTool);
                    if (Tool == null) { continue; }
                    ImageProcessSetting toolSetting = new ImageProcessSetting()
                    {
                        selectedImageProcessMode = Tool.toolEdit.SelectedImageProcessMode,
                        selectedThresholdMode = Tool.toolEdit.SelectedThresholdMode,
                        thresholdValue = Tool.toolEdit.ThresholdValue,
                    };
                    vsMnSb.imageProcessSettings.Add(toolSetting);
                }
                else if (tool.ToolType == VisionToolType.BLOB)
                {
                    BlobTool Tool = (tool as BlobTool);
                    if (Tool == null) { continue; }
                    BlobSetting toolSetting = new BlobSetting()
                    {
                        selectBlobMode = Tool.toolEdit.SelectBlobMode,
                        selectBlobType = Tool.toolEdit.SelectBlobType,
                        selectBlobPolarity = Tool.toolEdit.SelectBlobPolarity,
                        selectBlobBinary = Tool.toolEdit.SelectBlobBinary,
                        selectBlobPriority = Tool.toolEdit.SelectBlobPriority,
                        selectSort = Tool.toolEdit.SelectSort,
                        isCalBlob = Tool.toolEdit.IsCalBlob,
                        isExceptBound = Tool.toolEdit.IsExceptBound,
                        isFillHole = Tool.toolEdit.IsFillHole,
                        isAscend = Tool.toolEdit.IsAscend,
                        range = Tool.toolEdit.Range,
                        lowRange = Tool.toolEdit.LowRange,
                        highRange = Tool.toolEdit.HighRange,
                        blockSize = Tool.toolEdit.BlockSize,
                        coeff = Tool.toolEdit.Coeff,
                        coeffR = Tool.toolEdit.CoeffR,
                        constantSubtract = Tool.toolEdit.ConstSub,
                        constantMin = Tool.toolEdit.ConstMin,
                        blobFilters = Tool.toolEdit.BlobFilters,
                    };
                    vsMnSb.blobSettings.Add(toolSetting);
                }
                else if(tool.ToolType == VisionToolType.SEGMENTNEURO)
                {
                    if (!(tool is SegmentNeuroTool Tool)) { continue; }
                    SegmentNeuroSetting toolSetting = new SegmentNeuroSetting();
                    Tool.toolEdit.modelList.ForEach(model => toolSetting.modelNeuroes.Add(model));
                    vsMnSb.segmentNeuroSettings.Add(toolSetting);
                }
                else if (tool.ToolType == VisionToolType.VIDICOGNEX)
                {
                    if (!(tool is VidiCognexTool Tool)) { continue; }
                    VidiCognexSetting toolSetting = new VidiCognexSetting()
                    {
                        deviceName = Tool.toolEdit.DeviceSelected,
                        deviceIdx = Tool.toolEdit.DevCbxIdx,
                    };
                    Tool.toolEdit.modelList.ForEach(model => toolSetting.vidiModels.Add(model));
                    vsMnSb.vidiCognexSettings.Add(toolSetting);
                }
                else if (tool.ToolType == VisionToolType.VISIONPRO)
                {
                    if (!(tool is VisionProTool Tool)) { continue; }
                    VisionProSetting toolSetting = new VisionProSetting()
                    {
                        path = Tool.toolEdit.TxtVppPath.Text,
                        vppOutputInfos = Tool.toolEdit.VppOutInforRaws,
                    };
                    vsMnSb.visionProSettings.Add(toolSetting);
                }
                else if (tool.ToolType == VisionToolType.OUTBLOBRES)
                {
                    OutBlobResTool Tool = (tool as OutBlobResTool);
                    if (Tool == null) { continue; }
                    OutBlobResSetting toolSetting = new OutBlobResSetting()
                    { 
                        selectDevOutOK = Tool.toolEdit.SelectDevOutOK,
                        selectDevOutNG = Tool.toolEdit.SelectDevOutNG,
                        addrOutOK = Tool.toolEdit.txtAddrOutOK.Text,
                        addrOutNG = Tool.toolEdit.txtAddrOutNG.Text,
                        addrOKLst = Tool.toolEdit.addrOKLst,
                        addrNGLst = Tool.toolEdit.addrNGLst,
                        indexImgs = Tool.toolEdit.indexImgs,
                        distanceSet = Tool.toolEdit.DistSet,
                    };
                    vsMnSb.outBlobResSettings.Add(toolSetting);
                }
                else if (tool.ToolType == VisionToolType.OUTACQUISRES)
                {
                    OutAcquisResTool Tool = (tool as OutAcquisResTool);
                    if (Tool == null) { continue; }
                    OutAcquisResSetting toolSetting = new OutAcquisResSetting()
                    {
                        selectDevOutOK = Tool.toolEdit.SelectDevOutOK,
                        selectDevOutNG = Tool.toolEdit.SelectDevOutNG,
                        addrOutOK = Tool.toolEdit.txtAddrOutOK.Text,
                        addrOutNG = Tool.toolEdit.txtAddrOutNG.Text,
                    };
                    vsMnSb.outAcquisResSettings.Add(toolSetting);
                }
                else if (tool.ToolType == VisionToolType.OUTSEGNEURORES)
                {
                    OutSegNeuroResTool Tool = (tool as OutSegNeuroResTool);
                    if (Tool == null) { continue; }
                    OutSegNeuroResSetting toolSetting = new OutSegNeuroResSetting()
                    {
                        selectDevOut = Tool.toolEdit.SelectDevOut,
                        numberPos = Tool.toolEdit.NumberPos,
                        addrOut = Tool.toolEdit.TxtAddrOut,
                        addrLst = Tool.toolEdit.addrLst,
                    };
                    vsMnSb.outSegNeuroResSettings.Add(toolSetting);
                }
                else if (tool.ToolType == VisionToolType.OUTVIDICOGRES)
                {
                    OutVidiCogResTool Tool = (tool as OutVidiCogResTool);
                    if (Tool == null) { continue; }
                    OutVidiCogResSetting toolSetting = new OutVidiCogResSetting()
                    {
                        selectDevOut = Tool.toolEdit.SelectDevOut,
                        addrOut = Tool.toolEdit.TxtAddrOut,
                        arrAddr = Tool.toolEdit.arrAddr,
                    };
                    vsMnSb.outVidiCogResSettings.Add(toolSetting);
                }
            }

            //Arrow Connector
            foreach (var arrow in toolArea.arrowCntLst)
            {
                ArrowConnectSetting arrowConnectSetting = new ArrowConnectSetting()
                {
                    name = arrow.name,
                    data = arrow.data,
                    startPoint = arrow.startPoint,
                    endPoint = arrow.endPoint
                };
                vsMnSb.arrowConnect.Add(arrowConnectSetting);
            }
        }
        #endregion
    }

    public class ToolAreaGroup : INotifyPropertyChanged
    {
        public int index { get; set; }
        private ToolArea _toolAreaMain = new ToolArea();
        private List<ToolArea> _toolAreaSubs = new List<ToolArea>();
        public ToolArea ToolAreaMain { get => _toolAreaMain; set { _toolAreaMain = value; OnPropertyChanged(nameof(ToolAreaMain)); } }
        public List<ToolArea> ToolAreaSubs { get => _toolAreaSubs; set { _toolAreaSubs = value; OnPropertyChanged(nameof(ToolAreaSubs)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler ToolAMChildrenChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ToolAreaGroup()
        {
            index = 0;
            ToolAreaMain = new ToolArea();
            ToolAreaSubs = new List<ToolArea>();
        }

        public ToolAreaGroup Clone()
        {
            return new ToolAreaGroup
            {
                index = this.index,
                ToolAreaMain = this.ToolAreaMain,
                ToolAreaSubs = this.ToolAreaSubs
            };
        }

    }
}
