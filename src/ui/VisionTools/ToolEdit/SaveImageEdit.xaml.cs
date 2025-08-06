using OpenCvSharp;
using OpenCvSharp.Extensions;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    public partial class SaveImageEdit : GridBase, INotifyPropertyChanged
    {
        //Variables
        private MyLogger logger = new MyLogger("SaveImage Edit");
        public event RoutedEventHandler OnBtnRunClicked;

        
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

        //[*********************************** BINDING ***********************************]
        public event PropertyChangedEventHandler PropertyChanged;
        #region Internal Field
        private double _numUDImgStorage = 0d, _maxImgStorage = 10d;
        private int _numUDCounter = 0, _indexImage = 0;
        #endregion

        #region Property
        public string ImageFormatSelected { get; set; } = "BMP";
        public bool IsAddDateTime { get; set; } = false;
        public bool IsAddCounter { get; set; } = false;
        public string DiskSize { get; set; } = "GB";
        public int IndexImage { get => _indexImage; set { _indexImage = value; OnPropertyChanged(nameof(IndexImage)); } }
        public int NumUDCounter { get => _numUDCounter; set { _numUDCounter = value; OnPropertyChanged(nameof(NumUDCounter)); } }
        public double NumUDImageStorage { get=>_numUDImgStorage; set { _numUDImgStorage = value; OnPropertyChanged(nameof(NumUDImageStorage)); } }
        public double MaxImageStorage { get => _maxImgStorage; set { _maxImgStorage = value; OnPropertyChanged(nameof(MaxImageStorage)); } }
        protected void OnPropertyChanged(string pptName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pptName));
        }
        #endregion
        public SaveImageEdit()
        {
            InitializeComponent();
            DisplayInit();
            RegisterEvent();

            toolBase.DataContext = this;
        }
        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "Save Image";
            toolBase.cbxImage.Items.Add("[Save Image] Input Image");
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
        }
        public void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            OnBtnRunClicked?.Invoke(sender, e);
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
        private void BtnResetIndex_Click(object sender, RoutedEventArgs e)
        {
            IndexImage = 0;
        }
        public void BtnCheckDisk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(sender != null)
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


        private SvImage runImage = new SvImage();
        public override void Run()
        {
            if (this.InputImage == null || this.InputImage.Mat == null || this.InputImage.Mat.Width <= 0 || this.InputImage.Mat.Height <= 0)
            {
                if (toolBase.isImgPath && isEditMode)
                {
                    runImage.Mat = (ImgView.Source as BitmapSource).ToMat();
                    runImage.RegionRect.Rect = new Rect(0, 0, (int)ImgView.Source.Width, (int)ImgView.Source.Height);
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
                runImage.RegionRect.Rect = new Rect(0, 0, (int)ImgView.Source.Width, (int)ImgView.Source.Height);
            }
            else if (InputImage.Mat != null && InputImage.Mat.Width > 0 && InputImage.Mat.Height > 0 && !toolBase.isImgPath)
            {
                runImage = this.InputImage.Clone(true);
            }
            try
            {
                Task.Run(() =>
                {
                    string folderPath = "";
                    string fileName = "";
                    string imageFormat = "";
                    bool isAddCounter = IsAddCounter;
                    bool isAddDateTime = IsAddDateTime;
                    int indexImage = IndexImage;
                    int numUDCounter = NumUDCounter;
                    double numUDImageStorage = NumUDImageStorage;
                    Mat matToSave = runImage.Mat.Clone(); // Clone ảnh để tránh dùng reference

                    Dispatcher.Invoke(() =>
                    {
                        folderPath = txtFolderPath.Text;
                        fileName = txtFileName.Text;
                        imageFormat = ImageFormatSelected;
                    });

                    if (!IsValidPath(folderPath))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            meaRunTime.Stop();
                            toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "FolderPath Error Syntax!");
                        });
                        return;
                    }

                    try
                    {
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            meaRunTime.Stop();
                            toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Cannot create folder: " + ex.Message);
                        });
                        return;
                    }

                    if (isAddCounter)
                    {
                        if (indexImage >= numUDCounter)
                            indexImage = 0;
                        indexImage++;
                    }

                    string timestamp = isAddDateTime ? $"{DateTime.Now:yyyy-MM-dd HH-mm-ss-fff}" : "";
                    string strIndex = isAddCounter ? $"-{indexImage}" : "";

                    if (string.IsNullOrEmpty(fileName)) fileName = "Default";
                    if (string.IsNullOrEmpty(imageFormat)) imageFormat = "bmp";

                    string finalFileName = $"{fileName}{strIndex} {timestamp}.{imageFormat.ToLower()}";
                    string fullPath = Path.Combine(folderPath, finalFileName);

                    DeleteOldestFile(folderPath, numUDImageStorage);

                    bool saveSuccess = false;
                    try
                    {
                        saveSuccess = Cv2.ImWrite(fullPath, matToSave);
                    }
                    catch (Exception ex)
                    {
                        logger.Create("Image save error: " + ex.Message, ex);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        meaRunTime.Stop();
                        if (!saveSuccess)
                        {
                            toolBase.SetLbTime(false, meaRunTime.ElapsedMilliseconds, "Save Image Fail!");
                        }
                        else
                        {
                            IndexImage = indexImage;
                            toolBase.SetLbTime(true, meaRunTime.ElapsedMilliseconds, "Image Saved");
                        }
                    });
                });


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.Create("Run Error: " + ex.Message, ex);
            }
        }
    }
}
