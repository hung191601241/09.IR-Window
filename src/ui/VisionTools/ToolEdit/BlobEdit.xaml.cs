using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.Blob;
using OpenCvSharp.Extensions;
using VisionInspection;
using ListView = System.Windows.Controls.ListView;
using Rect = OpenCvSharp.Rect;
using TabControl = System.Windows.Controls.TabControl;
using UserControl = System.Windows.Controls.UserControl;

namespace VisionTools.ToolEdit
{
    /// <summary>
    /// Interaction logic for BlobEdit.xaml
    /// </summary>
    public partial class BlobEdit : GridBase, INotifyPropertyChanged
    {
        //Variables
        private MyLogger logger = new MyLogger("Blob Edit");
        int kSize, minArea, maxArea, rangeLow, rangeHigh, constant;
        double adequateCoefficient, adequateCoefficientR;
        private List<BlobFilter> blobFilters;
        private BlobFilter.Properties sortingOrder;
        private Mat regionImage = new Mat();
        private List<BlobObject> _blobResult = new List<BlobObject>();
        public event RoutedEventHandler OnBtnRunClicked;

        //InOut
        private SvImage _inputImage = new SvImage(); 
        public SvImage InputImage
        {
            get => _inputImage; set
            {
                if(value == null) return;
                _inputImage = value;
                if (_inputImage.Mat.Height > 0 && _inputImage.Mat.Width > 0)
                {
                    toolBase.imgView.Source = _inputImage.Mat.ToBitmapSource();
                }
            }
        }
        List<BlobObject> blobs;
        public double AreaSum { get; set; }
        public double AreaRate
        {
            get
            {
                if (runImage == null || runImage.RegionRect == null)
                    return 0;
                Rect2f regionRect = runImage.RegionRect.RectF;
                if (regionRect == null || regionRect.Width == 0 || regionRect.Height == 0) return 0;
                double rate = AreaSum / (regionRect.Width * regionRect.Height);
                return rate;
            }
        }
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
        public double RectLengthSum { get; set; }
        public double RectAreaSum { get; set; }
        public float TranslateX { get; set; }
        public float TranslateY { get; set; }
        public float Rotation { get; set; }
        public SvImage BinaryImage { get; set; } = new SvImage();
        public SvImage BlobImage { get; set; } = new SvImage();
        public SvImage OutputImage { get; set; } = new SvImage();

        //[*********************************** BINDING ***********************************]
        public event PropertyChangedEventHandler PropertyChanged;
        #region Enum value
        public enum BlobMode { Threshold, Adaptive, InRange, Bernsen, Nick, NiBackFast, SauvolaFast }
        public Array BlobModes => Enum.GetValues(typeof(BlobMode));
        public enum BlobType { Binary, Otsu, Gaussian, Mean }
        public Array BlobTypes => Enum.GetValues(typeof(BlobType));
        public enum BlobPolarity { White, Black }
        public Array BlobPolarities => Enum.GetValues(typeof(BlobPolarity));
        public enum BlobBinary { ToBlack, ToZero }
        public Array BlobBinaries => Enum.GetValues(typeof(BlobBinary));
        public enum BlobPriority { None, Left, Top, Right, Bottom }
        public Array Priorities => Enum.GetValues(typeof(BlobPriority));
        public enum SortType {ID, Area, CenterMassX, CenterMassY, Angle, RectSize, RectWidth, RectHeight  }
        public Array SortTypes => Enum.GetValues(typeof(SortType));
        #endregion

        #region Internal Field
        private BlobMode _blobMode = BlobMode.Threshold;
        private BlobType _blobType = BlobType.Binary;
        private BlobPolarity _blobPolarity = BlobPolarity.White;
        private BlobBinary _blobBinary = BlobBinary.ToBlack;
        private BlobPriority _blobPriority = BlobPriority.None;
        private SortType _sort = SortType.Area;
        private int _range = 128, _lowRange = 0, _highRange = 1, _blockSize = 3, _coeff = 0, _coeffR = 0, _constSub = 0, _constMin = 0, _maxCount = 0;
        private bool _isCalBlob = true, _isExceptBound = false, _isFillHole = false, _isAscend = false;
        private bool _isBlobTypeEnable = true, _isPolarityEnable = true, _isBinaryEnable = true, _isSortTypeEnable = true;
        #endregion

