using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using VisionInspection;
using VisionTools.ToolEdit;
using Point = System.Windows.Point;

namespace VisionTools.ToolDesign
{
    /// <summary>
    /// Interaction logic for ToolArea.xaml
    /// </summary>
    public partial class ToolArea : Canvas
    {
        private MyLogger logger = new MyLogger("ToolArea");
        public bool IsOutTool = false;
        public bool IsBlockOut = false;
        public double heightAllTool = 0;
        public List<double> heightToolLst = new List<double>();
        public List<ArrowConnector> arrowCntLst = new List<ArrowConnector>();
        List<Label> lbOfAllTools = new List<Label>();
        public Grid toolSelected = new Grid();
        public Label lbClicked = new Label();
        public int resRunTools = 0;
        public string sttRunTools = "";
        public SvImage OriginImage = new SvImage();
        public event RoutedEventHandler OnChildrenChanged;
        public event DragEventHandler OnToolDrop;
        public event RoutedEventHandler OnToolDeleted;

        private List<Tuple<int, SvImage, double>> _imgLstSub = new List<Tuple<int, SvImage, double>>();
        public List<Tuple<int, SvImage, double>> ImgLstSub { get => _imgLstSub; 
            set
            {
                _imgLstSub = value;
                foreach(var tool in this.Children)
                {
                    if(tool is OutBlobResTool outResTool)
                    {
                        outResTool.toolEdit.ImgLstSub = value;
                        break;
                    }    
                }    
            }
        }
        public bool IsToolMain { get; set; } = false; 
        //Draw Arrow
        Point startPoint = new Point();
        public ToolArea()
        {
            InitializeComponent();
            RegisEvent();
        }
        private void RegisEvent()
        {
            this.Drop += ToolArea_Drop;
        }
        private void ToolArea_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(typeof(VisionTool)))
                {
                    VisionTool receivedData = e.Data.GetData(typeof(VisionTool)) as VisionTool;
                    if (receivedData.Parent != null)
                        return;
                    else
                    {
                        CreateNewTool(receivedData);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Drop Tool to ToolArea Error: " + ex.Message, ex);
            }
        }

