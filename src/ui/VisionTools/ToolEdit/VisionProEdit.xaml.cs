using Cognex.VisionPro;
using Cognex.VisionPro.ToolBlock;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using static VisionTools.ToolEdit.BlobEdit;
using Path = System.IO.Path;
using Rect = OpenCvSharp.Rect;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for SaveImageEdit.xaml
    /// </summary>
    public partial class VisionProEdit : GridBase, INotifyPropertyChanged
    {
        //Variables
        private MyLogger logger = new MyLogger("VisionPro Edit");
        public event RoutedEventHandler OnBtnRunClicked;
        public event EventHandler OnOutputChanged;
        
        //InOut
        private SvImage _inputImage = new SvImage();
        public SvImage InputImage
        {
            get => _inputImage; set
            {
                if (value == null) return;
                _inputImage = value;
                if (_inputImage.Mat.Height > 0 && _inputImage.Mat.Width > 0)
                {
                    toolBase.imgView.Source = _inputImage.Mat.ToBitmapSource();
                }
            }
        }
        public SvImage OutputImage { get; set; } = new SvImage();

        //[*********************************** BINDING ***********************************]
        public event PropertyChangedEventHandler PropertyChanged;
        #region Internal Field
        private ObservableCollection<VppInOutInfo> _vppInInfors = new ObservableCollection<VppInOutInfo>();
        private ObservableCollection<VppInOutInfo> _vppOutInfors = new ObservableCollection<VppInOutInfo>();
        #endregion

        #region Property
        public CogToolBlock CogTool { get; set; } = new CogToolBlock();
        //For load Tool
        public bool IsNewVppData { get; set; } = false;
        public ObservableCollection<VppInOutInfo> VppInInforRaws
        {
            get
            {
                if (_vppInInfors == null)
                    return new ObservableCollection<VppInOutInfo>();
                return _vppInInfors;
            }
            set
            {
                _vppInInfors = value;
                OnPropertyChanged(nameof(VppInInforRaws));
            }
        }
        public ObservableCollection<VppInOutInfo> VppOutInforRaws 
        {
            get
            {
                if (_vppOutInfors == null)
                    return new ObservableCollection<VppInOutInfo>();
                return _vppOutInfors;
            }
            set 
            { 
                _vppOutInfors = value; 
                OnPropertyChanged(nameof(VppOutInfors));
            } 
        }

        public List<VppInOutInfo> VppInInfors = new List<VppInOutInfo>();
        public List<VppInOutInfo> VppOutInfors = new List<VppInOutInfo>();
        protected void OnPropertyChanged(string pptName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pptName));
        }
        #endregion
        public VisionProEdit()
        {
            InitializeComponent();
            DisplayInit();
            RegisterEvent();
            toolBase.DataContext = this;
        }
        protected override void DisplayInit()
        {
            toolBase.btnLoadTool.IsEnabled = false;
            toolBase.lbCurrentJob.Content = "Save Image";
            toolBase.cbxImage.Items.Add("[VisionPro Image] Input Image");
            toolBase.cbxImage.Items.Add("[VisionPro Image] Output Image");
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
            toolBase.cbxImage.SelectionChanged += CbxImage_SelectionChanged;
            btnChooseVppFile.Click += BtnChooseVppFile_Click;
            btnDecode.Click += BtnDecode_Click;
        }
        private void CbxImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (toolBase.cbxImage.SelectedIndex == 0)
                {
                    if (runImage.Mat != null && runImage.Mat.Height > 0 && runImage.Mat.Width > 0)
                    {
                        toolBase.imgView.Source = runImage.Mat.ToBitmapSource();
                    }
                    oldSelect = 0;
                }
                else if (toolBase.cbxImage.SelectedIndex == 1)
                {
                    if (OutputImage.Mat != null && OutputImage.Mat.Height > 0 && OutputImage.Mat.Width > 0)
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
        private void BtnDecode_Click(object sender, RoutedEventArgs e)
        {
            if(!String.IsNullOrEmpty(TxtVppPath.Text))
            {
                DecodeVppFile(TxtVppPath.Text);
            }    
        }
        private void BtnChooseVppFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".vpp",
                Filter = "VisionPro File Path (*.vpp)|*.vpp"
            };

            if ((bool)dialog.ShowDialog() == true)
            {
                using (var fs = new System.IO.FileStream(dialog.FileName, System.IO.FileMode.Open, FileAccess.Read))
                {
                    TxtVppPath.Text = dialog.FileName;
                    Mouse.OverrideCursor = Cursors.Wait;
                    Mouse.OverrideCursor = null;
                }
                DecodeVppFile(TxtVppPath.Text);
            }
        }

        public bool DecodeVppFile(string vppPath)
        {
            try
            {
                IsNewVppData = false;
                if (!File.Exists(vppPath))
                    return false;
                CogTool = CogSerializer.LoadObjectFromFile(vppPath) as CogToolBlock;
                if (CogTool.Outputs.Count == 0 || CogTool.Inputs.Count == 0)
                    return false;

                VppInInforRaws.Clear();
                VppInInfors.Clear();
                for (int i = 0; i < CogTool.Inputs.Count; i++)
                {
                    string name = CogTool.Inputs[i].Name;
                    string valueType = CogTool.Inputs[i].ValueType.FullName;
                    object value = CogTool.Inputs[i].Value;
                    VppInInforRaws.Add(new VppInOutInfo(i, name, valueType, value));
                    VppInInfors.Add(new VppInOutInfo(i, name, valueType.Contains("CogImage") ? "VisionInspection.SvImage" : valueType, value));
                }

                ObservableCollection<VppInOutInfo> _vppOuts = new ObservableCollection<VppInOutInfo>();
                for (int i = 0; i < CogTool.Outputs.Count; i++)
                {
                    string name = CogTool.Outputs[i].Name;
                    string valueType = CogTool.Outputs[i].ValueType.FullName;
                    object value = CogTool.Outputs[i].Value;
                    _vppOuts.Add(new VppInOutInfo(i, name, valueType, value));
                }
                if (_vppOuts.Count == this.VppOutInforRaws.Count)
                {
                    for(int i = 0; i < _vppOuts.Count; i++)
                    {
                        if (_vppOuts[i].Name != VppOutInforRaws[i].Name || _vppOuts[i].ValueType != VppOutInforRaws[i].ValueType )
                        {
                            this.VppOutInforRaws.Clear();
                            this.VppOutInfors.Clear();
                            foreach (var vppOut in _vppOuts)
                            {
                                this.VppOutInforRaws.Add(vppOut);
                                VppInOutInfo vppOutCl = vppOut.Clone();
                                if (vppOutCl.ValueType.Contains("CogImage"))
                                    vppOutCl.ValueType = "VisionInspection.SvImage";
                                this.VppOutInfors.Add(vppOutCl);
                            }
                            IsNewVppData = true;
                            break;
                        }       
                    }   
                    if(!IsNewVppData)
                    {
                        foreach (var vppOut in _vppOuts)
                        {
                            VppInOutInfo vppOutCl = vppOut.Clone();
                            if (vppOutCl.ValueType.Contains("CogImage"))
                                vppOutCl.ValueType = "VisionInspection.SvImage";
                            this.VppOutInfors.Add(vppOutCl);
                        }
                    }    
                }
                else
                {
                    this.VppOutInforRaws.Clear();
                    this.VppOutInfors.Clear();
                    foreach (var vppOut in _vppOuts)
                    {
                        this.VppOutInforRaws.Add(vppOut);
                        VppInOutInfo vppOutCl = vppOut.Clone();
                        if (vppOutCl.ValueType.Contains("CogImage"))
                            vppOutCl.ValueType = "VisionInspection.SvImage";
                        this.VppOutInfors.Add(vppOutCl);
                    }
                    IsNewVppData = true;
                }
                if (VppOutInforRaws.Count == 0)
                    return false;
                OnOutputChanged?.Invoke(VppOutInforRaws, new EventArgs());
                return true;
            }
            catch (Exception ex)
            {
                logger.Create("Decode Vpp File Error: " + ex.Message, ex);
                return false;
            }
        }

        public void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            OnBtnRunClicked?.Invoke(sender, e);
        }

        private SvImage runImage = new SvImage();
        public override void Run()
        {
            try
            {
                for (int i = 0; i < VppInInfors.Count; i++)
                {
                    if (VppInInfors[i].ValueType.Contains("SvImage"))
                    {
                        runImage = new SvImage();
                        runImage = VppInInfors[i].Value as SvImage;
                        if (runImage == null || runImage.Mat == null || runImage.Mat.Width <= 0 || runImage.Mat.Height <= 0)
                        {
                            meaRunTime.Stop();
                            toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "InputImage is null or error!");
                            return;
                        }
                        if (runImage.Mat.Channels() > 3)
                        {
                            runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.BGR2RGB);
                            runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.RGB2BGR);
                        }
                        string[] vppImgTypes = VppInInforRaws[i].Value.ToString().Split('.');
                        string vppImgType = vppImgTypes[vppImgTypes.Length - 1];
                        switch (vppImgType)
                        {
                            case "CogImage8Grey":
                                // === ẢNH XÁM 8-BIT → CogImage8Grey ===
                                if(runImage.Mat.Channels() > 1)
                                    runImage.Mat.CvtColor(ColorConversionCodes.BGR2GRAY);
                                VppInInforRaws[i].Value = new CogImage8Grey(runImage.Mat.ToBitmap());
                                break;
                            case "CogImage24PlanarColor":
                                // === ẢNH MÀU 8-BIT → CogImage24PlanarColor ===
                                if (runImage.Mat.Channels() < 3)
                                    runImage.Mat.CvtColor(ColorConversionCodes.GRAY2BGR);
                                Cv2.CvtColor(runImage.Mat, runImage.Mat, ColorConversionCodes.BGR2RGB);
                                VppInInforRaws[i].Value = new CogImage24PlanarColor(runImage.Mat.ToBitmap());
                                break;
                            case "CogImage16Grey":
                            default:
                                // không xác định
                                break;
                        }
                    }
                    else
                    {
                        VppInInforRaws[i].Value = VppInInfors[i].Value;
                    }
                    CogTool.Inputs[i].Value = VppInInforRaws[i].Value;
                }
                //Run Tool
                CogTool.Run();
                //Get Output Value
                for(int i = 0; i < VppOutInforRaws.Count; i++)
                {
                    VppOutInforRaws[i].Value = CogTool.Outputs[i].Value;
                    if(VppOutInforRaws[i].ValueType.Contains("CogImage"))
                    {
                        OutputImage.Mat = (VppOutInforRaws[i].Value as ICogImage).ToBitmap().ToMat();
                        if(OutputImage.Mat == null)
                        {
                            meaRunTime.Stop();
                            toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "OutputImage from Vpp file is NULL");
                            return;
                        }    
                        VppOutInfors[i].Value = OutputImage;
                    }  
                    else
                    {
                        VppOutInfors[i].Value = CogTool.Outputs[i].Value;
                    }    
                } 
                if(OutputImage.Mat != null && OutputImage.Mat.Width > 0 && OutputImage.Mat.Width > 0)
                    ImgView.Source = OutputImage.Mat.ToBitmapSource();
                    
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.Create("Run Error: " + ex.Message, ex);
            }
        }
    }
    public class VppInOutInfo
    {
        public int STT { get; set; } = 0;
        public string Name { get; set; } = "";
        public string ValueType { get; set; } = "";
        public object Value { get; set; } = new object();

        public VppInOutInfo()
        {
            this.STT = 0;
            this.Name = "";
            this.ValueType = "";
            this.Value = new object();
        }
        public VppInOutInfo(int stt, string name, string valueType, object value)
        {
            this.STT = stt;
            this.Name = name;
            this.ValueType = valueType;
            this.Value = value;
        }
        public VppInOutInfo Clone()
        {
            return new VppInOutInfo
            {
                STT = this.STT,
                Name = this.Name,
                ValueType = this.ValueType,
                Value = this.Value,
            };
        }
    }
}
