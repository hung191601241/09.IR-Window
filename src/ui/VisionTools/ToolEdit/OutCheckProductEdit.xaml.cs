using Development;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VisionInspection;
using Xceed.Wpf.AvalonDock.Themes;
using static VisionTools.ToolEdit.BlobEdit;
using Window = System.Windows.Window;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for WndOutResultEdit.xaml
    /// </summary>
    public partial class OutCheckProductEdit : GridBase, INotifyPropertyChanged
    {
        //Variable
        MyLogger logger = new MyLogger("OutVidiCogRes Edit");
        public string[] arrAddr = new string[3];
        private DataTable dataTable = new DataTable();
        private DataView dataView = new DataView();
        private List<BlobObject> blobs = new List<BlobObject>();
        public int JudgeVal = 0;
        public event RoutedEventHandler OnBtnRunClicked;

        //InOut
        public SvImage InputImage { get; set; } = new SvImage();
        public double Score { get; set; } = 0.0;
        public List<BlobObject> Blobs
        {
            get
            {
                if (blobs == null)
                    return new List<BlobObject>();
                return blobs;
            }
            set { blobs = value; OnPropertyChanged(nameof(Blobs)); }
        }
        public SvImage OutputImage { get; set; } = new SvImage();

        //[*********************************** BINDING ***********************************]
        public event PropertyChangedEventHandler PropertyChanged;
        public Array DeviceCodes => Enum.GetValues(typeof(DeviceCode));

        private DeviceCode _selectDevOut = DeviceCode.D;
        public DeviceCode SelectDevOut { get => _selectDevOut; set => _selectDevOut = value; }
        public string TxtAddrOut { get; set; } = "";
        public DataView DataView { get => dataView; set { dataView = value; OnPropertyChanged(nameof(DataView)); } }
        protected void OnPropertyChanged(string pptName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pptName));
        }
        public OutCheckProductEdit()
        {
            InitializeComponent();
            DisplayInit();
            RegisterEvent();
            toolBase.DataContext = this;
            DataView = dataTable.DefaultView;
        }

        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "OutVidiCogResult";
            toolBase.cbxImage.Items.Add("[OutVidiCogResult] Input Image");
            toolBase.cbxImage.SelectedIndex = 0;
            toolBase.btnLoadTool.IsEnabled = false;

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
            toolBase.cbxImage.SelectionChanged += CbxImage_SelectionChanged;
            this.Unloaded += OutBlobResultEdit_Unloaded;
        }
        private void BtnRefreshTb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(TxtAddrOut))
                {
                    if (!CheckIntSyntax(TxtAddrOut))
                    {
                        MessageBox.Show("Error PLC Address Syntax!");
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Address PLC Empty!");
                    return;
                }
                for (int i = 0; i < arrAddr.Length; i++)
                {
                    arrAddr[i] = String.Format($"{SelectDevOut.ToString()}{int.Parse(TxtAddrOut) + i}");
                } 
                UpdateDataGrid();
            }
            catch (Exception ex)
            {
                logger.Create("Button Refresh PLC Addr Table Error: " + ex.Message, ex);
            }
        }
        private void OutBlobResultEdit_Unloaded(object sender, RoutedEventArgs e)
        {
            UpdateAddrOut();
        }
        public void UpdateAddrOut()
        {
            try
            {
                for (int col = 1; col < dataTable.Columns.Count; col++)
                {
                    // Lấy và parse giá trị từ DataTable
                    if (string.IsNullOrEmpty(dataTable.Rows[0][col].ToString()))
                    {
                        MessageBox.Show("Address PLC can not empty!");
                        return;
                    }
                    else
                    {
                        arrAddr[col - 1] = dataTable.Rows[0][col]?.ToString().TrimEnd('\t');
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Update Addr Out Error: " + ex.Message, ex);
            }
        }
        public void UpdateDataGrid()
        {
            try
            {
                dataTable.Columns.Clear();
                dataTable.Rows.Clear();
                // Tạo cột đầu tiên là row header
                dataTable.Columns.Add("Position");
                // Tạo cột tương ứng với các phán định
                dataTable.Columns.Add("OK");
                dataTable.Columns.Add("NG");
                dataTable.Columns.Add("Empty");

                // Thêm dòng Address
                if (!string.IsNullOrEmpty(TxtAddrOut))
                {
                    DataRow rows = dataTable.NewRow();
                    rows["Position"] = "Address";
                    rows["OK"] = arrAddr[0] + "\t";
                    rows["NG"] = arrAddr[1] + "\t";
                    rows["Empty"] = arrAddr[2] + "\t";
                    dataTable.Rows.Add(rows);
                }

                // Gán vào DataGrid
                DataView = null;
                DataView = dataTable.DefaultView;
                if (dtgdAddr.Columns.Count > 0 && dtgdAddr.Columns[0] != null)
                {
                    dtgdAddr.Columns[0].IsReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                logger.Create("Update PLC Addr Table Error: " + ex.Message, ex);
            }
        }
        private bool CheckIntSyntax(string num)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(num, @"^[1-9]\d*$");
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
                logger.Create("Convert syntax error: " + ex.Message);
            }
            return isDefined;
        }
        public bool WriteBitToPLC(string addr, bool value)
        {
            try
            {
                String2Enum(addr, out DeviceCode devCode, out string devNo);
                return UiManager.PLC1.device.WriteBit(devCode, int.Parse(devNo), value);
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("SEND_RESULT: " + ex.Message));
                return false;
            }
        }
        public void SendVisionResult(int judge)
        {
            switch (judge)
            {
                //OK
                case 1:
                    WriteBitToPLC(arrAddr[0], true);
                    WriteBitToPLC(arrAddr[1], false);
                    WriteBitToPLC(arrAddr[2], false);
                    break;
                //NG
                case 2:
                    WriteBitToPLC(arrAddr[0], false);
                    WriteBitToPLC(arrAddr[1], true);
                    WriteBitToPLC(arrAddr[2], false);
                    break;
                //Empty
                case 3:
                    WriteBitToPLC(arrAddr[0], false);
                    WriteBitToPLC(arrAddr[1], false);
                    WriteBitToPLC(arrAddr[2], true);
                    break;
            } 
                
        }

        private void CbxImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (toolBase.cbxImage.SelectedIndex == 0)
                {
                    if (InputImage.Mat.Height > 0 && InputImage.Mat.Width > 0)
                    {
                        toolBase.imgView.Source = InputImage.Mat.ToBitmapSource();
                    }
                    oldSelect = 0;
                }
                else if (toolBase.cbxImage.SelectedIndex == 1)
                {
                    if (OutputImage.Mat.Height > 0 && OutputImage.Mat.Width > 0)
                    {
                        toolBase.imgView.Source = OutputImage.Mat.ToBitmapSource();
                    }
                    oldSelect = 1;
                }
            }
            catch (Exception ex)
            {
                logger.Create("Cbx Image Error: " + ex.Message, ex);
            }
        }
        public void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            OnBtnRunClicked?.Invoke(sender, e);
        }

        private SvImage runImage = new SvImage();
        public override void Run()
        {
            if (InputImage == null || InputImage.Mat == null || InputImage.Mat.Height <= 0 || InputImage.Mat.Width <= 0)
            {
                meaRunTime.Stop();
                toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "InputImage is null or error!");
                return;
            }
            runImage = InputImage.Clone(true);
            switch(Blobs.Count)
            {
                case 0:
                    JudgeVal = 3;
                    break;
                case 1:
                    JudgeVal = 1;
                    break;
                default:
                    JudgeVal = 2;
                    break;
            }    
            txtScore.Text = Score.ToString();
            OutputImage = runImage.Clone(true);
            toolBase.imgView.Source = OutputImage.Mat.ToBitmapSource();
        }
    }
}
