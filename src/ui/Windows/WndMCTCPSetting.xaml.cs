using Development;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using VisionInspection;

namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for WndMCTCPSetting.xaml
    /// </summary>
    public partial class WndMCTCPSetting : Window
    {
        private MyLogger logger = new MyLogger("WndMcTCPSetting");
        private TCPSetting McSetting;
        public WndMCTCPSetting()
        {
            InitializeComponent();
            this.Loaded += WndMCTCPSetting_Loaded;
            this.btnOk.Click += BtnOk_Click;
            this.btnOk.TouchDown += BtnOk_Click;

            this.btnCancle.Click += BtnCancle_Click;
            this.btnCancle.TouchDown += BtnCancle_Click;
        }

        private void WndMCTCPSetting_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTCPSetting();
        }
        private void LoadTCPSetting()
        {
            this.txtIp.Text = UiManager.appSettings.settingDevice.MC_TCP_Binary.Ip;
            this.txtPort.Text = UiManager.appSettings.settingDevice.MC_TCP_Binary.Port.ToString();
        }
        private void BtnCancle_Click(object sender, RoutedEventArgs e)
        {
            this.McSetting = null;
            this.Close();
        }
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.McSetting.Ip = this.txtIp.Text;
                this.McSetting.Port = ushort.Parse(this.txtPort.Text);
                this.Close();
            }
            catch (Exception ex)
            {
                logger.Create("BtnOk_Click: " + ex.Message);
            }
        }
        public TCPSetting DoSettings(Window owner, TCPSetting oldSettings)
        {
            this.McSetting = oldSettings;
            try
            {
                this.txtIp.Text = this.McSetting.Ip.ToString();
                this.txtPort.Text = this.McSetting.Port.ToString();

                this.ShowDialog();
            }
            catch (Exception ex)
            {
                logger.Create("DoSettings: " + ex.Message);
            }
            return this.McSetting;
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