        #region Property
        public int Range { get => _range; set { _range = value; OnPropertyChanged(nameof(Range)); } }
        public int LowRange { get => _lowRange; set { _lowRange = value; OnPropertyChanged(nameof(LowRange)); } }
        public int HighRange { get => _highRange; set { _highRange = value; OnPropertyChanged(nameof(HighRange)); } }
        public int BlockSize { get => _blockSize; set { _blockSize = value; OnBlockSizeChanged(BlockSize); OnPropertyChanged(nameof(BlockSize)); } }
        public int Coeff { get => _coeff; set { _coeff = value; OnPropertyChanged(nameof(Coeff)); } }
        public int CoeffR { get => _coeffR; set { _coeffR = value; OnPropertyChanged(nameof(CoeffR)); } }
        public int ConstSub { get => _constSub; set { _constSub = value; OnPropertyChanged(nameof(ConstSub)); } }
        public int ConstMin { get => _constMin; set { _constMin = value; OnPropertyChanged(nameof(ConstMin)); } }
        public int MaxCount { get => _maxCount; set { _maxCount = value; OnPropertyChanged(nameof(MaxCount)); } }
        public bool IsCalBlob { get => _isCalBlob; set { _isCalBlob = value; OnPropertyChanged(nameof(IsCalBlob)); } }
        public bool IsExceptBound { get => _isExceptBound; set { _isExceptBound = value; OnPropertyChanged(nameof(IsExceptBound)); } }
        public bool IsFillHole { get => _isFillHole; set { _isFillHole = value; OnPropertyChanged(nameof(IsFillHole)); } }
        public bool IsAscend { get => _isAscend; set { _isAscend = value; OnPropertyChanged(nameof(IsAscend)); } }
        public BlobMode SelectBlobMode { get => _blobMode; set { _blobMode = value; OnSelectBlobModeChanged(SelectBlobMode); } }
        public BlobType SelectBlobType { get => _blobType; set { _blobType = value; OnSelectBlobTypeChanged(SelectBlobType); } }
        public BlobPolarity SelectBlobPolarity { get => _blobPolarity; set { _blobPolarity = value; OnPropertyChanged(nameof(SelectBlobPolarity)); } }
        public BlobBinary SelectBlobBinary { get => _blobBinary; set { _blobBinary = value; OnPropertyChanged(nameof(SelectBlobBinary)); } }
        public BlobPriority SelectBlobPriority { get => _blobPriority; set { _blobPriority = value; OnBlobPriorityChanged(SelectBlobPriority); } }
        public SortType SelectSort { get => _sort; set { _sort = value; OnPropertyChanged(nameof(SelectSort)); } }
        public bool IsBlobTypeEnable { get => _isBlobTypeEnable; set => _isBlobTypeEnable = value; }
        public bool IsPolarityEnable { get => _isPolarityEnable; set => _isPolarityEnable = value; }
        public bool IsBinaryEnable { get => _isBinaryEnable; set => _isBinaryEnable = value; }
        public bool IsSortTypeEnable { get => _isSortTypeEnable; set => _isSortTypeEnable = value; }
        #endregion
        protected void OnPropertyChanged(string pptName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pptName)); 
            if(pptName == nameof(Range) || pptName == nameof(BlockSize) || pptName == nameof(Coeff) || pptName == nameof(CoeffR) || pptName == nameof(ConstMin) || pptName == nameof(ConstSub))
            {
                Run();
            }    
        }
        public int KernelSize { get => kSize; set => kSize = value; }
        public int RangeLow { get => rangeLow; set => rangeLow = value; }
        public int RangeHigh { get => rangeHigh; set => rangeHigh = value; }
        public int Constant { get => constant; set => constant = value; }
        public double AdequateCoefficient { get => adequateCoefficient; set => adequateCoefficient = value; }
        public double AdequateCoefficientR { get => adequateCoefficientR; set => adequateCoefficientR = value; }
        public BlobFilter.Properties SortingOrder { get { return sortingOrder; } set { sortingOrder = value; } }
        public List<BlobFilter> BlobFilters
        {
            get
            {
                if (blobFilters == null || blobFilters.Count != 7)
                {
                    blobFilters = new List<BlobFilter>
                    {
                        new BlobFilter(BlobFilter.Properties.Area),
                        new BlobFilter(BlobFilter.Properties.CenterMassX),
                        new BlobFilter(BlobFilter.Properties.CenterMassY),
                        new BlobFilter(BlobFilter.Properties.Angle),
                        new BlobFilter(BlobFilter.Properties.RectSize),
                        new BlobFilter(BlobFilter.Properties.RectWidth),
                        new BlobFilter(BlobFilter.Properties.RectHeight)
                    };
                }
                return blobFilters;
            }
            set => blobFilters = value;
        }
        public List<BlobObject> BlobResult
        {
            get => _blobResult;
            set { _blobResult = value; UpdateBlobResult(value); }
        }
        public Mat RegionImage
        {
            get => regionImage; 
            set
            {
                if (regionImage != null) regionImage.Dispose();
                regionImage = value.Clone();
            }
        }


        public BlobEdit()
        {
            kSize = 3;
            minArea = 1000; maxArea = 1000000;
            rangeLow = 0; rangeHigh = 255;
            InitializeComponent();
            DisplayInit();
            RegisterEvent(); 
            toolBase.DataContext = this;
        }
        public BlobEdit(Mat inputImage)
        {
            InputImage = new SvImage();
            InputImage.Mat = inputImage.Clone();
            kSize = 3;
            minArea = 1000; maxArea = 1000000;
            rangeLow = 0; rangeHigh = 255;
        }
        protected override void DisplayInit()
        {
            toolBase.lbCurrentJob.Content = "Blob";
            toolBase.cbxImage.Items.Add("[Blob] Input Image");
            toolBase.cbxImage.Items.Add("[Blob] Binary Image");
            toolBase.cbxImage.Items.Add("[Blob] Blob Image");
            toolBase.cbxImage.SelectedIndex = 0;
            try
            {
                Grid parent = toolBase.gridBase.Parent as Grid;
                parent.Children.Add(this);
                parent.Children.Remove(toolBase.gridBase);
                if (SelectBlobMode == BlobMode.Threshold)
                {
                    InitVisibility();
                }
            }
            catch(Exception ex)
            {
                logger.Create("Display Init Error: " + ex.Message, ex);
            } 
        }
        protected override void RegisterEvent()
        {
            toolBase.btnRun.Click += BtnRun_Click;
            toolBase.cbxImage.SelectionChanged += CbxImage_SelectionChanged;
            toolBase.OnLoadImage += ToolBase_OnLoadImage;
        }

        private void InitVisibility()
        {
            stRange.Visibility = Visibility.Visible;
            stBlockSize.Visibility = Visibility.Hidden;
            stCoeff.Visibility = Visibility.Hidden;
            stCoeffR.Visibility = Visibility.Hidden;
            stLowHigh.Visibility = Visibility.Hidden;
            stConstSub.Visibility = Visibility.Hidden;
            stConstMin.Visibility = Visibility.Hidden;
            IsBlobTypeEnable = true;
            IsBinaryEnable = true;
            IsPolarityEnable = true;
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
                    if (BinaryImage.Mat.Height > 0 && BinaryImage.Mat.Width > 0)
                    {
                        toolBase.imgView.Source = BinaryImage.Mat.ToBitmapSource();
                    }
                    oldSelect = 1;
                }
                else if (toolBase.cbxImage.SelectedIndex == 2)
                {
                    if (OutputImage.Mat.Height > 0 && OutputImage.Mat.Width > 0)
                    {
                        toolBase.imgView.Source = OutputImage.Mat.ToBitmapSource();
                    }
                    oldSelect = 2;
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
        private void ToolBase_OnLoadImage(object sender, RoutedEventArgs e)
        {
            loadedImg = (ImgView.Source as BitmapSource).ToMat();
        }
        void UpdateBlobResult(List<BlobObject> blobs)
        {
            try
            {
                if (blobs == null) return;

                lvResult.Items.Clear();
                foreach (BlobObject blob in blobs)
                {
                    string[] items = new string[] { blob.Label.ToString(), blob.Area.ToString(), blob.Centroid.X.ToString("F3"), blob.Centroid.Y.ToString("F3"), blob.Angle.ToString("F2"), blob.RectSize.ToString(), blob.RectWidth.ToString(), blob.RectHeight.ToString() };
                    lvResult.Items.Add(items);
                }
            }
            catch (Exception ex)
            {
                logger.Create("Update Blob Result Error: " + ex.Message, ex);
            }
        }
        public Mat Binarizing(Mat src, BlobMode mode, BlobType type, BlobPolarity pola, BlobBinary binaryType, int rangeLow, int rangeHigh, int kSize,
                               int constant, double adequateCoefficient, double adequateCoefficientR)
        {
            try
            {
                if (kSize < 1 || kSize % 2 == 0)
                    kSize = 3;

                if (rangeLow < 0 || rangeLow > 256)
                    throw new Exception("rangeLow < 0 || rangeLow > 255");

                if (rangeHigh < 0 || rangeHigh > 256)
                    throw new Exception("rangeHigh < 0 || rangeHigh > 255");

                Mat dst = new Mat();
                switch (mode)
                {
                    case BlobMode.Threshold:

                        ThresholdTypes thresholdType;

                        if (pola == BlobPolarity.White && binaryType == BlobBinary.ToBlack)
                            thresholdType = ThresholdTypes.Binary;
                        else if (pola == BlobPolarity.White && binaryType == BlobBinary.ToZero)
                            thresholdType = ThresholdTypes.Tozero;
                        else if (pola == BlobPolarity.Black && binaryType == BlobBinary.ToBlack)
                            thresholdType = ThresholdTypes.BinaryInv;
                        else if (pola == BlobPolarity.Black && binaryType == BlobBinary.ToZero)
                            thresholdType = ThresholdTypes.TozeroInv;
                        else
                            throw new Exception("thresholdType not find");

                        if (type == BlobType.Otsu)
                            thresholdType |= ThresholdTypes.Otsu;

                        if (type == BlobType.Gaussian || type == BlobType.Mean)
                            throw new Exception("wrong threshold type");

                        Cv2.Threshold(src, dst, rangeLow, 255, thresholdType);
                        break;
                    case BlobMode.Adaptive:

                        AdaptiveThresholdTypes adaptiveThresholdType;

                        if (type == BlobType.Gaussian)
                            adaptiveThresholdType = AdaptiveThresholdTypes.GaussianC;
                        else if (type == BlobType.Mean)
                            adaptiveThresholdType = AdaptiveThresholdTypes.MeanC;
                        else
                            throw new Exception("BlobMode must be Gaussian or Mean");

                        if (pola == BlobPolarity.White)
                            thresholdType = ThresholdTypes.Binary;
                        else if (pola == BlobPolarity.Black)
                            thresholdType = ThresholdTypes.BinaryInv;
                        else
                            throw new Exception("BlobPolarity must be Binary or BinaryInv");

                        Cv2.AdaptiveThreshold(src, dst, 255, adaptiveThresholdType, thresholdType, kSize, constant);
                        break;
                    case BlobMode.InRange:
                        Cv2.InRange(src, new Scalar(rangeLow), new Scalar(rangeHigh), dst);
                        break;
                    default:
                        // Tạo Mat kết quả cùng kích thước với src
                        Mat dstImage = new Mat(src.Rows, src.Cols, MatType.CV_8UC(src.Channels()));

                        // Gọi các phương thức xử lý tương ứng
                        switch (mode)
                        {
                            case BlobMode.Bernsen:
                                Binarizer.Bernsen(src, dstImage, kSize, (byte)constant, (byte)rangeLow);
                                break;
                            case BlobMode.Nick:
                                Binarizer.Nick(src, dstImage, kSize, adequateCoefficient);
                                break;
                            case BlobMode.NiBackFast:
                                Binarizer.Niblack(src, dstImage, kSize, adequateCoefficient);
                                break;
                            case BlobMode.SauvolaFast:
                                Binarizer.Sauvola(src, dstImage, kSize, adequateCoefficient, adequateCoefficientR);
                                break;
                        }
                        // Gán kết quả đầu ra
                        dst = dstImage.Clone();
                        break;
                }
                return dst;
            }
            catch (Exception ex)
            {
                logger.Create("Binarizing Error: " + ex.Message, ex);
                return null;
            }
        }
        public void ApplyFilter(CvBlobs cvBlobs)
        {
            List<int> requireRemoveList = new List<int>();
            try
            {
                for (int i = 1; i <= cvBlobs.Count; i++)
                {
                    bool requireRemove = false;
                    foreach (BlobFilter filter in this.BlobFilters)
                    {
                        if (!filter.Use) continue;

                        switch (filter.Property)
                        {
                            case BlobFilter.Properties.Area:
                                switch (filter.Range)
                                {
                                    case BlobFilter.RangeFilter.Include:
                                        if (cvBlobs[i].Area < filter.Low || cvBlobs[i].Area > filter.High) requireRemove = true;
                                        break;
                                    case BlobFilter.RangeFilter.Exclude:
                                        if (cvBlobs[i].Area >= filter.Low && cvBlobs[i].Area <= filter.High) requireRemove = true;
                                        break;
                                }
                                break;
                            case BlobFilter.Properties.CenterMassX:
                                switch (filter.Range)
                                {
                                    case BlobFilter.RangeFilter.Include:
                                        if (cvBlobs[i].Centroid.X < filter.Low || cvBlobs[i].Centroid.X > filter.High) requireRemove = true;
                                        break;
                                    case BlobFilter.RangeFilter.Exclude:
                                        if (cvBlobs[i].Centroid.X >= filter.Low && cvBlobs[i].Centroid.X <= filter.High) requireRemove = true;
                                        break;
                                }
                                break;
                            case BlobFilter.Properties.CenterMassY:
                                switch (filter.Range)
                                {
                                    case BlobFilter.RangeFilter.Include:
                                        if (cvBlobs[i].Centroid.Y < filter.Low || cvBlobs[i].Centroid.Y > filter.High) requireRemove = true;
                                        break;
                                    case BlobFilter.RangeFilter.Exclude:
                                        if (cvBlobs[i].Centroid.Y >= filter.Low && cvBlobs[i].Centroid.Y <= filter.High) requireRemove = true;
                                        break;
                                }
                                break;
                            //Angle Filter ADD
                            case BlobFilter.Properties.Angle:

                                double angle = 0;
                                angle = Math.Atan(2 * cvBlobs[i].N11 / (cvBlobs[i].N20 - cvBlobs[i].N02)) / 2;

                                if (angle > 0)
                                { if (cvBlobs[i].U20 < cvBlobs[i].U02) angle = angle - 90 * (Math.PI / 180); }
                                else
                                { if (cvBlobs[i].U20 < cvBlobs[i].U02) angle = 90 * (Math.PI / 180) + angle; }

                                angle = angle * (180 / Math.PI);

                                switch (filter.Range)
                                {
                                    case BlobFilter.RangeFilter.Include:
                                        if (angle < filter.Low || angle > filter.High) requireRemove = true;
                                        break;
                                    case BlobFilter.RangeFilter.Exclude:
                                        if (blobs[i].Angle >= filter.Low && blobs[i].Angle <= filter.High) requireRemove = true;
                                        break;
                                }
                                break;
                            case BlobFilter.Properties.RectSize:
                                switch (filter.Range)
                                {
                                    case BlobFilter.RangeFilter.Include:
                                        if ((cvBlobs[i].Rect.Width * cvBlobs[i].Rect.Height) < filter.Low || (cvBlobs[i].Rect.Width * cvBlobs[i].Rect.Height) > filter.High) requireRemove = true;
                                        break;
                                    case BlobFilter.RangeFilter.Exclude:
                                        if ((cvBlobs[i].Rect.Width * cvBlobs[i].Rect.Height) >= filter.Low && (cvBlobs[i].Rect.Width * cvBlobs[i].Rect.Height) <= filter.High) requireRemove = true;
                                        break;
                                }
                                break;
                            case BlobFilter.Properties.RectWidth:
                                switch (filter.Range)
                                {
                                    case BlobFilter.RangeFilter.Include:
                                        if (cvBlobs[i].Rect.Width < filter.Low || cvBlobs[i].Rect.Width > filter.High) requireRemove = true;
                                        break;
                                    case BlobFilter.RangeFilter.Exclude:
                                        if (cvBlobs[i].Rect.Width >= filter.Low && cvBlobs[i].Rect.Width <= filter.High) requireRemove = true;
                                        break;
                                }
                                break;
                            case BlobFilter.Properties.RectHeight:
                                switch (filter.Range)
                                {
                                    case BlobFilter.RangeFilter.Include:
                                        if (cvBlobs[i].Rect.Height < filter.Low || cvBlobs[i].Rect.Height > filter.High) requireRemove = true;
                                        break;
                                    case BlobFilter.RangeFilter.Exclude:
                                        if (cvBlobs[i].Rect.Height >= filter.Low && cvBlobs[i].Rect.Height <= filter.High) requireRemove = true;
                                        break;
                                }
                                break;
                        }
                    }
                    if (requireRemove) requireRemoveList.Add(i);
                }

                foreach (int index in requireRemoveList) { cvBlobs.Remove(index); }
            }
     
            catch(Exception ex)
            {
                logger.Create("Apply Filter Error: " + ex.Message, ex);
            }
        }
        public OpenCvSharp.Point[][] ApplyFilterContours(OpenCvSharp.Point[][] contours)
        {
            List<OpenCvSharp.Point[]> filteredContours = new List<OpenCvSharp.Point[]>();
            try
            {
                foreach (var contour in contours)
                {
                    bool requireRemove = false;

                    // Tính toán các thuộc tính cần thiết
                    double area = Cv2.ContourArea(contour);
                    var moments = Cv2.Moments(contour);
                    double centerX = moments.M10 / (moments.M00 != 0 ? moments.M00 : 1);
                    double centerY = moments.M01 / (moments.M00 != 0 ? moments.M00 : 1);
                    var rect = Cv2.BoundingRect(contour);

                    // Tính angle nếu contour đủ điểm
                    double angle = 0;
                    if (contour.Length >= 5)
                    {
                        var ellipse = Cv2.FitEllipse(contour);
                        angle = ellipse.Angle;
                    }

                    foreach (BlobFilter filter in this.BlobFilters)
                    {
                        if (!filter.Use) continue;

                        switch (filter.Property)
                        {
                            case BlobFilter.Properties.Area:
                                requireRemove |= !CheckRange(filter, area);
                                break;
                            case BlobFilter.Properties.CenterMassX:
                                requireRemove |= !CheckRange(filter, centerX);
                                break;
                            case BlobFilter.Properties.CenterMassY:
                                requireRemove |= !CheckRange(filter, centerY);
                                break;
                            case BlobFilter.Properties.Angle:
                                if (contour.Length >= 5) // Angle chỉ áp dụng nếu đủ điểm
                                    requireRemove |= !CheckRange(filter, angle);
                                break;
                            case BlobFilter.Properties.RectSize:
                                double rectSize = rect.Width * rect.Height;
                                requireRemove |= !CheckRange(filter, rectSize);
                                break;
                            case BlobFilter.Properties.RectWidth:
                                requireRemove |= !CheckRange(filter, rect.Width);
                                break;
                            case BlobFilter.Properties.RectHeight:
                                requireRemove |= !CheckRange(filter, rect.Height);
                                break;
                        }

                        if (requireRemove) break; // Nếu đã fail 1 tiêu chí thì bỏ luôn
                    }

                    if (!requireRemove)
                    {
                        filteredContours.Add(contour);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Apply Filter Contour Error: " + ex.Message, ex);
            }

            // Trả lại mảng sau khi lọc
            return filteredContours.ToArray();
        }

        // Hàm check range tiện dụng
        private bool CheckRange(BlobFilter filter, double value)
        {
            if (filter.Range == BlobFilter.RangeFilter.Include)
            {
                return value >= filter.Low && value <= filter.High;
            }
            else // Exclude
            {
                return !(value >= filter.Low && value <= filter.High);
            }
        }


        private SvImage runImage = new SvImage();
        private Mat loadedImg = new Mat();
        public override void Run()
        {
            try
            {
                if (this.InputImage == null || this.InputImage.Mat == null || this.InputImage.Mat.Width <= 0 || this.InputImage.Mat.Height <= 0)
                {
                    if (toolBase.isImgPath && isEditMode)
                    {
                        runImage.Mat = loadedImg.Clone();
                        runImage.RegionRect.Rect = new Rect(0, 0, (int)ImgView.Source.Width, (int)ImgView.Source.Height);
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
                    runImage.Mat = loadedImg.Clone();
                    runImage.RegionRect.Rect = new Rect(0, 0, (int)ImgView.Source.Width, (int)ImgView.Source.Height);
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

                OutputImage = runImage.Clone(true);
                //Chuyển về ảnh xám để xử lý tìm Contour
                if (runImage.Mat.Channels() > 1)
                {
                    Cv2.CvtColor(runImage.Mat, runImage.Mat, ColorConversionCodes.BGR2GRAY);
                }
                //Đổi kênh màu để vẽ contour
                if (OutputImage.Mat.Channels() == 1)
                {
                    OutputImage.Mat = OutputImage.Mat.CvtColor(ColorConversionCodes.GRAY2RGB);
                }

                AreaSum = 0; RectLengthSum = 0; RectAreaSum = 0;
                OpenCvSharp.Rect regionRect = runImage.RegionRect.Rect;

                Rect2f roiRectf = runImage.RegionRect.RectF;

                Point2f[] pts = runImage.RegionRect2fPtsImage;
                Point2f[] pts2 = runImage.RegionRect2fPts;

                Mat pMat = Cv2.GetPerspectiveTransform(pts, pts2);
                Mat temp = new Mat();
                Cv2.WarpPerspective(runImage.Mat, temp, pMat, new OpenCvSharp.Size(roiRectf.Size.Width, roiRectf.Size.Height));

                //Create Binary Image
                Mat binary = temp.Clone();
                if (SelectBlobMode == BlobMode.InRange)
                    binary = Binarizing(temp, SelectBlobMode, SelectBlobType, SelectBlobPolarity, SelectBlobBinary, RangeLow, RangeHigh, KernelSize, constant, AdequateCoefficient, AdequateCoefficientR);
                else
                    binary = Binarizing(temp, SelectBlobMode, SelectBlobType, SelectBlobPolarity, SelectBlobBinary, Range, RangeHigh, KernelSize, constant, AdequateCoefficient, AdequateCoefficientR);

                RegionImage = temp;
                if (!IsCalBlob)
                    AreaSum = binary.CountNonZero();


                // Display Blob Image
                Mat binarywarp = new Mat();
                pMat = Cv2.GetPerspectiveTransform(pts2, pts);
                Cv2.WarpPerspective(binary, binarywarp, pMat, runImage.Mat.Size());

                Mat mask = new Mat(runImage.Mat.Size(), runImage.Mat.Type());
                mask.SetTo(0);
                OpenCvSharp.Point[] regionRectPtsImageP = new OpenCvSharp.Point[runImage.RegionRectPtsImage.Length];
                for (int i = 0; i < runImage.RegionRectPtsImage.Length; i++)
                {
                    regionRectPtsImageP[i] = new OpenCvSharp.Point(runImage.RegionRectPtsImage[i].X, runImage.RegionRectPtsImage[i].Y);
                }
                mask.FillPoly(new OpenCvSharp.Point[][] { regionRectPtsImageP }, 1);

                BinaryImage = runImage.Clone(true);
                binarywarp.CopyTo(BinaryImage.Mat, mask);


                if (IsCalBlob)
                {
                    Mat roiMat = new Mat();

                    using (Mat regionMat = new Mat(BinaryImage.Mat.Size(), MatType.CV_8U))
                    {
                        Cv2.Rectangle(regionMat, regionRect, new Scalar(1), -1);
                        BinaryImage.Mat.CopyTo(roiMat, regionMat);
                    }

                    // Tìm contour
                    Cv2.FindContours(roiMat.Clone(), out OpenCvSharp.Point[][] contours, out HierarchyIndex[] hierarchyIndex, RetrievalModes.External, ContourApproximationModes.ApproxNone);
                    // Lọc contour qua các tiêu chuẩn
                    contours = ApplyFilterContours(contours);
                    // Fill holes bằng vẽ contour với độ dày -1 (fill)
                    Cv2.DrawContours(OutputImage.Mat, contours, -1, Scalar.Green, thickness: IsFillHole ? -1 : 1, lineType: LineTypes.Link8);

                    CvBlobs cvBlobs = new CvBlobs(roiMat);
                    // Áp dụng lọc blob (giữ nguyên nếu ApplyFilter đã hỗ trợ Mat hoặc chỉnh sửa lại)
                    ApplyFilter(cvBlobs);

                    blobs?.Clear();
                    blobs = new List<BlobObject>();
                    foreach (KeyValuePair<int, CvBlob> cvblobPair in cvBlobs)
                    {
                        CvBlob cvBlob = cvblobPair.Value;

                        double thetarad;

                        if ((cvBlob.N20 - cvBlob.N02) == 0) thetarad = 0;
                        else
                            thetarad = Math.Atan(2 * cvBlob.N11 / (cvBlob.N20 - cvBlob.N02)) / 2;

                        if (thetarad > 0)
                        { if (cvBlob.U20 < cvBlob.U02) thetarad = thetarad - 90 * (Math.PI / 180); }
                        else
                        { if (cvBlob.U20 < cvBlob.U02) thetarad = 90 * (Math.PI / 180) + thetarad; }

                        string thetaRound = (thetarad * (180 / Math.PI)).ToString("F2");
                        string centroidXRound = cvBlob.Centroid.X.ToString("F3");
                        string centroidYRound = cvBlob.Centroid.Y.ToString("F3");

                        double thetaDeg = double.Parse(thetaRound);
                        double centroidX = double.Parse(centroidXRound);
                        double centroidY = double.Parse(centroidYRound);
                        BlobObject blob = new BlobObject(cvBlob.Label, cvBlob.Area, new Point2d(centroidX, centroidY), thetaDeg, (cvBlob.Rect.Width * cvBlob.Rect.Height), cvBlob.Rect.Width, cvBlob.Rect.Height);

                        //Insert Sort
                        int insertIndex;
                        for (insertIndex = 0; insertIndex < blobs.Count; insertIndex++)
                        {
                            if (blob.Area > blobs[insertIndex].Area)
                                break;
                        }

                        Blobs.Insert(insertIndex, blob);
                    }

                    //if (isEditMode)
                    //{
                    //    if (BinaryImage != null)
                    //    {
                    //        // Tạo ảnh đầu ra (3 kênh để tô màu các blob)
                    //        Mat blobMat = new Mat(roiMat.Size(), MatType.CV_8UC3, Scalar.All(0));
                    //        try
                    //        {
                    //            cvBlobs.RenderBlobs(roiMat, blobMat, RenderBlobsMode.Angle | RenderBlobsMode.Centroid | RenderBlobsMode.Color | RenderBlobsMode.BoundingBox);
                    //            //cvBlobs.RenderBlobs(roiMat, blobMat, RenderBlobsMode.Centroid | RenderBlobsMode.Color);

                    //            BlobImage?.Dispose();
                    //            BlobImage = new SvImage(blobMat.Clone());

                    //            blobMat.Dispose();
                    //        }
                    //        catch
                    //        {
                    //            BlobImage?.Dispose();
                    //            blobMat.Dispose();
                    //        }
                    //    }
                    //}
                    if (BinaryImage != null)
                    {
                        try
                        {
                            cvBlobs.RenderBlobs(OutputImage.Mat, OutputImage.Mat, RenderBlobsMode.Angle | RenderBlobsMode.Centroid | RenderBlobsMode.BoundingBox);
                        }
                        catch
                        {
                            BlobImage?.Dispose();
                        }
                    }

                    cvBlobs.Clear();
                    roiMat.Dispose();


                    if (Blobs != null && Blobs.Count > 0)
                    {
                        switch (SelectBlobPriority)
                        {
                            case BlobPriority.None:
                                Blobs.Sort((x, y) =>
                                {
                                    double xVal, yVal;
                                    switch (sortingOrder)
                                    {
                                        case BlobFilter.Properties.Area:
                                            xVal = x.Area; yVal = y.Area;
                                            break;
                                        case BlobFilter.Properties.CenterMassX:
                                            xVal = x.Centroid.X; yVal = y.Centroid.X;
                                            break;
                                        case BlobFilter.Properties.CenterMassY:
                                            xVal = x.Centroid.Y; yVal = y.Centroid.Y;
                                            break;
                                        case BlobFilter.Properties.Angle:
                                            xVal = x.Angle; yVal = y.Angle;
                                            break;
                                        case BlobFilter.Properties.RectSize:
                                            xVal = x.RectSize; yVal = y.RectSize;
                                            break;
                                        case BlobFilter.Properties.RectWidth:
                                            xVal = x.RectWidth; yVal = y.RectWidth;
                                            break;
                                        case BlobFilter.Properties.RectHeight:
                                            xVal = x.RectHeight; yVal = y.RectHeight;
                                            break;
                                        default:
                                            return 0;
                                    }

                                    int retrunVal = 0;
                                    if (xVal > yVal) retrunVal = -1;
                                    else if (xVal < yVal) retrunVal = 1;
                                    else retrunVal = 0;

                                    if (IsAscend) retrunVal *= -1;

                                    return retrunVal;
                                });
                                break;
                            case BlobPriority.Left:
                                Blobs = Blobs.OrderBy(x => x.Centroid.X).ToList();
                                break;

                            case BlobPriority.Right:
                                Blobs = Blobs.OrderByDescending(x => x.Centroid.X).ToList();
                                break;

                            case BlobPriority.Top:
                                Blobs = Blobs.OrderBy(x => x.Centroid.Y).ToList();
                                break;

                            case BlobPriority.Bottom:
                                Blobs = Blobs.OrderByDescending(x => x.Centroid.Y).ToList();
                                break;
                        }
                        switch (SelectSort)
                        {
                            case SortType.ID:
                                Blobs = Blobs.OrderBy(a => a.Label).ToList();
                                break;
                            case SortType.Area:
                                Blobs = Blobs.OrderByDescending(a => a.Area).ToList();
                                break;
                            case SortType.CenterMassX:
                                Blobs = Blobs.OrderByDescending(a => a.CenterMassX).ToList();
                                break;
                            case SortType.CenterMassY:
                                Blobs = Blobs.OrderByDescending(a => a.CenterMassY).ToList();
                                break;
                            case SortType.Angle:
                                Blobs = Blobs.OrderByDescending(a => a.Angle).ToList();
                                break;
                            case SortType.RectSize:
                                Blobs = Blobs.OrderByDescending(a => a.RectSize).ToList();
                                break;
                            case SortType.RectWidth:
                                Blobs = Blobs.OrderByDescending(a => a.RectWidth).ToList();
                                break;
                            case SortType.RectHeight:
                                Blobs = Blobs.OrderByDescending(a => a.RectHeight).ToList();
                                break;
                        }
                        if (MaxCount > Blobs.Count || MaxCount == 0)
                            MaxCount = Blobs.Count;
                        for (int i = 0; i < MaxCount; i++)
                        {
                            AreaSum += Blobs[i].Area;
                            RectLengthSum += Blobs[i].RectWidth + blobs[i].RectHeight;
                            RectAreaSum += Blobs[i].RectSize;
                        }
                        TranslateX = (float)Blobs[0].Centroid.X;
                        TranslateY = (float)Blobs[0].Centroid.Y;
                        Rotation = (float)(Blobs[0].Angle * Math.PI / 180);
                        if (isEditMode) { OnPropertyChanged(nameof(Blobs)); }
                    }
                }
                else
                {
                    AreaSum = binary.CountNonZero();
                }

                temp.Dispose();
                binary.Dispose();
                binarywarp.Dispose();
                mask.Dispose();
                pMat.Dispose();

                this.Dispatcher.Invoke(() =>
                {
                    ImgView.Source = BinaryImage.Mat.ToBitmapSource();
                    toolBase.cbxImage.SelectedIndex = isEditMode ? 1 : 0;
                });
            }
            catch (Exception ex)
            {
                logger.Create("Run Error: " + ex.Message, ex);
            }
        }

        private void OnSelectBlobModeChanged(object sender)
        {
            InitVisibility();
            switch (SelectBlobMode)
            {
                case BlobMode.Threshold:
                    switch (SelectBlobType)
                    {
                        case BlobType.Binary:
                        case BlobType.Otsu:
                            break;
                        default:
                            SelectBlobType = BlobType.Binary;
                            break;
                    }
                    break;
                case BlobMode.Adaptive:
                    switch (SelectBlobType)
                    {
                        case BlobType.Gaussian:
                        case BlobType.Mean:
                            break;
                        default:
                            SelectBlobType = BlobType.Gaussian;
                            break;
                    }
                    stBlockSize.Visibility = Visibility.Visible;
                    stConstSub.Visibility = Visibility.Visible;
                    IsPolarityEnable = true;
                    IsBlobTypeEnable = true;
                    IsBinaryEnable = false;
                    break;
                case BlobMode.InRange:
                    stRange.Visibility = Visibility.Hidden;
                    stLowHigh.Visibility = Visibility.Visible;
                    IsPolarityEnable = false;
                    IsBlobTypeEnable = false;
                    IsBinaryEnable = false;
                    break;
                case BlobMode.Bernsen:
                    stBlockSize.Visibility = Visibility.Visible;
                    stConstMin.Visibility = Visibility.Visible;
                    IsPolarityEnable = false;
                    IsBlobTypeEnable = false;
                    IsBinaryEnable = false;
                    break;
                case BlobMode.Nick:
                    stRange.Visibility = Visibility.Hidden;
                    stBlockSize.Visibility = Visibility.Visible;
                    stCoeff.Visibility = Visibility.Visible;
                    IsPolarityEnable = false;
                    IsBlobTypeEnable = false;
                    IsBinaryEnable = false;
                    break ;
                case BlobMode.NiBackFast:
                    stRange.Visibility = Visibility.Hidden;
                    stBlockSize.Visibility = Visibility.Visible;
                    stCoeff.Visibility = Visibility.Visible;
                    IsPolarityEnable = false;
                    IsBlobTypeEnable = false;
                    IsBinaryEnable = false;
                    break;
                case BlobMode.SauvolaFast:
                    stRange.Visibility = Visibility.Hidden;
                    stBlockSize.Visibility = Visibility.Visible;
                    stCoeff.Visibility = Visibility.Visible;
                    stCoeffR.Visibility = Visibility.Visible;
                    IsPolarityEnable = false;
                    IsBlobTypeEnable = false;
                    IsBinaryEnable = false;
                    break;
            }
        }
        private void OnSelectBlobTypeChanged(object sender)
        {
            if (SelectBlobMode == BlobMode.Threshold)
            {
                if (SelectBlobType == BlobType.Otsu)
                {
                    stRange.Visibility = Visibility.Hidden;
                }
            }
        }
        private void OnBlockSizeChanged(object sender)
        {
            if (BlockSize % 2 == 0)
            {
                BlockSize += 1;
            }
        }
        private void OnBlobPriorityChanged(object sender)
        {
            if (SelectBlobPriority == BlobPriority.None)
                IsSortTypeEnable = true;
            else
                IsSortTypeEnable = false;
        }
    }
    public class BlobObject
    {
        int label;
        int area;
        Point2d centroid;
        double angle;
        int rectsize;
        int rectwidth;
        int rectheight;

        public BlobObject()
        { }

        public BlobObject(int label, int area, Point2d centroid, double angle, int rectsize, int rectwidth, int rectheight)
            : this()
        {
            this.label = label; this.area = area; this.centroid = centroid; this.angle = angle; this.rectsize = rectsize; this.rectwidth = rectwidth; this.rectheight = rectheight;
        }

        public int Label
        {
            get { return label; }
            set { label = value; }
        }

        public int Area
        {
            get { return area; }
            set { area = value; }
        }

        public Point2d Centroid
        {
            get { return centroid; }
            set { centroid = value; }
        }
        public double CenterMassX
        {
            get { return centroid.X; }
        }
        public double CenterMassY
        {
            get { return centroid.Y; }
        }

        public double Angle
        {
            get { return angle; }
            set { angle = value; }
        }

        public int RectSize
        {
            get { return rectsize; }
            set { rectsize = value; }
        }
        public int RectWidth
        {
            get { return rectwidth; }
            set { rectwidth = value; }
        }
        public int RectHeight
        {
            get { return rectheight; }
            set { rectheight = value; }
        }
    }
    public class BlobFilter
    {
        public enum Properties { Area, CenterMassX, CenterMassY, Angle, RectSize, RectWidth, RectHeight }
        public enum RangeFilter { Include, Exclude };

        public BlobFilter() { }
        public BlobFilter(Properties property) { this.property = property; }

        Properties property;
        public Properties Property { get { return property; } set { property = value; } }
        bool use;
        public bool Use { get { return use; } set { use = value; } }
        RangeFilter range;
        public RangeFilter Range { get { return range; } set { range = value; } }
        int low;
        public int Low { get { return low; } set { low = value; } }
        int high = 10000000;
        public int High { get { return high; } set { high = value; } }
    }
    public static class RangeValues
    {
        public static Array All => Enum.GetValues(typeof(BlobFilter.RangeFilter));
    }
}
