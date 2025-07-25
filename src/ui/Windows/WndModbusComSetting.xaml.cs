using Development;
using System;
using System.Collections.Generic;
using System.IO.Ports;
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
    /// Interaction logic for WndModbusComSetting.xaml
    /// </summary>
    public partial class WndModbusComSetting : Window
    {
        private MyLogger logger = new MyLogger("WndComSetting");
        private ModbusCOMSetting comSetting;
        public WndModbusComSetting()
        {
            InitializeComponent();
            this.Loaded += WndModbusComSetting_Loaded;
            this.btOk.Click += BtOk_Click;
            this.btOk.TouchDown += BtOk_Click;

            this.btCancel.Click += BtCancel_Click;
            this.btCancel.TouchDown += BtCancel_Click;
        }

        private void WndModbusComSetting_Loaded(object sender, RoutedEventArgs e)
        {
            this.LoadComPort();
        }

        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            this.comSetting = null;
            this.Close();
        }

        private void BtOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.comSetting.portName = this.cbPortName.SelectedValue.ToString();
                this.comSetting.baudrate = int.Parse(this.cbBaudrate.SelectedValue.ToString());
                this.comSetting.dataBits = int.Parse(this.cbDataBits.SelectedValue.ToString());
                this.comSetting.stopBits = COMSetting.ParseStopBits(this.cbStopBits.SelectedValue.ToString());
                this.comSetting.parity = COMSetting.ParseParity(this.cbParity.SelectedValue.ToString());
                this.comSetting.Handshake = Handshake.None;
                this.comSetting.AddressSlave = ushort.Parse(this.txtAddressMB.Text);

                this.Close();
            }
            catch (Exception ex)
            {
                logger.Create("BtOk_Click: " + ex.Message);
            }
        }
        private void LoadComPort()
        {
            try
            {
                var portNames = SerialPort.GetPortNames();
                foreach (var pn in portNames)
                {
                    var cbi = new ComboBoxItem();
                    cbi.Content = pn;
                    this.cbPortName.Items.Add(cbi);
                }
            }
            catch (Exception ex)
            {
                logger.Create("LoadComPort: " + ex.Message);
            }
        }
        public ModbusCOMSetting DoSettings(Window owner, ModbusCOMSetting oldSettings)
        {
            this.comSetting = oldSettings;
            try
            {
                this.cbPortName.SelectedValue = this.comSetting.portName;
                this.cbBaudrate.SelectedValue = this.comSetting.baudrate.ToString();
                this.cbDataBits.SelectedValue = this.comSetting.dataBits.ToString();
                var s = this.comSetting.parity.ToString();
                this.cbParity.SelectedValue = s;
                s = this.comSetting.stopBits.ToString();
                this.cbStopBits.SelectedValue = s;
                this.txtAddressMB.Text = comSetting.AddressSlave.ToString();
                this.ShowDialog();
            }
            catch (Exception ex)
            {
                logger.Create("DoSettings: " + ex.Message);
            }
            return comSetting;
        }
    }
}
