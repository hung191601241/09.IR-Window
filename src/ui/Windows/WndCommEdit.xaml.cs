using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Development;
using ITM_Semiconductor;
using VisionInspection;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for WndCommEdit.xaml
    /// </summary>
    public partial class WndCommEdit : Window
    {
        public Array DeviceCodes => Enum.GetValues(typeof(DeviceCode));
        public DeviceCode SelectDevJigPos { get; set; } = DeviceCode.D;
        public DeviceCode SelectDevJob { get; set; } = DeviceCode.D;
        public Dictionary<int, Point> mtrxPoint = new Dictionary<int, Point>();

        public CommProperty commProperty = UiManager.appSettings.commProperty;
        public event RoutedEventHandler OnSaveCommSetting;


        //hiep sửa
        private SettingDevice settingDevice;
        private SettingDevice settingDeviceMesVs;
        private MyLogger logger = new MyLogger("wndCommEdit");
        private DispatcherTimer timer;
        public WndCommEdit()
        {
            InitializeComponent();
            this.Loaded += WndCommEdit_Loaded;

            this.DataContext = this;

            // HIEP SỬA
            this.btnSettingCommPLC.Click += BtnSettingDevice_Click;
            this.btnSettingCommVs.Click += BtnSettingCommVs_Click;
        }
        private void WndCommEdit_Loaded(object sender, RoutedEventArgs e)
        {
            txtAddrJigPos.Text = commProperty.addrJigPos;
            SelectDevJigPos = commProperty.selectDevJigPos;
            txtAddrJob.Text = commProperty.addrJob;
            SelectDevJob = commProperty.selectDevJob;

            mtrxPoint = commProperty.mtrxPoint;
            txtRows.Text = mtrxPoint.Where(k => k.Value.X == 1).Count().ToString();
            txtCols.Text = mtrxPoint.Where(k => k.Value.Y == 1).Count().ToString();

            // Hiep sửa
            settingDevice = UiManager.appSettings.settingDevice;
            settingDeviceMesVs = UiManager.appSettings.settingDeviceMesVs;
            this.cbSelectDeviceType.SelectedValue = UiManager.appSettings.selectDevice.ToString();
            Time_Ticked();
        }

        private bool CheckIntSyntax(string num)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(num, @"^[1-9]\d*$");
        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckIntSyntax(txtAddrJob.Text) || !CheckIntSyntax(txtAddrJigPos.Text))
            {
                MessageBox.Show("Error PLC Address Syntax!");
                return;
            }
            commProperty.addrJigPos = txtAddrJigPos.Text;
            commProperty.selectDevJigPos = SelectDevJigPos;
            commProperty.addrJob = txtAddrJob.Text;
            commProperty.selectDevJob = SelectDevJob;

            //Tạo MatrixPoint
            mtrxPoint.Clear();
            int value = 1;
            for (int row = 0; row < int.Parse(txtRows.Text); row++)
            {
                for (int col = 0; col < int.Parse(txtCols.Text); col++)
                {
                    // col = X, row = Y
                    mtrxPoint.Add(value++, new Point(col, row));
                }
            }
            commProperty.mtrxPoint = mtrxPoint;
            UiManager.appSettings.commProperty = commProperty;

            // Hiệp sửa
            SaveDeviceTypeSetting();
            UiManager.SaveAppSettings();
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        /////// Hiep sửa 
        public void Time_Ticked()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000); // Thiết lập thời gian lặp (1 giây)
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (UiManager.PLC1 != null)
            {
                if (UiManager.PLC1.device.isOpen())
                {
                    this.lbConnect.Content = "PLC Connect Success";
                    this.lbConnect.Background = Brushes.Green;
                }
                else
                {
                    this.lbConnect.Content = "PLC Disconnect";
                    this.lbConnect.Background = Brushes.Red;
                }
            }
            if(UiManager.MesVsService != null)
            {
                if(UiManager.MesVsService.IsConnected())
                {
                    lbConnectVs.Content = "MES Vision Connect Success";
                    this.lbConnectVs.Background = Brushes.Green;
                }
                else
                {
                    this.lbConnectVs.Content = "MES Vision Disconnect";
                    this.lbConnectVs.Background = Brushes.Red;
                }
            }    
        }
        private void SaveDeviceTypeSetting()
        {
            try
            {
                if (this.cbSelectDeviceType.SelectedValue == null) return;
                switch (this.cbSelectDeviceType.SelectedValue.ToString())
                {
                    case "Mitsubishi_MC_Protocol_Binary_TCP":
                        UiManager.appSettings.selectDevice = SaveDevice.Mitsubishi_MC_Protocol_Binary_TCP;
                        break;
                    case "Mitsubishi_RS422_SC09":
                        UiManager.appSettings.selectDevice = SaveDevice.Mitsubishi_RS422_SC09;
                        break;
                    case "LS_XGTServer_TCP":
                        UiManager.appSettings.selectDevice = SaveDevice.LS_XGTServer_TCP;
                        break;
                    case "LS_XGTServer_COM":
                        UiManager.appSettings.selectDevice = SaveDevice.LS_XGTServer_COM;
                        break;
                }
            }
            catch (Exception ex)
            {
                this.logger.Create("SaveDeviceTypeSetting: " + ex.Message);
              
            }
        }

        private void BtnSettingDevice_Click(object sender, RoutedEventArgs e)
        {
            if (this.cbSelectDeviceType.SelectedValue == null) return;
            switch(this.cbSelectDeviceType.SelectedValue.ToString())
            {
                case "Mitsubishi_MC_Protocol_Binary_TCP":
                    WndMCTCPSetting wndMC = new WndMCTCPSetting();
                    var settingNew1 = wndMC.DoSettings(Window.GetWindow(this), this.settingDevice.MC_TCP_Binary);
                    if (settingNew1 != null)
                    {
                        this.settingDevice.MC_TCP_Binary = settingNew1;
                    }
                    break;
                case "Mitsubishi_RS422_SC09":
                    WndSC09Setting wndMB = new WndSC09Setting();
                    var settingNew2 = wndMB.DoSettings(Window.GetWindow(this), this.settingDevice.sc09Setting);
                    if (settingNew2 != null)
                    {
                        this.settingDevice.sc09Setting = settingNew2;
                    }
                    break;
                case "LS_XGTServer_TCP":
                    WndMCTCPSetting wndMC2 = new WndMCTCPSetting();
                    var settingNew3 = wndMC2.DoSettings(Window.GetWindow(this), this.settingDevice.LSXGTServerTCPSetting);
                    if (settingNew3 != null)
                    {
                        this.settingDevice.MC_TCP_Binary = settingNew3;
                    }
                    break;
                case "LS_XGTServer_COM":
                    WndModbusComSetting wndMb = new WndModbusComSetting();
                    var settingNew4 = wndMb.DoSettings(Window.GetWindow(this), this.settingDevice.XGTServerCOMSetting);
                    if (settingNew4 != null)
                    {
                        this.settingDevice.XGTServerCOMSetting = settingNew4;
                    }
                    break;
            }
        }

        private void BtnSettingCommVs_Click(object sender, RoutedEventArgs e)
        {
            WndMCTCPSetting wndMC = new WndMCTCPSetting();
            var settingNew = wndMC.DoSettings(Window.GetWindow(this), this.settingDeviceMesVs.MesVisionSetting);
            if (settingNew != null)
            {
                this.settingDeviceMesVs.MesVisionSetting = settingNew;
            }
        }
    }
}
