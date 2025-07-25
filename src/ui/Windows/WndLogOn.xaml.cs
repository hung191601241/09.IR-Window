using System;
using System.Diagnostics;
using System.Windows;

namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for WndLogOn.xaml
    /// </summary>
    public partial class WndLogOn : Window
    {
        private bool isLogonSuccess = false;

        public WndLogOn() {
            InitializeComponent();

            this.btOk.Click += this.BtOk_Click;
            this.btCancel.Click += this.BtCancel_Click;
            this.btChangePassword.Click += this.BtChangePassword_Click;
            this.txtPassword.TouchDown += TxtPassword_TouchDown;
        }

        private void TxtPassword_TouchDown(object sender, System.Windows.Input.TouchEventArgs e)
        {
            Process.Start(@"C:\Windows\System32\osk.exe");
        }

        public Boolean DoLogOn(Window owner = null) {
            UserManager.createUserLog(UserActions.LOGON_SHOW);

            this.Owner = owner;
            this.txtPassword.Focus();
            this.ShowDialog();
            return isLogonSuccess;
        }

        private void BtOk_Click(object sender, RoutedEventArgs e) {
            UserManager.createUserLog(UserActions.LOGON_BUTTON_OK);

            isLogonSuccess = UserManager.LogOn(this.cboUsername.Text, this.txtPassword.Password);
            if (!isLogonSuccess) {
                MessageBox.Show("Wrong Password!");
            } else {
                this.Close();
            }
        }

        private void BtCancel_Click(object sender, RoutedEventArgs e) {
            UserManager.createUserLog(UserActions.LOGON_BUTTON_CANCEL);

            this.Close();
            isLogonSuccess = false;
        }

        private void BtChangePassword_Click(object sender, RoutedEventArgs e) {
            UserManager.createUserLog(UserActions.LOGON_BUTTON_CHANGE_PASSWORD);
            if (cboUsername.SelectedIndex == -1)
            {
                MessageBox.Show("Chưa chọn cấp bậc User !", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else
            {
                var username = UiManager.appSettings.user;
                username.UserName = cboUsername.Text;
                UiManager.SaveAppSettings();
            }
            if (new WndChangePassword().DoChangePassword(this.Owner))
            {
                UserManager.createUserLog(UserActions.LOGON_CHANGE_PASS_SUCCESS);

                MessageBox.Show("Password changed!");
            }
        }
    }
}
