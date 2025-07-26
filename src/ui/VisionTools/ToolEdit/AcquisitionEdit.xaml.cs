using Microsoft.Win32;
using MVSDK_Net;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VisionInspection;
using Xceed.Wpf.AvalonDock.Controls;
using static MVSDK_Net.IMVDefine;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for AcquisitionEdit.xaml
    /// </summary>
    public partial class AcquisitionEdit : GridBase, INotifyPropertyChanged
    {
        //UI Element
        private MyLogger logger = new MyLogger("Aquisition Edit");
        List<Image> imgSmallList = new List<Image>();
        List<Canvas> canvasSmallList = new List<Canvas>();
        List<Border> brdrSmallList = new List<Border>();
        //Variable
        List<Mat> imgFolderLst = new List<Mat>();
        private int countNxBk = 0;
        public bool isImageFolder = false, isImageCam = false;
        private bool isStopCamera = false;
        private readonly object _cameraTrigger = new object();
        public ICamDevice Camera;
        public IntPtr CamDevSelected = IntPtr.Zero;
        private DeviceList deviceList = new DeviceList();
        public IMVDefine.IMV_DeviceInfo deviceInfo = new IMV_DeviceInfo();
        public event RoutedEventHandler OnBtnRunClicked;

        //In/Out
        SvImage _outputImage = new SvImage();
        public SvImage OutputImage { get => _outputImage; set => _outputImage = value; }

        //Binding
        public event PropertyChangedEventHandler PropertyChanged;
        public enum RotateMode { Rotate0, Rotate90, Rotate180, Rotate270 }
        public Array RotateModes => Enum.GetValues(typeof(RotateMode));
        public enum GrayMode { BGR2Gray, RGB2Gray, BGR2RGB }
        public Array GrayModes => Enum.GetValues(typeof(GrayMode));

        private RotateMode _selectedRotateMode;
        private GrayMode _selectedGrayMode;
        private double _exposureVal, _gainVal;
        private long _widthCam = 100, _heightCam = 100, _minWidth = 0, _minHeight = 0, _maxWidth = 100, _maxHeight = 100;
        private double _minExpo, _maxExpo, _minGain, _maxGain;
        private Visibility _isImgVisible = Visibility.Visible, _isAcqVisible = Visibility.Hidden;
        public double MinExpo { get => _minExpo; set { _minExpo = value; OnPropertyChanged(nameof(MinExpo)); } }     
        public double MaxExpo { get => _maxExpo; set { _maxExpo = value; OnPropertyChanged(nameof(MaxExpo)); } }
        public double MinGain { get => _minGain; set { _minGain = value; OnPropertyChanged(nameof(MinGain)); } }    
        public double MaxGain { get => _maxGain; set { _maxGain = value; OnPropertyChanged(nameof(MaxGain)); } }
        public long nMinWidth { get => _minWidth; set { _minWidth = value; OnPropertyChanged(nameof(nMinWidth)); } }
        public long nMinHeight { get => _minHeight; set { _minHeight = value; OnPropertyChanged(nameof(nMinHeight)); } }
        public long nMaxWidth { get => _maxWidth; set { _maxWidth = value; OnPropertyChanged(nameof(nMaxWidth)); } }
        public long nMaxHeight { get => _maxHeight; set { _maxHeight = value; OnPropertyChanged(nameof(nMaxHeight)); } }
        public RotateMode SelectedRotateMode { get => _selectedRotateMode; set { _selectedRotateMode = value; OnPropertyChanged(nameof(SelectedRotateMode)); } }
        public GrayMode SelectedGrayMode { get => _selectedGrayMode; set { _selectedGrayMode = value; OnPropertyChanged(nameof(SelectedGrayMode)); } }
        public BitmapSource ImgViewSource { get => (BitmapSource)toolBase.imgView.Source; set
            {
                toolBase.imgView.Source = value;
                OutputImage.Mat = value.ToMat();
            }
        }

        public double ExposureValue { get => _exposureVal; set
            {
                if(_exposureVal != value)
                {
                    _exposureVal = value;
                    this.Camera.SetExposeTime(_exposureVal);
                    OnPropertyChanged(nameof(ExposureValue));
                }    
            }
        }
        public double GainValue { get => _gainVal; set
            {
                if (_gainVal != value)
                {
                    _gainVal = value;
                    this.Camera.SetGain(_gainVal);
                    OnPropertyChanged(nameof(GainValue));
                }
            }
        }
        public long WidthCam { get => _widthCam; set 
            {
                _widthCam = value;
                this.Camera.SetHeight(_widthCam);
                OnPropertyChanged(nameof(WidthCam));
            } 
        }
        public long HeightCam { get => _heightCam; set 
            {
                _heightCam = value;
                this.Camera.SetHeight(_heightCam);
                OnPropertyChanged(nameof(HeightCam));
            } 
        }
        
        public bool IsStopCamera { get => isStopCamera; set
            {
                isStopCamera = value;
                numUDWidthCam.IsEnabled = isStopCamera;
                numUDHeightCam.IsEnabled = isStopCamera;
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public AcquisitionEdit()
        {
            InitializeComponent();
            DisplayInit();
            RegisterEvent();
            AddCamDevice();

            toolBase.DataContext = this;
        }
        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "Acquisition";
            toolBase.cbxImage.Items.Add("[Acquisition] Output Image");
            toolBase.cbxImage.SelectedIndex = 0;
            try
            {
                Grid parent = toolBase.gridBase.Parent as Grid;
                parent.Children.Add(this);
                parent.Children.Remove(toolBase.gridBase);

                isImageCam = true;
                isImageFolder = false;
                TabControl tabControl = this.Children.OfType<TabControl>().FirstOrDefault();
                List<TabItem> tabItems = tabControl.FindLogicalChildren<TabItem>().ToList();
                Grid outerGrid1 = tabItems[0].Content as Grid;
                List<Border> brdrTit1 = outerGrid1.Children.OfType<Border>().ToList();

                StackPanel st0StBrdrTit11 = (brdrTit1[1].Child as StackPanel).Children[0] as StackPanel;
                brdrSmallList = st0StBrdrTit11.Children.OfType<Border>().ToList();
                canvasSmallList = brdrSmallList.Select(x => x.Child as Canvas).ToList();
                imgSmallList = canvasSmallList.Select(x => x.Children.OfType<Image>().First()).ToList();
            }
            catch (Exception ex)
            {
                logger.Create("Display Init Error: " + ex.Message, ex);
            }
        }
        protected override void RegisterEvent()
        {
            toolBase.btnRun.Click += BtnRun_Click;
            canvasSmallList.ForEach(canvas => canvas.MouseLeftButtonDown += CanvasSmallList_MouseLeftButtonDown);
            this.Unloaded += AcquisitionEdit_Unloaded;
        }

        public void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            OnBtnRunClicked?.Invoke(sender, e);
        }

        private void BtnLiveView_Click(object sender, RoutedEventArgs e)
        {
            IsStopCamera = false;
            CallThreadStartLoop();
        }

        private void AcquisitionEdit_Unloaded(object sender, RoutedEventArgs e)
        {
            IsStopCamera = true;
        }

        private void CbxCamDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selectedItem = cbxCamDevice.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                try
                {
                    if (selectedItem.Content.ToString() == "Image Folder")
                    {
                        isImageFolder = true;
                        isImageCam = false;
                        lbTitle.Content = "Image List";
                        brdrImgSetting.Visibility = Visibility.Visible;
                        brdrAcqSetting.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        isImageFolder = false;
                        isImageCam = true;
                        lbTitle.Content = "Setting Camera";
                        brdrImgSetting.Visibility = Visibility.Hidden;
                        brdrAcqSetting.Visibility = Visibility.Visible;

                        IMVDefine.IMV_DeviceInfo device = Marshal.PtrToStructure<IMV_DeviceInfo>(deviceList.devInfo[cbxCamDevice.SelectedIndex - 1]);
                        if (device.nInterfaceType == IMVDefine.IMV_EInterfaceType.interfaceTypeGige)
                        {
                            if (device.cameraName != "")
                            {
                                string Caminfo = (String.Format("GigE: " + device.cameraName + " (" + device.serialNumber + ")"));
                                if (selectedItem.Content.ToString() == Caminfo)
                                {
                                    ConectCamera();
                                }
                            }
                            else
                            {
                                string Caminfo = String.Format(("GigE: " + device.vendorName + " " + device.modelName + " (" + device.serialNumber + ")"));
                                if (selectedItem.Content.ToString() == Caminfo)
                                {
                                    ConectCamera();
                                }
                            }
                        }
                        else if (device.nInterfaceType == IMVDefine.IMV_EInterfaceType.interfaceTypeUsb3)
                        {
                            if (device.cameraName != "")
                            {
                                string Caminfo = String.Format(("USB: " + device.cameraName + " (" + device.serialNumber + ")"));
                                if (selectedItem.Content.ToString() == Caminfo)
                                {
                                    ConectCamera();
                                }
                            }
                            else
                            {
                                string Caminfo = String.Format(("USB: " + device.vendorName + " " + device.modelName + " (" + device.serialNumber + ")"));
                                if (selectedItem.Content.ToString() == Caminfo)
                                {
                                    ConectCamera();
                                }
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    logger.Create("CbxCamDevice Error: " + ex.Message, ex);
                } 
            }       
        }

        private void CanvasSmallList_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Canvas canvas = sender as Canvas;
                Image img = canvas.Children.OfType<Image>().FirstOrDefault();
                if (img.Source == null) return;
                int idxSelected = canvasSmallList.FindIndex(x => x == canvas);
                int idxRed = brdrSmallList.FindIndex(x => x.BorderBrush == Brushes.Red);
                brdrSmallList.ForEach(brdr => brdr.BorderBrush = Brushes.Transparent);
                brdrSmallList[idxSelected].BorderBrush = Brushes.Red;
                countNxBk += idxSelected - idxRed;
                ImgViewSource = ImgRunPara(imgFolderLst[countNxBk]).ToBitmapSource();
                lbCurrentPos.Content = String.Format("{0} / {1}", countNxBk + 1, imgFolderLst.Count);
            }
            catch (Exception ex)
            {
                logger.Create("Choose Small Image Error: " + ex.Message, ex);
            } 
            
        }

        private void BtnBackImg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (imgFolderLst.Count == 0) return;
                countNxBk--;
                if (countNxBk >= 0)
                {
                    int idxRed = brdrSmallList.FindIndex(x => x.BorderBrush == Brushes.Red);
                    brdrSmallList.ForEach(brdr => brdr.BorderBrush = Brushes.Transparent);
                    ImgViewSource = ImgRunPara(imgFolderLst[countNxBk]).ToBitmapSource();
                    if (idxRed > 0)
                    {
                        brdrSmallList[idxRed - 1].BorderBrush = Brushes.Red;
                    }
                    else
                    {
                        brdrSmallList[0].BorderBrush = Brushes.Red;
                        DisplayInSmallImg(imgFolderLst.Skip(countNxBk).Take(imgSmallList.Count).ToList());
                        //for (int i = 0; i < imgSmallList.Count; i++)
                        //{
                        //    imgSmallList[i].Source = imgFolderLst[i + countNxBk].ToBitmapSource();
                        //    FitImage(imgSmallList[i], canvasSmallList[i]);
                        //}
                    }
                }
                else
                {
                    countNxBk = 0;
                }
                lbCurrentPos.Content = String.Format("{0} / {1}", countNxBk + 1, imgFolderLst.Count);
            }
            catch(Exception ex)
            {
                logger.Create("Button Back Error: " + ex.Message, ex);
            } 
        }

        private void BtnNextImg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (imgFolderLst.Count == 0) return;
                countNxBk++;
                if (countNxBk >= imgFolderLst.Count)
                {
                    countNxBk = ((Button)sender == toolBase.btnRun) ? 0 : imgFolderLst.Count;
                }
                if (countNxBk < imgFolderLst.Count)
                {
                    int idxRed = brdrSmallList.FindIndex(x => x.BorderBrush == Brushes.Red);
                    brdrSmallList.ForEach(brdr => brdr.BorderBrush = Brushes.Transparent);
                    ImgViewSource = ImgRunPara(imgFolderLst[countNxBk]).ToBitmapSource();
                    if (countNxBk < brdrSmallList.Count)
                    {
                        brdrSmallList[countNxBk].BorderBrush = Brushes.Red;
                        DisplayInSmallImg(imgFolderLst);
                    }
                    else if (idxRed < brdrSmallList.Count - 1)
                    {
                        brdrSmallList[idxRed + 1].BorderBrush = Brushes.Red;
                    }
                    else
                    {
                        brdrSmallList[brdrSmallList.Count - 1].BorderBrush = Brushes.Red;
                        DisplayInSmallImg(imgFolderLst.Skip(countNxBk - (brdrSmallList.Count - 1)).Take(imgSmallList.Count).ToList());
                        //for (int i = 0; i < imgSmallList.Count; i++)
                        //{
                        //    imgSmallList[i].Source = imgFolderLst[i + countNxBk - (brdrSmallList.Count - 1)].ToBitmapSource();
                        //    FitImage(imgSmallList[i], canvasSmallList[i]);
                        //}
                    }
                }
                else
                {
                    countNxBk = imgFolderLst.Count - 1;
                }
                lbCurrentPos.Content = String.Format("{0} / {1}", countNxBk + 1, imgFolderLst.Count);
            }
            catch(Exception ex)
            {
                logger.Create("Button Next Error: " + ex.Message, ex);
            } 
             
        }


        private void BtnImgLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Select BMP Image",
                    Filter = "All Images|*.bmp;*.jpg;*.jpeg;*.png|Bitmap Images (*.bmp)|*.bmp|JPEG Images (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG Images (*.png)|*.png",
                    Multiselect = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    imgFolderLst.Clear();
                    imgSmallList.ForEach(img => img.Source = null);
                    foreach (string filePath in openFileDialog.FileNames)
                    {
                        Mat matImage = new Mat(filePath, ImreadModes.Color); // Đọc ảnh vào Mat
                        imgFolderLst.Add(matImage); // Lưu vào List<Mat>
                    }
                    DisplayInSmallImg(imgFolderLst);
                    ImgViewSource = ImgRunPara(imgFolderLst[0]).ToBitmapSource();
                    brdrSmallList.ForEach(brdr => brdr.BorderBrush = Brushes.Transparent);
                    brdrSmallList[0].BorderBrush = Brushes.Red;
                    countNxBk = 0;
                    toolBase.FitImage();
                    lbCurrentPos.Content = String.Format("{0} / {1}", countNxBk + 1, imgFolderLst.Count);
                    isImageFolder = true;
                    isImageCam = false;
                }
            }
            catch(Exception ex)
            {
                logger.Create("Button Load Image Error: " + ex.Message, ex);
            } 
        }

        private void DisplayInSmallImg(List<Mat> imgLst)
        {
            try
            {
                List<BitmapSource> bmpSrcLst = imgLst.Select(x => x.ToBitmapSource()).ToList();
                if (imgSmallList.Count < bmpSrcLst.Count)
                {
                    for (int i = 0; i < imgSmallList.Count; i++)
                    {
                        imgSmallList[i].Source = bmpSrcLst[i];
                        FitImage(imgSmallList[i], canvasSmallList[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < bmpSrcLst.Count; i++)
                    {
                        imgSmallList[i].Source = bmpSrcLst[i];
                        FitImage(imgSmallList[i], canvasSmallList[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Display Small Image Error: " + ex.Message, ex);
            } 
        }
        private void FitImage(Image srcImage, Canvas boundImage)
        {
            if (srcImage.Source == null) return;
            try
            {
                double canvasWidth = boundImage.ActualWidth;
                double canvasHeight = boundImage.ActualHeight;
                double imageWidth = srcImage.Source.Width;
                double imageHeight = srcImage.Source.Height;

                double scaleX = canvasWidth / imageWidth;
                double scaleY = canvasHeight / imageHeight;
                double scale = Math.Min(scaleX, scaleY);

                // Đặt ScaleTransform
                ScaleTransform sclTrans = new ScaleTransform();
                sclTrans.ScaleX = scale;
                sclTrans.ScaleY = scale;

                // Căn giữa ảnh trong khung
                TranslateTransform transTrans = new TranslateTransform();
                transTrans.X = (canvasWidth - imageWidth * scale) / 2;
                transTrans.Y = (canvasHeight - imageHeight * scale) / 2;

                TransformGroup transformGroup = new TransformGroup();
                transformGroup.Children.Add(sclTrans);
                transformGroup.Children.Add(transTrans);
                srcImage.RenderTransform = transformGroup;
            }
            catch(Exception ex)
            {
                logger.Create("Fit Image Error: " + ex.Message, ex);
            }
        }
        public override void Run()
        {
            Mat img = null;
            OutputImage = new SvImage(new Mat());
            try
            {
                if (isImageFolder && !isImageCam)
                {
                    if (imgFolderLst.Count <= 0)
                    {
                        meaRunTime.Stop();
                        toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Can't find any image is imported from folder image!");
                        return;
                    }
                    BtnNextImg_Click(toolBase.btnRun, null);
                    img = ImgRunPara(imgFolderLst[countNxBk]);
                    if (img == null)
                    {
                        meaRunTime.Stop();
                        toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Image From Folder Path Error!");
                        return;
                    }
                    ImgViewSource = img.ToBitmapSource();
                    OutputImage.Mat = img;
                }
                else if (isImageCam && !isImageFolder)
                {
                    IsStopCamera = true;
                    img = this.Camera.CaptureImage();
                    if (img == null)
                    {
                        meaRunTime.Stop();
                        toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Camera Grab Image Fail!");
                        return;
                    }
                    ImgViewSource = img.ToBitmapSource();
                    OutputImage.Mat = img;
                }
                if (img == null)
                {
                    meaRunTime.Stop();
                    toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Have no Image Source!");
                    return;
                }
                OutputImage.RegionRect.Rect = new OpenCvSharp.Rect(0, 0, (int)ImgViewSource.Width, (int)ImgViewSource.Height);
            }
            catch(Exception ex) 
            {
                logger.Create("Run Error: " + ex.Message, ex);
            }
        }
        private Mat ImgRunPara(Mat src)
        {
            Mat img = src.Clone();
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    if ((bool)ckbxToGray.IsChecked)
                    {
                        switch (SelectedGrayMode)
                        {
                            case GrayMode.BGR2Gray:
                                Cv2.CvtColor(img, img, ColorConversionCodes.BGR2GRAY);
                                break;
                            case GrayMode.RGB2Gray:
                                Cv2.CvtColor(img, img, ColorConversionCodes.RGB2GRAY);
                                break;
                            case GrayMode.BGR2RGB:
                                Cv2.CvtColor(img, img, ColorConversionCodes.BGR2RGB);
                                break;
                        }
                    }
                    if ((bool)ckbxVFlip.IsChecked)
                    {
                        Cv2.Flip(img, img, FlipMode.X);
                    }
                    if ((bool)ckbxHFlip.IsChecked)
                    {
                        Cv2.Flip(img, img, FlipMode.Y);
                    }
                    switch (SelectedRotateMode)
                    {
                        case RotateMode.Rotate0:
                            break;
                        case RotateMode.Rotate90:
                            Cv2.Transpose(img, img);
                            Cv2.Flip(img, img, FlipMode.Y);
                            break;
                        case RotateMode.Rotate180:
                            Cv2.Flip(img, img, FlipMode.XY);
                            break;
                        case RotateMode.Rotate270:
                            Cv2.Transpose(img, img);
                            Cv2.Flip(img, img, FlipMode.X);
                            break;
                    }
                });
            }
            catch(Exception ex)
            {
                logger.Create("Image Parameter Error: " + ex.Message, ex);
            } 
            return img;
        }
        public void AddCamDevice()
        {
            try
            {
                while (cbxCamDevice.Items.Count > 1)
                {
                    cbxCamDevice.Items.RemoveAt(1);
                }
                this.deviceList = UiManager.DevList;
                for (int i = 0; i < this.deviceList.devNum; i++)
                {
                    IMVDefine.IMV_DeviceInfo device = Marshal.PtrToStructure<IMV_DeviceInfo>(this.deviceList.devInfo[i]);
                    if (device.nInterfaceType == IMVDefine.IMV_EInterfaceType.interfaceTypeGige)
                    {
                        if (device.cameraName != "")
                        {
                            string Caminfo = (String.Format("GigE: " + device.cameraName + " (" + device.serialNumber + ")"));
                            UpdateCbx(Caminfo);
                        }
                        else
                        {
                            string Caminfo = String.Format(("GigE: " + device.vendorName + " " + device.modelName + " (" + device.serialNumber + ")"));
                            UpdateCbx(Caminfo);
                        }
                    }
                    else if (device.nInterfaceType == IMVDefine.IMV_EInterfaceType.interfaceTypeUsb3)
                    {
                        if (device.cameraName != "")
                        {
                            string Caminfo = String.Format(("USB: " + device.cameraName + " (" + device.serialNumber + ")"));
                            UpdateCbx(Caminfo);
                        }
                        else
                        {
                            string Caminfo = String.Format(("USB: " + device.vendorName + " " + device.modelName + " (" + device.serialNumber + ")"));
                            UpdateCbx(Caminfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Add Camera Device Error: " + ex.Message, ex);
            }
        }
        private void UpdateCbx(string CamInfor)
        {
            var cbi1 = new ComboBoxItem();
            cbi1.Content = CamInfor;
            this.cbxCamDevice.Items.Add(cbi1);
        }

        public void ShowCamDevice(IMVDefine.IMV_DeviceInfo device)
        {
            try
            {
                if (device.nInterfaceType == IMVDefine.IMV_EInterfaceType.interfaceTypeGige)
                {
                    if (device.cameraName != "")
                    {
                        string Caminfo = (String.Format("GigE: " + device.cameraName + " (" + device.serialNumber + ")"));
                        cbxCamDevice.SelectedValue = Caminfo;
                    }
                    else
                    {
                        string Caminfo = String.Format(("GigE: " + device.vendorName + " " + device.modelName + " (" + device.serialNumber + ")"));
                        cbxCamDevice.SelectedValue = Caminfo;
                    }
                }
                else if (device.nInterfaceType == IMVDefine.IMV_EInterfaceType.interfaceTypeUsb3)
                {
                    if (device.cameraName != "")
                    {
                        string Caminfo = String.Format(("USB: " + device.cameraName + " (" + device.serialNumber + ")"));
                        cbxCamDevice.SelectedValue = Caminfo;
                    }
                    else
                    {
                        string Caminfo = String.Format(("USB: " + device.vendorName + " " + device.modelName + " (" + device.serialNumber + ")"));
                        cbxCamDevice.SelectedValue = Caminfo;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Show Camera Device Error: " + ex.Message, ex);
            } 
        }
        public bool ConectCamera()
        {
            try
            {
                //this.Camera = SelectCamIndex(cbxCamDevice.SelectedIndex);
                if (UiManager.CamList.Count == 0)
                    return false;
                this.Camera = UiManager.CamList[cbxCamDevice.SelectedIndex - 1];
                this.CamDevSelected = UiManager.CamDevList[cbxCamDevice.SelectedIndex - 1];
                this.deviceInfo = Marshal.PtrToStructure<IMV_DeviceInfo>(UiManager.DevList.devInfo[cbxCamDevice.SelectedIndex - 1]);
                if (this.Camera != null)
                {
                    int ret = this.Camera.Open();
                    Thread.Sleep(2);
                    if (ret == IMVDefine.IMV_OK)
                    {
                        this.Camera.GetDoubleMinValue("ExposureTime", out double dTemp);
                        this.MinExpo = dTemp;
                        this.Camera.GetDoubleMaxValue("ExposureTime", out dTemp);
                        this.MaxExpo = dTemp;
                        this.Camera.GetDoubleValue("ExposureTime", out dTemp);
                        this.ExposureValue = dTemp;

                        string gainStr = "";
                        switch(this.Camera.eCamDevType)
                        {
                            case ECamDevType.Hikrobot:
                                gainStr = "Gain";
                                break;
                            case ECamDevType.Irayple:
                                gainStr = "GainRaw";
                                break;
                        }
                        this.Camera.GetDoubleMinValue(gainStr, out dTemp);
                        this.MinGain = dTemp;
                        this.Camera.GetDoubleMaxValue(gainStr, out dTemp);
                        this.MaxGain = dTemp;
                        this.Camera.GetDoubleValue(gainStr, out dTemp);
                        this.GainValue = dTemp;

                        this.Camera.GetIntMinValue("Width", out long temp);
                        this.nMinWidth = temp;
                        this.Camera.GetIntMaxValue("Width", out temp);
                        this.nMaxWidth = temp;
                        numUDWidthCam.IsEnabled = true;
                        numUDWidthCam.IsEnabled &= this.Camera.SetWidth(WidthCam);
                        if(!numUDWidthCam.IsEnabled)
                        {
                            this.Camera.GetIntValue("Width", out temp);
                            this.WidthCam = temp;
                        }

                        this.Camera.GetIntMinValue("Height", out temp);
                        this.nMinHeight = temp;
                        this.Camera.GetIntMaxValue("Height", out temp);
                        this.nMaxHeight = temp;
                        numUDHeightCam.IsEnabled = true;
                        numUDHeightCam.IsEnabled &= this.Camera.SetHeight(HeightCam);
                        if(!numUDHeightCam.IsEnabled)
                        {
                            this.Camera.GetIntValue("Height", out temp);
                            this.HeightCam = temp;
                        }
                        return true;
                    }
                    else { return false; }
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                logger.Create("Connect Camera Error: " + ex.Message, ex);
                return false;
            }
        }
        private void CallThreadStartLoop()
        {
            try
            {
                Thread startThread = new Thread(new ThreadStart(waitTrigger));
                startThread.IsBackground = true;
                startThread.Start();
            }
            catch (Exception ex)
            {
                logger.Create("Call Thread Start Loop Err: " + ex.ToString());
            }

        }
        private void waitTrigger()
        {
            TriggerCamera();
            if (IsStopCamera)
            {
                return;
            }
            CallThreadStartLoop();
            Thread.Sleep(1);
        }
        private void TriggerCamera()
        {
            lock (_cameraTrigger)
            {
                OpenCvSharp.Mat src1 = new Mat();
                //OpenCvSharp.Mat srcDisplay1 = new Mat();
                OpenCvSharp.Mat srcDisplay2 = new Mat();
                try
                {
                    src1 = this.Camera.CaptureImage();
                    if (src1 != null)
                    {
                        //src1.SaveImage("temp1Hung.bmp");
                        //src1 = Cv2.ImRead("temp1Hung.bmp", ImreadModes.Color);
                        //srcDisplay2 = src1.Clone();
                        srcDisplay2 = ImgRunPara(src1);
                        if (srcDisplay2.Channels() == 3)
                        {
                            if(SelectedGrayMode == GrayMode.BGR2RGB)
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    ImgViewSource = src1.ToWriteableBitmap(PixelFormats.Rgb24);
                                    GC.Collect();
                                });
                            }
                            else
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    ImgViewSource = src1.ToWriteableBitmap(PixelFormats.Bgr24);
                                    GC.Collect();
                                });
                            }
                        }
                        else
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                ImgViewSource = srcDisplay2.ToWriteableBitmap(PixelFormats.Gray8);
                                GC.Collect();
                            });
                        }  
                        OutputImage.Mat = srcDisplay2;
                    }
                    else
                    {
                        logger.Create("Camera Trigger Err: Have no Data from camera - Image is null");
                        IsStopCamera = true;
                        return;
                    }
                    Thread.Sleep(1);
                    return;
                }
                catch (Exception ex)
                {
                    logger.Create("Camera Trigger Err: " + ex.Message.ToString());
                }
            }

        }
    }
}
