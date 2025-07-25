using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VisionInspection;
using VisionTools.ToolEdit;
using Window = System.Windows.Window;

namespace VisionTools.ToolDesign
{
    /// <summary>
    /// Interaction logic for OutResultDesign.xaml
    /// </summary>
    /// 

    public partial class OutBlobResTool : VisionTool
    {
        private MyLogger logger = new MyLogger("OutBlobRes Tool");
        public OutBlobResEdit toolEdit = new OutBlobResEdit();
        public VisionToolType VsToolType { get => base.ToolType; }
        public OutBlobResTool()
        {
            base.Name = "OutBlobResTool";
            base.ToolType = VisionToolType.OUTBLOBRES;
            InitializeComponent();
            RegisEvent();
        }
        protected override void RegisEvent()
        {
            this.MouseMove += OutResultDesign_MouseMove;
            this.toolEdit.OnBtnRunClicked += ToolEdit_OnBtnRunClicked;
            this.toolEdit.toolBase.BitStatusChanged += ToolBase_BitStatusChanged;
            this.toolEdit.toolBase.OnSaveTool += ToolBase_OnSaveTool;
        }
        private void ToolBase_OnSaveTool(object sender, RoutedEventArgs e)
        {
            PgCamera pgCamera = UiManager.FindParentOfType<PgCamera>(this);
            pgCamera?.SaveData();
        }
        private void ToolEdit_OnBtnRunClicked(object sender, RoutedEventArgs e)
        {
            ToolArea toolArea = this.Parent as ToolArea;
            RunToolInOut(toolArea.arrowCntLst, toolArea.CreateConnectTags(toolArea.arrowCntLst));
        }
        private void ToolBase_BitStatusChanged(object sender, EventArgs e)
        {
            elipSttRun.Fill = toolEdit.toolBase.BitStatus ? (Brush)new BrushConverter().ConvertFromString("#FF00F838") : (Brush)new BrushConverter().ConvertFromString("#FFE90E0E");
        }
        public override bool RunToolInOut(List<ArrowConnector> arrowConnectLst, Dictionary<int, string[]> connectTags)
        {
            toolEdit.meaRunTime.Restart();
            toolEdit.resultOut = true;
            //Get Input Data
            GetInput(arrowConnectLst, connectTags);
            ToolArea toolAreaSub = (ToolArea)this.Parent;

            try
            {
                //Trường hợp có 1 Tool báo lỗi
                if (toolAreaSub.resRunTools > 0)
                {
                    toolEdit.resultOut = false;
                    //toolEdit.SendResultToPLC(toolEdit.addrOKLst[0], toolEdit.addrNGLst[0], toolEdit.resultOut);
                    if (toolEdit.InputImage == null || toolEdit.InputImage.Mat == null || toolEdit.InputImage.Mat.Height <= 0 || toolEdit.InputImage.Mat.Width <= 0)
                    {
                        toolEdit.meaRunTime.Stop();
                        toolEdit.toolBase.SetLbTime(false, toolEdit.meaRunTime.ElapsedMilliseconds, "InputImage is null or error!");
                        return false;
                    }
                    toolEdit.OutputImage = toolEdit.InputImage.Clone(true);
                    if (toolEdit.OutputImage.Mat.Channels() == 1)
                    {
                        toolEdit.OutputImage.Mat = toolEdit.OutputImage.Mat.CvtColor(ColorConversionCodes.GRAY2RGB);
                    }
                    if (toolEdit.OriginImage.Mat != null && toolEdit.OriginImage.Height != 0 && toolEdit.OriginImage.Width != 0)
                    {
                        if (toolEdit.OriginImage.Mat.Channels() == 1)
                        {
                            toolEdit.OriginImage.Mat = toolEdit.OriginImage.Mat.CvtColor(ColorConversionCodes.GRAY2RGB);
                        }
                    }
                    Cv2.PutText(toolEdit.OutputImage.Mat, toolAreaSub.sttRunTools, new OpenCvSharp.Point(10, 20), HersheyFonts.Italic, 0.5d, Scalar.Red, thickness: 1);
                    Cv2.PutText(toolEdit.OutputImage.Mat, toolEdit.resultOut ? "OK" : "NG", new OpenCvSharp.Point(10, 60), HersheyFonts.Italic, 1d, toolEdit.resultOut ? Scalar.Green : Scalar.Red, thickness: 5);
                    Cv2.PutText(toolEdit.OriginImage.Mat, toolAreaSub.sttRunTools, new OpenCvSharp.Point(10, 40), HersheyFonts.Italic, 1d, Scalar.Red, thickness: 2);
                    //Draw in OriginImage
                    Cv2.PutText(toolEdit.OriginImage.Mat, toolEdit.resultOut ? "OK" : "NG", new OpenCvSharp.Point(10, 120), HersheyFonts.Italic, 3d, toolEdit.resultOut ? Scalar.Green : Scalar.Red, thickness: 15);
                    toolEdit.meaRunTime.Stop();
                    toolEdit.toolBase.SetLbTime(false, toolEdit.meaRunTime.ElapsedMilliseconds, "Have any Tool Error!");
                }
                else
                {
                    toolEdit.Run();
                }
            }
            catch (Exception ex)
            {
                logger.Create("Run Tool Error: " + ex.Message, ex);
            }

            //Set Output Data
            if (!toolEdit.meaRunTime.IsRunning) return false;
            toolEdit.meaRunTime.Stop();
            toolEdit.toolBase.SetLbTime(true, toolEdit.meaRunTime.ElapsedMilliseconds);
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
                            case "lbOriginImage":
                                if (!(arrowConnectLst[cntTag.Key].data is SvImage)) continue;
                                toolEdit.OriginImage = arrowConnectLst[cntTag.Key].data as SvImage;
                                break;
                            case "lbInputImage":
                                if (!(arrowConnectLst[cntTag.Key].data is SvImage)) continue;
                                toolEdit.InputImage = arrowConnectLst[cntTag.Key].data as SvImage;
                                break;
                            case "lbBlobs1":
                                if (!(arrowConnectLst[cntTag.Key].data is List<BlobObject>)) continue;
                                toolEdit.Blobs1 = arrowConnectLst[cntTag.Key].data as List<BlobObject>;
                                break;
                            case "lbBlobs2":
                                if (!(arrowConnectLst[cntTag.Key].data is List<BlobObject>)) continue;
                                toolEdit.Blobs2 = arrowConnectLst[cntTag.Key].data as List<BlobObject>;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("Get Input Error: " + ex.Message, ex);
            }
        }

        private void OutResultDesign_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) // Kiểm tra nếu giữ chuột trái
            {
                if (this != null)
                {
                    DragDrop.DoDragDrop(this, this, DragDropEffects.Move);
                }
            }
        }
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
                        Title = "Out Blob Result Window",
                        Width = 1100,
                        Height = 600,
                        //Owner = Window.GetWindow(this),
                        //Topmost = true,
                        Content = toolEdit.toolBase,
                    };
                    //Lấy data dầu vào
                    GetInput(toolArea.arrowCntLst, toolArea.CreateConnectTags(toolArea.arrowCntLst));
                    wndBase.Closing += WndBase_Closing;
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
            SvImage inputImage = toolEdit.InputImage;
            if (inputImage != null && inputImage.Mat != null && inputImage.Mat.Width != 0 && inputImage.Mat.Height != 0)
                return;
            ToolArea toolArea = this.Parent as ToolArea;
            RunToolInOut(toolArea.arrowCntLst, toolArea.CreateConnectTags(toolArea.arrowCntLst));
        }
    }
}
