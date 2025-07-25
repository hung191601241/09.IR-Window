using AutoLaserCuttingInput;
using System;
using System.Windows;

namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for WndLotFull.xaml
    /// </summary>
    public partial class WndLotFull : Window
    {
        private static int seqId = 0;
        private Boolean isConfirmYes = false;

        public WndLotFull(int code = 0, string mode = null) {
            InitializeComponent();

            this.btYes.Click += this.BtYes_Click;
            this.btNo.Click += this.BtNo_Click;

            if (code != 0) {
                this.tvCode.Text = code.ToString();
            }
            if (mode != null) {
                this.tvMode.Text = mode;
            }
            this.tvTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Increase SeqId:
            seqId++;
            this.tvSeqId.Text = seqId.ToString();
        }

        private void BtNo_Click(object sender, RoutedEventArgs e) {
            // Confirm:
            var wnd = new WndConfirm();
            if (wnd.DoComfirmYesNo("[Test End] selected. Right?", this)) {
                isConfirmYes = false;
                this.Close();
            }
        }

        private void BtYes_Click(object sender, RoutedEventArgs e) {
            // Confirm:
            var wnd = new WndConfirm();
            if (wnd.DoComfirmYesNo("[Continue] selected. Right?", this)) {
                isConfirmYes = true;
                this.Close();
            }
        }

        public Boolean DoConfirm(String msg = null, Window owner = null) {
            this.Owner = owner;
            if (msg != null) {
                this.lblMessage.Content = msg;
            } else {
                this.lblMessage.Content = "Lot In Input count exceeded.\r\n\r\n" +
                          "1.Continue: Initialize the counter and continue execution.\r\n" +
                          "2.LotEnd: Fully initialized and restart.";
            }

            this.ShowDialog();
            return isConfirmYes;
        }
    }
}
