using System;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;

namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for WndComSettings.xaml
    /// </summary>
    public partial class WndComSettings : Window
    {
        private static MyLogger logger = new MyLogger("ComSettings");

        private ComSettings settings;

        public WndComSettings() {
            InitializeComponent();

            this.Loaded += this.WndComSettings_Loaded;
            this.btOk.Click += this.BtOk_Click;
            this.btCancel.Click += this.BtCancel_Click;
        }

        private void WndComSettings_Loaded(object sender, RoutedEventArgs e) {
            try {
                var portNames = SerialPort.GetPortNames();
                foreach (var pn in portNames) {
                    var cbi = new ComboBoxItem();
                    cbi.Content = pn;
                    this.cbPortName.Items.Add(cbi);
                }
            } catch (Exception ex) {
                logger.Create("WndComSettings_Loaded error:" + ex.Message);
            }
        }

        public ComSettings DoSettings(Window owner, ComSettings oldSettings) {
            this.Owner = owner;

            this.settings = ComSettings.Clone(oldSettings);
            this.cbPortName.SelectedValue = settings.portName;
            this.cbBaudrate.SelectedValue = settings.baudrate.ToString();
            this.cbDataBits.SelectedValue = settings.dataBits.ToString();
            var s = settings.parity.ToString();
            this.cbParity.SelectedValue = s;
            s = settings.stopBits.ToString();
            this.cbStopBits.SelectedValue = s;

            this.ShowDialog();
            return this.settings;
        }

        private void BtOk_Click(object sender, RoutedEventArgs e) {
            try {
                settings.portName = this.cbPortName.SelectedValue.ToString();
                settings.baudrate = int.Parse(this.cbBaudrate.SelectedValue.ToString());
                settings.dataBits = int.Parse(this.cbDataBits.SelectedValue.ToString());
                settings.stopBits = ComSettings.ParseStopBits(this.cbStopBits.SelectedValue.ToString());
                settings.parity = ComSettings.ParseParity(this.cbParity.SelectedValue.ToString());
                this.Close();
            } catch (Exception ex) {
                logger.Create("BtOk_Click error:" + ex.Message);
            }
        }

        private void BtCancel_Click(object sender, RoutedEventArgs e) {
            this.settings = null;
            this.Close();
        }
    }
}
