using AutoLaserCuttingInput;
using Development;
using nrt;
using OpenCvSharp;
using OpenCvSharp.Cuda;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ViDi2.Common;
using VisionInspection;
using static OpenCvSharp.ConnectedComponents;
using Image = System.Windows.Controls.Image;
using Path = System.IO.Path;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for SegmentNeuroEdit.xaml
    /// </summary>
    public partial class SegmentNeuroEdit : GridBase, INotifyPropertyChanged
    {
        //Variables
        private MyLogger logger = new MyLogger("SegmentNeuro Edit");
        private DataTable dataTable = new DataTable();
        private DataView dataView = new DataView();
        public List<string> addrOKLst = new List<string>();
        public List<string> addrNGLst = new List<string>();
        public event RoutedEventHandler OnBtnRunClicked;
        private Dictionary<int, nrt.Predictor> predictors = new Dictionary<int, Predictor>();
        private Dictionary<int, nrt.Flowchart> flowcharts = new Dictionary<int, nrt.Flowchart>();
        private nrt.Status status;
        private List<NeuroFc> neuroFcLst = new List<NeuroFc>();
        public List<NeuroModel> modelList = new List<NeuroModel>();
        public bool isPdInitCompl = true;
        public bool isFcInitCompl = true;

        private SvImage runImage = new SvImage();
        public List<float> ScoreLst = new List<float>();
        public List<int> JudgeLst = new List<int>();
        public List<string> MessLst = new List<string>();

        public List<Mat> RunImageLst = new List<Mat>();
        public List<Mat> OutputImageLst = new List<Mat>();
        private nrt.Input inputs = new nrt.Input();
        public int Inc = 0;

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
        private List<BlobObject> blobs = new List<BlobObject>();
        public List<BlobObject> Blobs
        {
            get
            {
                if (blobs == null)
                    return new List<BlobObject>();
                return blobs;
            }
            set => blobs = value;
        }
        public SvImage OutputImage { get; set; } = new SvImage();
        public double Score { get; set; } = 0.0;
        public string StrResult { get; set; } = "OK";
        public int Judge { get; set; } = 0;
        //[*********************************** BINDING ***********************************]
        public event PropertyChangedEventHandler PropertyChanged;
        #region Fields
        public Array DeviceCodes => Enum.GetValues(typeof(DeviceCode));
        private DeviceCode _selectDevOK = DeviceCode.M, _selectDevNG = DeviceCode.M, _selectDevReset = DeviceCode.M, _selectDevRcvImg = DeviceCode.M;
        private double _numUDImgStorage = 0d, _maxImgStorage = 10d;
        private int _numUDCounter = 0, _indexImage = 0;
        #endregion

        #region Properties
        public string ImageFormatSelected { get; set; } = "BMP";
        public bool IsAddDateTime { get; set; } = false;
        public bool IsAddCounter { get; set; } = false;
        public string DiskSize { get; set; } = "GB";
        public int IndexImage { get => _indexImage; set { _indexImage = value; OnPropertyChanged(nameof(IndexImage)); } }
        public int NumUDCounter { get => _numUDCounter; set { _numUDCounter = value; OnPropertyChanged(nameof(NumUDCounter)); } }
        public double NumUDImageStorage { get => _numUDImgStorage; set { _numUDImgStorage = value; OnPropertyChanged(nameof(NumUDImageStorage)); } }
        public double MaxImageStorage { get => _maxImgStorage; set { _maxImgStorage = value; OnPropertyChanged(nameof(MaxImageStorage)); } }
        public DeviceCode SelectDevOK { get => _selectDevOK; set => _selectDevOK = value; }
        public DeviceCode SelectDevNG { get => _selectDevNG; set => _selectDevNG = value; }
        public DeviceCode SelectDevReset { get => _selectDevReset; set => _selectDevReset = value; }
        public DeviceCode SelectDevRcvImg { get => _selectDevRcvImg; set => _selectDevRcvImg = value; }
        public int NumberPos { get; set; } = 0;
        public string TxtAddrOK { get; set; } = "";
        public string TxtAddrNG { get; set; } = "";
        public string TxtAddrReset { get; set; } = ""; 
        public string TxtAddrRcvImg { get; set; } = ""; 
        public DataView DataView { get => dataView; set { dataView = value; OnPropertyChanged(nameof(DataView)); } }
        public ObservableCollection<string> BindableDevices { get; set; } = new ObservableCollection<string>();
        public string DeviceSelected { get; set; } = "";
        public int DevCbxIdx { get; set; } = 0;
        public int xctScore { get; set; } = 0;
        public int xctBatchSize { get; set; } = 0;
        #endregion

        protected void OnPropertyChanged(string pptName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pptName));
        }
        public SegmentNeuroEdit()
        {
            InitializeComponent();
            toolBase.DataContext = this;
            DisplayInit();
            RegisterEvent();
        }
        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "Segment Neuro";
            toolBase.cbxImage.Items.Add("[Segment Neuro] Input Image");
            toolBase.cbxImage.Items.Add("[Segment Neuro] Output Image");
            toolBase.cbxImage.SelectedIndex = 0;

            try
            {
                Grid parent = toolBase.gridBase.Parent as Grid;
                parent.Children.Add(this);
                parent.Children.Remove(toolBase.gridBase);

                var devices = GetHardwareDevices();
                BindableDevices.Clear();
                foreach (var d in devices)
                    BindableDevices.Add(d);
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
            this.Unloaded += SegmentNeuroEdit_Unloaded;
        }
        private void CbxImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (toolBase.cbxImage.SelectedIndex == 0)
                {
                    if (runImage.Mat.Height > 0 && runImage.Mat.Width > 0)
                    {
                        toolBase.imgView.Source = runImage.Mat.ToBitmapSource();
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
        private void BtnCreateModel_Click(object sender, RoutedEventArgs e)
        {
            if (txtModelName.Text == "")
            {
                MessageBox.Show("Name is Empty!!!");
                return;
            }
            if (txtModelPath.Text == "")
            {
                MessageBox.Show("Model Path is Empty!!!");
                return;
            }
            if (xctScore.ToString() == "")
            {
                MessageBox.Show("Score is Empty!!!");
                return;
            }
            CreatNewModel(txtModelName.Text, txtModelPath.Text, "", DeviceSelected, DevCbxIdx-1, Convert.ToInt32(xctScore), Convert.ToInt32(xctBatchSize), modelList.Count);
        }
        private List<TextBox> txtModelNameList = new List<TextBox>();
        public void CreatNewModel(string name, string path, string pathRuntime, string devSelected, int deviceIdx, int Score, int BatchSize, int index)
        {
            try
            {
                if (modelList.Count <= 0)
                {
                    stModelList.Children.RemoveRange(1, stModelList.Children.Count - 1);
                }
                Expander expander = new Expander
                {
                    Name = "Model00" + index,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                Border bdBound = new Border
                {
                    BorderBrush = (Brush)new BrushConverter().ConvertFromString("#888888"),
                    BorderThickness = new Thickness(0.5),
                    Child = expander
                };
                stModelList.Children.Add(bdBound);
                // Tạo Label làm Header để có thể bắt sự kiện
                Label headerLabel = new Label
                {
                    Content = name,
                    Background = Brushes.Transparent, // Nền mặc định
                    Foreground = Brushes.Black,
                    Padding = new Thickness(5)
                };
                headerLabel.MouseRightButtonUp += (s, ev) =>
                {
                    headerLabel.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 85, 85));
                    headerLabel.Foreground = Brushes.White;
                };
                headerLabel.MouseLeave += (s, ev) =>
                {
                    headerLabel.Background = Brushes.Transparent;
                    headerLabel.Foreground = Brushes.Black;
                };

                // Tạo ContextMenu
                ContextMenu contextMenu = new ContextMenu();
                MenuItem deleteItem = new MenuItem
                {
                    Header = "Delete",
                    Foreground = Brushes.Black,
                    Background = Brushes.Transparent
                };
                deleteItem.Click += (s, ev) =>
                {
                    // Xử lý sự kiện xóa
                    if (MessageBox.Show("Delete this Model?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        // Xóa Expander khỏi giao diện và danh sách
                        stModelList.Children.Remove(bdBound);

                        // Nếu cần, xóa job khỏi danh sách DLJob
                        NeuroModel jobToDelete = modelList.FirstOrDefault(job => job.Name == name);
                        if (jobToDelete != null)
                        {
                            modelList.Remove(jobToDelete);
                        }
                        if (stModelList.Children.Count <= 1)
                        {
                            Label label = new Label()
                            {
                                Content = "Have No Model To Display", // Nội dung hiển thị
                                Margin = new Thickness(10, 0, 0, 0),
                                BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#888888")),
                                BorderThickness = new Thickness(0.5),
                                Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#888888")),
                                FontStyle = FontStyles.Italic
                            };
                            stModelList.Children.Add(label);
                        }
                    }
                };
                // Thêm nút "Delete" vào ContextMenu
                contextMenu.Items.Add(deleteItem);
                // Gắn ContextMenu vào Label
                headerLabel.ContextMenu = contextMenu;
                // Gán Grid làm Header của Expander
                expander.Header = headerLabel;

                StackPanel st = new StackPanel();

                //Model Path
                StackPanel st1 = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5, 0, 0, 0),
                };
                Label lb1 = new Label
                {
                    Content = "Model Path",
                    Width = 100,
                    Foreground = Brushes.Black
                };
                TextBox txt = new TextBox
                {
                    Name = String.Format("txtModelPath{0}", index.ToString()),
                    Width = 300,
                    Text = path,
                    TextWrapping = TextWrapping.NoWrap,
                };
                txtModelNameList.Add(txt);
                Button btn = new Button
                {
                    Name = String.Format("btnModelPathChooseFile00{0}", index.ToString()),
                    Content = " . . . ",
                    Margin = new Thickness(5, 0, 0, 0),
                    Width = 30,
                };
                btn.Click += BtnModelChooseFile00_Click;
                st1.Children.Add(lb1);
                st1.Children.Add(txt);
                st1.Children.Add(btn);

                //Model Path Runtime
                StackPanel st3 = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5, 5, 0, 0),
                };
                Label lb3 = new Label
                {
                    Content = "Path Runtime",
                    Width = 100,
                    Foreground = Brushes.Black
                };
                TextBox txt3 = new TextBox
                {
                    Name = String.Format("txtPathRuntime{0}", index.ToString()),
                    Width = 300,
                    Text = pathRuntime,
                    TextWrapping = TextWrapping.NoWrap,
                    IsReadOnly = true
                };
                st3.Children.Add(lb3);
                st3.Children.Add(txt3);

                //Device
                StackPanel st4 = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5, 5, 0, 0),
                };
                Label lb4 = new Label
                {
                    Content = "Device",
                    Width = 100,
                    Foreground = Brushes.Black
                };
                ComboBox cbx4 = new ComboBox
                {
                    Name = String.Format("cbxDevice{0}", index.ToString()),
                    Width = 300,
                    ItemsSource = BindableDevices,
                    SelectedItem = devSelected,
                    IsEditable = false,
                };
                cbx4.SelectionChanged += CbxDevice_SelectionChanged;
                st4.Children.Add(lb4);
                st4.Children.Add(cbx4);

                //Score
                StackPanel st2 = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5, 5, 0, 5),
                };
                Label lb2 = new Label
                {
                    Content = "Score",
                    Width = 100,
                    Foreground = Brushes.Black
                };
                Xceed.Wpf.Toolkit.IntegerUpDown xct = new Xceed.Wpf.Toolkit.IntegerUpDown
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = Score,
                    Width = 100,
                    Name = String.Format("xctModelScore00{0}", index.ToString())
                };
                Label lbBatch = new Label
                {
                    Content = "Batch Size",
                    Width = 90,
                    Foreground = Brushes.Black,
                    Margin = new Thickness(45, 0, 0, 0)
                };
                Xceed.Wpf.Toolkit.IntegerUpDown xctBatchSize = new Xceed.Wpf.Toolkit.IntegerUpDown
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = BatchSize,
                    Width = 100,
                    IsReadOnly = true,
                    Name = String.Format("xctModelBatchSize00{0}", index.ToString())
                };
                xct.ValueChanged += Xct_ValueChanged;
                xctBatchSize.ValueChanged += XctBatchSize_ValueChanged;
                st2.Children.Add(lb2);
                st2.Children.Add(xct);
                st2.Children.Add(lbBatch);
                st2.Children.Add(xctBatchSize);

                //Expander
                st.Children.Add(st1);
                st.Children.Add(st3);
                st.Children.Add(st4);
                st.Children.Add(st2);
                expander.Content = st;
                NeuroModel neuroModel = new NeuroModel(name, path, pathRuntime, devSelected, deviceIdx, Score, BatchSize);
                modelList.Add(neuroModel);
                //Chạy khởi tạo lại các Predictor & Flowchart
                if (path.Contains(".net")) { PredictInit(); }
                else if (path.Contains(".nrfc")) { FlowchartInit(); }
                //Wait until load Model complete
                while (!isPdInitCompl || !isFcInitCompl) ;
            }
            catch (Exception ex)
            {
                logger.Create("Creat New Model Error: " + ex.Message, ex);
            }
        }
        private void BtnModelChooseFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    DefaultExt = ".net",
                    Filter = "Neurocle Model Files (*.net;*.nrfc)|*.net;*.nrfc|Neurocle Model (*.net)|*.net|Neurocle Runtime Model (*.nrfc)|*.nrfc"
                };

                if ((bool)dialog.ShowDialog() == true)
                {
                    using (var fs = new System.IO.FileStream(dialog.FileName, System.IO.FileMode.Open, FileAccess.Read))
                    {
                        txtModelPath.Text = dialog.FileName;
                        Mouse.OverrideCursor = Cursors.Wait;
                        Mouse.OverrideCursor = null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Button Model Choose File Error: " + ex.Message, ex);
            }
        }
        private void BtnModelChooseFile00_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = sender as Button;
                int index = Convert.ToInt32(button.Name.Replace("btnModelPathChooseFile00", string.Empty));
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    DefaultExt = ".net",
                    Filter = "Neurocle Model Files (*.net;*.nrfc)|*.net;*.nrfc|Neurocle Model (*.net)|*.net|Neurocle Runtime Model (*.nrfc)|*.nrfc"
                };

                if ((bool)dialog.ShowDialog() == true)
                {
                    using (var fs = new System.IO.FileStream(dialog.FileName, System.IO.FileMode.Open, FileAccess.Read))
                    {
                        txtModelNameList[index].Text = dialog.FileName;
                        //UiManager.appSettings.CurrentModel.dLJobs[index].Wspace = dialog.FileName;
                        modelList[index].Path = dialog.FileName;
                        Mouse.OverrideCursor = Cursors.Wait;
                        Mouse.OverrideCursor = null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Button Model Choose File 00 Error: " + ex.Message, ex);
            }
        }
        private void CbxDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ComboBox cmbDevice = sender as ComboBox;
                int index = Convert.ToInt32(cmbDevice.Name.Replace("cbxDevice", string.Empty));
                modelList[index].Device = cmbDevice.SelectedItem as String;
                modelList[index].DeviceIdx = cmbDevice.SelectedIndex - 1;
                Mouse.OverrideCursor = Cursors.Wait;
                Mouse.OverrideCursor = null;
            }
            catch (Exception ex)
            {
                logger.Create("Cbx Device Error: " + ex.Message, ex);
            }
        }
        private void Xct_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown xct = sender as Xceed.Wpf.Toolkit.IntegerUpDown;
            int index = Convert.ToInt32(xct.Name.Replace("xctModelScore00", string.Empty));
            modelList[index].Score = Convert.ToInt32(xct.Value);
        }
        private void XctBatchSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown xct = sender as Xceed.Wpf.Toolkit.IntegerUpDown;
            int index = Convert.ToInt32(xct.Name.Replace("xctModelBatchSize00", string.Empty));
            modelList[index].BatchSize = Convert.ToInt32(xct.Value);
        }
        private Mat NrtInput2Mat(nrt.Input input, int batchIdx)
        {
            nrt.Shape shape = input.get_org_input_shape(batchIdx);
            // Lấy con trỏ dữ liệu
            IntPtr dataPtr = input.get_org_input_ndbuff(batchIdx).get_data_ptr();
            int height = shape.get_dim(0);
            int width = shape.get_dim(1);
            return new Mat(height, width, MatType.CV_8UC3, dataPtr);
        }
        public void PredictInit()
        {
            isPdInitCompl = false;
            Task.Run(() =>
            {
                try
                {
                    predictors.Clear();
                    for (int i = 0; i < modelList.Count; i++)
                    {
                        string ext = Path.GetExtension(modelList[i].Path)?.ToLower();
                        if (ext == ".net")
                        {
                            nrt.Status status;
                            Predictor curPredict = new Predictor();
                            if (modelList[i].DeviceIdx >= 0 && File.Exists(modelList[i].PathRuntime) && Path.GetExtension(modelList[i].PathRuntime)?.ToLower() == ".nrpd")
                            {
                                curPredict = new nrt.Predictor(modelList[i].DeviceIdx, modelList[i].PathRuntime);
                                predictors.Add(i, curPredict);
                            }
                            else if (modelList[i].DeviceIdx == -1)
                            {
                                // The default device setting for the Predictor is CPU(-1), nrt.DEVICE_CPU.
                                // predictor = new nrt.Predictor(modelPath);
                                curPredict = new nrt.Predictor(modelList[i].Path, nrt.Model.MODELIO_OUT_PROB, modelList[i].DeviceIdx, modelList[i].BatchSize);
                                predictors.Add(i, curPredict);
                            }
                            else
                            {
                                curPredict = new nrt.Predictor(modelList[i].Path, nrt.Model.MODELIO_OUT_PROB, modelList[i].DeviceIdx, modelList[i].BatchSize, false, false, nrt.DevType.DEVICE_CUDA_GPU);
                                predictors.Add(i, curPredict);

                                // Save the predictor information optimized for this device,
                                // so if the same model is used in the same device environment later, the optimized predictor can be reused.
                                // This can be operated only in DEVICE_CUDA_GPU 
                                if (modelList[i].DeviceIdx >= 0 && curPredict.get_device_type() == ((int)nrt.DevType.DEVICE_CUDA_GPU))
                                {
                                    string folderPath = AppDomain.CurrentDomain.BaseDirectory + @"Model Optimize";
                                    if (!Directory.Exists(folderPath))
                                    {
                                        Directory.CreateDirectory(folderPath);
                                    }
                                    modelList[i].PathRuntime = System.IO.Path.Combine(folderPath, $"{modelList[i].Name}.nrpd");
                                    //if (!File.Exists(modelList[i].PathRuntime))
                                    //{
                                    //    // Dispose để đóng file sau khi tạo
                                    //    File.Create(modelList[i].PathRuntime).Dispose();
                                    //}
                                    status = curPredict.save_predictor(modelList[i].PathRuntime);
                                    if (status != nrt.Status.STATUS_SUCCESS)
                                    {
                                        meaRunTime.Stop();
                                        MessageBox.Show("Predictor save failed. : " + nrt.nrt.get_last_error_msg());
                                        this.Dispatcher.Invoke(() => toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Predictor save failed. : " + nrt.nrt.get_last_error_msg()));
                                        return;
                                    }
                                }
                            }
                            if (curPredict.get_status() != nrt.Status.STATUS_SUCCESS)
                            {
                                meaRunTime.Stop();
                                MessageBox.Show("Predictor initialization failed. : " + nrt.nrt.get_last_error_msg());
                                this.Dispatcher.Invoke(() => toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Predictor initialization failed. : " + nrt.nrt.get_last_error_msg()));
                                return;
                            }
                            if (curPredict.get_model_type() != nrt.ModelType.SEGMENTATION)
                            {
                                meaRunTime.Stop();
                                MessageBox.Show("ModelType must be SEGMETATION!");
                                this.Dispatcher.Invoke(() => toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "ModelType must be SEGMETATION!"));
                                return;
                            }
                        }

                    }
                    isPdInitCompl = true;
                }
                catch (Exception ex)
                {
                    logger.Create("Predict Init Error: " + ex.Message, ex);
                    isPdInitCompl = true;
                }
                
            });
            
        }
        public void FlowchartInit()
        {
            isFcInitCompl = false;
            Task.Run(() =>
            {
                try
                {
                    flowcharts.Clear();
                    for (int i = 0; i < modelList.Count; i++)
                    {
                        string ext = Path.GetExtension(modelList[i].Path)?.ToLower();
                        if (ext == ".nrfc")
                        {
                            nrt.Status status;
                            nrt.Flowchart curFlowchart = new Flowchart();
                            if (modelList[i].DeviceIdx >= 0 && File.Exists(modelList[i].PathRuntime) && Path.GetExtension(modelList[i].PathRuntime)?.ToLower() == ".nrfe")
                            {
                                curFlowchart = new nrt.Flowchart(modelList[i].DeviceIdx, modelList[i].PathRuntime);
                                flowcharts.Add(i, curFlowchart);
                            }
                            else if (modelList[i].DeviceIdx == -1)
                            {
                                // The default device setting for the Flowchart is CPU(-1), nrt.DEVICE_CPU.
                                // flowchart = new nrt.flowchart(flowchartPath);
                                curFlowchart = new nrt.Flowchart(modelList[i].Path, nrt.Model.MODELIO_OUT_PROB, modelList[i].DeviceIdx, modelList[i].BatchSize, false, false);
                                flowcharts.Add(i, curFlowchart);
                            }
                            else
                            {
                                // CPU's device_idx = -1, GPU's device_idx = [0, num of device)
                                // The default device setting for the Flowchart is CPU(-1).
                                // flowchart = new nrt.Flowchart(flowchartPath);
                                curFlowchart = new nrt.Flowchart(modelList[i].Path, nrt.Model.MODELIO_OUT_PROB, modelList[i].DeviceIdx, modelList[i].BatchSize, false, false, nrt.DevType.DEVICE_CUDA_GPU);
                                flowcharts.Add(i, curFlowchart);
                                // Save the flowchart information optimized for this device,
                                // so if the same model is used in the same device environment later, the optimized flowchart can be reused.
                                // This can be operated only in DEVICE_CUDA_GPU 
                                if (modelList[i].DeviceIdx >= 0 && curFlowchart.get_node(0).get_predictor().get_device_type() == ((int)nrt.DevType.DEVICE_CUDA_GPU))
                                {
                                    string folderPath = AppDomain.CurrentDomain.BaseDirectory + @"Model Optimize";
                                    if (!Directory.Exists(folderPath))
                                    {
                                        Directory.CreateDirectory(folderPath);
                                    }
                                    modelList[i].PathRuntime = System.IO.Path.Combine(folderPath, $"{modelList[i].Name}.nrfe");
                                    //if (!File.Exists(modelList[i].PathRuntime))
                                    //{
                                    //    // Dispose để đóng file sau khi tạo
                                    //    File.Create(modelList[i].PathRuntime).Dispose();
                                    //}
                                    status = curFlowchart.save_flowchart_engine(modelList[i].PathRuntime);
                                    if (status != nrt.Status.STATUS_SUCCESS)
                                    {
                                        meaRunTime.Stop();
                                        MessageBox.Show("Flowchart save failed. : " + nrt.nrt.get_last_error_msg());
                                        this.Dispatcher.Invoke(() => toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Flowchart save failed. : " + nrt.nrt.get_last_error_msg()));
                                        return;
                                    }
                                }
                            }
                            if (curFlowchart.get_status() != nrt.Status.STATUS_SUCCESS)
                            {
                                meaRunTime.Stop();
                                MessageBox.Show("Flowchart save failed. : " + nrt.nrt.get_last_error_msg());
                                this.Dispatcher.Invoke(() => toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Flowchart initialization failed. : " + nrt.nrt.get_last_error_msg()));
                                return;
                            }
                        }
                    }
                    isFcInitCompl = true;
                }
                catch (Exception ex)
                {
                    logger.Create("Flowchart Init Error: " + ex.Message, ex);
                    isFcInitCompl = true;
                }
                
            });   
        }

        void CheckModelType(nrt.Result res, nrt.FlowchartNode node)
        {
            try
            {
                nrt.Predictor predictor = node.get_predictor();
                nrt.ModelType modelType = predictor.get_model_type();
                neuroFcLst.Add(new NeuroFc(modelType, res, node));
                switch (modelType)
                {
                    case ModelType.ROTATION:
                        int angleCount = (int)res.angles.get_count();
                        if (angleCount > 0)
                        {
                            nrt.Angle angle = res.angles.get(0);
                            if (angle.has_child())
                            {
                                CheckModelType(angle.get_child_result(), node.get_child(0));
                            }
                        }
                        break;
                    case ModelType.PATCHED_CLASSIFICATION:
                    case ModelType.DETECTION:
                        int bboxCount = (int)res.bboxes.get_count();
                        if (bboxCount > 0)
                        {
                            nrt.Bbox bbox = res.bboxes.get(0);
                            int clsIdx = bbox.class_idx;
                            if (bbox.has_child())
                            {
                                CheckModelType(bbox.get_child_result(), node.get_child(clsIdx));
                            }
                        }
                        break;
                    case ModelType.SEGMENTATION:
                        int blobCount = (int)res.blobs.get_count();
                        if (blobCount > 0)
                        {
                            nrt.Blob blob = res.blobs.get(0);
                            int clsIdx = blob.class_idx;
                            if (blob.has_child())
                            {
                                CheckModelType(blob.get_child_result(), node.get_child(clsIdx));
                            }
                        }
                        break;
                }

                int imageCount = (int)res.images.get_count();
                for (int i = 0; i < imageCount; i++)
                {
                    nrt.Image img = res.images.get(i);
                    if (img.has_child())
                    {
                        int classCount = predictor.get_num_classes();
                        for (int j = 0; j < classCount; j++)
                        {
                            if (node.has_child(j))
                            {
                                CheckModelType(img.get_child_result(), node.get_child(j));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Check Model Type Error: " + ex.Message, ex);
            }
        }
        private bool RotateImage(Mat src, int deg, out Mat dst)
        {
            dst = new Mat();
            try
            {
                // Xác định tâm ảnh
                Point2f center = new Point2f(src.Width / 2f, src.Height / 2f);
                // Tỷ lệ zoom sau khi xoay (1.0 = giữ nguyên)
                double scale = 1d;
                // Tạo ma trận xoay
                Mat rotMatrix = Cv2.GetRotationMatrix2D(center, deg, scale);
                // Xác định kích thước ảnh đầu ra (giữ nguyên kích thước cũ)
                OpenCvSharp.Size size = new OpenCvSharp.Size(src.Width, src.Height);
                // Biến đổi affine
                Cv2.WarpAffine(src, dst, rotMatrix, size, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0));
                rotMatrix.Dispose();
                if (dst.Cols == 0 || dst.Rows == 0)
                {
                    dst.Dispose();
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Create("Rotate Image Error: " + ex.Message, ex);
                return false;
            }
            
        }
        private bool ProcessImageDet(Mat src, nrt.Rect rect, OIConverter oiConverter, out Mat dst)
        {
            dst = new Mat();
            try
            {
                if (rect.width <= 0 || rect.height <= 0)
                {
                    return false;
                }
                OpenCvSharp.Rect rectBound = new OpenCvSharp.Rect(rect.x, rect.y, rect.width, rect.height);
                OIConvertType oiType = oiConverter.get_convert_type();
                switch (oiType)
                {
                    case OIConvertType.CROP_EQUAL:
                        src = new Mat(src, rectBound);
                        break;
                    case OIConvertType.CROP_ACTUAL:
                        int padding = oiConverter.get_padding();
                        OpenCvSharp.Rect rectPadding = new OpenCvSharp.Rect(rect.x + padding, rect.y + padding, rect.width - padding * 2, rect.height - padding * 2);
                        src = new Mat(src, rectPadding);
                        break;
                    case OIConvertType.MASK:
                        //src.Rectangle(rectBound, Scalar.Blue, -1);
                        break;
                    case OIConvertType.INVERTED_MASK:
                        //// Tạo mask đen (1 kênh)
                        //Mat mask = new Mat(src.Size(), MatType.CV_8UC1, Scalar.All(0));
                        //// Vẽ hình chữ nhật trắng (mask vùng cần giữ)
                        //Cv2.Rectangle(mask, rectBound, Scalar.All(255), -1); // -1: fill
                        ////chuyển mask thành 3 kênh
                        //Cv2.CvtColor(mask, mask, ColorConversionCodes.GRAY2BGR);
                        //// Áp mask lên ảnh
                        //Cv2.BitwiseAnd(src, mask, src);
                        break;
                }
                if (src.Width == 0 || src.Height == 0)
                {
                    return false;
                }
                dst = src.Clone();
                return true;
            }
            catch (Exception ex)
            {
                logger.Create("Draw Image Error: " + ex.Message, ex);
                return false;
            }
        }
        private bool ProcessImageSeg(Mat src, List<OpenCvSharp.Point> contour, nrt.Rect rect, OIConverter oiConverter, out Mat dst)
        {
            dst = new Mat();
            try
            {
                if (rect.width <= 0 || rect.height <= 0)
                {
                    return false;
                }
                OpenCvSharp.Rect rectBound = new OpenCvSharp.Rect(rect.x, rect.y, rect.width, rect.height);
                //Lấy góc của Contour
                float degAngle = 0;
                if (contour.Count >= 5)
                {
                    RotatedRect ellipse = Cv2.FitEllipse(contour);
                    // Góc của blob (đơn vị: độ)
                    degAngle = ellipse.Angle;
                    //Biến đổi góc: Xoay ngược về mốc 90 thành 0 và đặt góc trong khoảng [0; 360)
                    degAngle = ((degAngle - 90) + 360) % 360;
                }
                OIConvertShape oiShape = oiConverter.get_convert_shape();
                switch (oiShape)
                {
                    case OIConvertShape.ORIGINAL:
                        //1.Tạo mask trắng(đen hết)
                        using (Mat mask = Mat.Zeros(src.Size(), MatType.CV_8UC1))
                        {
                            // 2. Vẽ contour lên mask (trắng vùng contour)
                            Cv2.FillPoly(mask, new[] { contour.ToArray() }, Scalar.White);
                            // 3. Áp mask lên ảnh gốc (bitwise AND)
                            using (Mat matRes = new Mat())
                            {
                                Cv2.BitwiseAnd(src.Clone(), src.Clone(), matRes, mask);
                                // 4. Cắt vùng ảnh chứa sản phẩm 
                                src = new Mat(matRes, rectBound);
                            }
                        }
                        break;
                    case OIConvertShape.BOX:
                        src = new Mat(src, rectBound);
                        break;
                    case OIConvertShape.FITTED_BOX:
                        src = GetROIRegion(src.Clone(), rectBound, degAngle);
                        break;
                }
                if (src.Width == 0 || src.Height == 0)
                {
                    return false;
                }
                dst = src.Clone();
                return true;
            }
            catch (Exception ex)
            {
                logger.Create("Draw Image Error: " + ex.Message, ex);
                return false;
            }
        }
        public Mat GetROIRegion(Mat image, OpenCvSharp.Rect rectBound, double degAngle)
        {
            try
            {
                if (image == null || image.IsDisposed || rectBound == null) return null;
                double radAngle = (degAngle * Math.PI) / 180d;

                // Tính tọa độ 4 góc trước khi xoay
                Point2d pLT = new Point2d(rectBound.Left, rectBound.Top);
                Point2d centerPoint = new Point2d(rectBound.Left + rectBound.Right / 2, rectBound.Top + rectBound.Bottom / 2);
                Point2d pRB = new Point2d(rectBound.Right, rectBound.Bottom);
                Point2d pLB = new Point2d(rectBound.Left, rectBound.Bottom);
                Point2d pRT = new Point2d(rectBound.Right, rectBound.Top);
                // Tính tọa độ 4 góc sau khi xoay
                Point2d pLTr = SvFunc.RotateAtCenter(pLT, centerPoint, radAngle);
                Point2d pRBr = SvFunc.RotateAtCenter(pRB, centerPoint, radAngle);
                Point2d pLBr = SvFunc.RotateAtCenter(pLB, centerPoint, radAngle);
                Point2d pRTr = SvFunc.RotateAtCenter(pRT, centerPoint, radAngle);
                Point2f center = new Point2f((float)pLTr.X, (float)pLTr.Y);

                double T = Math.Atan2(pRTr.Y - pLTr.Y, pRTr.X - pLTr.X);
                double H = Point2d.Distance(pLTr, pRTr);
                double V = Point2d.Distance(pLTr, pLBr);

                Mat Rmat = Cv2.GetRotationMatrix2D(center, T * 180 / Math.PI, 1);
                Rmat.Set<double>(0, 2, Rmat.At<double>(0, 2) - center.X);
                Rmat.Set<double>(1, 2, Rmat.At<double>(1, 2) - center.Y);

                OpenCvSharp.Rect rect = new OpenCvSharp.Rect((int)center.X, (int)center.Y, (int)H, (int)V);

                Mat templateImg = new Mat(rect.Size, image.Type());
                Cv2.WarpAffine(image, templateImg, Rmat, rect.Size);
                if (templateImg.Channels() > 3)
                {
                    Cv2.CvtColor(templateImg, templateImg, ColorConversionCodes.BGR2RGB);
                    Cv2.CvtColor(templateImg, templateImg, ColorConversionCodes.RGB2BGR);
                }
                Rmat.Dispose();

                if (templateImg.Cols == 0 || templateImg.Rows == 0)
                {
                    templateImg.Dispose();
                    return null;
                }
                return templateImg;
            }
            catch (Exception ex)
            {
                logger.Create("Get ROI Error: " + ex.Message, ex);
                return null;
            }
        }
        public List<string> GetHardwareDevices1()
        {
            List<string> devices = new List<string>();

            // Lấy CPU
            using (var searcher = new ManagementObjectSearcher("select Name from Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                {
                    devices.Add("CPU - " + obj["Name"].ToString());
                }
            }

            // Lấy GPU
            using (var searcher = new ManagementObjectSearcher("select Name from Win32_VideoController"))
            {
                foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                {
                    devices.Add("GPU - " + obj["Name"].ToString());
                }
            }
            return devices;
        }
        public List<string> GetHardwareDevices()
        {
            List<string> devices = new List<string>();
            try
            {
                int gpuCount = nrt.Device.get_num_gpu_devices();
                // Lấy CPU
                nrt.Device cpuDevice = nrt.Device.get_device(-1, nrt.DevType.DEVICE_CPU);
                string cpuName = cpuDevice.get_device_name();
                devices.Add("CPU - " + cpuName);

                // Lấy GPU
                for (int i = 0; i < gpuCount; i++)
                {
                    nrt.Device gpu = nrt.Device.get_device(i, nrt.DevType.DEVICE_CUDA_GPU);
                    string gpuName = gpu.get_device_name();
                    devices.Add($"GPU{i + 1} - {gpuName}");
                }
            }
            catch (Exception ex)
            {
                logger.Create("Get Hardware Devices Error: " + ex.Message, ex);
            }
            return devices;
        }
        private Mat FcRotProcess(Mat src, nrt.Result res, nrt.FlowchartNode node)
        {
            try
            {
                nrt.Predictor predictor = node.get_predictor();
                int angleCount = (int)res.angles.get_count();
                for (int i = 0; i < angleCount; i++)
                {
                    nrt.Angle angle = res.angles.get(i);
                    int deg = angle.degree;
                    if (angle.has_child() || node.has_child(i))
                    {
                        RotateImage(src, deg, out src);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("FcRot Process Error: " + ex.Message, ex);
            }
            return src.Clone();
        }
        private Mat FcDetProcess(Mat src, nrt.Result res, nrt.FlowchartNode node, out float score)
        {
            score = 0;
            try
            {
                nrt.Predictor predictor = node.get_predictor();
                int bboxCount = (int)res.bboxes.get_count();
                for (int i = 0; i < bboxCount; i++)
                {
                    nrt.Bbox bbox = res.bboxes.get(i);
                    int clsIdx = bbox.class_idx;
                    float scoreI = res.probs.get(i, clsIdx);
                    if (scoreI > score)
                    {
                        score = scoreI;
                    }

                    if (bbox.has_child() || node.has_child(clsIdx))
                    {
                        //Draw Image
                        OIConverter oiConverter = node.get_child(clsIdx).get_oiconverter();
                        ProcessImageDet(src, bbox.rect, oiConverter, out src);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("FcDet Process Error: " + ex.Message, ex);
            }
            
            return src.Clone();
        }
        private void FcClaResult(nrt.Result res, nrt.FlowchartNode node, bool anc_flag = false)
        {
            nrt.Predictor predictor = node.get_predictor();
            for (int i = 0; i < (int)res.classes.get_count(); i++)
            {
                nrt.Class cla = res.classes.get(i);
                int cla_idx = anc_flag ? 0 : cla.idx;
                float prob = res.probs.get(i, cla_idx);
                if (cla.has_child())
                {
                    //FlowchartOutput(cla.get_child_result(), node.get_child(cla.idx));
                }
            }
        }
        private void FcRotResult(nrt.Result res, nrt.FlowchartNode node)
        {
            nrt.Predictor predictor = node.get_predictor();
            for (int i = 0; i < (int)res.angles.get_count(); i++)
            {
                nrt.Angle angle = res.angles.get(i);
                int degree = angle.degree;
                if (angle.has_child())
                {
                    foreach (nrt.FlowchartNode child_node in node.get_children())
                    {
                        //FlowchartOutput(angle.get_child_result(), child_node);
                    }
                }
            }
        }
        private void FcDetResult(nrt.Result res, nrt.FlowchartNode node)
        {
            nrt.Predictor predictor = node.get_predictor();
            for (int i = 0; i < (int)res.bboxes.get_count(); i++)
            {
                nrt.Bbox bbox = res.bboxes.get(i);
                int batchIdx = bbox.batch_idx;
                int clsIdx = bbox.class_idx;
                float prob = res.probs.get(i, clsIdx);

                if (bbox.has_child())
                {
                    //FlowchartOutput(bbox.get_child_result(), node.get_child(clsIdx));
                }
            }
            // From version 4.1, the masking function has been updated to mask all areas of a predicted class in the predicted image.
            // You can choose to copy either the entire image or only the predicted images for the next prediction step in the Inference Center.
            // For more details, please refer to the Inference Center section of the Neuro-T Manual.
            for (int i = 0; i < (int)res.images.get_count(); i++)
            {
                nrt.Image img = res.images.get(i);
                if (img.has_child())
                {
                    foreach (nrt.FlowchartNode child_node in node.get_children())
                    {
                        //FlowchartOutput(img.get_child_result(), child_node);
                    }
                }
            }
        }
        private void FcSegResult(nrt.Result res, nrt.FlowchartNode node, int depth, ref List<Mat> srcLst, ref Mat src, ref List<float> scoreLst, ref float score)
        {
            try
            {
                nrt.Predictor predictor = node.get_predictor();
                int blobCount = (int)res.blobs.get_count();
                for (int i = 0; i < blobCount; i++)
                {
                    nrt.Blob blob = res.blobs.get(i);
                    int clsIdx = blob.class_idx;
                    float scoreI = blob.prob;
                    if (scoreI > score)
                    {
                        score = scoreI;
                    }
                    if(depth == 0)
                    {
                        src = srcLst[blob.batch_idx].Clone();
                        score = scoreLst[blob.batch_idx];
                    }  
                    nrt.Points points = blob.get_contour();
                    //nrt.Points pointsHole = blob.get_holes();
                    List<OpenCvSharp.Point> contour = new List<OpenCvSharp.Point>();
                    List<List<OpenCvSharp.Point>> contoursList = new List<List<OpenCvSharp.Point>> { contour };
                    for (int pi = 0; pi < (int)points.get_count(); pi++)
                    {
                        nrt.Point point = points.get(pi);
                        contour.Add(new OpenCvSharp.Point((int)point.x, (int)point.y));
                    }
                    //Phần xử lý ảnh Mat
                    if (blob.has_child() || node.has_child(clsIdx))
                    {
                        foreach (nrt.FlowchartNode child_node in node.get_children())
                        {
                            //Draw Image
                            OIConverter oiConverter = node.get_child(clsIdx).get_oiconverter();
                            ProcessImageSeg(src.Clone(), contour, blob.rect, oiConverter, out src);
                            FlowchartOutput(blob.get_child_result(), child_node, depth + 1, ref srcLst, ref src, ref scoreLst, ref score);
                        }
                    }
                    else
                    {
                        //Draw Image
                        Cv2.DrawContours(src, contoursList, -1, Scalar.Red, 2, lineType: LineTypes.Link8);
                    }
                    if (depth == 0)
                    {
                        scoreLst[blob.batch_idx] = score;
                        srcLst[blob.batch_idx] = src.Clone();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("FcSeg Process Error: " + ex.Message, ex);
            }
        }

        private void FlowchartOutput(nrt.Result res, nrt.FlowchartNode node, int depth, ref List<Mat> srcLst, ref Mat src, ref List<float> scoreLst, ref float score)
        {
            nrt.Predictor predictor = node.get_predictor();
            nrt.ModelType modelType = predictor.get_model_type();
            switch (modelType)
            {
                case nrt.ModelType.CLASSIFICATION:
                    FcClaResult(res, node);
                    break;
                case nrt.ModelType.ANOMALY_CLASSIFICATION:
                    FcClaResult(res, node, true);
                    break;
                case nrt.ModelType.DETECTION:
                case nrt.ModelType.PATCHED_CLASSIFICATION:
                case nrt.ModelType.OCR:
                    FcDetResult(res, node);
                    break;
                case nrt.ModelType.SEGMENTATION:
                case nrt.ModelType.ANOMALY_SEGMENTATION:
                    FcSegResult(res, node, depth, ref srcLst, ref src, ref scoreLst, ref score);
                    break;
                case nrt.ModelType.ROTATION:
                    FcRotResult(res, node);
                    break;
                default:
                    break;
            }
        }
        public void ResetBuffer()
        {
            Inc = 0;
            RunImageLst.Clear();
            ScoreLst.Clear();
            JudgeLst.Clear();
            MessLst.Clear();
            inputs.clear();
        }
        private void SelectBorder(Border bdSelected)
        {
            try
            {
                toolBase.cbxImage.SelectedItem = 1;
                List<Expander> expdLst = stViewImg.Children.OfType<Expander>().ToList();
                List<Border> bdLst = expdLst.Select(exp => exp.Content as Border).Where(border => border != null).ToList();
                bdLst.ForEach(border => border.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC"));
                bdSelected.BorderBrush = Brushes.Red;
                Grid gridImg = (bdSelected.Child as StackPanel).Children.OfType<Grid>().FirstOrDefault();
                toolBase.imgView.Source = gridImg.Children.OfType<Image>().FirstOrDefault().Source;
                toolBase.FitImage();
            }
            catch (Exception ex)
            {
                logger.Create("Select Image Log Error: " + ex.Message, ex);
            }
        }
        private void CreateImgBuffView(int index, Mat matImg)
        {
            if (matImg == null || matImg.Width == 0 || matImg.Height == 0)
                return;
            Expander expd = new Expander
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC")
            };
            Label lbHeader = new Label
            {
                FontStyle = FontStyles.Italic,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(3, 5, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(72, 158, 55)), // #FF489E37
                Content = $"Image {index}"
            };
            expd.Header = lbHeader;
            StackPanel mainPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = Brushes.Cornsilk
            };
            // Grid with Image
            Grid grid = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(153, 153, 153)), // #FF999999
                Width = 250,
                Height = 150,
                Margin = new Thickness(10, 0, 10, 0)
            };
            Image img = new Image { Source = matImg.ToBitmapSource() };
            grid.Children.Add(img);

            Border brdr = new Border() { Name = $"Bd{index}", BorderThickness = new Thickness(2), BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC"), };
            brdr.MouseMove += (sender, e) =>
            {
                Border bdSelected = sender as Border;
                List<Expander> expdLst = stViewImg.Children.OfType<Expander>().ToList();
                List<Border> bdLst = expdLst.Select(exp => exp.Content as Border).Where(border => border != null).ToList();
                foreach (var bd in bdLst)
                {
                    if (bd.BorderBrush == Brushes.Red)
                        continue;
                    bd.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FFCCCCCC");
                }
                if (bdSelected.BorderBrush != Brushes.Red)
                    bdSelected.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF489E37");
            };
            brdr.MouseLeftButtonDown += (sender, e) => SelectBorder(sender as Border);
            // Right StackPanel
            StackPanel rightPanel = new StackPanel();

            // Shared Style for Label
            Style labelStyle = new Style(typeof(Label));
            labelStyle.Setters.Add(new Setter(Label.PaddingProperty, new Thickness(1)));
            labelStyle.Setters.Add(new Setter(Label.ForegroundProperty, new SolidColorBrush(Color.FromRgb(72, 158, 55)))); // #FF489E37
            labelStyle.Setters.Add(new Setter(Label.FontStyleProperty, FontStyles.Italic));

            // Add labels with style
            Label label1 = new Label { Content = $"Index : {index}", Padding = new Thickness(0, 5, 0, 1), Style = labelStyle };
            Label label2 = new Label { Content = $"Time : {DateTime.Now: dd/MM/yyyy HH:mm:ss:fff}", Style = labelStyle };
            Label label3 = new Label { Content = $"Width x Height : {matImg.Width} x {matImg.Height}", Style = labelStyle };

            rightPanel.Children.Add(label1);
            rightPanel.Children.Add(label2);
            rightPanel.Children.Add(label3);

            // Assemble
            mainPanel.Children.Add(grid);
            mainPanel.Children.Add(rightPanel);
            brdr.Child = mainPanel;
            expd.Content = brdr;

            //Add Expander
            stViewImg.Children.Add(expd);
        }
        private Mat ProcessBlobsImage(Mat src, List<OpenCvSharp.Point> contour, OpenCvSharp.Rect rectBound)
        {
            //1.Tạo mask trắng(đen hết)
            using (Mat mask = Mat.Zeros(src.Size(), MatType.CV_8UC1))
            {
                // 2. Vẽ contour lên mask (trắng vùng contour)
                Cv2.FillPoly(mask, new[] { contour.ToArray() }, Scalar.White);
                // 3. Áp mask lên ảnh gốc (bitwise AND)
                using (Mat matRes = new Mat())
                {
                    Cv2.BitwiseAnd(src.Clone(), src.Clone(), matRes, mask);
                    // 4. Cắt vùng ảnh chứa sản phẩm 
                    src = new Mat(matRes, rectBound);
                }
            }
            return src.Clone();
        }
        #region Save Image
        private bool IsValidPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            char[] invalidChars = Path.GetInvalidPathChars();
            return !path.Any(c => invalidChars.Contains(c));
        }
        private void DeleteOldestFile(string folderPath, double maxSizeInGB)
        {
            try
            {
                var allFiles = Directory.GetFiles(folderPath);
                double totalSizeInBytes = allFiles.Sum(file =>
                {
                    try
                    {
                        return new FileInfo(file).Length;
                    }
                    catch
                    {
                        return 0; // Bỏ qua nếu file đang bị khóa
                    }
                });

                double totalSizeInGB = totalSizeInBytes / (1024 * 1024 * 1024);

                if (totalSizeInGB >= maxSizeInGB)
                {
                    var oldestFile = allFiles
                        .Select(f =>
                        {
                            try
                            {
                                return new FileInfo(f);
                            }
                            catch
                            {
                                return null;
                            }
                        })
                        .Where(f => f != null)
                        .OrderBy(f => f.LastWriteTime)
                        .FirstOrDefault();

                    if (oldestFile != null)
                    {
                        try
                        {
                            File.Delete(oldestFile.FullName);
                        }
                        catch (Exception ex)
                        {
                            logger.Create("Delete File Error: " + ex.Message, ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                meaRunTime.Stop();
                toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Delete file error: " + ex.Message);
                logger.Create("Delete oldest file error: " + ex.Message, ex);
            }
        }

        private string FormatBytes(long bytes, out string size)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            try
            {
                //Bỏ qua TB
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }
            }
            catch (Exception ex)
            {
                logger.Create("Format Bytes Error: " + ex.Message, ex);
            }
            //return $"{len:0.##} {sizes[order]}";
            size = sizes[order];
            return $"{len:0.##}";
        }
        private void BtnCheckDisk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender != null)
                {
                    if (string.IsNullOrEmpty(txtFolderPath.Text))
                    {
                        MessageBox.Show("Folder Path is Empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (!IsValidPath(txtFolderPath.Text))
                    {
                        MessageBox.Show("FolderPath Error Syntax!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(txtFolderPath.Text) || !IsValidPath(txtFolderPath.Text))
                        return;
                }
                string drive = Path.GetPathRoot(txtFolderPath.Text);
                DriveInfo driveInfo = new DriveInfo(drive);
                if (driveInfo.IsReady)
                {
                    long totalFree = driveInfo.AvailableFreeSpace;
                    txtFreeDisk.Text = FormatBytes(totalFree, out string tempSize);
                    this.DiskSize = tempSize;
                    MaxImageStorage = double.Parse(txtFreeDisk.Text) - 1d;
                }
                else
                {
                    MessageBox.Show("Disk C is not Ready!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnResetIndex_Click(object sender, RoutedEventArgs e)
        {
            IndexImage = 0;
        }
        private void BtnChooseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select Folder";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    txtFolderPath.Text = dialog.SelectedPath;
                }
            }
        }
        private void SaveGraphicImage(Mat src)
        {
            // Kiểm tra folder tồn tại
            if (!IsValidPath(txtFolderPath.Text))
            {
                meaRunTime.Stop();
                toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "FolderPath Error Syntax!");
                return;
            }
            if (!Directory.Exists(txtFolderPath.Text))
                Directory.CreateDirectory(txtFolderPath.Text);

            // Xử lý tên file
            if (IsAddCounter)
            {
                if (IndexImage >= NumUDCounter)
                    IndexImage = 0;
                IndexImage++;
            }
            string timestamp = IsAddDateTime
                ? $"{DateTime.Now:yyyy-MM-dd HH-mm-ss-fff}"
                : "";
            string strIndex = IsAddCounter ? $"-{IndexImage}" : "";
            if (string.IsNullOrEmpty(txtFileName.Text))
                txtFileName.Text = "Default";
            if (string.IsNullOrEmpty(ImageFormatSelected))
                ImageFormatSelected = "BMP";
            string finalFileName = $"{txtFileName.Text}{strIndex} {timestamp}.{ImageFormatSelected.ToLower()}";
            string fullPath = Path.Combine(txtFolderPath.Text, finalFileName);

            //Kiểm tra và xóa file ảnh cũ nhất trong trường hợp folder đầy dung lượng cho phép
            DeleteOldestFile(txtFolderPath.Text, NumUDImageStorage);
            // Lưu ảnh
            if (!Cv2.ImWrite(fullPath, src))
            {
                meaRunTime.Stop();
                toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Save Image Fail!");
                return;
            }
        }
        #endregion

        #region PLC
        public void CheckAddrUse()
        {
            this.Dispatcher.Invoke(() =>
            {
                if ((bool)ckbxIsUseBitRcvImg.IsChecked)
                {
                    if (string.IsNullOrEmpty(TxtAddrRcvImg) || !IsPositiveInteger(TxtAddrRcvImg))
                    {
                        MessageBox.Show("PLC Address RcvImage is Empty or Error Syntax!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        logger.Create("PLC Address RcvImage is Empty or Error Syntax!");
                        return;
                    }
                }
                if ((bool)ckbxIsUseBitReset.IsChecked)
                {

                    if (string.IsNullOrEmpty(TxtAddrReset) || !IsPositiveInteger(TxtAddrReset))
                    {
                        MessageBox.Show("PLC Address Reset is Empty or Error Syntax!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        logger.Create("PLC Address Reset is Empty or Error Syntax!");
                        return;
                    }
                }
            });
        }
        public bool String2Enum(string strDev, out DeviceCode _devType, out string _strDevNo)
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
                logger.Create("Convert syntax error: " + ex.Message);
            }
            return isDefined;
        }
        bool IsPositiveInteger(string input)
        {
            return int.TryParse(input, out int number) && number >= 0;
        }
        public bool SendBitRcvImg(bool value)
        {
            try
            {
                return UiManager.PLC1.device.WriteBit(SelectDevRcvImg, int.Parse(TxtAddrRcvImg), value);
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("SEND_TRIGGER_COMPLETE Error: " + ex.Message));
                return false;
            }
        }
        public bool ReceiveBitReset(out bool value)
        {
            value = false;
            try
            {
                return UiManager.PLC1.device.ReadBit(SelectDevReset, int.Parse(TxtAddrReset), out value);
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("RECEIVE_RESET Error: " + ex.Message));
                return false;
            }
        }
        public bool SendBitReset(bool value)
        {
            try
            {
                return UiManager.PLC1.device.WriteBit(SelectDevReset, int.Parse(TxtAddrReset), value);
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("SEND_RESET Error: " + ex.Message));
                return false;
            }
        }
        public void SendJudge(int index, bool value)
        {
            try
            {
                String2Enum(addrOKLst[index], out DeviceCode devTypeOK, out string strDevNoOK);
                Task.Run(() => UiManager.PLC1.device.WriteBit(devTypeOK, int.Parse(strDevNoOK), value)); 
                String2Enum(addrNGLst[index], out DeviceCode devTypeNG, out string strDevNoNG);
                Task.Run(() => UiManager.PLC1.device.WriteBit(devTypeNG, int.Parse(strDevNoNG), !value));
            }
            catch (Exception ex)
            {
                logger.Create(String.Format("SEND_JUDGE Error: " + ex.Message));
            }
        }
        public void UpdateAddrOut()
        {
            try
            {
                for (int col = 1; col < dataTable.Columns.Count; col++)
                {
                    // Lấy và parse giá trị từ DataTable
                    if (string.IsNullOrEmpty(dataTable.Rows[0][col].ToString()) || string.IsNullOrEmpty(dataTable.Rows[1][col].ToString()))
                    {
                        MessageBox.Show("Address PLC can not empty!");
                        return;
                    }
                    else
                    {
                        addrOKLst[col - 1] = dataTable.Rows[0][col]?.ToString().TrimEnd('\t');
                        addrNGLst[col - 1] = dataTable.Rows[1][col]?.ToString().TrimEnd('\t');
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
                // Tạo cột tương ứng với các phần tử trong img
                for (int i = 0; i < NumberPos; i++)
                {
                    string columnHeader = $"Pos {i + 1}";
                    dataTable.Columns.Add(columnHeader);
                }

                // Thêm dòng Address
                if (!string.IsNullOrEmpty(TxtAddrOK))
                {
                    DataRow rowOK = dataTable.NewRow();
                    rowOK["Position"] = "Address OK";
                    for (int i = 0; i < NumberPos; i++)
                    {
                        rowOK[$"Pos {i + 1}"] = addrOKLst[i] + "\t";
                    }
                    dataTable.Rows.Add(rowOK);
                }
                if (!string.IsNullOrEmpty(TxtAddrNG))
                {
                    DataRow rowNG = dataTable.NewRow();
                    rowNG["Position"] = "Address NG";
                    for (int i = 0; i < NumberPos; i++)
                    {
                        rowNG[$"Pos {i + 1}"] = addrNGLst[i] + "\t";
                    }
                    dataTable.Rows.Add(rowNG);
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
        private void BtnRefreshTb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(TxtAddrOK) || !IsPositiveInteger(TxtAddrOK))
                {
                    MessageBox.Show("PLC Address OK is Empty or Error Syntax!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (string.IsNullOrEmpty(TxtAddrNG) || !IsPositiveInteger(TxtAddrNG))
                {
                    MessageBox.Show("PLC Address NG is Empty or Error Syntax!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (NumberPos <= 0)
                {
                    MessageBox.Show("Number Position must be > 0!");
                    return;
                }
                addrOKLst.Clear();
                addrNGLst.Clear();
                for (int i = 0; i < NumberPos; i++)
                {
                    if (!string.IsNullOrEmpty(TxtAddrOK))
                    {
                        addrOKLst.Add(String.Format($"{SelectDevOK.ToString()}{int.Parse(TxtAddrOK) + i}"));
                    }
                    if (!string.IsNullOrEmpty(TxtAddrNG))
                    {
                        addrNGLst.Add(String.Format($"{SelectDevNG.ToString()}{int.Parse(TxtAddrNG) + i}"));
                    }
                }
                UpdateDataGrid();
            }
            catch (Exception ex)
            {
                logger.Create("Button Refresh PLC Addr Table Error: " + ex.Message, ex);
            }
        }
        private void SegmentNeuroEdit_Unloaded(object sender, RoutedEventArgs e)
        {
            UpdateAddrOut();
            CheckAddrUse();
        }
        #endregion

        public override void Run()
        {
            try
            {
                CheckAddrUse();
                if (this.InputImage == null || this.InputImage.Mat == null || this.InputImage.Mat.Width <= 0 || this.InputImage.Mat.Height <= 0)
                {
                    if (toolBase.isImgPath && isEditMode)
                    {
                        this.Dispatcher.Invoke(() => runImage.Mat = (ImgView.Source as BitmapSource).ToMat());
                        runImage.RegionRect.Rect = new OpenCvSharp.Rect(0, 0, (int)ImgView.Source.Width, (int)ImgView.Source.Height);
                        if (runImage.Mat.Channels() != 3)
                        {
                            runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.BGR2RGB);
                            runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.RGB2BGR);
                        }
                    }
                    else
                    {
                        meaRunTime.Stop();
                        toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "InputImage is null or error!");
                        return;
                    }
                }
                else if (InputImage.Mat != null && toolBase.isImgPath && isEditMode)
                {
                    this.Dispatcher.Invoke(() => runImage.Mat = (ImgView.Source as BitmapSource).ToMat());
                    runImage.RegionRect.Rect = new OpenCvSharp.Rect(0, 0, (int)ImgView.Source.Width, (int)ImgView.Source.Height);
                    if (runImage.Mat.Channels() != 3)
                    {
                        runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.BGR2RGB);
                        runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.RGB2BGR);
                    }
                }
                else if (InputImage.Mat != null && InputImage.Mat.Width > 0 && InputImage.Mat.Height > 0 && !toolBase.isImgPath)
                {
                    runImage = this.InputImage.Clone(true);
                    if (runImage.Mat.Channels() != 3)
                    {
                        runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.BGR2RGB);
                        runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.RGB2BGR);
                    }
                }

                while (!isFcInitCompl || !isPdInitCompl) ;
                if (Inc < NumberPos)
                {
                    if(Inc == 0)
                    {
                        ResetBuffer();
                    }
                    //Khởi tạo các List phán định
                    RunImageLst.Add(runImage.Mat);
                    ScoreLst.Add(0);
                    JudgeLst.Add(0);
                    MessLst.Add("");
                    //Convert OpencvSharp.Mat data to nrt.Input
                    IntPtr frameData = runImage.Mat.Data;
                    int batchSize = 1;
                    int inputH = runImage.Mat.Height;
                    int inputW = runImage.Mat.Width;
                    // Extend memory address value to input
                    status = inputs.extend(frameData, new nrt.Shape(batchSize, inputH, inputW, 3), nrt.DType.DTYPE_UINT8);
                    if (status != nrt.Status.STATUS_SUCCESS)
                    {
                        meaRunTime.Stop();
                        toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, String.Format("Input extend failed : " + nrt.nrt.get_last_error_msg()));
                        logger.Create(String.Format("Input extend failed : " + nrt.nrt.get_last_error_msg()));
                    }
                    Inc++;
                    //Gửi Bit chụp ảnh hoàn thành cho PLC
                    Dispatcher.Invoke(() =>
                    {
                        if ((bool)ckbxIsUseBitRcvImg.IsChecked)
                            SendBitRcvImg(false);
                    });
                    OutputImage.Mat = runImage.Mat.Clone();
                }    
                if(Inc == NumberPos)
                {
                    Inc = 0;
                    //Kiểm tra các Flowchart
                    try
                    {
                        foreach (var flowchart in flowcharts)
                        {
                            nrt.FlowchartNode root = flowchart.Value.get_node(0);
                            nrt.Result result = flowchart.Value.predict(inputs);
                            if (result.get_status() != nrt.Status.STATUS_SUCCESS)
                            {
                                meaRunTime.Stop();
                                toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, String.Format("Predict failed : " + nrt.nrt.get_last_error_msg()));
                                logger.Create("Predict failed : " + nrt.nrt.get_last_error_msg());
                                return;
                            }
                            float score = 0;
                            Mat src = new Mat();
                            FlowchartOutput(result, root, 0, ref RunImageLst, ref src, ref ScoreLst, ref score);
                            for (int i = 0; i < ScoreLst.Count; i++)
                            {
                                int rangeScore = modelList[flowchart.Key].Score;
                                if (ScoreLst[i] > (float)modelList[flowchart.Key].Score / 100f)
                                {
                                    JudgeLst[i] = 2;
                                    MessLst[i] = "NG";
                                    RunImageLst[i].PutText(String.Format($"Status: {MessLst[i]}"), new OpenCvSharp.Point(20, 40), HersheyFonts.HersheyDuplex, 1d, Scalar.Red, 2);
                                    RunImageLst[i].PutText(String.Format($"Score:  {(ScoreLst[i] * 100).ToString("F2")}/{rangeScore}"), new OpenCvSharp.Point(20, 70), HersheyFonts.HersheyDuplex, 1d, Scalar.Red, 2);
                                }
                                else if (ScoreLst[i] == 0)
                                {
                                    int blobCount = 0;
                                    Dispatcher.Invoke(() =>
                                    {
                                        BlobEdit blob = new();
                                        blob.InputImage = new SvImage()
                                        {
                                            Mat = RunImageLst[i]
                                        };
                                        blob.Range = 140;
                                        blob.SelectBlobPolarity = BlobEdit.BlobPolarity.Black;
                                        blob.BlobFilters[0].Use = true;
                                        blob.BlobFilters[0].Low = 1500000;
                                        blob.Run();
                                        blobCount = blob.Blobs.Count;
                                    });
                                    if (blobCount > 0)
                                    {
                                        JudgeLst[i] = 1;
                                        MessLst[i] = "OK";
                                        RunImageLst[i].PutText(String.Format($"Status: {MessLst[i]}"), new OpenCvSharp.Point(20, 40), HersheyFonts.HersheyDuplex, 1d, Scalar.LightGreen, 2);
                                        RunImageLst[i].PutText(String.Format($"Score:  {(ScoreLst[i] * 100).ToString("F2")}/{rangeScore}"), new OpenCvSharp.Point(20, 70), HersheyFonts.HersheyDuplex, 1d, Scalar.LightGreen, 2);
                                    }
                                    else
                                    {
                                        JudgeLst[i] = 2;
                                        MessLst[i] = "Empty";
                                        RunImageLst[i].PutText(String.Format($"Status: {MessLst[i]}"), new OpenCvSharp.Point(20, 40), HersheyFonts.HersheyDuplex, 1d, Scalar.Red, 2);
                                        RunImageLst[i].PutText(String.Format($"Score:  {(ScoreLst[i] * 100).ToString("F2")}/{rangeScore}"), new OpenCvSharp.Point(20, 70), HersheyFonts.HersheyDuplex, 1d, Scalar.Red, 2);
                                    }
                                }
                                else
                                {
                                    JudgeLst[i] = 1;
                                    MessLst[i] = "OK";
                                    RunImageLst[i].PutText(String.Format($"Status: {MessLst[i]}"), new OpenCvSharp.Point(20, 40), HersheyFonts.HersheyDuplex, 1d, Scalar.LightGreen, 2);
                                    RunImageLst[i].PutText(String.Format($"Score:  {(ScoreLst[i] * 100).ToString("F2")}/{rangeScore}"), new OpenCvSharp.Point(20, 70), HersheyFonts.HersheyDuplex, 1d, Scalar.LightGreen, 2);
                                }
                                //Gửi phán định OK/NG cho PLC
                                SendJudge(i, (JudgeLst[i] == 1)); 
                            }
                            Task.Run(() =>
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    //stViewImg.Children.Clear();
                                    for (int i = 0; i < RunImageLst.Count; i++)
                                    {
                                        //CreateImgBuffView(i, RunImageLst[i].Clone());
                                        if (!(bool)ckbxIsSaveImg.IsChecked)
                                            continue;
                                        SaveGraphicImage(RunImageLst[i]);
                                    }
                                });

                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create($"Process Flowchart Error :{ex}", ex);
                        meaRunTime.Stop();
                        toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, $"Process Flowchart Error :{ex}");
                        return;
                    }
                    OutputImage.Mat = RunImageLst[RunImageLst.Count - 1].Clone();
                    this.Dispatcher?.Invoke(() =>
                    {
                        toolBase.cbxImage.SelectedIndex = isEditMode ? 1 : 0;
                        toolBase.FitImage();
                    });
                }   
                else
                {
                    return;
                }    
            }
            catch (Exception ex)
            {
                logger.Create("Run Error: " + ex.Message, ex);
            }
        }
    }
    public class NeuroModel
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string PathRuntime { get; set; } = "";
        public string Device { get; set; } = "";
        public int DeviceIdx { get; set; } = -1;
        public int Score { get; set; } = 0;
        public int BatchSize { get; set; } = 1;

        public NeuroModel()
        {
            Name = "";
            Path = "";
            PathRuntime = "";
            Device = "";
            DeviceIdx = -1;
            Score = 0;
            BatchSize = 1;
        }
        public NeuroModel(string name = "", string path = "", string pathRuntime = "", string device = "", int deviceIdx = -1, int score = 0, int batchSize = 1)
        {
            this.Name = name;
            this.Path = path;
            this.PathRuntime = pathRuntime;
            this.Device = device;
            this.DeviceIdx = deviceIdx;
            this.Score = score;
            this.BatchSize = batchSize;
        }
        public NeuroModel Clone()
        {
            return new NeuroModel()
            {
                Name = this.Name,
                Path = this.Path,
                PathRuntime = this.PathRuntime,
                Device = this.Device,
                DeviceIdx = this.DeviceIdx,
                Score = this.Score,
                BatchSize = this.BatchSize
            };
        }
    }
    public class NeuroFc
    {
        public nrt.ModelType ModelType { get; set; } = ModelType.NONE;
        public nrt.Result Result { get; set; }
        public nrt.FlowchartNode FlowchartNode { get; set; }
        public NeuroFc()
        {
            this.ModelType = ModelType.NONE;
            this.Result = new nrt.Result();
            this.FlowchartNode = new nrt.FlowchartNode();
        }
        public NeuroFc(nrt.ModelType modelType, nrt.Result result, nrt.FlowchartNode flowchartNode)
        {
            this.ModelType = modelType;
            this.Result = result;
            this.FlowchartNode = flowchartNode;
        }
        public NeuroFc Clone()
        {
            return new NeuroFc
            {
                ModelType = this.ModelType,
                Result = this.Result,
                FlowchartNode = this.FlowchartNode
            };
        }
    }
}
