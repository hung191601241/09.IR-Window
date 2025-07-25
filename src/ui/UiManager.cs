using AutoLaserCuttingInput;
using AutoLaserCuttingInput.src.ui;
using Cognex.VisionPro;
using Cognex.VisionPro.ToolBlock;
using Development;
using ITM_Semiconductor;
using MvCamCtrl.NET;
using MVSDK_Net;
using nrt;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using VisionTCPClient;
using static MvCamCtrl.NET.MyCamera;
using static MVSDK_Net.IMVDefine;
using IMVCamera = MVSDK_Net.MyCamera;
using MVSCamera = MvCamCtrl.NET.MyCamera;


namespace VisionInspection
{
    public enum PAGE_ID
    {
        PAGE_MAIN_ID = 0,
        PAGE_MAIN_VISION,

        PAGE_MENU_ID,
        PAGE_MENU_TEACHING_ID,
        PAGE_MENU_TEACHING_ID1,
        PAGE_MENU_TEACHING_ID2,

        PAGE_SYSTEM_MENU,
        PAGE_SYSTEM_MENU_SYSTEM_MACHINE,


        PAGE_MENNU_SYSTEM_VISUAL,

        PAGE_MENU_MECHANICAL_PLC,
        PAGE_MENU_MECHANICAL_BARCODE1,
        PAGE_MENU_MECHANICAL_BARCODE2,
        PAGE_MENU_MECHANICAL_MES,

        PAGE_MENU_MANUAL_ID,
        PAGE_MENU_MANUALCH2_ID,
        PAGE_MENU_STATUS_LOG,
        PAGE_MENU_STATUS_SPC_OUTPUT,
        PAGE_MENU_STATUS_SPC_SEARCH,
        PAGE_MENU_SAVE_ID,
        PAGE_MENU_LOAD_ID,

        PAGE_SUPER_USER_MENU,
        PAGE_SUPER_USER_MENU_DELAY_MACHINE,
        PAGE_SUPER_USER_MENU_SETTING_ALARM,
        PAGE_SUPER_USER_MENU_SETTING_SERVO,



        PAGE_IO_ID,
        PAGE_LAST_JAM_ID,
        PAGE_CAMERA_SETTING,
        PAGE_RECIPE,
        PAGE_DEEPLEARNING
    };

    class UiManager
    {
        private const String APP_SETTINGS_FILE_NAME = "app_settings.json";
        private static MyLogger logger = new MyLogger("UiManager");

        public static AppSettings appSettings { get; set; }
        public static Hashtable pageTable = new Hashtable();
        private static WndMain wndMain;

        public static ScannerTCP Scanner1;
        public static ScannerTCP Scanner2;


        private static Object LockerPLC = new object();

        public const String INPUT_IO_FILE_NAME = "input_machine.ini";
        public static String[] SectionNameInput;
        public static INIFile fileInput;

        public const String OUTPUT_IO_FILE_NAME = "output_machine.ini";
        public static String[] SectionNameOutput;
        public static INIFile fileOutput;

        //Camera
        public static IraypleCam IrpCamera = new IraypleCam();
        public static List<IntPtr> CamDevList = new List<IntPtr>();
        public static List<ICamDevice> CamList = new List<ICamDevice> ();
        public static DeviceList DevList = new DeviceList ();

        //hiep sửa
        public static SelectDevice PLC1;
        public static MESVisionService MesVsService;

        public static void Startup() 
        {
            logger.Create("Startup:");
            try
            {

                // Load global settings:
                LoadAppSettings(APP_SETTINGS_FILE_NAME);
                if (appSettings == null)
                {
                    appSettings = new AppSettings();
                }
                // hiệp sửa
                ConnectPLC();
                ConnectMesVs();

                // Create Database if not existed
                Dba.createDatabaseIfNotExisted();

                //Connect Camera
                CameraListAcq();

                // Create Main window:
                wndMain = new WndMain();

                // Create all pages and add to the local table:
                InitPages();

                // Start Main window:
                wndMain.mainContent.NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden;
                wndMain.Show();
                // Load File I/O INI Of Machine
                loadFileINIIO_INPUT(INPUT_IO_FILE_NAME);
                loadFileINIIO_OUTPUT(OUTPUT_IO_FILE_NAME);

                // Load Alarm 
                AlarmList.LoadAlarm();
            }
            catch (Exception ex)
            {
                logger.Create("Startup error:" + ex.Message);
            }
        }

        #region CAMERA CONTROL

