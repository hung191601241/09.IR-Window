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

namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for WndSC09Setting.xaml
    /// </summary>
    public partial class WndSC09Setting : Window
    {
        private static MyLogger logger = new MyLogger("ComSettings");

        private SC09Setting settings;
        public WndSC09Setting()
        {
            InitializeComponent();
            this.Loaded += this.WndComSettings_Loaded;
            this.btOk.Click += this.BtOk_Click;
            this.btCancel.Click += this.BtCancel_Click;
        }
        private void WndComSettings_Loaded(object sender, RoutedEventArgs e)
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
                logger.Create("WndComSettings_Loaded error:" + ex.Message);
            }
        }
        public SC09Setting DoSettings(Window owner, SC09Setting oldSettings)
        {
            this.Owner = owner;

            settings = oldSettings;
            this.cbPortName.SelectedValue = settings.COM;
          
            this.ShowDialog();
            return settings;
        }

        private void BtOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.COM = this.cbPortName.SelectedValue.ToString();
                this.Close();
            }
            catch (Exception ex)
            {
                logger.Create("BtOk_Click error:" + ex.Message);
            }
        }

        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            this.settings = null;
            this.Close();
        }
    }
}
