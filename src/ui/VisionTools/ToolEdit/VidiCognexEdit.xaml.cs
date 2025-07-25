using AutoLaserCuttingInput;
using ITM_Semiconductor;
using nrt;
using OpenCvSharp;
using OpenCvSharp.Cuda;
using OpenCvSharp.Extensions;
using OpenCvSharp.ML;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices.ComTypes;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ViDi2;
using ViDi2.UI;
using VisionInspection;
using static OpenCvSharp.ConnectedComponents;
using Image = System.Windows.Controls.Image;
using Path = System.IO.Path;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for SegmentNeuroEdit.xaml
    /// </summary>
    public partial class VidiCognexEdit : GridBase, INotifyPropertyChanged
    {
        //Variables
        private MyLogger logger = new MyLogger("VidiCognex Edit");
        public event RoutedEventHandler OnBtnRunClicked;
        //VIDI
        ViDi2.Runtime.IControl control;
        ViDi2.IWorkspace workspace;
        ViDi2.IStream stream1;
        public List<VidiModel> modelList = new List<VidiModel>();
        public bool isInitModelCompl = true;
        public ViDi2.Runtime.IControl Control
        {
            get => control;
            set
            {
                control = value;
                OnPropertyChanged(nameof(Control));
                OnPropertyChanged(nameof(Workspaces));
                OnPropertyChanged(nameof(Stream1));
            }
        }

        public IList<ViDi2.Runtime.IWorkspace> Workspaces => Control.Workspaces.ToList();
        public ViDi2.IWorkspace Workspace
        {
            get { return workspace; }
            set
            {
                workspace = value;
                Mat src = Cv2.ImRead("temp2.bmp");
                IImage img = GetVidiImage(src);
                Stream1 = workspace.Streams.First();
                // warm up operation, first call to process takes additionnal time
                Stream1?.Process(img);
                OnPropertyChanged(nameof(Workspace));
            }
        }
        public ViDi2.IStream Stream1
        {
            get { return stream1; }
            set
            {
                stream1 = value;
                OnPropertyChanged(nameof(Stream1));
            }
        }

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
        private string _deviceSelected = "";
        public ObservableCollection<string> BindableDevices { get; set; } = new ObservableCollection<string>();
        public string DeviceSelected { get => _deviceSelected; set { _deviceSelected = value; OnPropertyChanged(nameof(DeviceSelected)); } }
        public int DevCbxIdx { get; set; } = 0;
        public int xctScore { get; set; } = 0;
        protected void OnPropertyChanged(string pptName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pptName));
        }
        //public string DeviceName { get; set; } = "";
        //public int DeviceIdx { get; set; } = 0;
        public VidiCognexEdit()
        {
            InitializeComponent();
            toolBase.DataContext = this;
            DisplayInit();
            RegisterEvent();
        }
        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "Vidi Cognex";
            toolBase.cbxImage.Items.Add("[Vidi Cognex] Input Image");
            toolBase.cbxImage.Items.Add("[Vidi Cognex] Output Image");
            toolBase.cbxImage.SelectedIndex = 0;

            try
            {
                Grid parent = toolBase.gridBase.Parent as Grid;
                parent.Children.Add(this);
                parent.Children.Remove(toolBase.gridBase);

                InitControl();
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
        private void InitControl()
        {
            try
            {
                this.Control?.Dispose();
                this.Control = new ViDi2.Runtime.Local.Control(ViDi2.GpuMode.Deferred);
                List<string> devices = GetHardwareDevices();
                List<int> devIdxLst = new List<int>();
                for (int i = 0; i < devices.Count; i++)
                {
                    devIdxLst.Add(i);
                }

                //control.InitializeComputeDevices(ViDi2.GpuMode.SingleDevicePerTool, new List<int>() { 0, 1 });
                this.Control.InitializeComputeDevices(ViDi2.GpuMode.SingleDevicePerTool, devIdxLst);
                //this.Control = control;
            }
            catch (Exception ex)
            {
                logger.Create("Init Control Error: " + ex.Message, ex);
            }
        }
        public WpfImage GetVidiImage(Mat src)
        {
            //Mat src = Image1.Clone();
            if (src == null)
                return null;
            BitmapSource source = OpenCvSharp.Extensions.BitmapSourceConverter.ToBitmapSource(src);
            var image = new ViDi2.UI.WpfImage(source);
            src.Dispose();
            return image;
        }
        private void CbxDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                DeviceSelected = cbxDevice.SelectedItem as String;
                DevCbxIdx = cbxDevice.SelectedIndex;
                Mouse.OverrideCursor = Cursors.Wait;
                Mouse.OverrideCursor = null;
            }
            catch (Exception ex)
            {
                logger.Create("Cbx Device Error: " + ex.Message, ex);
            }
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
            
            CreatNewModel(txtModelName.Text, txtModelPath.Text, Convert.ToInt32(xctScore), modelList.Count);
            //if (UiManager.appSettings.CurrentModel.dLJobs == null)
            //{
            //    UiManager.appSettings.CurrentModel.dLJobs = new List<DLJob>();
            //}
            //UiManager.appSettings.CurrentModel.dLJobs.Add(dlJob);
        }
        private void SortModel()
        {
            // Lấy danh sách các Expander từ Children
            var borderList = stModelList.Children
                .OfType<Border>() // Lọc chỉ lấy Expander
                .OrderBy(border =>
                {
                    Expander expander = border.Child as Expander;
                    // Lấy Content từ Header (giả định Header là Label)
                    StackPanel mainSP = expander.Content as StackPanel;
                    if (mainSP == null) return string.Empty;

                    string wspacePath = mainSP.Children
                                            .OfType<StackPanel>()
                                            .Select(subSP => subSP.Children
                                                                .OfType<TextBox>()
                                                                .Select(tbxPath => tbxPath.Text)
                                                                .FirstOrDefault())
                                            .FirstOrDefault();
                    return wspacePath != null ? System.IO.Path.GetFileNameWithoutExtension(wspacePath) : string.Empty;
                })
                .ToList();

            // Xóa toàn bộ các phần tử hiện tại trong stackVidiJobList
            stModelList.Children.Clear();
            txtModelNameList.Clear();

            // Thêm lại các Expander theo thứ tự đã sắp xếp
            for (int i = 0; i < borderList.Count; i++)
            {
                Expander expander = borderList[i].Child as Expander;
                //Truy vấn đến nút ấn và thay đổi tên nó: 
                // Lấy StackPanel chính từ Content của Expander
                StackPanel mainSP = expander.Content as StackPanel;

                if (mainSP == null) return; // Nếu không có StackPanel thì thoát

                // Tìm StackPanel con
                StackPanel childSP1 = mainSP.Children
                    .OfType<StackPanel>() // Lọc các phần tử là StackPanel
                    .FirstOrDefault();    // Lấy StackPanel đầu tiên (nếu có)

                if (childSP1 == null) return; // Nếu không có StackPanel con thì thoát

                // Tìm Button trong StackPanel nhỏ hơn
                Button targetBtn = childSP1.Children
                    .OfType<Button>() // Lọc các phần tử là Button
                    .FirstOrDefault(); // Lấy Button đầu tiên (nếu có)
                if (targetBtn == null) return; // Nếu không có Button thì thoát
                // Thay đổi tên của Button
                targetBtn.Name = String.Format("btnDlWorkSpaceChooseFile00{0}", i.ToString());
                TextBox targetTxt = childSP1.Children
                    .OfType<TextBox>()
                    .FirstOrDefault();
                if (targetTxt == null) return;
                targetTxt.Name = String.Format("txtDlWsp00{0}", i.ToString());
                txtModelNameList.Add(targetTxt);

                StackPanel childSP2 = mainSP.Children
                    .OfType<StackPanel>() // Lọc các phần tử là StackPanel
                    .Skip(1)              // Bỏ qua StackPanel đầu tiên
                    .FirstOrDefault();    // Lấy StackPanel thứ 2 (nếu có)
                if (childSP2 == null) return;
                Xceed.Wpf.Toolkit.IntegerUpDown targetXct = childSP2.Children
                    .OfType<Xceed.Wpf.Toolkit.IntegerUpDown>() // Lọc các phần tử là Button
                    .FirstOrDefault();
                targetXct.Name = String.Format("xctDlScore00{0}", i.ToString());
                stModelList.Children.Add(borderList[i]);
            }
        }
        private List<TextBox> txtModelNameList = new List<TextBox>();
        public void CreatNewModel(string name, string path, int Score, int index)
        {
            try
            {
                if (modelList.Count <= 0)
                {
                    stModelList.Children.Clear();
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
                        VidiModel jobToDelete = modelList.FirstOrDefault(job => job.Name == name);
                        if (jobToDelete != null)
                        {
                            modelList.Remove(jobToDelete);
                        }
                        if (stModelList.Children.Count == 0)
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
                st.Children.Add(st2);
                expander.Content = st;
                VidiModel vidiModel = new VidiModel(name, path, Score);
                modelList.Add(vidiModel);
                SortModel();
                //Chạy khởi tạo lại các Predictor & Flowchart
                InitModel();
                //Wait until load Model complete
                while (!isInitModelCompl);
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
                    DefaultExt = ".vrws",
                    Filter = "ViDi Runtime Workspaces (*.vrws)|*.vrws"
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
                    DefaultExt = ".vrws",
                    Filter = "ViDi Runtime Workspaces (*.vrws)|*.vrws"
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
        private void Xct_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown xct = sender as Xceed.Wpf.Toolkit.IntegerUpDown;
            int index = Convert.ToInt32(xct.Name.Replace("xctModelScore00", string.Empty));
            modelList[index].Score = Convert.ToInt32(xct.Value);
        }
        private void OpenWorkSpace(String fileName)
        {
            using (var fs = new System.IO.FileStream(fileName, System.IO.FileMode.Open, FileAccess.Read))
            {
                Workspace = Control.Workspaces.Add(System.IO.Path.GetFileNameWithoutExtension(fileName), fs);
            }
        }
        void InitModel()
        {
            isInitModelCompl = false;
            Task tsk1 = new Task(() =>
            {
                if (this.control.Workspaces.Count > 0)
                //if (Control.Workspaces.Count > 0)
                {
                    // Lưu danh sách Workspace cần xóa
                    var toRemove = control.Workspaces.ToList();
                    foreach (var ws in toRemove)
                    {
                        string name = ws.UniqueName;

                        // Nếu có stream liên kết, gán null trước khi remove workspace
                        if (Stream1 != null && Workspaces.Contains(ws) && Stream1 == ws.Streams.First())
                        {
                            Stream1 = null;
                        }

                        try
                        {
                            control.Workspaces.Remove(name);
                        }
                        catch (Exception ex)
                        {
                            logger.Create($"Remove Workspace {name} failed: {ex.Message}");
                        }
                    }
                    Workspaces.Clear(); // gán danh sách Workspace của bạn rỗng nếu có
                }
                for (int i = 0; i < modelList.Count; i++)
                {
                    if (modelList[i].Path.Contains(".vrws"))
                    {
                        try
                        {
                            OpenWorkSpace(modelList[i].Path);
                            logger.Create(String.Format(@"Open Workspace {0} Success", modelList[i].Name));
                        }
                        catch
                        {
                            logger.Create(String.Format(@"Open Workspace {0} Error....", modelList[i].Name));
                        }
                    }
                }
                isInitModelCompl = true;
            });
            tsk1.Start();
        }
        public List<string> GetHardwareDevices1()
        {
            List<string> devices = new List<string>();

            // Lấy CPU
            //using (var searcher = new ManagementObjectSearcher("select Name from Win32_Processor"))
            //{
            //    foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
            //    {
            //        devices.Add("CPU - " + obj["Name"].ToString());
            //    }
            //}

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

            // Lấy GPU rời (loại bỏ Intel, Microsoft Basic, ...)
            using (var searcher = new ManagementObjectSearcher("select Name from Win32_VideoController"))
            {
                foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                {
                    string name = obj["Name"]?.ToString() ?? "";

                    // Loại bỏ GPU tích hợp (Intel, Microsoft Basic Display Adapter...)
                    if (!name.ToLower().Contains("intel") && !name.ToLower().Contains("microsoft basic"))
                    {
                        devices.Add("GPU - " + name);
                    }
                }
            }

            return devices;
        }

        private ISample DeepCheck(WpfImage src)
        {
            try
            {
                return Stream1.Process(src, "1");
            }
            catch (Exception ex)
            {
                logger.Create("Deep Check Error: " + ex.Message, ex);
                return null;
            }

        }
        private ISample DeepCheckMultiGPU(WpfImage src, int GPUNo)
        {
            try
            {
                if (GPUNo == 0)
                {
                    return Stream1.Process(src, "1", new List<int>() { 0 });
                }
                else
                {
                    return Stream1.Process(src, "1", new List<int>() { 1 });
                }
            }
            catch (Exception ex)
            {
                logger.Create("Deep Check MultiGPU Error: " + ex.Message, ex);
                return null;
            }

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
                        if (runImage.Mat.Channels() > 3)
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
                    if (runImage.Mat.Channels() > 3)
                    {
                        runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.BGR2RGB);
                        runImage.Mat = runImage.Mat.CvtColor(ColorConversionCodes.RGB2BGR);
                    }
                }
                else if (InputImage.Mat != null && InputImage.Mat.Width > 0 && InputImage.Mat.Height > 0 && !toolBase.isImgPath)
                {
                    runImage = this.InputImage.Clone(true);
                }

                Mat imgGraphic = runImage.Mat.Clone();
                StrResult = "OK";
                Judge = 1;
                Score = 0d;

                try
                {
                    var wsEmpty = Workspaces.FirstOrDefault(s => s.UniqueName.Contains("Empty"));
                    if(wsEmpty == null)
                        goto VisionProcess;
                    using (WpfImage vidiMatLst = GetVidiImage(runImage.Mat.Clone()))
                    {
                        Stream1 = wsEmpty.Streams.First();
                        double score = 0;
                        using (ISample eE = DeepCheckMultiGPU(vidiMatLst, DevCbxIdx))
                        {
                            IRedMarking redMarking = eE.Markings["Analyze"] as IRedMarking;

                            foreach (IRedView view in redMarking.Views)
                            {
                                score = view.Score;
                            }

                            VidiModel modelEmpty = modelList.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x.Path).Contains("Empty"));
                            if (modelEmpty == null)
                                goto VisionProcess;
                            if (score > (double)modelEmpty.Score / 100.0)
                            {
                                Judge = 3;
                                StrResult = "Empty";
                                Score = score;
                                goto End;
                            }
                        }
                    }

                VisionProcess:
                    double scoreMax = 0;
                    using (WpfImage vidiMatLst = GetVidiImage(runImage.Mat.Clone()))
                    {
                        for (int i = 0; i < Workspaces.Count; i++)
                        {
                            if (Workspaces[i].UniqueName.Contains("Empty")) { continue; }
                            Stream1 = Workspaces[i].Streams.First();
                            double score = 0;
                            using (ISample e = DeepCheckMultiGPU(vidiMatLst, DevCbxIdx))
                            {
                                IRedMarking redMarking = e.Markings["Analyze"] as IRedMarking;

                                foreach (IRedView view in redMarking.Views)
                                {
                                    score = view.Score;
                                }

                                if (score > (double)modelList[i].Score / 100.0)
                                {
                                    if (score > scoreMax)
                                    {
                                        scoreMax = score;
                                        StrResult = Workspaces[i].UniqueName;
                                    }
                                    Judge = 2;
                                }
                            }
                        }
                    }
                End:;               
                }
                catch (Exception ex)
                {
                    logger.Create($"Vision Process Error :{ex}", ex);
                    Judge = 3;
                    Score = -1d;
                    StrResult = "Error";
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, $"Vision Process Error :{ex}");
                    return;
                }  
                OutputImage = runImage.Clone(true);
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
    public class VidiModel
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public int Score { get; set; } = 0;

        public VidiModel()
        {
            Name = "";
            Path = "";
            Score = 0;
        }
        public VidiModel(string name = "", string path = "", int score = 0)
        {
            this.Name = name;
            this.Path = path;
            this.Score = score;
        }
        public VidiModel Clone()
        {
            return new VidiModel()
            {
                Name = this.Name,
                Path = this.Path,
                Score = this.Score
            };
        }
    }
}