        public static void CameraListAcq()
        {
            CamDevList.Clear();
            CamList.Clear();
            IrpCamera.DeviceListAcq();
            DevList = IrpCamera.deviceList;

            for(int i = 0; i < DevList.devNum; i++)
            {
                IMVDefine.IMV_DeviceInfo DeviceInfo = Marshal.PtrToStructure<IMV_DeviceInfo>(DevList.devInfo[i]);
                switch(DeviceInfo.vendorName)
                {
                    case "GEV":
                    case "Hikvision":
                    case "Hikrobot":
                        HikCam hikCam1 = new HikCam();
                        hikCam1.DeviceListAcq();
                        for(int j = 0; j < hikCam1.deviceList.devNum; j++)
                        {
                            MVSCamera.MV_CC_DEVICE_INFO device = Marshal.PtrToStructure<MVSCamera.MV_CC_DEVICE_INFO>(hikCam1.deviceList.devInfo[j]);
                            if(device.nTLayerType == MVSCamera.MV_GIGE_DEVICE)
                            {
                                IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
                                MV_GIGE_DEVICE_INFO gigeInfo = Marshal.PtrToStructure<MV_GIGE_DEVICE_INFO>(buffer);
                                if(gigeInfo.chSerialNumber == DeviceInfo.serialNumber)
                                {
                                    HikCam hikCamera = new HikCam(device);
                                    CamList.Add(hikCamera);
                                    CamDevList.Add(hikCam1.deviceList.devInfo[j]);
                                }    
                            }
                            else if (device.nTLayerType == MVSCamera.MV_USB_DEVICE)
                            {
                                IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stUsb3VInfo, 0);
                                MV_USB3_DEVICE_INFO usbInfo = Marshal.PtrToStructure<MV_USB3_DEVICE_INFO>(buffer);
                                if (usbInfo.chSerialNumber == DeviceInfo.serialNumber)
                                {
                                    HikCam hikCamera = new HikCam(device);
                                    CamList.Add(hikCamera);
                                    CamDevList.Add(hikCam1.deviceList.devInfo[j]);
                                }
                            }
                        }    
                        break;
                    case "Basler":
                    case "Huaray Technology":
                        IraypleCam irpCamera = new IraypleCam(DeviceInfo);
                        CamList.Add(irpCamera);
                        CamDevList.Add(DevList.devInfo[i]);
                        break;
                }    
            }    
        }

        public static void ConectCamera(ICamDevice camera)
        {
            int ret = camera.Open();
            camera.SetExposeTime((int)UiManager.appSettings.connection.camera1.ExposeTime);
            Thread.Sleep(2);
            if (ret == IMVDefine.IMV_OK)
            {
                //return true;
            }
            else
            {
                //return false;
            }

        }

        #endregion
        public static void SwitchPage(PAGE_ID pgId) {
            if (pageTable.ContainsKey(pgId)) {
                var pg = (System.Windows.Controls.Page)pageTable[pgId];
                wndMain.UpdateMainContent(pg);

                // Update Main status bar:
                //if (pgId == PAGE_ID.PAGE_MAIN_ID) {
                //    wndMain.btMain.Background = Brushes.DarkBlue;
                //} else {
                //    wndMain.btMain.ClearValue(Label.BackgroundProperty);
                //}
                //if (pgId == PAGE_ID.PAGE_MENU_ID ) {
                //    wndMain.btPlc.Background = Brushes.DarkBlue;
                //} else {
                //    wndMain.btPlc.ClearValue(Label.BackgroundProperty);
                //}
                //if (pgId == PAGE_ID.PAGE_IO_ID) {
                //    wndMain.btCamera.Background = Brushes.DarkBlue;
                //} else {
                //    wndMain.btCamera.ClearValue(Label.BackgroundProperty);
                //}
                //if (pgId == PAGE_ID.PAGE_MENU_STATUS_SPC_OUTPUT)
                //{
                //    wndMain.btScanner.Background = Brushes.DarkBlue;
                //}
                //else
                //{
                //    wndMain.btScanner.ClearValue(Label.BackgroundProperty);
                //}
                //if (pgId == PAGE_ID.PAGE_MENU_STATUS_LOG)
                //{
                //    wndMain.btMesPage.Background = Brushes.DarkBlue;
                //}
                //else
                //{
                //    wndMain.btMesPage.ClearValue(Label.BackgroundProperty);
                //}
            }
        }

        public static PgMain GetMainPage()
        {
            return (PgMain)pageTable[PAGE_ID.PAGE_MAIN_ID];
        }

