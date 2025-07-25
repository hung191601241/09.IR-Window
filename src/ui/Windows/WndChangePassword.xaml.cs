using System;
using System.Windows;

namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for WndChangePassword.xaml
    /// </summary>
    public partial class WndChangePassword : Window
    {
        private bool isSuccess = false;

        public WndChangePassword() {
            InitializeComponent();

            this.btOk.Click += this.BtOk_Click;
            this.btCancel.Click += this.BtCancel_Click;
        }

        public bool DoChangePassword(Window owner = null) {
            UserManager.createUserLog(UserActions.CHANGEPASS_SHOW);

            this.Owner = owner;
            this.ShowDialog();
            return isSuccess;
        }

        private void BtOk_Click(object sender, RoutedEventArgs e) {
            UserManager.createUserLog(UserActions.CHANGEPASS_BUTTON_OK);

            var passNew = this.txtPassNew.Password;
            var passOld = this.txtPassOld.Password;
            var passCom = this.txtConfirm.Password;
            var username = UiManager.appSettings.user;
            if (username.UserName == "SuperUser")
            {
                if (!String.IsNullOrEmpty(passNew) || !String.IsNullOrEmpty(passOld) || !String.IsNullOrEmpty(passCom))
                {
                    if (username.IDSuperuser == passOld && passNew == passCom)
                    {
                        username.IDSuperuser = passNew;
                        UiManager.SaveAppSettings();
                        MessageBox.Show("Password changed!");
                    }
                    else
                    {
                        MessageBox.Show("Password does NOT change!");
                    }

                }
                else
                {
                    isSuccess = UserManager.ChangePassword(passOld, passNew);
                    if (!isSuccess)
                    {
                        MessageBox.Show("Password does NOT change!");
                    }
                    this.Close();
                }
            }
            else if (username.UserName == "Manager")
            {
                if (!String.IsNullOrEmpty(passNew) || !String.IsNullOrEmpty(passOld) || !String.IsNullOrEmpty(passCom))
                {
                    if (username.IDManager == passOld && passNew == passCom)
                    {
                        username.IDManager = passNew;
                        UiManager.SaveAppSettings();
                        MessageBox.Show("Password changed!");
                    }
                    else
                    {
                        MessageBox.Show("Password does NOT change!");
                    }

                }
                else
                {
                    isSuccess = UserManager.ChangePassword(passOld, passNew);
                    if (!isSuccess)
                    {
                        MessageBox.Show("Password does NOT change!");
                    }
                    this.Close();
                }
            }
            else if (username.UserName == "Operator")
            {
                if (!String.IsNullOrEmpty(passNew) || !String.IsNullOrEmpty(passOld) || !String.IsNullOrEmpty(passCom))
                {
                    if (username.IdOP == passOld && passNew == passCom)
                    {
                        username.IdOP = passNew;
                        UiManager.SaveAppSettings();
                        MessageBox.Show("Password changed!");
                    }
                    else
                    {
                        MessageBox.Show("Password does NOT change!");
                    }

                }
                else
                {
                    isSuccess = UserManager.ChangePassword(passOld, passNew);
                    if (!isSuccess)
                    {
                        MessageBox.Show("Password does NOT change!");
                    }
                    this.Close();
                }
            }
        }

        private void BtCancel_Click(object sender, RoutedEventArgs e) {
            UserManager.createUserLog(UserActions.CHANGEPASS_BUTTON_CANCEL);

            this.Close();
        }
    }
}
