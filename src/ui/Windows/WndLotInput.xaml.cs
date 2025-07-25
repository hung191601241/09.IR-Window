using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using AutoLaserCuttingInput;
using ITM_Semiconductor;

namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for WndLotInput.xaml
    /// </summary>
    public partial class WndLotInput : Window
    {
        private static MyLogger logger = new MyLogger("WndLotInput");

        private LotInData settings;

        public WndLotInput()
        {
            InitializeComponent();

            this.btOk.Click += this.BtOk_Click;
            this.btCancel.Click += this.BtCancel_Click;
            //this.btnPrimeTest.Click += this.btnPrimeTest_Clicked;
            //this.btnReTest.Click += this.btnReTest_Clicked;
            this.txtDeviceId.TouchDown += TxtDeviceId_TouchDown;
            this.txtLotId.TouchDown += TxtLotId_TouchDown;
            this.txtLotQty.TouchDown += TxtLotQty_TouchDown;
            this.txtWorkGroup.TouchDown += TxtWorkGroup_TouchDown;
        }

        private void TxtWorkGroup_TouchDown(object sender, TouchEventArgs e)
        {
            Process.Start(@"C:\Windows\System32\osk.exe");
        }

        private void TxtLotQty_TouchDown(object sender, TouchEventArgs e)
        {
            Process.Start(@"C:\Windows\System32\osk.exe");
        }

        private void TxtLotId_TouchDown(object sender, TouchEventArgs e)
        {
            Process.Start(@"C:\Windows\System32\osk.exe");
        }

        private void TxtDeviceId_TouchDown(object sender, TouchEventArgs e)
        {
            Process.Start(@"C:\Windows\System32\osk.exe");
        }

       
        private void BtOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UserManager.createUserLog(UserActions.LOTIN_BUTTON_OK);

                // Validate data:
                if (this.txtLotId.Text.Length < 1)
                {
                    MessageBox.Show("Invalid LOT ID: it must has atleast 1 characters!", "PARAMETER ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (String.IsNullOrEmpty(this.txtDeviceId.Text))
                {
                    MessageBox.Show("Invalid Device ID: it must has atleast 1 character!", "PARAMETER ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                int lotQty = 0;
                try
                {
                    lotQty = int.Parse(txtLotQty.Text);
                }
                catch
                {
                    lotQty = 0;
                }
                const int MAX_QTY = 1000000000;
                if (lotQty == 0 || lotQty > MAX_QTY)
                {
                    var msg = String.Format("Invalid LOT QTY: it must be a positive number and not over {0}!", MAX_QTY);
                    MessageBox.Show(msg, "PARAMETER ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Confirm:
                var wnd = new WndConfirm();
                if (wnd.DoComfirmYesNo("Reset the counter?", this))
                {
                    settings.workGroup = txtWorkGroup.Text;
                    settings.deviceId = txtDeviceId.Text;
                    settings.lotId = txtLotId.Text;
                    settings.lotQty = int.Parse(txtLotQty.Text);
                }
                else
                {
                    this.settings = null;
                }
                this.Close();
            }
            catch (Exception ex)
            {
                logger.Create("BtOk_Click error:" + ex.Message);
            }
        }

        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.LOTIN_BUTTON_CANCEL);

            this.settings = null;
            this.Close();
        }

        public LotInData DoSettings(Window owner, LotInData oldSettings)
        {
            UserManager.createUserLog(UserActions.LOTIN_SHOW);

            this.Owner = owner;

            settings = oldSettings.Clone();

            txtWorkGroup.Text = settings.workGroup;
            txtDeviceId.Text = settings.deviceId;
            txtLotId.Text = settings.lotId;
            if (settings.lotQty > 0)
            {
                txtLotQty.Text = settings.lotQty.ToString();
            }

            this.ShowDialog();
            return this.settings;
        }
    }
}
