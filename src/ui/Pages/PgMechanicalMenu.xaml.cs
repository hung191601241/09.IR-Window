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
using System.Windows.Navigation;
using System.Windows.Shapes;
using VisionInspection;

namespace AutoLaserCuttingInput
{
    /// <summary>
    /// Interaction logic for PgMechanicalMenu.xaml
    /// </summary>
    public partial class PgMechanicalMenu : Page
    {
        public PgMechanicalMenu()
        {
            InitializeComponent();
            this.btSetting1.Click += BtSetting1_Click;
            this.btSetting2.Click += BtSetting2_Click;
            this.btSetting3.Click += BtSetting3_Click;
            this.btSetting4.Click += BtSetting4_Click;
            this.BtnSave.Click += BtnSave_Click;

            this.Loaded += PgMechanicalMenu_Loaded;
        }

        private void BtSetting4_Click(object sender, RoutedEventArgs e)
        {
            UiManager.SwitchPage(PAGE_ID.PAGE_MENU_MECHANICAL_MES);
        }

        private void PgMechanicalMenu_Loaded(object sender, RoutedEventArgs e)
        {
            txbIp.Text = UiManager.appSettings.PLCTCP.PLCip;
            txbPort.Text = UiManager.appSettings.PLCTCP.PLCport.ToString();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var PLC = UiManager.appSettings.PLCTCP;
            PLC.PLCip = txbIp.Text;
            PLC.PLCport = Convert.ToInt32(txbPort.Text);
            PLC.PLCSlot = 1;
        }

        private void BtSetting3_Click(object sender, RoutedEventArgs e)
        {

            UiManager.SwitchPage(PAGE_ID.PAGE_MENU_MECHANICAL_BARCODE2);
        }

        private void BtSetting2_Click(object sender, RoutedEventArgs e)
        {
            UiManager.SwitchPage(PAGE_ID.PAGE_MENU_MECHANICAL_BARCODE1);
        }

        private void BtSetting1_Click(object sender, RoutedEventArgs e)
        {
            UiManager.SwitchPage(PAGE_ID.PAGE_MENU_MECHANICAL_PLC);
        }
    }
}