        public static void SaveAppSettings1()
        {
            try
            {
                String filePath = Path.Combine(Directory.GetCurrentDirectory(), APP_SETTINGS_FILE_NAME);
                var js = appSettings.TOJSON();
                File.WriteAllText(filePath, js);
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("saveAppSettings error:" + ex.Message));
            }
        }
        public static void SaveAppSettings()
        {
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), APP_SETTINGS_FILE_NAME);
                var json = appSettings.TOJSON();

                using (StreamWriter writer = new StreamWriter(filePath, false))
                {
                    writer.Write(json);
                }
            }
            catch (Exception ex)
            {
                logger.Create($"saveAppSettings error: {ex.Message}");
            }
        }

        public static void SaveCurrentModelSettings()
        {
            //ModelStore.UpdateModelSettings(currentModel);
        }
        private static void LoadAppSettings(String fileName)
        {
            try
            {
                String filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), fileName);
                if (File.Exists(filePath))
                {
                    using (StreamReader file = File.OpenText(filePath))
                    {
                        appSettings = AppSettings.FromJSON(file.ReadToEnd());
                    }
                }
                else
                {
                    appSettings = new AppSettings();
                }
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("loadAppSettings error:" + ex.Message));
            }
        }


        private static void InitPages() 
        {
            //pageTable.Add(PAGE_ID.PAGE_MAIN_ID, new PgMain());
            pageTable.Add(PAGE_ID.PAGE_MENU_ID, new PgPlc());


            pageTable.Add(PAGE_ID.PAGE_MENU_MECHANICAL_PLC, new PgMechanicalMenu());
            pageTable.Add(PAGE_ID.PAGE_SUPER_USER_MENU_SETTING_ALARM, new PgSuperUserMenu2());

            pageTable.Add(PAGE_ID.PAGE_IO_ID, new PgIO());


            pageTable.Add(PAGE_ID.PAGE_CAMERA_SETTING, new PgCamera());
            pageTable.Add(PAGE_ID.PAGE_RECIPE, new PgRecipe());

            pageTable.Add(PAGE_ID.PAGE_MAIN_VISION, new PgMainVision());
        }
        #region Load File I/O
        public static Boolean IsRunningAuto()
        {
            var pg = (PgMainVision)pageTable[PAGE_ID.PAGE_MAIN_ID];
            if (pg != null)
            {
                return pg.IsRunningAuto();
            }
            return false;
        }
        private static void loadFileINIIO_INPUT(String fileName)
        {
            try
            {
                var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IO Machine", "Input");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                var filePath = System.IO.Path.Combine(folder, fileName);

                fileInput = new INIFile(filePath);
                if (!File.Exists(filePath))
                {
                    fileInput.Write("P000", "Name Of Adrress P000", "00");
                    SectionNameInput = fileInput.GetSectionNames();
                }
                SectionNameInput = fileInput.GetSectionNames();
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("Load File {0} Error:", fileName + ex.Message));
            }
        }
        private static void loadFileINIIO_OUTPUT(String fileName)
        {
            try
            {
                var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IO Machine", "Output");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                var filePath = System.IO.Path.Combine(folder, fileName);

                fileOutput = new INIFile(filePath);
                if (!File.Exists(filePath))
                {
                    fileOutput.Write("P000", "Name Of Adrress P000", "00");
                    SectionNameOutput = fileOutput.GetSectionNames();
                }
                SectionNameOutput = fileOutput.GetSectionNames();
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("Load File {0} Error:", fileName + ex.Message));
            }
        }
        #endregion

        #region Check Neurocle License
        public static bool CheckNeurocleLicense()
        {
            bool result = true;
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "CheckNeurocleLicense.exe",      // Tên tiến trình sẽ chạy (file .exe kiểm tra license)
                RedirectStandardOutput = true,              // Cho phép đọc output từ tiến trình con (nếu cần log)
                UseShellExecute = false,                    // Bắt buộc khi dùng Redirect; chạy mà không dùng shell
                CreateNoWindow = true                       // Không hiển thị cửa sổ console của tiến trình con
            });

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                MessageBox.Show("Failed to find a valid Neurocle License!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.Create("Failed to find a valid Neurocle License!");
                result = false;
            }
            return result;
        }
        #endregion
        #region Check Cognex License
        public static bool CheckVisionProLicense()
        {
            try
            {
                CogLicense.CheckLicense(CogLicenseConstants.Blob);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("License VisionPro Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.Create(ex.Message);
                return false;
            }
        }
        public static bool CheckVidiLicense()
        {
            try
            {

                using (var control = new ViDi2.Runtime.Local.Control()) { }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.Create(ex.Message);
                return false;
            }
        }
        #endregion

        public static Type GetTypeByName(string typeName)
        {
            // 1) thử trực tiếp
            var t = Type.GetType(typeName, false);
            if (t != null) return t;

            // 2) duyệt tất cả assembly đang loaded
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(typeName, false);
                if (t != null) return t;
            }
            return null;
        }
        public static T FindParentOfType<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null) return null;

            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as T;
        }

        #region Connect PLC
        public static void ConnectPLC()
        {
            PLC1 = new SelectDevice(UiManager.appSettings.selectDevice, UiManager.appSettings.settingDevice);
            PLC1.device.Open();
        }
        public static void DisconnectPLC()
        {
            PLC1?.device.Close();
        }
        #endregion

        #region Connect MesVisionService
        public static async Task ConnectMesVs()
        {
            try
            {
                MesVsService = new MESVisionService(UiManager.appSettings.settingDeviceMesVs.MesVisionSetting.Ip, UiManager.appSettings.settingDeviceMesVs.MesVisionSetting.Port);
                await MesVsService.Connect();
            }
            catch (Exception ex)
            {
                logger.Create($"Connection MES Vision Error: {ex.Message}");
            }
        }
        public static void DisconnectMesVs()
        {
            MesVsService?.Disconnect();
        }
        #endregion
    }
}