        public void CreateNewTool(VisionTool receivedData)
        {
            try
            {
                VisionTool newEle;
                switch (receivedData.ToolType)
                {
                    case VisionToolType.ACQUISITION:
                        if (!this.IsToolMain)
                            return;
                        newEle = (VisionTool)Activator.CreateInstance(typeof(AcquisitionTool));
                        break;
                    case VisionToolType.IMAGEBUFF:
                        newEle = (VisionTool)Activator.CreateInstance(typeof(ImageBuffTool));
                        break;
                    case VisionToolType.TEMPLATEMATCH:
                        newEle = (VisionTool)Activator.CreateInstance(typeof(TemplateMatchTool));
                        break;
                    case VisionToolType.TEMPMATCHZERO:
                        newEle = (VisionTool)Activator.CreateInstance(typeof(TempMatchZeroTool));
                        break;
                    case VisionToolType.FIXTURE:
                        newEle = (VisionTool)Activator.CreateInstance(typeof(FixtureTool));
                        break;
                    case VisionToolType.EDITREGION:
                        newEle = (VisionTool)Activator.CreateInstance(typeof(EditRegionTool));
                        break;
                    case VisionToolType.IMAGEPROCESS:
                        newEle = (VisionTool)Activator.CreateInstance(typeof(ImageProcessTool));
                        break;
                    case VisionToolType.CONTRASTnBRIGHTNESS:
                        newEle = (VisionTool)Activator.CreateInstance(typeof(ContrastNBrightnessTool));
                        break;
                    case VisionToolType.BLOB:
                        newEle = (VisionTool)Activator.CreateInstance(typeof(BlobTool));
                        break;
                    case VisionToolType.SAVEIMAGE:
                        newEle = (VisionTool)Activator.CreateInstance(typeof(SaveImageTool));
                        break;
                    case VisionToolType.SEGMENTNEURO:
                        if (!UiManager.CheckNeurocleLicense())
                            return;
                        newEle = (VisionTool)Activator.CreateInstance(typeof(SegmentNeuroTool));
                        break;
                    case VisionToolType.VISIONPRO:
                        if (!UiManager.CheckVisionProLicense())
                            return;
                        newEle = (VisionTool)Activator.CreateInstance(typeof(VisionProTool));
                        (newEle as VisionProTool).OnOutputChanged += VisionProTool_OnOutputChanged;
                        break;
                    case VisionToolType.VIDICOGNEX:
                        if (!UiManager.CheckVidiLicense())
                            return;
                        newEle = (VisionTool)Activator.CreateInstance(typeof(VidiCognexTool));
                        break;
                    case VisionToolType.OUTIMAGESUB:
                        newEle = (VisionTool)Activator.CreateInstance(typeof(OutputImageSubTool));
                        break;
                    case VisionToolType.OUTBLOBRES:
                        OnToolDrop?.Invoke(this, null);
                        //if (IsOutTool || IsBlockOut) return;
                        IsOutTool = true;
                        IsBlockOut = true;
                        newEle = (VisionTool)Activator.CreateInstance(typeof(OutBlobResTool));
                        break;
                    case VisionToolType.OUTCHECKPRODUCT:
                        OnToolDrop?.Invoke(this, null);
                        //if (IsOutTool || IsBlockOut) return;
                        IsOutTool = true;
                        IsBlockOut = true;
                        newEle = (VisionTool)Activator.CreateInstance(typeof(OutCheckProductTool));
                        break;
                    case VisionToolType.OUTSEGNEURORES:
                        OnToolDrop?.Invoke(this, null);
                        //if (IsOutTool || IsBlockOut) return;
                        IsOutTool = true;
                        IsBlockOut = true;
                        newEle = (VisionTool)Activator.CreateInstance(typeof(OutSegNeuroResTool));
                        break;
                    case VisionToolType.OUTVIDICOGRES:
                        OnToolDrop?.Invoke(this, null);
                        //if (IsOutTool || IsBlockOut) return;
                        IsOutTool = true;
                        IsBlockOut = true;
                        newEle = (VisionTool)Activator.CreateInstance(typeof(OutVidiCogResTool));
                        break;
                    default:
                        return;
                }

                List<StackPanel> stBound = newEle.Children.OfType<StackPanel>().Skip(1).ToList();
                foreach (var st in stBound)
                {
                    if (st.Children.Count == 0) continue;
                    Label lb = st.Children.OfType<Label>().First();
                    if(lb == null) continue;    
                    lb.AllowDrop = true;
                    lb.MouseDown += LbTool_MouseDown;
                    lb.Drop += LbTool_Drop;
                    lbOfAllTools.Add(st.Children.OfType<Label>().First());
                }

                var listSame = this.Children.OfType<VisionTool>().Where(ele => ele.GetType() == newEle.GetType()).ToList();
                newEle.Name += listSame.Count.ToString();
                UpdateTitleTool(newEle);
                newEle.MouseMove += NewEle_MouseMove;
                newEle.MouseDown += NewEle_MouseDown;
                this.Children.Add(newEle);
                if (heightToolLst.Count == 0)
                {
                    heightAllTool = 0;
                    heightToolLst.Add(0d);
                }
                Canvas.SetTop(newEle, heightAllTool);

                var timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    heightAllTool += newEle.ActualHeight;
                    heightToolLst.Add(heightAllTool);
                };
                timer.Start();

                //Link để Hàm Check OutResult tool để hiển thị ra PgCamera
                switch (receivedData.ToolType)
                {
                    case VisionToolType.OUTBLOBRES:
                    case VisionToolType.OUTCHECKPRODUCT:
                    case VisionToolType.OUTSEGNEURORES:
                    case VisionToolType.OUTVIDICOGRES:
                        OnChildrenChanged?.Invoke(receivedData, new RoutedEventArgs());
                        break;
                }

            }
            catch (Exception ex)
            {
                logger.Create("Create New Tool Error: " + ex.Message, ex);
            }
        }

        private void VisionProTool_OnOutputChanged(object sender, EventArgs e)
        {
            if (sender is VisionProTool vsProTool)
            {
                List<VisionTool> tools = this.Children.OfType<VisionTool>().ToList();
                if (tools.Count == 0)
                    return;
                int idxTool = tools.IndexOf(vsProTool);
                double heightToolVpp = heightToolLst[idxTool] + vsProTool.Height;
                heightAllTool = heightToolVpp;
                heightToolLst.RemoveRange(idxTool + 1, heightToolLst.Count - (idxTool + 1));
                heightToolLst.Add(heightToolVpp);

                if (idxTool + 1 < tools.Count)
                {
                    for (int i = idxTool + 1; i < tools.Count; i++)
                    {
                        Canvas.SetTop(tools[i], heightAllTool);
                        heightAllTool += (tools[i] as VisionTool).ActualHeight;
                        heightToolLst.Add(heightAllTool);
                    }
                }
                else if (idxTool + 1 == tools.Count)
                {
                    heightToolLst.Add(heightAllTool);
                }
                InitLabelTool(vsProTool);

                if (vsProTool.toolEdit.IsNewVppData == false)
                    return;
                //Delete Arrow connect to tool
                List<int> idxDeletes = new List<int>();
                for (int i = 0; i < arrowCntLst.Count; i++)
                {
                    if (arrowCntLst[i].name.Contains(vsProTool.Name))
                    {
                        this.Children.Remove(arrowCntLst[i].arrowLine);
                        this.Children.Remove(arrowCntLst[i].arrowHead);
                        idxDeletes.Add(i);
                    }
                }
                foreach (int index in idxDeletes.OrderByDescending(i => i))
                {
                    arrowCntLst.RemoveAt(index);
                }
            }
        }

        public void UpdateTitleTool(VisionTool ele)
        {
            string contentTool = ele.Name.Replace("Tool", "");

            dynamic tool = ele;
            try
            {
                tool.lbTitle.Content = contentTool;
            }
            catch { /* Không có lbTitle thì bỏ qua */ }
        }

        public void InitLabelTool(VisionTool receivedData)
        {
            try
            {
                //Skip Title Label
                List<StackPanel> stBound = receivedData.Children.OfType<StackPanel>().Skip(1).ToList();
                foreach (var st in stBound)
                {
                    Label lb = st.Children.OfType<Label>().First();
                    if(lb == null) continue;
                    lb.AllowDrop = true;
                    lb.MouseDown += LbTool_MouseDown;
                    lb.Drop += LbTool_Drop;
                    lbOfAllTools.Add(lb);
                }
            }
            catch (Exception ex)
            {
                logger.Create("Init Tool Error: " + ex.Message, ex);
            }
        }
        public void LoadTool(VisionTool receivedData, double locateTop)
        {
            try
            {
                UpdateTitleTool(receivedData);
                receivedData.MouseMove += NewEle_MouseMove;
                receivedData.MouseDown += NewEle_MouseDown;
                this.Children.Add(receivedData);
                Canvas.SetTop(receivedData, locateTop);
                //Link để Hàm Check OutResult tool để hiển thị ra PgCamera
                switch (receivedData.ToolType)
                {
                    case VisionToolType.VISIONPRO:
                        (receivedData as VisionProTool).OnOutputChanged += VisionProTool_OnOutputChanged;
                        break;
                    case VisionToolType.OUTBLOBRES:
                    case VisionToolType.OUTCHECKPRODUCT:
                    case VisionToolType.OUTSEGNEURORES:
                    case VisionToolType.OUTVIDICOGRES:
                        OnChildrenChanged?.Invoke(receivedData, new RoutedEventArgs());
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Create("Load Tool Error: " + ex.Message, ex);
            }
        }
        public Dictionary<int, string[]> CreateConnectTags(List<ArrowConnector> arrowCnts)
        {
            try
            {
                if (arrowCnts.Count == 0) return null;
                Dictionary<int, string[]> connectTags = new Dictionary<int, string[]>();
                for (int i = 0; i < arrowCnts.Count; i++)
                {
                    string[] nameInOut = arrowCnts[i].name.Split('-');
                    string[] nameIn = nameInOut[0].Split('.');
                    string[] nameOut = nameInOut[1].Split('.');
                    connectTags.Add(i, new string[] { nameIn[0], nameIn[1], nameOut[0], nameOut[1] });
                }
                return connectTags;
            }
            catch (Exception ex)
            { 
                logger.Create("Create Connect Tags Error: " + ex.Message, ex);
                return null;
            }
        }

        private Label lbSource, lbTarget;
        private VisionTool sourceTool, targetTool;
        private Type sourceType;
        private void LbTool_MouseDown(object sender, MouseButtonEventArgs e)
        {
            sourceType = null;
            lbSource = sender as Label;
            try
            {
                startPoint = lbSource.TranslatePoint(new Point(lbSource.ActualWidth, lbSource.ActualHeight / 2), this);
                sourceTool = (lbSource.Parent as StackPanel).Parent as VisionTool;
                //Output
                switch (sourceTool.ToolType)
                {
                    case VisionToolType.ACQUISITION:
                    case VisionToolType.IMAGEBUFF:
                    case VisionToolType.FIXTURE:
                    case VisionToolType.EDITREGION:
                    case VisionToolType.CONTRASTnBRIGHTNESS:
                    case VisionToolType.IMAGEPROCESS:
                        switch (lbSource.Name)
                        {
                            case "lbOutputImage":
                                sourceType = typeof(SvImage);
                                break;
                        }
                        break;
                    case VisionToolType.OUTIMAGESUB:
                        switch (lbSource.Name)
                        {
                            case "lbOriginImage":
                                sourceType = typeof(SvImage);
                                break;
                            case "lbOutputImage":
                                sourceType = typeof(SvImage);
                                break;
                        }
                        break;
                    case VisionToolType.TEMPLATEMATCH:
                    case VisionToolType.TEMPMATCHZERO:
                        switch (lbSource.Name)
                        {
                            case "lbScore":
                                sourceType = typeof(double);
                                break;
                            case "lbTranslateX":
                                sourceType = typeof(double);
                                break;
                            case "lbTranslateY":
                                sourceType = typeof(double);
                                break;
                            case "lbRotation":
                                sourceType = typeof(double);
                                break;
                        }
                        break;
                    case VisionToolType.BLOB:
                        switch (lbSource.Name)
                        {
                            case "lbOutputImage":
                                sourceType = typeof(SvImage);
                                break;
                            case "lbBlobCount":
                                sourceType = typeof(int);
                                break;
                            case "lbBlobs":
                                sourceType = typeof(List<BlobObject>);
                                break;
                        }
                        break;
                    case VisionToolType.SEGMENTNEURO:
                    case VisionToolType.VIDICOGNEX:
                        switch (lbSource.Name)
                        {
                            case "lbOutputImage":
                                sourceType = typeof(SvImage);
                                break;
                            case "lbScore":
                                sourceType = typeof(double);
                                break;
                            case "lbStrResult":
                                sourceType = typeof(string);
                                break;
                            case "lbJudge":
                                sourceType = typeof(int);
                                break;
                        }
                        break;
                    case VisionToolType.VISIONPRO:
                        var vppOuts = ((VisionProTool)sourceTool).toolEdit.VppOutInfors;
                        foreach (VppInOutInfo vppOut in vppOuts)
                        {
                            if ((string)lbSource.Content == vppOut.Name)
                            {
                                sourceType = UiManager.GetTypeByName(vppOut.ValueType);
                                break;
                            }
                        }
                        break;
                            
                    default:
                        return;
                }
            }
            catch (Exception ex)
            {
                logger.Create("Create Output Connect Error: " + ex.Message, ex);
            }
        }
        private void LbTool_Drop(object sender, DragEventArgs e)
        {
            if (sourceType == null) { return; }
            lbTarget = sender as Label;
            try
            {
                targetTool = (lbTarget.Parent as StackPanel).Parent as VisionTool;
                //Input
                switch (targetTool.ToolType)
                {
                    case VisionToolType.TEMPLATEMATCH:
                    case VisionToolType.TEMPMATCHZERO:
                    case VisionToolType.EDITREGION:
                    case VisionToolType.IMAGEPROCESS:
                    case VisionToolType.CONTRASTnBRIGHTNESS:
                    case VisionToolType.BLOB:
                    case VisionToolType.IMAGEBUFF:
                    case VisionToolType.SAVEIMAGE:
                    case VisionToolType.VIDICOGNEX:
                        switch (lbTarget.Name)
                        {
                            case "lbInputImage":
                                if (sourceType != typeof(SvImage)) { return; }
                                break;
                            default:
                                return;
                        }
                        break;
                    case VisionToolType.FIXTURE:
                        switch (lbTarget.Name)
                        {
                            case "lbInputImage":
                                if (sourceType != typeof(SvImage)) { return; }
                                break;
                            case "lbTranslateX":
                                if (sourceType != typeof(double)) { return; }
                                break;
                            case "lbTranslateY":
                                if (sourceType != typeof(double)) { return; }
                                break;
                            case "lbRotation":
                                if (sourceType != typeof(double)) { return; }
                                break;
                            default:
                                return;
                        }
                        break;
                    case VisionToolType.VISIONPRO:
                        VisionProEdit vsProEdit = (targetTool as VisionProTool).toolEdit;
                        if(vsProEdit ==  null) return;
                        foreach(var vppIn in vsProEdit.VppInInfors)
                        {
                            if(lbTarget.Content.ToString() == vppIn.Name)
                            {
                                if(sourceType != UiManager.GetTypeByName(vppIn.ValueType)) { return; }
                                break;
                            }    
                        }    
                        break;
                    case VisionToolType.OUTBLOBRES:
                        switch (lbTarget.Name)
                        {
                            case "lbOriginImage":
                                if (sourceType != typeof(SvImage)) { return; }
                                break;
                            case "lbInputImage":
                                if (sourceType != typeof(SvImage)) { return; }
                                break;
                            case "lbBlobs1":
                                if (sourceType != typeof(List<BlobObject>)) { return; }
                                break;
                            case "lbBlobs2":
                                if (sourceType != typeof(List<BlobObject>)) { return; }
                                break;
                            default:
                                return;
                        }
                        break;
                    case VisionToolType.SEGMENTNEURO:
                    case VisionToolType.OUTCHECKPRODUCT:
                        switch (lbTarget.Name)
                        {
                            case "lbInputImage":
                                if (sourceType != typeof(SvImage)) { return; }
                                break;
                            case "lbScore":
                                if (sourceType != typeof(double)) { return; }
                                break;
                            case "lbBlobs":
                                if (sourceType != typeof(List<BlobObject>)) { return; }
                                break;
                            default:
                                return;
                        }
                        break;
                    case VisionToolType.OUTSEGNEURORES:
                    case VisionToolType.OUTVIDICOGRES:
                        switch (lbTarget.Name)
                        {
                            case "lbInputImage":
                                if (sourceType != typeof(SvImage)) { return; }
                                break;
                            case "lbScore":
                                if (sourceType != typeof(double)) { return; }
                                break;
                            case "lbStrResult":
                                if (sourceType != typeof(string)) { return; }
                                break;
                            case "lbJudge":
                                if (sourceType != typeof(int)) { return; }
                                break;
                            default:
                                return;
                        }
                        break;
                    default:
                        return;
                }

                //Tạo Name để kết nối giữa các label tool
                Point endPoint = lbTarget.TranslatePoint(new Point(lbTarget.ActualWidth, lbTarget.ActualHeight / 2), this);
                StackPanel stBound = lbTarget.Parent as StackPanel;
                string namePolyline = $"{sourceTool.Name}.{lbSource.Name}-{targetTool.Name}.{lbTarget.Name}";
                for (int i = 0; i < arrowCntLst.Count; i++)
                {
                    string targetName = arrowCntLst[i].name.Substring(arrowCntLst[i].name.IndexOf('-') + 1);
                    if (targetName == $"{targetTool.Name}.{lbTarget.Name}")
                    {
                        this.Children.Remove(arrowCntLst[i].arrowLine);
                        this.Children.Remove(arrowCntLst[i].arrowHead);
                        arrowCntLst.RemoveAt(i);
                    }
                }
                // Vẽ mũi tên
                CreateArrowConnect(namePolyline, startPoint, endPoint); ;
            }
            catch (Exception ex)
            {
                logger.Create("Create Input Connect Error: " + ex.Message, ex);
            }
        }

        public void ArrowLin_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Polyline polyline)
            {
                polyline.StrokeThickness = 4; // Độ dày tăng lên
                polyline.Stroke = Brushes.LightCoral; // Màu đỏ nhạt
            }
        }

        public void ArrowLine_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Polyline polyline)
            {
                polyline.StrokeThickness = 2; // Độ dày tăng lên
                polyline.Stroke = Brushes.Red; // Trở về màu đỏ
            }
        }
        public void ArrowLine_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Polyline polyline)
            {
                polyline.StrokeThickness = 4; // Độ dày tăng lên
                polyline.Stroke = Brushes.Red; // Màu đỏ nhạt
            }
            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (this.FindResource("cmTool") is ContextMenu cm)
                {
                    cm.PlacementTarget = sender as UIElement; // Đặt đúng kiểu dữ liệu
                    Polyline arrowLine = sender as Polyline;
                    arrowLine.ContextMenu = cm;
                    cm.IsOpen = true; // Mở ContextMenu
                }
            }
        }

        public Polyline DrawPolyline(Point startPoint, Point endPoint, double heightArrow = 100)
        {
            Polyline arrowLine = new Polyline()
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };
            arrowLine.MouseDown += ArrowLine_MouseDown;
            arrowLine.MouseLeave += ArrowLine_MouseLeave;
            arrowLine.MouseEnter += ArrowLin_MouseEnter;

            arrowLine.Points.Add(startPoint);
            arrowLine.Points.Add(new Point(Math.Max(startPoint.X, endPoint.X) + heightArrow, startPoint.Y));
            arrowLine.Points.Add(new Point(Math.Max(startPoint.X, endPoint.X) + heightArrow, endPoint.Y));
            arrowLine.Points.Add(endPoint);
            return arrowLine;
        }

        public Polygon DrawArrowHead(Point prev, Point end)
        {
            Polygon arrowHead = new Polygon
            {
                Fill = Brushes.Red
            };
            double arrowSize = 10;
            Vector direction = new Vector(end.X - prev.X, end.Y - prev.Y);
            direction.Normalize();
            Vector perpendicular = new Vector(-direction.Y, direction.X);

            Point arrowPoint1 = end - direction * arrowSize;
            Point arrowPoint2 = arrowPoint1 + perpendicular * (arrowSize / 2);
            Point arrowPoint3 = arrowPoint1 - perpendicular * (arrowSize / 2);

            arrowHead.Points.Clear();
            arrowHead.Points.Add(end);
            arrowHead.Points.Add(arrowPoint2);
            arrowHead.Points.Add(arrowPoint3);
            return arrowHead;
        }

        public void CreateArrowConnect(string name, Point startPoint, Point endPoint)
        {
            ArrowConnector arrowConnect = new ArrowConnector()
            {
                name = name,
                startPoint = startPoint,
                endPoint = endPoint
            };
            double heightArrow = 100d;
            try
            {
                foreach (var arrow in arrowCntLst)
                {
                    if (startPoint.Y > arrow.endPoint.Y && endPoint.Y > arrow.endPoint.Y || startPoint.Y < arrow.startPoint.Y && endPoint.Y < arrow.startPoint.Y)
                    {
                        continue;
                    }
                    double curArr = Math.Max(startPoint.X, endPoint.X);
                    double checkArr = Math.Max(arrow.startPoint.X, arrow.endPoint.X);
                    if (Math.Abs(curArr - checkArr) < 2 && startPoint.Y != arrow.startPoint.Y)
                    {
                        heightArrow += 10d;
                    }
                }
                arrowConnect.arrowLine = this.DrawPolyline(arrowConnect.startPoint, arrowConnect.endPoint, heightArrow);
                arrowConnect.arrowHead = this.DrawArrowHead(arrowConnect.arrowLine.Points[arrowConnect.arrowLine.Points.Count - 2], arrowConnect.endPoint);
                this.Children.Add(arrowConnect.arrowLine);
                this.Children.Add(arrowConnect.arrowHead);
                arrowCntLst.Add(arrowConnect);
            }
            catch (Exception ex)
            {
                logger.Create("Create Arrow Connect Error: " + ex.Message, ex);
            }
            
        }

        private void NewEle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (this.FindResource("cmTool") is ContextMenu cm && !(sender is OutputImageSubTool))
                {
                    cm.PlacementTarget = sender as UIElement; // Đặt đúng kiểu dữ liệu
                    VisionTool toolDesign = sender as VisionTool;
                    toolDesign.ContextMenu = cm;
                    cm.IsOpen = true; // Mở ContextMenu
                }
            }
        }
        private void MnItToolDelete_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = (sender as MenuItem).Parent as ContextMenu;
            if (cm.PlacementTarget is VisionTool tool)
            {
                try
                {
                    List<VisionTool> tools = this.Children.OfType<VisionTool>().ToList();
                    int idxDelete = tools.IndexOf(tool);
                    heightAllTool = heightToolLst[idxDelete];
                    heightToolLst.RemoveRange(idxDelete, heightToolLst.Count - idxDelete);

                    tools.Remove(tool);
                    this.Children.Remove(tool);
                    if (idxDelete < tools.Count)
                    {
                        for (int i = idxDelete; i < tools.Count; i++)
                        {
                            Canvas.SetTop(tools[i], heightAllTool);
                            heightToolLst.Add(heightAllTool);
                            heightAllTool += tools[i].ActualHeight;
                            if (i == tools.Count - 1)
                                heightToolLst.Add(heightAllTool);
                        }
                    }
                    else if (idxDelete == tools.Count)
                    {
                        heightToolLst.Add(heightAllTool);
                    }

                    //Delete Arrow connect to tool
                    List<int> idxDeletes = new List<int>();
                    for (int i = 0; i < arrowCntLst.Count; i++)
                    {
                        if (arrowCntLst[i].name.Contains(tool.Name))
                        {
                            this.Children.Remove(arrowCntLst[i].arrowLine);
                            this.Children.Remove(arrowCntLst[i].arrowHead);
                            idxDeletes.Add(i);
                        }
                    }
                    foreach (int index in idxDeletes.OrderByDescending(i => i))
                    {
                        arrowCntLst.RemoveAt(index);
                    }
                    //Link để Hàm Check OutResult tool để hiển thị ra PgCamera
                    switch (tool.ToolType)
                    {
                        //Phần này kiểm soát việc chặn OutTool
                        case VisionToolType.OUTBLOBRES:
                        case VisionToolType.OUTCHECKPRODUCT:
                        case VisionToolType.OUTSEGNEURORES:
                        case VisionToolType.OUTVIDICOGRES:
                            //Tạm thời bỏ chặn OutTool
                            IsOutTool = false;
                            IsBlockOut = false;
                            //OnToolDeleted?.Invoke(this, e);
                            //OnChildrenChanged?.Invoke(tool, new RoutedEventArgs());
                            break;
                    }

                }
                catch (Exception ex)
                {
                    logger.Create("Delete Tool Error: " + ex.Message, ex);
                }
            }
            else if (cm.PlacementTarget is Polyline arrowLine)
            {
                try
                {
                    int idxDelete = this.Children.IndexOf(arrowLine);
                    arrowCntLst.RemoveAll(arrow => arrow.arrowLine == arrowLine);
                    //Delete Arrow line
                    this.Children.RemoveAt(idxDelete);
                    //Delete Arrow head
                    this.Children.RemoveAt(idxDelete);
                }
                catch (Exception ex)
                {
                    logger.Create("Delete ArrowConnect Error: " + ex.Message, ex);
                }
            }
        }
        private void NewEle_MouseMove(object sender, MouseEventArgs e)
        {
            //if (e.LeftButton == MouseButtonState.Pressed) // Kiểm tra nếu giữ chuột trái
            //{
            //    Border draggedBorder = sender as Border;
            //    if (draggedBorder != null)
            //    {
            //        DragDrop.DoDragDrop(draggedBorder, draggedBorder, DragDropEffects.Move);
            //    }
            //}
        }
    }
}
