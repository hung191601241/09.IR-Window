using AutoLaserCuttingInput;
using Development;
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
using VisionTCPClient;
using VisionTools.ToolDesign;
using VisionTools.ToolEdit;
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

        Boolean IsRunning = false;
        Boolean Camera1IsConnect = false;
        //List Result Communication
        private bool READ_VISION_TRIG = false;
        private bool READ_CHANGE_JOB1 = false;
        private bool READ_CHANGE_JOB2 = false;
        private bool READ_CHANGE_JOB3 = false;
        private Thread runThread1;
        private Thread runThread2;
        private bool Flag1 = false;

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

            StartRun();
            CallReadPLC();
            clock.Start();
            LoadMatrixPoint();
        }
        private void PgMainVision_Unloaded(object sender, RoutedEventArgs e)
        {
            clock.Stop();
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
        public bool Get_MACHINE_RUNNING(out bool _value)
        {
            try
            {
                return UiManager.PLC1.device.ReadBit(DeviceCode.M, int.Parse(PLCMap.READ_MACHINE_RUNING), out _value);
            }
            catch (Exception ex)
            {
                _value = false;
                logger.Create(String.Format("READ_MACHINE_RUNNING: " + ex.Message));
                return false;
            }
        }
        public bool Get_BT_START(out bool _value)
        {
            try
            {
                return UiManager.PLC1.device.ReadBit(DeviceCode.M, int.Parse(PLCMap.READ_BT_START), out _value);
            }
            catch (Exception ex)
            {
                _value = false;
                logger.Create(String.Format("READ_BT_START: " + ex.Message));
                return false;
            }
        }
        public bool Get_BT_STOP(out bool _value)
        {
            try
            {
                return UiManager.PLC1.device.ReadBit(DeviceCode.M, int.Parse(PLCMap.READ_BT_STOP), out _value);
            }
            catch (Exception ex)
            {
                _value = false;
                logger.Create(String.Format("READ_BT_STOP: " + ex.Message));
                return false;
            }
        }
        public bool Get_BT_HOME(out bool _value)
        {
            try
            {
                return UiManager.PLC1.device.ReadBit(DeviceCode.M, int.Parse(PLCMap.READ_BT_HOME), out _value);
            }
            catch (Exception ex)
            {
                _value = false;
                logger.Create(String.Format("READ_BT_HOME: " + ex.Message));
                return false;
            }
        }
        public bool Get_BT_RESET(out bool _value)
        {
            try
            {
                return UiManager.PLC1.device.ReadBit(DeviceCode.M, int.Parse(PLCMap.READ_BT_RESET), out _value);
            }
            catch (Exception ex)
            {
                _value = false;
                logger.Create(String.Format("READ_BT_RESET: " + ex.Message));
                return false;
            }
        }
        public bool Get_RESET(out bool _value)
        {
            try
            {
                return UiManager.PLC1.device.ReadBit(DeviceCode.M, int.Parse(PLCMap.READ_RESET), out _value);
            }
            catch (Exception ex)
            {
                _value = false;
                logger.Create(String.Format("READ_BT_RESET: " + ex.Message));
                return false;
            }
        }
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
        private bool SetTriggerVision(DeviceCode devIn, string addrIn, bool _value)
        {
            try
            {
                return UiManager.PLC1.device.WriteBit(devIn, int.Parse(addrIn), _value);
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("WRITE_VISION_TRIG: " + ex.Message));
                return false;
            }
        }
        private bool GetChangeJob(out int _value)
        {
            try
            {
                return UiManager.PLC1.device.ReadWord(UiManager.appSettings.commProperty.selectDevJob, int.Parse(UiManager.appSettings.commProperty.addrJob), out _value);
            }
            catch (Exception ex)
            {
                _value = 0;
                logger.Create(String.Format("GET_CHANGE_JOB: " + ex.Message));
                return false;
            }
        }
        private bool GetJigPos(out int _value)
        {
            try
            {
                return UiManager.PLC1.device.ReadWord(UiManager.appSettings.commProperty.selectDevJigPos, int.Parse(UiManager.appSettings.commProperty.addrJigPos), out _value);
            }
            catch (Exception ex)
            {
                _value = 0;
                logger.Create(String.Format("GET_JIG_POS: " + ex.Message));
                return false;
            }
        }
        private bool GetBitClrAll(out bool _value)
        {
            try
            {
                return UiManager.PLC1.device.ReadBit(DeviceCode.M, 540, out _value);
            }
            catch (Exception ex)
            {
                _value = false;
                logger.Create(String.Format("READ_CLEAR_ALL: " + ex.Message));
                return false;
            }
        }
        private bool SetBitClrAllOK(bool _value)
        {
            try
            {
                return UiManager.PLC1.device.WriteBit(DeviceCode.M, 640, _value);
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("WRITE_CLEAR_ALL_OK: " + ex.Message));
                return false;
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
        private bool SendVisionResult(DeviceCode dev, string addr, int value)
        {
            try
            {
                return UiManager.PLC1.device.WriteWord(dev, int.Parse(addr), value);
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("WRITE_VISION_RESULT: " + ex.Message));
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
        private void CallReadPLC()
        {
            try
            {
                runThread2 = new Thread(ReadPLC);
                runThread2.IsBackground = true;
                runThread2.Start();
            }
            catch (Exception ex)
            {
                logger.Create("Start thread Read PLC Err : " + ex.ToString());
            }
        }
        private void ReadPLC()
        {
            try
            {

                if (UiManager.PLC1.device.isOpen())
                {
                    //Get_MACHINE_RUNNING(out READ_MACHINE_RUNNING);
                    //Get_BT_START(out bool Read_bt_Start);
                    //Get_BT_STOP(out bool Read_bt_Stop);
                    //Get_BT_HOME(out bool Read_bt_Home);
                    //Get_RESET(out READ_LAMP_RESET);
                    //Get_BT_RESET(out bool Read_bt_Reset);


                    GetAlarm01(out READ_ALARM_01);
                    this.Dispatcher.Invoke(() =>
                    {

                        //this.btnMainStart.Background = new SolidColorBrush(Read_bt_Start ? Colo_ON : Colo_OFF);
                        //this.btnMainStop.Background = new SolidColorBrush(Read_bt_Stop ? Colo_ON : Colo_OFF);
                        //this.btnMainHome.Background = new SolidColorBrush(Read_bt_Home ? Colo_ON : Colo_OFF);
                        //this.btnMainReset.Background = new SolidColorBrush(Read_bt_Reset ? Colo_ON : Colo_OFF);

                        //this.lbl_status.Background = new SolidColorBrush(READ_MACHINE_RUNNING ? Colo_ON : Colo_OFF1);
                        //this.lbl_status.Content = READ_MACHINE_RUNNING ? "MACHINE RUN" : "MACHINE STOP";

                    });
                    UpdateError();
                }
                Thread.Sleep(10);
                CallReadPLC();
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
                    ClearAll();
                    Step = STEP.GetPrdPos;
                }
                catch (Exception ex)
                {
                    logger.Create("btStart.click error: " + ex.Message);
                }
            }
        }
        private void RunThread()
        {
            CallThreadStart();
        }
        private void CallThreadStart()
        {
            try
            {
                runThread1 = new Thread(RunManager);
                runThread1.IsBackground = true;
                runThread1.Start();
            }
            catch (Exception ex)
            {
                logger.Create("Start thread Auto loop Err : " + ex.ToString());
            }
        }
        private bool ResetJobPLC()
        {
            bool ret = true;
            try
            {
                ret &= UiManager.PLC1.device.WriteWord(UiManager.appSettings.commProperty.selectDevJob, int.Parse(UiManager.appSettings.commProperty.addrJob), 0);
                ret &= UiManager.PLC1.device.WriteWord(UiManager.appSettings.commProperty.selectDevJigPos, int.Parse(UiManager.appSettings.commProperty.addrJigPos), 0);
                if (ret)
                {
                    AddLog($"Product Point: {UiManager.appSettings.commProperty.selectDevJob}{UiManager.appSettings.commProperty.addrJob} = 0");
                    AddLog($"Jig Point: {UiManager.appSettings.commProperty.selectDevJigPos}{UiManager.appSettings.commProperty.addrJigPos} = 0");
                    AddLog("----------------------");
                }
                else
                {
                    AddLog($"Clear Data Error!");
                    AddLog($"Check Connect to PLC!");
                    AddLog("----------------------");
                }    
                    
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("Reset Change Job Device in PLC Error: " + ex.Message));
                return false;
            }
            return ret;
        }

        private bool vsComplete = false;
        private int idxOldJob = 0, prdPos = 0, jigPosPLC = 0;
        private int countImg = 0;
        private List<int> JigPosLst = new List<int>();
        private DataVsFormat[] DataVsBuffs = new DataVsFormat[5];
        private List<DataVsFormat> DataVsSearches = new List<DataVsFormat>();
        private STEP Step = 0;
        private async void RunManager()
        {
            try
            {
                //Kiểm tra bit Clear từ PLC ở đầu mỗi vòng lặp
                if (GetBitClrAll(out bool res) && res)
                {
                    Step = STEP.ClearAll;
                }    
                switch (Step)
                {
                    case STEP.ClearAll:
                        ClearAll();
                        AddLog("CLEAR ALL!");
                        if(SetBitClrAllOK(false))
                        {
                            Step = STEP.Wait;
                        }    
                        break;
                    case STEP.Wait:
                        if (GetChangeJob(out prdPos) && prdPos != 0)
                        {
                            DataVsSearches.Clear();
                            GC.Collect();
                            Step = STEP.GetPrdPos;
                        }
                        break;
                    case STEP.GetPrdPos:
                        if (GetChangeJob(out prdPos) && prdPos != 0)
                        {
                            if (idxOldJob != prdPos)
                            {
                                Step = STEP.SetVisionJob;
                                this.Dispatcher.Invoke(() =>
                                {
                                    foreach (var ele in gridView.Children)
                                    {
                                        (ele as Border).Background = (Brush)new BrushConverter().ConvertFromString("#fff8dc");
                                    }
                                });
                            }    
                            else
                                Step = STEP.GetJigPosPLC;
                        }
                        break;
                    case STEP.GetJigPosPLC:
                        AddLog($"Check Point: {UiManager.appSettings.commProperty.selectDevJob}{UiManager.appSettings.commProperty.addrJob} = {prdPos}");
                        if(GetJigPos(out jigPosPLC) && jigPosPLC != 0)
                        {
                            AddLog($"Jig Pos: {UiManager.appSettings.commProperty.selectDevJigPos}{UiManager.appSettings.commProperty.addrJigPos} = {jigPosPLC}");
                            CreateImageBuff();
                            ClearImageView();
                            Step = STEP.TriggerVision;
                        }
                        break;
                    case STEP.SetVisionJob:
                        SetVisionJob();
                        Step = STEP.GetJigPosPLC;
                        break;
                    case STEP.TriggerVision:
                        if (pgCamera.curVsProgram.vsProgramNs.Count == 0)
                        {
                            AddLog("Have no VP in Current Job");
                            Step = STEP.VisionComplete;
                        }
                        //Quét qua tất cả Input Trigger
                        for (int i = 0; i < pgCamera.curVsProgram.vsProgramNs.Count; i++)
                        {
                            DeviceCode devIn = pgCamera.curVsProgram.vsProgramNs[i].selectDevIn;
                            string addrIn = pgCamera.curVsProgram.vsProgramNs[i].addrIn;
                            GetTriggerVision(devIn, addrIn, out READ_VISION_TRIG);
                            if (READ_VISION_TRIG && !Flag1)
                            {
                                AddLog($"TRIGGER: {devIn}{addrIn} = ON");
                                vsComplete = false;
                                Flag1 = true;
                                await Task.Run(() =>
                                {
                                    this.RunManagerVision_S26(i);
                                    Flag1 = false;
                                    countImg++;
                                });
                                while (!vsComplete) ;
                                //Clear Bit Trigger Vision
                                if (SetTriggerVision(devIn, addrIn, false))
                                    AddLog($"TRIGGER: {devIn}{addrIn} = OFF");
                                //Clear thanh ghi chứa giá trị Check Point trên PLC khi nhận đủ 5 ảnh
                                if (countImg == JigPosLst.Count)
                                {
                                    Step = STEP.VisionComplete;
                                }
                            }
                        }
                        break;
                    case STEP.VisionComplete:
                        DATACheck DataCheck = await GetDataVision(DataVsBuffs);
                        var Ready = await UiManager.MesVsService.SendReady(DataCheck);
                        if (!Ready)
                        {
                            AddLog("Connect Fail. Server no Responding");
                            goto EndMesVs;
                        }
                        bool SendData = await UiManager.MesVsService.SendDataVision(DataCheck);
                        if (SendData)
                        {
                            AddLog("Data sent successfully.");
                        }
                        else
                        {
                            AddLog("Data sent Fail.");
                        }
                    EndMesVs:
                        Thread.Sleep(1000);
                        if (GetBitClrAll(out bool val) && val)
                            Step = STEP.ClearAll;
                        else
                        {
                            ResetJobPLC();
                            countImg = 0;
                            Step = STEP.GetPrdPos;
                        }    
                        break;
                }    
                Thread.Sleep(10);
                CallThreadStart();
            }
            catch (Exception ex)
            {
                logger.Create($"Auto Run Manager Error : +{ex}");
                Thread.Sleep(10);
                CallThreadStart();
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
        private void RunManagerVision_IRWindow(int idxVPn = 0)
        {
            //Gửi kết quả sau khi chụp xong
            Dispatcher.Invoke(() =>
            {
                pgCamera.RunTreeToolVP(pgCamera.toolAreaGrs[idxVPn]);
                Flag1 = false;
            });

            //Gửi kết quả Segment Neuro sau khi xử lý xong tất cả vị trí
            //Dispatcher.Invoke(() =>
            //{
            //    OutSegNrResultDesign outSegNrResultDesign = pgCamera.toolAreaGrs[idxVPn].ToolAreaSubs[0].Children.OfType<OutSegNrResultDesign>().FirstOrDefault();
            //    if (outSegNrResultDesign == null)
            //        return;
            //    OutSegNrResultEdit outSegEdit = outSegNrResultDesign.toolEdit;
            //    pgCamera.RunTreeToolVP(pgCamera.toolAreaGrs[idxVPn]);
            //    //Send OK/NG Signal
            //    if (outSegEdit.ResultOut.Count == outSegEdit.NumberPos)
            //    {
            //        for (int i = 0; i < outSegNrResultDesign.toolEdit.ResultOut.Count; i++)
            //        {
            //            String2Enum(outSegNrResultDesign.toolEdit.ResultOut[i].Key, out DeviceCode devCode, out string devNo);
            //            SendVisionResult(devCode, devNo, outSegEdit.ResultOut[i].Value);
            //        }
            //    }
            //});
            // Chạy phần xử lý ảnh ở luồng khác
            Task.Run(() =>
            {
                if (pgCamera.curVsProgram == UiManager.appSettings.vsPrograms[0])
                {
                    Dispatcher.Invoke(() =>
                    {
                        //OutSegNrResultDesign outResDesign = pgCamera.QueryOutBlobResultTool(pgCamera.cbxDisplayResult.Items[idxVPn] as String) as OutSegNrResultDesign;
                        OutSegNeuroResTool outResDesign = pgCamera.QueryOutBlobResTool(pgCamera.cbxDisplayResult.Items[1] as String) as OutSegNeuroResTool;
                        if (outResDesign == null)
                        {
                            //MessageBox.Show("Can't find any OutResult match!");
                            //Flag1 = false;
                            return;
                        }
                        switch (idxVPn)
                        {
                            case 0:
                                imgView1.Source = outResDesign.toolEdit.InputImage.Mat.ToBitmapSource();
                                break;
                            case 1:
                            case 2:
                            case 3:
                                break;
                        }
                    });
                }
            });
        }
        private void RunManagerVision_S26(int idxVPn = 0)
        {
            //Gửi kết quả sau khi chụp xong
            Dispatcher.Invoke(() =>
            {
                pgCamera.RunTreeToolVP(pgCamera.toolAreaGrs[idxVPn]);
            });
            // Chạy phần xử lý ảnh ở luồng khác
            Task.Run(() =>
            {
                int index = idxVPn;
                Dispatcher.Invoke(() =>
                {
                    OutVidiCogResTool outVidiCogResTool = pgCamera.toolAreaGrs[index].ToolAreaMain.Children.OfType<OutVidiCogResTool>().FirstOrDefault();
                    if (outVidiCogResTool == null)
                        return;
                    OutVidiCogResEdit toolEdit = outVidiCogResTool.toolEdit;
                    if (toolEdit.OutputImage.Mat == null || toolEdit.OutputImage.Mat.Width == 0 || toolEdit.OutputImage.Mat.Height == 0)
                    {
                        AddLog("Image Capture NULL!");
                        return;
                    }    
                    //Cv2.ImEncode(".jpg", toolEdit.OutputImage.Mat, out byte[] imageBytes);
                    byte[] imageBytes = CompressJpeg(toolEdit.OutputImage.Mat, 0.5);
                    DataVsFormat dataVs = new();
                    if (pgCamera.curVsProgram.NameDisp.ToLower().Contains("qr code"))
                    {
                        dataVs = new DataVsFormat(prdPos, JigPosLst[index], imageBytes, JigPosLst.Count, toolEdit.StrResult);
                    }
                    else
                    {
                        dataVs = new DataVsFormat(prdPos, JigPosLst[index], imageBytes, JigPosLst.Count, "", toolEdit.JudgeVal, toolEdit.StrResult);
                    }
                    DataVsSearches.Add(dataVs);
                    DataVsBuffs[index] = dataVs;
                    vsComplete = true;
                    switch (index)
                    {
                        case 0:
                            imgView1.Source = toolEdit.OutputImage.Mat.ToBitmapSource();
                            break;
                        case 1:
                            imgView2.Source = toolEdit.OutputImage.Mat.ToBitmapSource();
                            break;
                        case 2:
                            imgView3.Source = toolEdit.OutputImage.Mat.ToBitmapSource();
                            break;
                        case 3:
                            imgView4.Source = toolEdit.OutputImage.Mat.ToBitmapSource();
                            break;
                        case 4:
                            imgView5.Source = toolEdit.OutputImage.Mat.ToBitmapSource();
                            break;
                    }
                    Border bd = gridView.Children.OfType<Border>().FirstOrDefault(b => b.Name == $"bd_{JigPosLst[index]}");
                    if (bd == null) return;
                    bd.Background = (Brush)new BrushConverter().ConvertFromString("#FFB7E4FF");
                });
            });
        }

        private void SetVisionJob()
        {
            Dispatcher.Invoke(() =>
            {
                switch (prdPos)
                {
                    //QR Code
                    case 1:
                        pgCamera.curVsProgram = UiManager.appSettings.vsPrograms[4];
                        break;
                    //Point 1
                    case 2:
                        pgCamera.curVsProgram = UiManager.appSettings.vsPrograms[0];
                        break;
                    //Point 2
                    case 3:
                        pgCamera.curVsProgram = UiManager.appSettings.vsPrograms[1];
                        break;
                    //Point 3
                    case 4:
                        pgCamera.curVsProgram = UiManager.appSettings.vsPrograms[2];
                        break;
                    //Point 4
                    case 5:
                        pgCamera.curVsProgram = UiManager.appSettings.vsPrograms[3];
                        break;
                    default:
                        return;
                }
                pgCamera.LoadJob();
                idxOldJob = prdPos;
                AddLog($"*** {pgCamera.curVsProgram.NameDisp.ToUpper()} ***");
            });
        }
        private void CreateImageBuff()
        {
            var matrixPoint = UiManager.appSettings.commProperty.mtrxPoint;
            int targetRow = 0;
            int startCol = 0;
            switch (jigPosPLC)
            {
                case 1: targetRow = 0; startCol = 0; break; // r1c1, r1c3...
                case 2: targetRow = 0; startCol = 1; break; // r1c2, r1c4...
                case 3: targetRow = 1; startCol = 0; break; // r2c1, r2c3...
                case 4: targetRow = 1; startCol = 1; break; // r2c2, r2c4...
            }
            JigPosLst.Clear();
            string str = "";
            foreach (var kv in matrixPoint)
            {
                if (kv.Value.Y == targetRow && (kv.Value.X % 2 == startCol % 2))
                {
                    str += kv.Key + " ";
                    JigPosLst.Add(kv.Key);
                }
            }
            AddLog("Point: " + str);
        }
        private void LoadMatrixPoint()
        {
            //Hiển thị ma trận
            var matrixPoint = UiManager.appSettings.commProperty.mtrxPoint;
            if (matrixPoint == null) 
                return;
            int rows = matrixPoint.Where(k => k.Value.X == 1).Count();
            int cols = matrixPoint.Where(k => k.Value.Y == 1).Count();
            AddLog($"Matrix: {rows}x{cols}");

            DataVsSearches.Clear();
            gridView.Children.Clear();
            gridView.ColumnDefinitions.Clear();
            gridView.RowDefinitions.Clear();
            for (int row = 0; row < rows; row++)
            {
                string str = "";
                for (int col = 0; col < cols; col++)
                {
                    int val = matrixPoint.First(kv => kv.Value.X == col && kv.Value.Y == row).Key;
                    str += val.ToString().PadLeft(3);
                    if (row != 0)
                        continue;
                    gridView.ColumnDefinitions.Add(new ColumnDefinition());
                }
                AddLog(str);
                gridView.RowDefinitions.Add(new RowDefinition());
            }

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    Label lb = new Label
                    {
                        Content = matrixPoint.First(k => k.Value.X == col && k.Value.Y == row).Key,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        FontSize = 20, 
                        FontWeight = FontWeights.Bold,
                    };
                    Border bdBound = new Border
                    {
                        Child = lb,
                        Name = "bd_" + matrixPoint.First(k => k.Value.X == col && k.Value.Y == row).Key.ToString(),

                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0.5),
                    };
                    bdBound.MouseMove += BdBound_MouseMove;
                    bdBound.MouseLeftButtonDown += BdBound_MouseLeftButtonDown;

                    gridView.Children.Add(bdBound);
                    Grid.SetRow(bdBound, row);
                    Grid.SetColumn(bdBound, col);
                }
            }
        }
        private void ClearAll()
        {
            try
            {
                ResetJobPLC();
                countImg = 0;
            }
            catch (Exception ex)
            {
                logger.Create($"Clear All Error : {ex}");
            } 
            
        }
        private void ClearImageView()
        {
            this.Dispatcher.Invoke(() =>
            {
                imgView1.Source = null;
                imgView2.Source = null;
                imgView3.Source = null;
                imgView4.Source = null;
                imgView5.Source = null;
            });
        }
        private BitmapImage ByteArrToBitmapImg(byte[] imageBytes)
        {
            using (var ms = new MemoryStream(imageBytes))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze(); // Quan trọng nếu dùng trong thread khác
                return bitmap;
            }
        }
        private byte[] CompressJpeg(Mat mat, double quality)
        {
            // Clamp quality từ 0–1 → 0–100
            int jpegQuality = Math.Min(100, Math.Max(0, (int)(quality * 100)));
            // Thiết lập thông số nén JPEG
            var parameters = new ImageEncodingParam(ImwriteFlags.JpegQuality, jpegQuality);
            // Nén ảnh Mat thành byte[] JPEG
            return mat.ImEncode(".jpg", parameters);
        }
        private string ConvertImageToBase64(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return "";
            return $"data:image/jpeg;base64,{Convert.ToBase64String(imageBytes)}";
        }
        private string ConvertStrJudge(int judge)
        {
            string strJudge = "";
            switch (judge)
            {
                case 1:
                    strJudge = "OK";
                    break;
                case 2:
                    strJudge = "NG";
                    break;
                case 3:
                    strJudge = "Empty";
                    break;
            }    
            return strJudge;
        }
        private async Task<DATACheck> GetDataVision(DataVsFormat[] dataVsBuffs)
        {
            DATACheck DataCheck = new DATACheck();

            DataCheck.EquipmentId = "DATAVISION";
            DataCheck.Status = "AUTO10";
            int dataCount = dataVsBuffs.Length;
            DataCheck.FormatVision.DATA_PCB_CAM_01 = DateTime.Now.ToString("yyyy-MM HH:mm:ss");
            DataCheck.FormatVision.INDEX_PCB_CAM_01 = dataVsBuffs[0].JigPos.ToString();
            DataCheck.FormatVision.BARCODE_PCB_CAM_01 = dataVsBuffs[0].QrCode;
            DataCheck.FormatVision.LIST_PCB_CAM_01 = dataVsBuffs[0].TotalCam.ToString();
            DataCheck.FormatVision.NUMBER_IN_LIST_PCB_CAM_01 = dataVsBuffs[0].PrdPos.ToString();
            DataCheck.FormatVision.RESULT_PCB_CAM_01 = ConvertStrJudge(dataVsBuffs[0].JudgeResult);
            DataCheck.FormatVision.MESSENGER_PCB_CAM_01 = dataVsBuffs[0].MessResult;

            DataCheck.FormatVision.DATA_PCB_CAM_02 = DateTime.Now.ToString("yyyy-MM HH:mm:ss");
            DataCheck.FormatVision.INDEX_PCB_CAM_02 = dataVsBuffs[1].JigPos.ToString();
            DataCheck.FormatVision.BARCODE_PCB_CAM_02 = dataVsBuffs[1].QrCode;
            DataCheck.FormatVision.LIST_PCB_CAM_02 = dataVsBuffs[1].TotalCam.ToString();
            DataCheck.FormatVision.NUMBER_IN_LIST_PCB_CAM_02 = dataVsBuffs[1].PrdPos.ToString();
            DataCheck.FormatVision.RESULT_PCB_CAM_02 = ConvertStrJudge(dataVsBuffs[1].JudgeResult);
            DataCheck.FormatVision.MESSENGER_PCB_CAM_02 = dataVsBuffs[1].MessResult;

            DataCheck.FormatVision.DATA_PCB_CAM_03 = DateTime.Now.ToString("yyyy-MM HH:mm:ss");
            DataCheck.FormatVision.INDEX_PCB_CAM_03 = dataVsBuffs[2].JigPos.ToString();
            DataCheck.FormatVision.BARCODE_PCB_CAM_03 = dataVsBuffs[2].QrCode;
            DataCheck.FormatVision.LIST_PCB_CAM_03 = dataVsBuffs[2].TotalCam.ToString();
            DataCheck.FormatVision.NUMBER_IN_LIST_PCB_CAM_03 = dataVsBuffs[2].PrdPos.ToString();
            DataCheck.FormatVision.RESULT_PCB_CAM_03 = ConvertStrJudge(dataVsBuffs[1].JudgeResult);
            DataCheck.FormatVision.MESSENGER_PCB_CAM_03 = dataVsBuffs[2].MessResult;

            DataCheck.FormatVision.DATA_PCB_CAM_04 = DateTime.Now.ToString("yyyy-MM HH:mm:ss");
            DataCheck.FormatVision.INDEX_PCB_CAM_04 = dataVsBuffs[3].JigPos.ToString();
            DataCheck.FormatVision.BARCODE_PCB_CAM_04 = dataVsBuffs[3].QrCode;
            DataCheck.FormatVision.LIST_PCB_CAM_04 = dataVsBuffs[3].TotalCam.ToString();
            DataCheck.FormatVision.NUMBER_IN_LIST_PCB_CAM_04 = dataVsBuffs[3].PrdPos.ToString();
            DataCheck.FormatVision.RESULT_PCB_CAM_04 = ConvertStrJudge(dataVsBuffs[1].JudgeResult);
            DataCheck.FormatVision.MESSENGER_PCB_CAM_04 = dataVsBuffs[3].MessResult;

            DataCheck.FormatVision.DATA_PCB_CAM_05 = DateTime.Now.ToString("yyyy-MM HH:mm:ss");
            DataCheck.FormatVision.INDEX_PCB_CAM_05 = dataVsBuffs[4].JigPos.ToString();
            DataCheck.FormatVision.BARCODE_PCB_CAM_05 = dataVsBuffs[4].QrCode;
            DataCheck.FormatVision.LIST_PCB_CAM_05 = dataVsBuffs[4].TotalCam.ToString();
            DataCheck.FormatVision.NUMBER_IN_LIST_PCB_CAM_05 = dataVsBuffs[4].PrdPos.ToString();
            DataCheck.FormatVision.RESULT_PCB_CAM_05 = ConvertStrJudge(dataVsBuffs[1].JudgeResult);
            DataCheck.FormatVision.MESSENGER_PCB_CAM_05 = dataVsBuffs[4].MessResult;

            try
            {
                // Chạy các tác vụ ConvertImageToBase64 song song
                Task<string>[] tasks = new Task<string>[]
                {
                    Task.Run(() => ConvertImageToBase64(dataVsBuffs[0].DataImg)),
                    Task.Run(() => ConvertImageToBase64(dataVsBuffs[1].DataImg)),
                    Task.Run(() => ConvertImageToBase64(dataVsBuffs[2].DataImg)),
                    Task.Run(() => ConvertImageToBase64(dataVsBuffs[3].DataImg)),
                    Task.Run(() => ConvertImageToBase64(dataVsBuffs[4].DataImg))
                };


                string[] results = await Task.WhenAll(tasks);


                DataCheck.FormatVision.IMAGE_PCB_CAM_01 = results[0];
                DataCheck.FormatVision.IMAGE_PCB_CAM_02 = results[1];
                DataCheck.FormatVision.IMAGE_PCB_CAM_03 = results[2];
                DataCheck.FormatVision.IMAGE_PCB_CAM_04 = results[3];
                DataCheck.FormatVision.IMAGE_PCB_CAM_05 = results[4];
            }
            catch (Exception ex)
            {
                DataCheck.FormatVision.IMAGE_PCB_CAM_01 = "";
                DataCheck.FormatVision.IMAGE_PCB_CAM_02 = "";
                DataCheck.FormatVision.IMAGE_PCB_CAM_03 = "";
                DataCheck.FormatVision.IMAGE_PCB_CAM_04 = "";
                DataCheck.FormatVision.IMAGE_PCB_CAM_05 = "";
            }

            return DataCheck;
        }

        private void BdBound_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Border bdSelected = sender as Border;
            SelectBorder(bdSelected);
        }
        private void BdBound_MouseMove(object sender, MouseEventArgs e)
        {
            Border bdSelected = sender as Border;
            List<Border> bdLst = gridView.Children.OfType<Border>().ToList();
            foreach (var bd in bdLst)
            {
                if (bd.BorderBrush == Brushes.Red)
                    continue;
                bd.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC");
            }
            if (bdSelected.BorderBrush != Brushes.Red)
                bdSelected.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF489E37");
        }
        private void SelectBorder(Border bdSelected)
        {
            try
            {
                List<Border> bdLst = gridView.Children.OfType<Border>().ToList();
                bdLst.ForEach(border => border.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC"));
                bdSelected.BorderBrush = Brushes.Red;

                string[] bdNameSpl = bdSelected.Name.Split('_');
                if (bdNameSpl.Length <= 1) return;
                int jigPos = int.Parse(bdNameSpl[1]);

                tblJigPos.Text = jigPos.ToString();
                stImgLink.Children.Clear();
                for(int pt = 1; pt <= 5; pt++)
                {
                    TextBlock tbl = new TextBlock
                    {
                        Text = (pt <= UiManager.appSettings.vsPrograms.Length) ? UiManager.appSettings.vsPrograms[pt - 1].NameDisp : "NULL",
                        Name = $"tb_{pt}_{jigPos}",
                        Padding = new Thickness(5, 3, 0, 0),
                        FontSize = 13,
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic,
                        TextDecorations = TextDecorations.Underline,
                        Cursor = Cursors.Hand,
                    };
                    foreach(var dataVs in DataVsSearches)
                    {
                        if(pt == dataVs.PrdPos && jigPos == dataVs.JigPos)
                        {
                            tbl.Foreground = dataVs.JudgeResult switch
                            {
                                1 => (Brush)new BrushConverter().ConvertFromString("#FF489E37"),
                                2 => (Brush)new BrushConverter().ConvertFromString("#FFE90E0E"),
                                _ => Brushes.Gray,
                            };
                            break;
                        }    
                    } 
                    tbl.MouseLeftButtonDown += Tbl_MouseLeftButtonDown;
                    stImgLink.Children.Add(tbl);
                }    
            }
            catch (Exception ex)
            {
                logger.Create("Select Image Log Error: " + ex.Message, ex);
            }
        }
        private void Tbl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock tbl = sender as TextBlock;
            string[] tblNameSpl = tbl.Name.Split('_');
            if (tblNameSpl.Length <= 2) return;

            int pointSp = int.Parse(tblNameSpl[1]);
            int jigPos = int.Parse(tblNameSpl[2]);

            DataVsFormat dataVs;
            dataVs = DataVsSearches.FirstOrDefault(d => (d.PrdPos == pointSp && d.JigPos == jigPos));
            if(dataVs == null) return;  

            switch ((jigPos - 1) % 5)
            {
                case 0:
                    imgView1.Source = ByteArrToBitmapImg(dataVs.DataImg);
                    break;
                case 1:
                    imgView2.Source = ByteArrToBitmapImg(dataVs.DataImg);
                    break;
                case 2:
                    imgView3.Source = ByteArrToBitmapImg(dataVs.DataImg);
                    break;
                case 3:
                    imgView4.Source = ByteArrToBitmapImg(dataVs.DataImg);
                    break;
                case 4:
                    imgView5.Source = ByteArrToBitmapImg(dataVs.DataImg);
                    break;
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
                        if (LogEntries.Count > 300)
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
        public int PrdPos { get; set; }
        public int JigPos { get; set; }
        public byte[] DataImg { get; set; }
        public string QrCode {  get; set; }
        public int TotalCam { get; set; }
        public int JudgeResult { get; set; }
        public string MessResult { get; set; }

        public DataVsFormat()
        {
            this.PrdPos = 0;
            this.JigPos = 0;
            this.DataImg = [];
            this.QrCode = "";
            this.TotalCam = 5;
            this.JudgeResult = 0;
            this.MessResult = "";
        }
        public DataVsFormat(int prdPos, int jigPos, byte[] dataImg, int totalCam = 5, string qrCode = "", int judgeResult = 1, string messResult = "")
        {
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
