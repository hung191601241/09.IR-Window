using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Development;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using VisionInspection;
using Xceed.Wpf.AvalonDock.Themes;
using static VisionTools.ToolEdit.BlobEdit;
using Window = System.Windows.Window;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for WndOutResultEdit.xaml
    /// </summary>
    public partial class OutAcquisResEdit : GridBase, INotifyPropertyChanged
    {
        //Variable
        MyLogger logger = new MyLogger("OutAcquisRes Edit");
        public bool resultOut = true; 
        public event RoutedEventHandler OnBtnRunClicked;

        //InOut
        public SvImage InputImage { get; set; } = new SvImage();

        //[*********************************** BINDING ***********************************]
        public event PropertyChangedEventHandler PropertyChanged;
        public Array DeviceCodes => Enum.GetValues(typeof(DeviceCode));

        private DeviceCode _selectDevOutOK = DeviceCode.M, _selectDevOutNG = DeviceCode.M;
        public DeviceCode SelectDevOutOK { get => _selectDevOutOK; set => _selectDevOutOK = value; }
        public DeviceCode SelectDevOutNG { get => _selectDevOutNG; set => _selectDevOutNG = value; }
        protected void OnPropertyChanged(string pptName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pptName));
        }

        public OutAcquisResEdit()
        {
            InitializeComponent();
            DisplayInit();
            RegisterEvent();
            toolBase.DataContext = this;
        }

        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "OutAcquisResult";
            toolBase.cbxImage.Items.Add("[OutAcquisResult] Input Image");
            toolBase.cbxImage.SelectedIndex = 0; 
            
            try
            {
                Grid parent = toolBase.gridBase.Parent as Grid;
                parent.Children.Add(this);
                parent.Children.Remove(toolBase.gridBase);
            }
            catch (Exception ex)
            {
                logger.Create("Display Init Error: " + ex.Message, ex);
            }
        }
        protected override void RegisterEvent()
        {
            toolBase.btnRun.Click += BtnRun_Click;
            this.Unloaded += OutBlobResultEdit_Unloaded;
        }

        private void OutBlobResultEdit_Unloaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtAddrOutOK.Text) || string.IsNullOrEmpty(txtAddrOutNG.Text))
            {
                MessageBox.Show("Address PLC can not empty!");
                return;
            }
            if (!CheckIntSyntax(txtAddrOutOK.Text) || !CheckIntSyntax(txtAddrOutNG.Text))
            {
                MessageBox.Show("Error PLC Address Syntax!");
                return;
            }
        }
        private bool CheckIntSyntax(string num)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(num, @"^[1-9]\d*$");
        }
        private bool SetResultOK(DeviceCode devOK, string addrOK, bool value)
        {
            try
            {
                return UiManager.PLC1.device.WriteBit(devOK, int.Parse(addrOK), value);
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("WRITE_RESULT_OK: " + ex.Message));
                return false;
            }
        }
        private bool SetResultNG(DeviceCode devNG, string addrNG, bool value)
        {
            try
            {
                return UiManager.PLC1.device.WriteBit(devNG, int.Parse(addrNG), value);
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("WRITE_RESULT_NG: " + ex.Message));
                return false;
            }
        }
        private bool String2Enum(string strDev, out DeviceCode _devType, out string _strDevNo)
        {
            bool isDefined = false;
            string letters = "";
            _devType = DeviceCode.M;
            _strDevNo = "";
            try
            {
                foreach (char synx in strDev)
                {
                    if (char.IsLetter(synx)) { letters += synx; }
                    else if (char.IsDigit(synx)) { _strDevNo += synx; }
                }

                isDefined = Enum.IsDefined(typeof(DeviceCode), letters);
                if (isDefined)
                {
                    _devType = (DeviceCode)Enum.Parse(typeof(DeviceCode), letters);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                logger.Create("Convert syntax error: " + ex.Message, ex);
            }
            return isDefined;
        }
        public void SendResultToPLC(string addrOK, string addrNG, bool result)
        {
            String2Enum(addrOK, out DeviceCode devCodeOK, out string devNoOK);
            SetResultOK(devCodeOK, devNoOK, result);
            String2Enum(addrNG, out DeviceCode devCodeNG, out string devNoNG);
            SetResultNG(devCodeNG, devNoNG, !result);
        }
        public void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            OnBtnRunClicked?.Invoke(sender, e);
        }

        public override void Run()
        {
            if (InputImage == null || InputImage.Mat == null || InputImage.Mat.Height <= 0 || InputImage.Mat.Width <= 0)
            {
                meaRunTime.Stop();
                toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "InputImage is null or error!");
                resultOut = false;
                return;
            }
        }
    }
}
