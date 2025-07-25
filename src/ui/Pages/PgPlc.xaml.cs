using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.IO.Ports;
using System;
using System.Drawing;

namespace VisionInspection
{
    /// <summary>
    /// Interaction logic for PgPlc.xaml
    /// </summary>
    public partial class PgPlc : Page
    {
        //private ConnectionSettings connectionSettings;
        //private ComSettings settings;
        //private PlcComm plcComm;
        private static MyLogger logger = new MyLogger("PlcComm");
        //private Postgres postgres = new Postgres();
        public PgPlc()
        {
            InitializeComponent();
            this.Loaded += PgPlc_Loaded;
            this.btLogout.Click += btLogout_CLicked;
            this.btLogin.Click += btLogin_Clicked;




            this.btTeaching.Click += btTeaching_Clicked;
            this.btSystem.Click += btSystem_Clicked;
            this.btStatus.Click += BtStatus_Click;
            this.btSuperUser.Click += btSupperUser_Clicked;

            this.btMechanical.Click += btMechanical_Clicked;
            this.btManual.Click += BtManual_Click;
            this.btModel.Click += BtModel_Click;
            this.btCameraSetting.Click += BtCameraSetting_Click;



        }

        private void BtModel_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void BtManual_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void BtStatus_Click(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.APP_BUTTON_LASTJAM);

            UiManager.SwitchPage(PAGE_ID.PAGE_LAST_JAM_ID);
        }



        private void PgPlc_Loaded(object sender, RoutedEventArgs e)
        {
            updateUI();
            return;
        }

        private void updateUI()
        {
            if (UserManager.isLogOnManager)
            {
                UiManager.appSettings.user.UserName = "Manager";
                this.txtAccountID.Text = "MANAGER";
                this.txtAccountTime.Text = DateTime.Now.ToString();

                this.btLogin.IsEnabled = true;
                this.btLogout.IsEnabled = true;

                this.btTeaching.IsEnabled = true;
                this.btMechanical.IsEnabled = true;
                this.btManual.IsEnabled = true;
                this.btSuperUser.IsEnabled = true;


                this.btStatus.IsEnabled = true;
                this.btCameraSetting.IsEnabled = true;
                this.btSystem.IsEnabled = true;
                this.btModel.IsEnabled = true;
            }
            else if (UserManager.isLogOnSuper)
            {
                UiManager.appSettings.user.UserName = "SuperUser";
                this.txtAccountID.Text = "SUPPER USER";
                this.txtAccountTime.Text = DateTime.Now.ToString();

                this.btLogin.IsEnabled = true;
                this.btLogout.IsEnabled = true;

                this.btTeaching.IsEnabled = true;
                this.btMechanical.IsEnabled = true;
                this.btManual.IsEnabled = true;
                this.btSuperUser.IsEnabled = true;


                this.btStatus.IsEnabled = true;
                this.btCameraSetting.IsEnabled = true;
                this.btSystem.IsEnabled = true;
                this.btModel.IsEnabled = true;
            }
            else
            {
                UiManager.appSettings.user.UserName = "Operator";
                this.txtAccountID.Text = "OPERATOR";
                this.txtAccountTime.Text = DateTime.Now.ToString();

                this.btLogin.IsEnabled = true;
                this.btLogout.IsEnabled = true;

                this.btTeaching.IsEnabled = false;
                this.btMechanical.IsEnabled = false;
                this.btManual.IsEnabled = true;
                this.btSuperUser.IsEnabled = false;


                this.btStatus.IsEnabled = true;
                this.btCameraSetting.IsEnabled = false;
                this.btSystem.IsEnabled = false;
                this.btModel.IsEnabled = true;
            }
        }

        private void btTeaching_Clicked(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.MENU_BUTTON_TEACHING);

            UiManager.SwitchPage(PAGE_ID.PAGE_MENU_TEACHING_ID);
        }
        private void btSystem_Clicked(object sender, RoutedEventArgs e)
        {
            UiManager.SwitchPage(PAGE_ID.PAGE_SYSTEM_MENU);
        }
        private void btSupperUser_Clicked(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.MENU_BUTTON_SUPERUSER);

            UiManager.SwitchPage(PAGE_ID.PAGE_SUPER_USER_MENU);
        }


        private void BtCameraSetting_Click(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.ST_CAMERA);

            UiManager.SwitchPage(PAGE_ID.PAGE_CAMERA_SETTING);
        }
        private void btMechanical_Clicked(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.MN_MECHANICAL_DELAY_ENTER);

            UiManager.SwitchPage(PAGE_ID.PAGE_MENU_MECHANICAL_PLC);
        }
        private void btManualOperation_Clicked(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.MENU_BUTTON_MANUAL);

            UiManager.SwitchPage(PAGE_ID.PAGE_MENU_MANUAL_ID);

        }
        private void btMaintenance_Clicked(object sender, RoutedEventArgs e)
        {

        }
        private void BtAlarmCode_Click(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.APP_BUTTON_LASTJAM);

            UiManager.SwitchPage(PAGE_ID.PAGE_LAST_JAM_ID);
        }
        private void btLogout_CLicked(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.MENU_BUTTON_LOGOUT);

            UserManager.isLogOnManager = false;
            UserManager.isLogOnOperater = false;
            UserManager.isLogOnSuper = false;
            updateUI();
            return;
        }
        private void btLogin_Clicked(object sender, RoutedEventArgs e)
        {
            UserManager.createUserLog(UserActions.MENU_BUTTON_LOGIN);

            new WndLogOn().DoLogOn(Window.GetWindow(this));
            updateUI();
            return;
        }
     
       

    }
}
