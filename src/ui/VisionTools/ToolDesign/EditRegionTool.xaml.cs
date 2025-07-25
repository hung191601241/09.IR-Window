using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VisionInspection;
using VisionTools.ToolEdit;

namespace VisionTools.ToolDesign
{
    /// <summary>
    /// Interaction logic for EditRegion.xaml
    /// </summary>
    public partial class EditRegionTool : VisionTool
    {
        private MyLogger logger = new MyLogger("EditRegion Tool");
        public EditRegionEdit toolEdit = new EditRegionEdit();
        public VisionToolType VsToolType { get => base.ToolType; }
        List<ToolArea> toolAreaSubs = new List<ToolArea>();
        public EditRegionTool()
        {
            base.Name = "EditRegionTool";
            base.ToolType = VisionToolType.EDITREGION;
            InitializeComponent();
            RegisEvent();
        }
        protected override void RegisEvent()
        {
            this.MouseMove += EditRegion_MouseMove;
            this.toolEdit.OnBtnRunClicked += ToolEdit_OnBtnRunClicked;
            this.toolEdit.OnBtnRefreshClicked += ToolEdit_OnBtnRefreshClicked;
            this.toolEdit.toolBase.BitStatusChanged += ToolBase_BitStatusChanged;
            this.toolEdit.toolBase.OnSaveTool += ToolBase_OnSaveTool;
        }
        private void ToolBase_OnSaveTool(object sender, RoutedEventArgs e)
        {
            PgCamera pgCamera = UiManager.FindParentOfType<PgCamera>(this);
            pgCamera?.SaveData();

        }
        private void ToolBase_BitStatusChanged(object sender, EventArgs e)
        {
            elipSttRun.Fill = toolEdit.toolBase.BitStatus ? (Brush)new BrushConverter().ConvertFromString("#FF00F838") : (Brush)new BrushConverter().ConvertFromString("#FFE90E0E");
        }

