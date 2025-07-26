using nrt;
using OpenCvSharp;
using OpenCvSharp.Cuda;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public event RoutedEventHandler OnBtnRunClicked;
        private Dictionary<int, nrt.Predictor> predictors = new Dictionary<int, Predictor>();
        private Dictionary<int, nrt.Flowchart> flowcharts = new Dictionary<int, nrt.Flowchart>();
        private nrt.Status status;
        private List<NeuroFc> neuroFcLst = new List<NeuroFc>();
        public List<NeuroModel> modelList = new List<NeuroModel>();
        public bool isPdInitCompl = true;
        public bool isFcInitCompl = true;

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
        public double Score { get; set; } = 0.0;
        public string StrResult { get; set; } = "OK";
        public int Judge { get; set; } = 0;
        //[*********************************** BINDING ***********************************]
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<string> BindableDevices { get; set; } = new ObservableCollection<string>();
        public string DeviceSelected { get; set; } = "";
        public int DevCbxIdx { get; set; } = 0;
        public int xctScore { get; set; } = 0;
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
            CreatNewModel(txtModelName.Text, txtModelPath.Text, "", DeviceSelected, DevCbxIdx-1, Convert.ToInt32(xctScore), modelList.Count);
        }
        private List<TextBox> txtModelNameList = new List<TextBox>();
        public void CreatNewModel(string name, string path, string pathRuntime, string devSelected, int deviceIdx, int Score, int index)
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
                xct.ValueChanged += Xct_ValueChanged;
                st2.Children.Add(lb2);
                st2.Children.Add(xct);

                //Expander
                st.Children.Add(st1);
                st.Children.Add(st3);
                st.Children.Add(st4);
                st.Children.Add(st2);
                expander.Content = st;
                NeuroModel neuroModel = new NeuroModel(name, path, pathRuntime, devSelected, deviceIdx, Score);
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
                                curPredict = new nrt.Predictor(modelList[i].Path, nrt.Model.MODELIO_OUT_PROB, modelList[i].DeviceIdx);
                                predictors.Add(i, curPredict);
                            }
                            else
                            {
                                curPredict = new nrt.Predictor(modelList[i].Path, nrt.Model.MODELIO_OUT_PROB, modelList[i].DeviceIdx, 1, false, false, nrt.DevType.DEVICE_CUDA_GPU);
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
                                curFlowchart = new nrt.Flowchart(modelList[i].Path, nrt.Model.MODELIO_OUT_PROB, modelList[i].DeviceIdx, 1, false, false);
                                flowcharts.Add(i, curFlowchart);
                            }
                            else
                            {
                                // CPU's device_idx = -1, GPU's device_idx = [0, num of device)
                                // The default device setting for the Flowchart is CPU(-1).
                                // flowchart = new nrt.Flowchart(flowchartPath);
                                curFlowchart = new nrt.Flowchart(modelList[i].Path, nrt.Model.MODELIO_OUT_PROB, modelList[i].DeviceIdx, 1, false, false, nrt.DevType.DEVICE_CUDA_GPU);
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
                                matRes.SaveImage("01. MatRes.bmp");
                                src.SaveImage("01. src.bmp");
                                mask.SaveImage("01. mask.bmp");
                            }
                        }
                        src = GetROIRegion(src.Clone(), rectBound, degAngle);
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
        private Mat FcSegProces(Mat src, nrt.Result res, nrt.FlowchartNode node, out float score)
        {
            score = 0;
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
                    nrt.Points points = blob.get_contour();
                    List<OpenCvSharp.Point> contour = new List<OpenCvSharp.Point>();
                    List<List<OpenCvSharp.Point>> contoursList = new List<List<OpenCvSharp.Point>> { contour };
                    for (int pi = 0; pi < (int)points.get_count(); pi++)
                    {
                        nrt.Point point = points.get(pi);
                        contour.Add(new OpenCvSharp.Point((int)point.x, (int)point.y));
                    }
                    if (blob.has_child() || node.has_child(clsIdx))
                    {
                        //Draw Image
                        OIConverter oiConverter = node.get_child(clsIdx).get_oiconverter();
                        ProcessImageSeg(src, contour, blob.rect, oiConverter, out src);
                    }
                    else
                    {
                        //Draw Image
                        Cv2.DrawContours(src, contoursList, -1, Scalar.Red, 2, lineType: LineTypes.Link8);
                        src.SaveImage("1.TestSeg.bmp");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("FcSeg Process Error: " + ex.Message, ex);
            }
            return src.Clone();
        }
        private SvImage runImage = new SvImage();
        public override void Run()
        {
            try
            {
                if (this.InputImage == null || this.InputImage.Mat == null || this.InputImage.Mat.Width <= 0 || this.InputImage.Mat.Height <= 0)
                {
                    if (toolBase.isImgPath && isEditMode)
                    {
                        runImage.Mat = (ImgView.Source as BitmapSource).ToMat();
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
                    runImage.Mat = (ImgView.Source as BitmapSource).ToMat();
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
                }

                //Conver OpencvSharp.Mat data to nrt.Input
                nrt.Input input = new nrt.Input();
                IntPtr frameData = runImage.Mat.Data;
                int batchSize = 1;
                int inputH = runImage.Mat.Height;
                int inputW = runImage.Mat.Width;
                // Extend memory address value to input
                status = input.extend(frameData, new nrt.Shape(batchSize, inputH, inputW, 3), nrt.DType.DTYPE_UINT8);
                if (status != nrt.Status.STATUS_SUCCESS)
                {
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, String.Format("Input extend failed. : " + nrt.nrt.get_last_error_msg()));
                }



                bool isFcComplete = false;
                bool isPdComplete = false;
                float maxScore1 = 0, maxScore2 = 0;
                string strRes1 = "OK", strRes2 = "OK";
                int judge1 = 1, judge2 = 1;

                nrt.Result maxResult = new Result();
                Mat imgGraphic = runImage.Mat.Clone();
                StrResult = "OK";
                Judge = 1;
                Score = 0d;

                while (!isFcInitCompl || !isPdInitCompl) ;
                //Kiểm tra các Flowchart
                Task.Run(() =>
                {
                    try
                    {
                        foreach (var flowchart in flowcharts)
                        {
                            float score = 0;
                            nrt.FlowchartNode root = flowchart.Value.get_node(0);
                            nrt.Result result = flowchart.Value.predict(input);
                            if (result.get_status() != nrt.Status.STATUS_SUCCESS)
                            {
                                meaRunTime.Stop();
                                toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, String.Format("Predict failed. : " + nrt.nrt.get_last_error_msg()));
                                return;
                            }
                            //Clear toàn bộ các Model cũ trước khi nhận Model mới
                            neuroFcLst.Clear();
                            CheckModelType(result, root);
                            Mat imgMat = runImage.Mat.Clone();
                            for (int id = 0; id < neuroFcLst.Count; id++)
                            {
                                switch (neuroFcLst[id].ModelType)
                                {
                                    case nrt.ModelType.ROTATION:
                                        imgMat = FcRotProcess(imgMat, neuroFcLst[id].Result, neuroFcLst[id].FlowchartNode);
                                        break;
                                    case nrt.ModelType.CLASSIFICATION:
                                        break;
                                    case nrt.ModelType.DETECTION:
                                    case nrt.ModelType.PATCHED_CLASSIFICATION:
                                        imgMat = FcDetProcess(imgMat, neuroFcLst[id].Result, neuroFcLst[id].FlowchartNode, out score);
                                        break;
                                    case nrt.ModelType.SEGMENTATION:
                                        imgMat = FcSegProces(imgMat, neuroFcLst[id].Result, neuroFcLst[id].FlowchartNode, out score);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            if (score > maxScore1)
                            {
                                maxScore1 = score;
                                if (score > (float)modelList[flowchart.Key].Score / 100f)
                                {
                                    imgGraphic = imgMat.Clone();
                                    strRes1 = modelList[flowchart.Key].Name;
                                    judge1 = 2;
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create($"Process Flowchart Error :{ex}", ex);
                        Judge = 3;
                        Score = -1d;
                        StrResult = "Error";
                        meaRunTime.Stop();
                        toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, $"Process Flowchart Error :{ex}");
                        return;
                    }
                    isFcComplete = true;
                });


                //Kiểm tra các Predict đơn lẻ
                Task.Run(() =>
                {
                    try
                    {
                        foreach (var predictor in predictors)
                        {
                            float score = 0;
                            nrt.Result result = predictor.Value.predict(input);
                            if (result.get_status() != nrt.Status.STATUS_SUCCESS)
                            {
                                meaRunTime.Stop();
                                toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, String.Format("Predict failed. : " + nrt.nrt.get_last_error_msg()));
                                return;
                            }
                            for (int j = 0; j < (int)result.blobs.get_count(); j++)
                            {
                                nrt.Blob blob = result.blobs.get(j);
                                score = blob.prob;
                                if (score > maxScore2)
                                {
                                    maxScore2 = score;
                                    if (score > (float)modelList[predictor.Key].Score / 100f)
                                    {
                                        maxResult = result;
                                        strRes2 = modelList[predictor.Key].Name;
                                        judge2 = 2;
                                    }
                                }

                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create($"Process Predict Error :{ex}", ex);
                        Judge = 3;
                        Score = -1d;
                        StrResult = "Error";
                        meaRunTime.Stop();
                        toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, $"Process Predict Error :{ex}");
                        return;
                    }
                    isPdComplete = true;
                });

                while (!isFcComplete || !isPdComplete) ;
                if (maxScore2 > maxScore1)
                {
                    Score = maxScore2;
                    Judge = judge2;
                    StrResult = strRes2;
                    imgGraphic = runImage.Mat.Clone();
                    for (int i = 0; i < (int)maxResult.blobs.get_count(); i++)
                    {
                        // Get contour
                        nrt.Blob blob = maxResult.blobs.get(i);
                        nrt.Points points = blob.get_contour();
                        List<OpenCvSharp.Point> contour = new List<OpenCvSharp.Point>();
                        List<List<OpenCvSharp.Point>> contoursList = new List<List<OpenCvSharp.Point>> { contour };

                        for (int pi = 0; pi < (int)points.get_count(); pi++)
                        {
                            nrt.Point point = points.get(pi);
                            contour.Add(new OpenCvSharp.Point((int)point.x, (int)point.y));
                        }
                        Cv2.DrawContours(imgGraphic, contoursList, -1, Scalar.Red, 2, lineType: LineTypes.Link8);
                    }
                }
                else if(maxScore2 < maxScore1)
                {
                    Score = maxScore1; 
                    Judge = judge1;
                    StrResult = strRes1;
                }    
                OutputImage = runImage.Clone(true);
                if (maxScore2 == 0 && maxScore1 == 0)
                {
                    //meaRunTime.Stop();
                    //toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Have no model match!");
                    //return;
                }  
                OutputImage.Mat = imgGraphic;
                toolBase.cbxImage.SelectedIndex = isEditMode ? 1 : 0;
                toolBase.FitImage();
            }
            catch (Exception ex)
            {
                logger.Create("Run Error: " + ex.Message, ex);
                Judge = 3;
                Score = -1d;
                StrResult = "Error";
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

        public NeuroModel()
        {
            Name = "";
            Path = "";
            PathRuntime = "";
            Device = "";
            DeviceIdx = -1;
            Score = 0;
        }
        public NeuroModel(string name = "", string path = "", string pathRuntime = "", string device = "", int deviceIdx = -1, int score = 0)
        {
            this.Name = name;
            this.Path = path;
            this.PathRuntime = pathRuntime;
            this.Device = device;
            this.DeviceIdx = deviceIdx;
            this.Score = score;
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
                Score = this.Score
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
