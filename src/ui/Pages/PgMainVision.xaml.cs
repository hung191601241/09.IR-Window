using AutoLaserCuttingInput;
using Development;
using ITM_Semiconductor;
using MvCamCtrl.NET;
using nrt;
using OpenCvSharp;
using OpenCvSharp.Cuda;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VisionInspection;
using VisionTools.ToolDesign;
using VisionTools.ToolEdit;
using Device = Development.Device;
using Point = System.Windows.Point;

namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for PgMainVision.xaml
    /// </summary>
    public partial class PgMainVision : Page
    {
        MyLogger logger = new MyLogger("PgMainVision");
        private readonly object _lock = new object();
        private System.Timers.Timer clock;
        private int gLogIndex;
        private bool autoScrollMode = true;
        public ObservableCollection<logEntry> LogEntries { get; set; } = new ObservableCollection<logEntry>();
        private Color Colo_ON;
        private Color Colo_OFF;
        private Color Colo_ON1;
        private Color Colo_OFF1;
        private bool READ_MACHINE_RUNNING;
        private bool READ_LAMP_RESET;
        private int READ_ALARM_01;
        // Cờ kiểm tra xem ClearError() đã chạy hay chưa
        private bool hasClearedError = false;
        //Camera
        private PgCamera pgCamera;
        private int numVpEachJob = 0;
        private List<ToolAreaGroup> ToolAreaGrs = new List<ToolAreaGroup>();
        private readonly StackPanel[] stVisionAutoes = new StackPanel[5];
        private readonly List<VisionProgramN> VPnLst = new List<VisionProgramN>();
        private readonly List<string> NameVPLst = new List<string>();
        private List<List<VisionTool>> VsToolLst = new List<List<VisionTool>>();

        Boolean IsRunning = false;
        //List Result Communication
        private bool READ_VISION_TRIG1 = false;
        private bool READ_VISION_TRIG2 = false;
        private bool READ_VISION_TRIG3 = false;
        private bool READ_VISION_TRIG4 = false;
        private bool READ_VISION_RESET1 = false;
        private bool READ_VISION_RESET2 = false;
        private Thread runThread1;
        private Thread runThread2;
        private Thread runThread3;
        private Thread runThread4;
        private bool IsStartThread = false;
        private bool Flag1 = false, Flag2 = false, Flag3 = false, Flag4 = false;

        public PgMainVision()
        {
            InitializeComponent();
            InitializeColors();
            InitializeErrorCodes();
            ActionClearAlarm.ClearErrorAction = ClearError;

            pgCamera = UiManager.pageTable[PAGE_ID.PAGE_CAMERA_SETTING] as PgCamera;
            this.Loaded += PgMainVision_Loaded;
            this.Unloaded += PgMainVision_Unloaded;
            this.DataContext = this;

        }

        private void PgMainVision_Loaded(object sender, RoutedEventArgs e)
        {
            this.clock = new System.Timers.Timer(1000);
            this.clock.AutoReset = true;
            this.clock.Elapsed += this.Clock_Elapsed;
            IsStartThread = true;
            clock.Start(); 
            LoadAllToolArea();
        }
        private void PgMainVision_Unloaded(object sender, RoutedEventArgs e)
        {
            clock.Stop();
            IsStartThread = false;
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
            catch
            {
            }
        }
        private void LoadAllToolArea()
        {
            numVpEachJob = 0;
            Task.Run(() =>
            {
                ToolAreaGrs.Clear();
                NameVPLst.Clear();
                VPnLst.Clear();
                VsToolLst.Clear();
                for (int id = 0; id < stVisionAutoes.Length; id++)
                {
                    for (int i = 0; i < UiManager.appSettings.vsPrograms[id].vsProgramNs.Count; i++)
                    {
                        NameVPLst.Add(UiManager.appSettings.vsPrograms[id].NameDisp);
                        VPnLst.Add(UiManager.appSettings.vsPrograms[id].vsProgramNs[i]);
                        Dispatcher.Invoke(() =>
                        {
                            pgCamera.CreateVisionProgram(UiManager.appSettings.vsPrograms[id], ref ToolAreaGrs, ref VsToolLst, i);
                        });
                    }
                    AddLog($"{UiManager.appSettings.vsPrograms[id].NameDisp} Load Success!");
                }
                numVpEachJob = pgCamera.curVsProgram.vsProgramNs.Count;
                StartRun();
            });
        }
        private void InitializeColors()
        {
            string hexColorOn = "#66FF66"; // Mã màu ON (XANH)
            string hexColorOff = "#EEEEEE"; // Mã màu OFF (TRẮNG)
            string hexColorOff1 = "#FF0033"; // Mã màu OFF (ĐỎ)

            Colo_ON = (Color)ColorConverter.ConvertFromString(hexColorOn);
            Colo_OFF = (Color)ColorConverter.ConvertFromString(hexColorOff);
            Colo_ON1 = (Color)ColorConverter.ConvertFromString(hexColorOn);
            Colo_OFF1 = (Color)ColorConverter.ConvertFromString(hexColorOff1);
        }
        #region Camera Connect
        #endregion

        #region Auto Run
        public bool GetAlarm01(out int _value)
        {
            bool ret = false;
            try
            {
                ret = UiManager.PLC1.device.ReadWord(DeviceCode.M, int.Parse(PLCMap.ALARM_01), out _value);
            }
            catch (Exception ex)
            {
                _value = 0;
                logger.Create(String.Format("Read Position Current Axis Y Error: " + ex.Message));
                return ret;
            }
            return ret;
        }
        private bool GetTriggerVision(DeviceCode devIn, string addrIn, out bool _value)
        {
            try
            {
                return UiManager.PLC1.device.ReadBit(devIn, int.Parse(addrIn), out _value);
            }
            catch (Exception ex)
            {
                _value = false;
                logger.Create(String.Format("READ_VISION_TRIG: " + ex.Message));
                return false;
            }
        }
        private void Clock_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (UiManager.PLC1.device.isOpen())
                {
                    //UpdateError();
                }
                CheckConnect();

                if (READ_MACHINE_RUNNING)
                {
                    IsRunning = true;
                }
                else
                {
                    IsRunning = false;
                }

                var converter = new BrushConverter();
            }
            catch (Exception ex)
            {
                logger.Create(ex.Message.ToString());
            }
        }

        public bool ReceiveBitReset(DeviceCode devType, string devNum, out bool value)
        {
            value = false;
            try
            {
                return UiManager.PLC1.device.ReadBit(devType, int.Parse(devNum), out value);
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("RECEIVE_RESET Error: " + ex.Message));
                return false;
            }
        }
        public bool SendBitReset(DeviceCode devType, string devNum, bool value)
        {
            try
            {
                return UiManager.PLC1.device.WriteBit(devType, int.Parse(devNum), value);
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("SEND_RESET Error: " + ex.Message));
                return false;
            }
        }
        private void ReadPLC()
        {
            try
            {

                if (UiManager.PLC1.device.isOpen())
                {
                    GetAlarm01(out READ_ALARM_01);
                    UpdateError();
                }
                Thread.Sleep(10);
            }
            catch (Exception ex)
            {

                logger.Create($"Thread Read PLC :{ex}");
            }
        }
        private void CheckConnect()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (UiManager.PLC1.device.isOpen())
                {
                    lblPLCOnline.Content = "Connect";
                    lblPLCOnline.Background = Brushes.Green;
                }
                else
                {
                    lblPLCOnline.Content = "Disconnect";
                    lblPLCOnline.Background = Brushes.Red;
                }
            });
        }
        private void StartRun()
        {
            lock (_lock)
            {
                try
                {
                    if (IsRunning)
                    {
                        AddLog("already RUNNING!");
                        return;
                    }

                    IsRunning = true;
                    RunThread();
                }
                catch (Exception ex)
                {
                    logger.Create("btStart.click error: " + ex.Message);
                }
            }
        }
        private void RunThread()
        {
            //CallThreadStart1();
            CallThreadStart2();
            CallThreadStart3();
            CallThreadStart4();
        }
        private void CallThreadStart1()
        {
            try
            {
                runThread1 = new Thread(() =>
                {
                    if (IsStartThread)
                        RunManager1();
                });
                runThread1.IsBackground = true;
                runThread1.Start();
            }
            catch (Exception ex)
            {
                logger.Create("Start thread Auto loop 1 Err : " + ex.ToString());
            }
        }
        private void CallThreadStart2()
        {
            try
            {
                runThread2 = new Thread(() =>
                {
                    if (IsStartThread)
                        RunManager2();
                });
                runThread2.IsBackground = true;
                runThread2.Start();
            }
            catch (Exception ex)
            {
                logger.Create("Start thread Auto loop 2 Err : " + ex.ToString());
            }
        }
        private void CallThreadStart3()
        {
            try
            {
                runThread3 = new Thread(() =>
                {
                    if (IsStartThread)
                        RunManager3();
                });
                runThread3.IsBackground = true;
                runThread3.Start();
            }
            catch (Exception ex)
            {
                logger.Create("Start thread Auto loop 3 Err : " + ex.ToString());
            }
        }
        private void CallThreadStart4()
        {
            try
            {
                runThread4 = new Thread(() =>
                {
                    if (IsStartThread)
                        RunManager4();
                });
                runThread4.IsBackground = true;
                runThread4.Start();
            }
            catch (Exception ex)
            {
                logger.Create("Start thread Auto loop 4 Err : " + ex.ToString());
            }
        }

        private void RunManager1()
        {
            try
            {
                DeviceCode devIn = pgCamera.curVsProgram.vsProgramNs[0].selectDevIn;
                string addrIn = pgCamera.curVsProgram.vsProgramNs[0].addrIn;
                GetTriggerVision(devIn, addrIn, out READ_VISION_TRIG1);
                if (READ_VISION_TRIG1 && !Flag1)
                {
                    AddLog($"TRIGGER: {devIn}{addrIn} = ON");
                    Flag1 = true;

                    this.RunManagerVision_Align();
                    Flag1 = false;
                }
                else 
                {
                    Thread.Sleep(10);
                    CallThreadStart1(); 
                }
            }
            catch (Exception ex)
            {
                logger.Create($"Auto Run Manager 1 Error : {ex}");
                Thread.Sleep(10);
                CallThreadStart1();
            }
        }
        private void RunManager2()
        {
            try
            {
                DeviceCode devIn = pgCamera.curVsProgram.vsProgramNs[1].selectDevIn;
                string addrIn = pgCamera.curVsProgram.vsProgramNs[1].addrIn;
                GetTriggerVision(devIn, addrIn, out READ_VISION_TRIG2);
                if (READ_VISION_TRIG2 && !Flag2)
                {
                    AddLog($"TRIGGER: {devIn}{addrIn} = ON");
                    Flag2 = true;
                    this.RunManagerVision_CheckProduct(1);
                    Flag2 = false;
                    CallThreadStart2();
                }
                else
                {
                    Thread.Sleep(10);
                    CallThreadStart2(); 
                }
            }
            catch (Exception ex)
            {
                logger.Create($"Auto Run Manager 2 Error : {ex}");
                Thread.Sleep(10);
                CallThreadStart2();
            }
        }
        private void RunManager3()
        {
            try
            {
                //Reset Vision
                SegmentNeuroTool tool = VsToolLst[2].OfType<SegmentNeuroTool>().First();
                SegmentNeuroEdit toolEdit = tool.toolEdit;
                toolEdit.ReceiveBitReset(out READ_VISION_RESET1);
                if (READ_VISION_RESET1)
                {
                    AddLog($"BIT RESET 1 ON");
                    toolEdit.ResetBuffer();
                    toolEdit.SendBitReset(false);
                }
                //Trigger Vision
                VisionProgramN vsProgramN = pgCamera.curVsProgram.vsProgramNs[2];
                DeviceCode devIn = vsProgramN.selectDevIn;
                string addrIn = vsProgramN.addrIn;
                GetTriggerVision(devIn, addrIn, out READ_VISION_TRIG3);
                if (READ_VISION_TRIG3 && !Flag3)
                {
                    AddLog($"TRIGGER: {devIn}{addrIn} = ON");
                    Flag3 = true;
                    this.RunManagerVision_Inspection1(2);
                    Flag3 = false;
                    Thread.Sleep(10);
                    CallThreadStart3();
                }
                else
                {
                    Thread.Sleep(10);
                    CallThreadStart3(); 
                }
                
            }
            catch (Exception ex)
            {
                logger.Create($"Auto Run Manager 3 Error : {ex}");
                Thread.Sleep(10);
                CallThreadStart3();
            }
        }
        private void RunManager4()
        {
            try
            {
                //Reset Vision
                SegmentNeuroTool tool = VsToolLst[3].OfType<SegmentNeuroTool>().First();
                SegmentNeuroEdit toolEdit = tool.toolEdit;
                toolEdit.ReceiveBitReset(out READ_VISION_RESET2);
                if (READ_VISION_RESET2)
                {
                    AddLog($"BIT RESET 2 ON");
                    toolEdit.ResetBuffer();
                    toolEdit.SendBitReset(false);
                }

                //Trigger Vision
                VisionProgramN vsProgramN = pgCamera.curVsProgram.vsProgramNs[3];
                DeviceCode devIn = vsProgramN.selectDevIn;
                string addrIn = vsProgramN.addrIn;
                GetTriggerVision(devIn, addrIn, out READ_VISION_TRIG4);
                if (READ_VISION_TRIG4 && !Flag4)
                {
                    AddLog($"TRIGGER: {devIn}{addrIn} = ON");
                    Flag4 = true;
                    this.RunManagerVision_Inspection2(3);
                    Flag4 = false;
                    Thread.Sleep(10);
                    CallThreadStart4();
                }
                else
                {
                    Thread.Sleep(10);
                    CallThreadStart4(); 
                }
            }
            catch (Exception ex)
            {
                logger.Create($"Auto Run Manager 4 Error : {ex}");
                Thread.Sleep(10);
                CallThreadStart4();
            }
        }
        private void RunManagerVision_IV3(int idxVPn = 0)
        {
            //Dispatcher.Invoke(() =>
            //{
            //    pgCamera.RunTreeToolVP(pgCamera.toolAreaGrs[idxVPn]);
            //    //Send OK/NG Signal
            //    if (pgCamera.resultOK.Count > 0 && pgCamera.resultNG.Count > 0)
            //    {
            //        for (int i = 0; i < pgCamera.resultOK.Count; i++)
            //        {
            //            String2Enum(pgCamera.resultOK[i].Key, out DeviceCode devCodeOK, out string devNoOK);
            //            SetResultOK(devCodeOK, devNoOK, pgCamera.resultOK[i].Value);
            //            String2Enum(pgCamera.resultNG[i].Key, out DeviceCode devCodeNG, out string devNoNG);
            //            SetResultNG(devCodeNG, devNoNG, pgCamera.resultNG[i].Value);
            //        }
            //    }
            //    Flag1 = false;
            //});
            //// Chạy phần xử lý ảnh ở luồng khác
            //Task.Run(() =>
            //{
            //    if (pgCamera.curVsProgram == UiManager.appSettings.vsPrograms[0])
            //    {
            //        Dispatcher.Invoke(() =>
            //        {
            //            OutBlobResTool outResDesign = pgCamera.QueryOutBlobResTool(pgCamera.cbxDisplayResult.Items[idxVPn] as String) as OutBlobResTool;
            //            if (outResDesign == null)
            //            {
            //                //MessageBox.Show("Can't find any OutResult match!");
            //                //Flag1 = false;
            //                return;
            //            }
            //            switch (idxVPn)
            //            {
            //                case 0:
            //                    imgView1.Source = outResDesign.toolEdit.OriginImage.Mat.ToBitmapSource();
            //                    imgDetail1.Source = outResDesign.toolEdit.OutputImage.Mat.ToBitmapSource();
            //                    SaveImage(outResDesign.toolEdit.OriginImage.Mat, @"\ImageResult", "Judged");
            //                    AddLog($"FBCB1.Distance: {outResDesign.toolEdit.DistReal}");
            //                    AddLog("FBCB1.Result = " + (outResDesign.toolEdit.resultOut ? "OK" : "NG"));
            //                    break;
            //                case 1:
            //                    imgView2.Source = outResDesign.toolEdit.OriginImage.Mat.ToBitmapSource();
            //                    imgDetail2.Source = outResDesign.toolEdit.OutputImage.Mat.ToBitmapSource();
            //                    SaveImage(outResDesign.toolEdit.OriginImage.Mat, @"\ImageResult", "Judged");
            //                    AddLog($"FBCB2.Distance: {outResDesign.toolEdit.DistReal}");
            //                    AddLog("FBCB2.Result = " + (outResDesign.toolEdit.resultOut ? "OK" : "NG"));
            //                    break;
            //                case 2:
            //                    imgView3.Source = outResDesign.toolEdit.OriginImage.Mat.ToBitmapSource();
            //                    imgDetail3.Source = outResDesign.toolEdit.OutputImage.Mat.ToBitmapSource();
            //                    SaveImage(outResDesign.toolEdit.OriginImage.Mat, @"\ImageResult", "Judged");
            //                    AddLog($"SUS1.Distance: {outResDesign.toolEdit.DistReal}");
            //                    AddLog("SUS1.Result = " + (outResDesign.toolEdit.resultOut ? "OK" : "NG"));
            //                    break;
            //                case 3:
            //                    imgView4.Source = outResDesign.toolEdit.OriginImage.Mat.ToBitmapSource();
            //                    imgDetail4.Source = outResDesign.toolEdit.OutputImage.Mat.ToBitmapSource();
            //                    SaveImage(outResDesign.toolEdit.OriginImage.Mat, @"\ImageResult", "Judged");
            //                    AddLog($"SUS2.Distance: {outResDesign.toolEdit.DistReal}");
            //                    AddLog("SUS2.Result = " + (outResDesign.toolEdit.resultOut ? "OK" : "NG"));
            //                    break;
            //            }
            //        });
            //    }
            //});

        }
        private void RunManagerVision_Align()
        {
            //Gửi kết quả sau khi chụp xong
            Dispatcher.Invoke(() =>
            {
                pgCamera.RunTreeToolVP(pgCamera.toolAreaGrs[0]);
                CallThreadStart1();
            });
            // Chạy phần xử lý ảnh ở luồng khác
            Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    TempMatchZeroTool visionTool = pgCamera.toolAreaGrs[0].ToolAreaMain.Children.OfType<TempMatchZeroTool>().FirstOrDefault();
                    if (visionTool == null)
                        return;
                    TempMatchZeroEdit toolEdit = visionTool.toolEdit;
                    if (toolEdit.OutputImage.Mat == null || toolEdit.OutputImage.Mat.Width == 0 || toolEdit.OutputImage.Mat.Height == 0)
                    {
                        AddLog("Image Capture Align NULL!");
                        CallThreadStart1();
                        return;
                    }
                    AddLog($"Score = {toolEdit.OutScore}");
                    AddLog($"Offset X = {toolEdit.OutOffsetX} mm");
                    AddLog($"Offset Y = {toolEdit.OutOffsetY} mm");
                    imgView1.Source = toolEdit.OutputImage.Mat.ToBitmapSource();
                    toolEdit.OutputImage.Mat.SaveImage($"D:\\LogImageAlign\\AlignGrap {DateTime.Now:yyyy-MM-dd HH-mm-ss-fff}.bmp");
                });
            });
        }
        private void RunManagerVision_CheckProduct(int idxVPn = 0)
        {
            //Gửi kết quả sau khi chụp xong
            Dispatcher.Invoke(() =>
            {
                pgCamera.RunTreeToolVP(pgCamera.toolAreaGrs[1]);
            });
            // Chạy phần xử lý ảnh ở luồng khác
            Dispatcher.Invoke(() =>
            {
                OutCheckProductTool visionTool = pgCamera.toolAreaGrs[1].ToolAreaMain.Children.OfType<OutCheckProductTool>().FirstOrDefault();
                if (visionTool == null)
                    return;
                OutCheckProductEdit toolEdit = visionTool.toolEdit;
                if (toolEdit.OutputImage.Mat == null || toolEdit.OutputImage.Mat.Width == 0 || toolEdit.OutputImage.Mat.Height == 0)
                {
                    AddLog("Image Capture Check Product NULL!");
                    CallThreadStart2();
                    return;
                }
                AddLog($"Score = {toolEdit.Score}");
                AddLog($"Blob Count = {toolEdit.Blobs.Count}");
                imgView2.Source = toolEdit.OutputImage.Mat.ToBitmapSource();
            });
        }
        private void RunManagerVision_Inspection1(int idxVPn = 0)
        {
            //Gửi kết quả sau khi chụp xong
            pgCamera.RunTreeToolVP(ToolAreaGrs[idxVPn]);
            SegmentNeuroTool visionTool = VsToolLst[idxVPn].OfType<SegmentNeuroTool>().FirstOrDefault();
            if (visionTool == null) return;
            SegmentNeuroEdit toolEdit = visionTool.toolEdit;

            if (toolEdit.OutputImage.Mat == null || toolEdit.OutputImage.Mat.Width == 0)
            {
                AddLog("Image Capture Inspection 1 NULL!");
                return;
            }
            Dispatcher.Invoke(() => imgView3.Source = toolEdit.OutputImage.Mat.ToBitmapSource());
        }
        private void RunManagerVision_Inspection2(int idxVPn = 0)
        {
            //Gửi kết quả sau khi chụp xong
            pgCamera.RunTreeToolVP(ToolAreaGrs[idxVPn]); 
            SegmentNeuroTool visionTool = VsToolLst[idxVPn].OfType<SegmentNeuroTool>().FirstOrDefault();
            if (visionTool == null) return;
            SegmentNeuroEdit toolEdit = visionTool.toolEdit;

            if (toolEdit.OutputImage.Mat == null || toolEdit.OutputImage.Mat.Width == 0)
            {
                AddLog("Image Capture Inspection 2 NULL!");
                return;
            }
            Dispatcher.Invoke(() => imgView4.Source = toolEdit.OutputImage.Mat.ToBitmapSource());
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
        private void SaveImage(Mat imgSave, string path, string nameSave)
        {
            try
            {
                string folderPath = @"D:\AutoSIPLoader" + path;
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                string newImageFileName = nameSave + ".bmp";
                string newImagePath = System.IO.Path.Combine(folderPath, newImageFileName);
                Cv2.ImWrite(newImagePath, imgSave);
            }
            catch (Exception ex)
            {
                AddLog($"Lỗi lưu ảnh: {ex.Message}");
            }

        }
        private void SaveImage1(Mat imgSave, string path, string nameSave)
        {
            try
            {
                string folderPath = @"D:\AutoSIPLoader" + path;
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                string newImageFileName = DateTime.Now.ToString("yyyy-dd-MM ") + DateTime.Now.ToString("HH-mm-ss ") + nameSave + ".png";
                string newImagePath = System.IO.Path.Combine(folderPath, newImageFileName);

                DriveInfo driveInfo = new DriveInfo(System.IO.Path.GetPathRoot(folderPath));
                long freeSpace = driveInfo.AvailableFreeSpace;

                long minimumFreeSpace = 1000 * 1024 * 1024;

                if (freeSpace < minimumFreeSpace)
                {
                    DeleteOldestFile(folderPath);
                }
                Cv2.ImWrite(newImagePath, imgSave);
            }
            catch (Exception ex)
            {
                AddLog($"Lỗi lưu ảnh: {ex.Message}");
            }

        }
        private void DeleteOldestFile(string folderPath)
        {
            var directoryInfo = new DirectoryInfo(folderPath);
            var files = directoryInfo.GetFiles();

            if (files.Length == 0)
            {
                logger.Create("Không có file nào trong thư mục để xóa.");
                return;
            }

            // Tìm file cũ nhất
            var oldestFile = files[0];

            foreach (var file in files)
            {
                if (file.LastWriteTime < oldestFile.LastWriteTime)
                {
                    oldestFile = file;
                }
            }

            // Xóa file cũ nhất
            logger.Create($"Đang xóa file: {oldestFile.Name}");
            oldestFile.Delete();
        }
        #endregion

        #region ALARM LOG
        private List<int> errorCodes;
        List<DateTime> timeerror = new List<DateTime>();
        private String lastLog = "";
        public Boolean uiLogEnable { get; set; } = true;
        private void UpdateError()
        {

            Dispatcher.Invoke(() =>
            {
                if (UiManager.PLC1.device.isOpen())
                {
                    this.AddError(READ_ALARM_01);
                    ////this.AddError(READ_ALARM_02);
                    ////this.AddError(READ_ALARM_03);
                    ////this.AddError(READ_ALARM_04);
                    ////this.AddError(READ_ALARM_05);
                    ////this.AddError(READ_ALARM_06);
                    ////this.AddError(READ_ALARM_07);
                    ////this.AddError(READ_ALARM_08);
                    ////this.AddError(READ_ALARM_09);
                    ////this.AddError(READ_ALARM_10);


                    if ((READ_ALARM_01 == 0) && !hasClearedError)
                    {
                        this.ClearError();
                        hasClearedError = true; // Đặt cờ để ngăn chạy lại hàm này
                    }
                    else if (READ_ALARM_01 != 0)
                    {
                        // Khi D_ListShortDevicePLC_0[200] khác 0, reset lại cờ
                        hasClearedError = false;
                    }
                    if (READ_LAMP_RESET)
                    {
                        this.ClearError();
                    }


                }

            });

        }
        private void AddLog(String log)
        {
            try
            {
                if (log != null && !log.Equals(lastLog))
                {
                    lastLog = log;
                    logger.Create("addLog:" + log);

                    // UI log:
                    logEntry x = new logEntry()
                    {
                        logIndex = gLogIndex++,
                        logTime = DateTime.Now.ToString("HH:mm:ss.ff"),
                        logMessage = log,
                    };
                    this.Dispatcher.Invoke(() =>
                    {
                        LogEntries.Add(x);

                        // Nếu số lượng log vượt quá 300
                        if (LogEntries.Count > 1*0)
                        {
                            // Giữ lại 50 dòng gần nhất
                            var recentLogs = LogEntries.Skip(LogEntries.Count - 100).ToList();
                            LogEntries.Clear();
                            foreach (var item in recentLogs)
                                LogEntries.Add(item);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Create("addLog error:" + ex.Message);
            }
        }
        private void InitializeErrorCodes()
        {
            errorCodes = new List<int>();
            timeerror = new List<DateTime>();
        }
        public Boolean IsRunningAuto()
        {
            return IsRunning;
        }
        private void Number_Alarm()
        {
            int NumberAlarm = errorCodes.Count;
            this.CbShow.Content = NumberAlarm > 0 ? $"Errors : {NumberAlarm}" : "Not Show";
        }
        private void AlarmCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            isAlarmWindowOpen = true;
        }
        private void AlarmCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            isAlarmWindowOpen = false;
        }
        private bool isAlarmWindowOpen = false;
        private void ShowAlarm()
        {
            WndAlarm wndAlarm = new WndAlarm();
            wndAlarm.UpdateErrorList(errorCodes);
            wndAlarm.UpdateTimeList(timeerror);
            if (!isAlarmWindowOpen)
            {
                wndAlarm.Show();
            }
        }
        private void AddError(int errorCode)
        {
            Dispatcher.Invoke(() =>
            {
                if (errorCode == 0)
                {
                    return;
                }

                if (errorCodes.Contains(errorCode))
                {
                    return;
                }

                else if (errorCodes.Count >= 31)
                {
                    errorCodes.Add(1);
                    return;
                }

                //Thêm lỗi vào SQL
                if (errorCode <= 100)
                {
                    string mes = AlarmInfo.getMessage(errorCode);
                    string sol = AlarmList.GetSolution(errorCode);
                    var alarm = new AlarmInfo(errorCode, mes, sol);
                    DbWrite.createAlarm(alarm);
                }
                else
                {
                    string mes = AlarmList.GetMes(errorCode);
                    string sol = AlarmList.GetSolution(errorCode);
                    var alarm = new AlarmInfo(errorCode, mes, sol);
                    DbWrite.createAlarm(alarm);
                }
                errorCodes.Add(errorCode);
                timeerror.Add(DateTime.Now);
                for (int i = 0; i < errorCodes.Count; i++)

                {
                    int code = errorCodes[i];
                    switch (i)
                    {
                        case 0: this.DisplayAlarm(1, code); break;
                        case 1: this.DisplayAlarm(2, code); break;
                        case 2: this.DisplayAlarm(3, code); break;
                        case 3: this.DisplayAlarm(4, code); break;
                        case 4: this.DisplayAlarm(5, code); break;
                        case 5: this.DisplayAlarm(6, code); break;
                        case 6: this.DisplayAlarm(7, code); break;
                        case 7: this.DisplayAlarm(8, code); break;
                        case 8: this.DisplayAlarm(9, code); break;
                        case 9: this.DisplayAlarm(10, code); break;
                        case 10: this.DisplayAlarm(11, code); break;
                        case 11: this.DisplayAlarm(12, code); break;
                        case 12: this.DisplayAlarm(13, code); break;
                        case 13: this.DisplayAlarm(14, code); break;
                        case 14: this.DisplayAlarm(15, code); break;
                        case 15: this.DisplayAlarm(16, code); break;
                        case 16: this.DisplayAlarm(17, code); break;
                        case 17: this.DisplayAlarm(18, code); break;
                        case 18: this.DisplayAlarm(19, code); break;
                        case 19: this.DisplayAlarm(20, code); break;
                        case 20: this.DisplayAlarm(21, code); break;
                        case 21: this.DisplayAlarm(22, code); break;
                        case 22: this.DisplayAlarm(23, code); break;
                        case 23: this.DisplayAlarm(24, code); break;
                        case 24: this.DisplayAlarm(25, code); break;
                        case 25: this.DisplayAlarm(26, code); break;
                        case 26: this.DisplayAlarm(27, code); break;
                        case 27: this.DisplayAlarm(28, code); break;
                        case 28: this.DisplayAlarm(29, code); break;
                        case 29: this.DisplayAlarm(30, code); break;

                        default:
                            break;
                    }
                }
                if (!isAlarmWindowOpen)
                {
                    this.ShowAlarm();
                }

                this.Number_Alarm();
            });


        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (errorCodes == null)
                return;
            if (errorCodes.Count >= 1)
            {
                WndAlarm wndAlarm = new WndAlarm();
                wndAlarm.UpdateErrorList(errorCodes);
                wndAlarm.UpdateTimeList(timeerror);
                wndAlarm.Show();
            }

        }
        public void ClearError()
        {
            timeerror.Clear();
            errorCodes.Clear();
            Dispatcher.Invoke(new Action(() =>
            {
                for (int i = 1; i <= 30; i++)
                {
                    var label = (Label)FindName("lbMes" + i);
                    label.Content = "";
                    label.Background = Brushes.Black;
                }
            }));

            WndAlarm wndAlarm = new WndAlarm();
            wndAlarm.UpdateErrorList(errorCodes);
            wndAlarm.UpdateTimeList(timeerror);
            this.Number_Alarm();
        }
        private void DisplayAlarm(int index, int code)
        {
            try
            {
                if (code <= 100)
                {
                    Label label = (Label)FindName($"lbMes{index}");
                    if (label != null)
                    {
                        string mes = AlarmInfo.getMessage(code);
                        this.Dispatcher.Invoke(() =>
                        {
                            DateTime currentTime = DateTime.Now;
                            string currentTimeString = currentTime.ToString();
                            string newContent = currentTime.ToString() + " : " + mes;

                            label.Content = newContent;
                            label.Background = Brushes.Red;
                            //label.FontWeight = FontWeights.ExtraBold;
                            //label.Foreground = Brushes.Black;
                        });
                    }
                }
                else
                {
                    Label label = (Label)FindName($"lbMes{index}");
                    if (label != null)
                    {
                        string mes = AlarmList.GetMes(code);
                        this.Dispatcher.Invoke(() =>
                        {
                            string currentTime = DateTime.Now.ToString("HH:mm:ss");
                            string currentTimeString = currentTime.ToString();
                            string newContent = currentTime.ToString() + " : " + mes;

                            label.Content = newContent;
                            label.Background = Brushes.Red;
                            //label.FontWeight = FontWeights.ExtraBold;
                            //label.Foreground = Brushes.Black;
                        });
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Create($"DisplayAlarm PgMain: {ex.Message}");
            }
        }


        #endregion
    }
    public static class PLCMap
    {
        public const string ALARM_01 = "200";

        public const string READ_BT_START = "102";
        public const string READ_BT_STOP = "103";
        public const string READ_BT_HOME = "105";
        public const string READ_BT_RESET = "107";

        public const string WRITE_BT_START = "202";
        public const string WRITE_BT_STOP = "203";
        public const string WRITE_BT_HOME = "205";
        public const string WRITE_BT_RESET = "207";

        public const string READ_RESET = "11";
        public const string READ_MACHINE_RUNING = "10";
    }

    public enum STEP
    {
        ClearAll = 0,
        GetPrdPos = 1,
        GetJigPosPLC = 2,
        SetVisionJob = 3,
        TriggerVision = 4,
        VisionProcess = 5,
        VisionComplete = 6,
        Wait = 7,
    }
    public class DataVsFormat
    {
        public string VisionTime { get; set; }
        public int PrdPos { get; set; }
        public int JigPos { get; set; }
        public byte[] DataImg { get; set; }
        public string QrCode { get; set; }
        public int TotalCam { get; set; }
        public int JudgeResult { get; set; }
        public string MessResult { get; set; }

        public DataVsFormat()
        {
            this.VisionTime = "";
            this.PrdPos = 0;
            this.JigPos = 0;
            this.DataImg = [];
            this.QrCode = "";
            this.TotalCam = 5;
            this.JudgeResult = 1;
            this.MessResult = "";
        }
        public DataVsFormat(string vsTime, int prdPos, int jigPos, byte[] dataImg, int totalCam = 5, string qrCode = "", int judgeResult = 1, string messResult = "")
        {
            this.VisionTime = vsTime;
            this.PrdPos = prdPos;
            this.JigPos = jigPos;
            this.DataImg = dataImg;
            this.QrCode = qrCode;
            this.TotalCam = totalCam;
            this.JudgeResult = judgeResult;
            this.MessResult = messResult;
        }

        public DataVsFormat Clone()
        {
            return new DataVsFormat
            {
                VisionTime = this.VisionTime,
                PrdPos = this.PrdPos,
                JigPos = this.JigPos,
                DataImg = this.DataImg,
                QrCode = this.QrCode,
                TotalCam = this.TotalCam,
                JudgeResult = this.JudgeResult,
                MessResult = this.MessResult,
            };
        }
    }

}