        public override bool RunToolInOut(List<ArrowConnector> arrowConnectLst, Dictionary<int, string[]> connectTags)
        {
            toolEdit.meaRunTime.Restart();
            //Get Input Data
            GetInput(arrowConnectLst, connectTags);
            toolEdit.Run();
            toolEdit.DisplayResult();
            //Set Output Data
            SetOutput(arrowConnectLst, connectTags);

            if (!toolEdit.meaRunTime.IsRunning) return false;
            toolEdit.meaRunTime.Stop();
            toolEdit.toolBase.SetLbTime(true, toolEdit.meaRunTime.ElapsedMilliseconds);

            //Đẩy ảnh xuống các VisionProgram Sub
            QueryNumberSubProgram(out toolAreaSubs);
            if (toolAreaSubs.Count == 0)
            {
                toolEdit.meaRunTime.Stop();
                toolEdit.toolBase.SetLbTime(false, toolEdit.meaRunTime.ElapsedMilliseconds, "Have no Sub VP!");
                return false;
            }
            if (toolAreaSubs.Count != toolEdit.imageSubList.Count)
            {
                toolEdit.meaRunTime.Stop();
                toolEdit.toolBase.SetLbTime(false, toolEdit.meaRunTime.ElapsedMilliseconds, "Number of Sub VP and Number of list Image Sub is not equal!");
                return false;
            }
            try
            {
                for (int i = 0; i < toolEdit.imageSubList.Count; i++)
                {
                    toolAreaSubs[i].ImgLstSub = toolEdit.imageSubList[i];
                    //Gán hình ảnh gốc khi chưa cắt ảnh
                    toolAreaSubs[i].OriginImage = toolEdit.InputImage.Clone(true);
                    //Gán kết quả bộ đếm NG và sttRun của các tool VS Main cho Sub
                    ToolArea toolAreaMain = this.Parent as ToolArea;
                    toolAreaSubs[i].resRunTools = toolAreaMain.resRunTools;
                    toolAreaSubs[i].sttRunTools = toolAreaMain.sttRunTools;
                }
            }
            catch (Exception ex)
            {
                logger.Create("Move Image to Sub Error: " + ex.Message, ex);
            }

            return true;
        }
        protected override void GetInput(List<ArrowConnector> arrowConnectLst, Dictionary<int, string[]> connectTags)
        {
            try
            {
                if (connectTags == null) return;
                foreach (var cntTag in connectTags)
                {
                    if (cntTag.Value[2] == this.Name)
                    {
                        switch (cntTag.Value[3])
                        {
                            case "lbInputImage":
                                if (!(arrowConnectLst[cntTag.Key].data is SvImage)) continue;
                                toolEdit.InputImage = arrowConnectLst[cntTag.Key].data as SvImage;
                                if (!toolEdit.InputImage.IsNull && toolEdit.InputImage.Mat != null)
                                    toolEdit.toolBase.isImgPath = false;
                                break;
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Get Input Error: " + ex.Message, ex);
            }
        }
        protected override void SetOutput(List<ArrowConnector> arrowConnectLst, Dictionary<int, string[]> connectTags)
        {
            try
            {
                if (connectTags == null) return;
                foreach (var cntTag in connectTags)
                {
                    if (cntTag.Value[0] == this.Name)
                    {
                        switch (cntTag.Value[1])
                        {
                            case "lbOutputImage":
                                arrowConnectLst[cntTag.Key].data = toolEdit.OutImageList;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Set Output Error: " + ex.Message, ex);
            }
        }
        public int QueryNumberSubProgram(out List<ToolArea> toolAreaSubs)
        {
            toolAreaSubs = new List<ToolArea>();
            try
            {
                ToolArea toolAreaMain = this.Parent as ToolArea;
                if (!toolAreaMain.Name.Contains("ToolAreaMain"))
                    return -1;
                Expander expdMain = (toolAreaMain.Parent as ScrollViewer).Parent as Expander;
                StackPanel stVP = expdMain.Parent as StackPanel;

                //Skip ToolArea Main & InputTrigger
                List<Expander> expdSubLst = stVP.Children.OfType<Expander>().Skip(2).ToList();
                foreach (Expander expdSub in expdSubLst)
                {
                    if ((expdSub.Content as ScrollViewer).Content is ToolArea toolAreaSub)
                        toolAreaSubs.Add(toolAreaSub);
                }
                return expdSubLst.Count;
            }
            catch (Exception ex)
            {
                logger.Create("Query Number Sub Program Error: " + ex.Message, ex);
                return 0;
            }
        }
        private void ToolEdit_OnBtnRefreshClicked(object sender, RoutedEventArgs e)
        {
            toolEdit.GenerateTable(QueryNumberSubProgram(out toolAreaSubs), toolEdit.imgLst);
        }
        private void ToolEdit_OnBtnRunClicked(object sender, RoutedEventArgs e)
        {
            ToolArea toolArea = this.Parent as ToolArea;
            RunToolInOut(toolArea.arrowCntLst, toolArea.CreateConnectTags(toolArea.arrowCntLst));
        }
        private void EditRegion_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) // Kiểm tra nếu giữ chuột trái
            {
                if (this != null)
                {
                    DragDrop.DoDragDrop(this, this, DragDropEffects.Move);
                }
            }
        }
        //private void Lb_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    //Canvas stTools = (Canvas)this.Parent;
        //    //List<Grid> mainGrids = stTools.Children.OfType<Grid>().ToList();
        //    //foreach (Grid grd in mainGrids)
        //    {
        //        List<StackPanel> stackPns = this.Children.OfType<StackPanel>().ToList();
        //        for (int i = 0; i < stackPns.Count; i++)
        //        {
        //            Label lb = stackPns[i].Children.OfType<Label>().FirstOrDefault();
        //            lb.Background = Brushes.Transparent;
        //            switch (i)
        //            {
        //                case 0:
        //                    lb.Foreground = Brushes.Black;
        //                    break;
        //                case 1:
        //                    lb.Foreground = (Brush)new BrushConverter().ConvertFromString("#FF0B5FEC");
        //                    break;
        //                case 2:
        //                    lb.Foreground = (Brush)new BrushConverter().ConvertFromString("#FFD751EA");
        //                    break;
        //            }
        //        }

        //        // Đổi màu label được nhấn
        //        Label clickedLabel = sender as Label;
        //        if (clickedLabel != null)
        //        {
        //            clickedLabel.Background = Brushes.Blue;
        //            clickedLabel.Foreground = Brushes.White;
        //        }
        //    }
        //    if (e.ClickCount >= 2 && e.LeftButton == MouseButtonState.Pressed)
        //    {
        //        //var element = sender as UIElement;
        //        WndToolEditBase wndToolEditBase = new WndToolEditBase
        //        {
        //            Title = "Edit Region Window",
        //            Width = 1100,
        //            Height = 600,
        //            Content = toolEdit,
        //        };
        //        wndToolEditBase.Show();
        //    }
        //}
        private void Lb_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ToolArea toolArea = (ToolArea)this.Parent;
                //Tools
                List<VisionTool> tools = toolArea.Children.OfType<VisionTool>().ToList();
                foreach (var tool in tools)
                {
                    bool breakFor = false;
                    List<StackPanel> stackPnTools = tool.Children.OfType<StackPanel>().ToList();
                    for (int i = 0; i < stackPnTools.Count; i++)
                    {
                        if (stackPnTools[i].Children.Count == 0) continue;
                        Label lb = stackPnTools[i].Children.OfType<Label>().FirstOrDefault();
                        if (lb == null) continue;
                        if (lb.Name == toolArea.lbClicked.Name && tool.Name == toolArea.toolSelected.Name)
                        {
                            lb.Background = Brushes.Transparent;
                            lb.Foreground = toolArea.lbClicked.Foreground;
                            breakFor = true;
                            break;
                        }
                    }
                    if (breakFor) { break; }
                }
                if (sender is Label clickedLabel)
                {
                    VisionTool toolClicked = ((StackPanel)clickedLabel.Parent).Parent as VisionTool;
                    toolArea.toolSelected.Name = toolClicked.Name;
                    toolArea.lbClicked.Name = clickedLabel.Name;
                    toolArea.lbClicked.Foreground = clickedLabel.Foreground;
                    clickedLabel.Background = Brushes.Blue;
                    clickedLabel.Foreground = Brushes.White;
                }

                if (e.ClickCount >= 2 && e.LeftButton == MouseButtonState.Pressed)
                {
                    Window wndBase = new Window
                    {
                        Title = "Edit Region Window",
                        Width = 1100,
                        Height = 600,
                        Content = toolEdit.toolBase,
                    };
                    //Lấy data dầu vào
                    GetInput(toolArea.arrowCntLst, toolArea.CreateConnectTags(toolArea.arrowCntLst));
                    wndBase.Closing += WndBase_Closing;
                    toolEdit.isEditMode = true;
                    wndBase.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                logger.Create("Label Down Error: " + ex.Message, ex);
            }
        }
        private void WndBase_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            toolEdit.isEditMode = false; 
            SvImage inputImage = toolEdit.InputImage;
            if (inputImage != null && inputImage.Mat != null && inputImage.Mat.Width != 0 && inputImage.Mat.Height != 0)
                return;
            ToolArea toolArea = this.Parent as ToolArea;
            RunToolInOut(toolArea.arrowCntLst, toolArea.CreateConnectTags(toolArea.arrowCntLst));
        }
    }
}
