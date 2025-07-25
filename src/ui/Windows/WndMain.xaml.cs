using AutoLaserCuttingInput;
using nrt;
using OpenCvSharp;
using System;
using System.Text;
using System.Windows;
using Window = System.Windows.Window;

namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class WndMain : Window
    {
        public const String AppVersionNumber = "1.0.0";
        public const String AppVersionTime = "01-July-2021";
        private MyLogger logger = new MyLogger("WndMain");

        private System.Timers.Timer clock = new System.Timers.Timer(1000);

        public WndMain()
        {
            InitializeComponent();

            this.Loaded += this.WndMain_Loaded;
            this.Closed += this.WndMain_Closed;
            this.btMain.Click += this.BtMain_Click;
            this.btMenu.Click += BtPlc_Click;
            this.btLastJam.Click += BtLastJam_Click;
            this.btIO.Click += BtIO_Click;
            this.btPower.Click += BtPower_Click;



            var str = ASCIIEncoding.ASCII.GetString(new byte[] { 0x03, 0x41, 0x42 });
            logger.Create(str);
        }

        private void BtIO_Click(object sender, RoutedEventArgs e)
        {
            //if (UiManager.IsRunningAuto())
            //{
            //    new WndConfirm().DoComfirmYesNo("Is Running...\r\nStop and Push menu.", this);
            //    return;
            //}
            //UserManager.createUserLog(UserActions.IO_ENTER);

            //UiManager.SwitchPage(PAGE_ID.PAGE_IO_ID);
        }

        private void BtLastJam_Click(object sender, RoutedEventArgs e)
        {
            //if (UiManager.IsRunningAuto())
            //{
            //    new WndConfirm().DoComfirmYesNo("Is Running...\r\nStop and Push menu.", this);
            //    return;
            //}
            //UserManager.createUserLog(UserActions.APP_BUTTON_LASTJAM);

            //UiManager.SwitchPage(PAGE_ID.PAGE_LAST_JAM_ID);
        }

        public void UpdateMainContent(object obj)
        {
            this.mainContent.Navigate(obj);
        }

        private void WndMain_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UserManager.createUserLog(UserActions.APP_START);

                clock.AutoReset = true;
                clock.Elapsed += this.Clock_Elapsed;
                clock.Start();

                this.Title = String.Format("VisionInspection - v{0}", AppVersionNumber);
                //this.lblVersion.Content = String.Format("Ver {0} ({1})", AppVersionNumber, AppVersionTime);

                //UiManager.SwitchPage(PAGE_ID.PAGE_MAIN_ID);
                //UiManager.SwitchPage(PAGE_ID.PAGE_CAMERA_SETTING);
                UiManager.SwitchPage(PAGE_ID.PAGE_MAIN_VISION);

            }
            catch (Exception ex)
            {
                logger.Create("WndMain_Loaded error:" + ex.Message);
            }
        }

        private void WndMain_Closed(object sender, EventArgs e)
        {
            if (clock != null)
            {
                this.clock.Stop();
            }
            //Đóng tất cả camera trước khi thoát
            UiManager.CamList.ForEach(cam => cam.Close());
            UiManager.DisconnectMesVs();
            UiManager.DisconnectPLC();
        }

        private void Clock_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() => {
                try
                {
                    this.lblCurrentTime.Content = String.Format("{0:yyyy-MM-dd:HH:mm:ss}", DateTime.Now);
                }
                catch (Exception ex)
                {
                    logger.Create("Clock_Elapsed error:" + ex.Message);
                }
            });
        }

        private void BtPlc_Click(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.APP_BUTTON_MENU);

            //if (UiManager.IsRunningAuto())
            //{
            //    new WndConfirm().DoComfirmYesNo("Is Running...\r\nStop and Push menu.", this);
            //    return;
            //}
            //UiManager.SwitchPage(PAGE_ID.PAGE_MENU_ID);
            UiManager.SwitchPage(PAGE_ID.PAGE_CAMERA_SETTING);
        }

        private void BtMain_Click(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.APP_BUTTON_MAIN);
            //if (UiManager.IsRunningAuto())
            //{
            //    new WndConfirm().DoComfirmYesNo("Is Running...\r\nStop and Push menu.", this);
            //    return;
            //}
            //UiManager.SwitchPage(PAGE_ID.PAGE_MAIN_ID);
            UiManager.SwitchPage(PAGE_ID.PAGE_MAIN_VISION);
        }

        private void BtCamera_Click(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.ST_CAMERA);

            //if (UiManager.IsRunningAuto())
            //{
            //    new WndConfirm().DoComfirmYesNo("Is Running...\r\nStop and Push menu.", this);
            //    return;
            //}
            UiManager.SwitchPage(PAGE_ID.PAGE_CAMERA_SETTING);
        }

        private void BtScanner_Click(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.APP_BUTTON_LASTJAM);

            //if (UiManager.IsRunningAuto())
            //{
            //    new WndConfirm().DoComfirmYesNo("Is Running...\r\nStop and Push menu.", this);
            //    return;
            //}
            UiManager.SwitchPage(PAGE_ID.PAGE_MENU_STATUS_SPC_OUTPUT);
        }

        private void btMesPage_Click(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.APP_BUTTON_MES);

            //if (UiManager.IsRunningAuto())
            //{
            //    new WndConfirm().DoComfirmYesNo("Is Running...\r\nStop and Push menu.", this);
            //    return;
            //}
            UiManager.SwitchPage(PAGE_ID.PAGE_MENU_STATUS_LOG);
        }

        private void BtPower_Click(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.APP_BUTTON_SHUTDOWN);

            //if (UiManager.IsRunningAuto())
            //{
            //    new WndConfirm().DoComfirmYesNo("Is Running...\r\nStop and Push POWER.", this);
            //    return;
            //}
            App.Current.Shutdown(0);
        }
    }
}