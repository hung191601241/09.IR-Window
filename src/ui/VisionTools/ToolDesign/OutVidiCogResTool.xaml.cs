using Development;
using OpenCvSharp;
using OpenCvSharp.Extensions;
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

    public partial class OutVidiCogResTool : VisionTool
    {
        private MyLogger logger = new MyLogger("OutVidiCogRes Tool");
        public OutVidiCogResEdit toolEdit = new OutVidiCogResEdit();
        public VisionToolType VsToolType { get => base.ToolType; }
        public OutVidiCogResTool()
        {
            base.Name = "OutVidiCogResTool";
            base.ToolType = VisionToolType.OUTVIDICOGRES;
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
            toolEdit.UpdateAddrOut();
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
            StatusChange(elipSttRun, toolEdit);
        }
        public override bool RunToolInOut(List<ArrowConnector> arrowConnectLst, Dictionary<int, string[]> connectTags)
        {
            toolEdit.meaRunTime.Restart();
            //Get Input Data
            GetInput(arrowConnectLst, connectTags);
            ToolArea toolArea = (ToolArea)this.Parent;

            try
            {
                //Trường hợp có 1 Tool báo lỗi
                if (toolArea.resRunTools > 0)
                {
                    toolEdit.JudgeVal = 3;
                    toolEdit.StrResult = "Vision Tool Error";
                    //Gửi NG đến PLC
                    toolEdit.SendVisionResult(2);

                    if (toolEdit.InputImage == null || toolEdit.InputImage.Mat == null || toolEdit.InputImage.Mat.Height <= 0 || toolEdit.InputImage.Mat.Width <= 0)
                    {
                        toolEdit.meaRunTime.Stop();
                        toolEdit.toolBase.SetLbTime(false, toolEdit.meaRunTime.ElapsedMilliseconds, "InputImage is null or error!");
                        logger.Create("InputImage is null or error!");
                        return false;
                    }

                    toolEdit.OutputImage = toolEdit.InputImage.Clone(true);
                    if (toolEdit.OutputImage.Mat.Channels() == 1)
                    {
                        toolEdit.OutputImage.Mat = toolEdit.OutputImage.Mat.CvtColor(ColorConversionCodes.GRAY2RGB);
                    }
                    Cv2.PutText(toolEdit.OutputImage.Mat, toolArea.sttRunTools, new OpenCvSharp.Point(10, 20), HersheyFonts.Italic, 0.5d, Scalar.Red, thickness: 1);
                    Cv2.PutText(toolEdit.OutputImage.Mat, "NG", new OpenCvSharp.Point(10, 60), HersheyFonts.Italic, 1d, Scalar.Red, thickness: 5);
                    toolEdit.toolBase.imgView.Source = toolEdit.OutputImage.Mat.ToBitmapSource();

                    toolEdit.meaRunTime.Stop();
                    toolEdit.toolBase.SetLbTime(false, toolEdit.meaRunTime.ElapsedMilliseconds, "Have any Tool Error!"); 
                    logger.Create("Have any Tool Error!");
                }
                else
                {
                    toolEdit.Run();
                    toolEdit.SendVisionResult(toolEdit.JudgeVal); 
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
                            case "lbInputImage":
                                if (!(arrowConnectLst[cntTag.Key].data is SvImage)) continue;
                                toolEdit.InputImage = arrowConnectLst[cntTag.Key].data as SvImage;
                                break;
                            case "lbScore":
                                if (!(arrowConnectLst[cntTag.Key].data is double)) continue;
                                toolEdit.Score = (double)arrowConnectLst[cntTag.Key].data;
                                break;
                            case "lbStrResult":
                                if (!(arrowConnectLst[cntTag.Key].data is string)) continue;
                                toolEdit.StrResult = arrowConnectLst[cntTag.Key].data.ToString();
                                break;
                            case "lbJudge":
                                if (!(arrowConnectLst[cntTag.Key].data is int)) continue;
                                toolEdit.JudgeVal = (int)arrowConnectLst[cntTag.Key].data;
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
                        Title = "Out Vidi Cognex Result Window",
                        Width = 1100,
                        Height = 600,
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
            //SvImage inputImage = toolEdit.InputImage;
            //if (inputImage != null && inputImage.Mat != null && inputImage.Mat.Width != 0 && inputImage.Mat.Height != 0)
            //    return;
            //ToolArea toolArea = this.Parent as ToolArea;
            //RunToolInOut(toolArea.arrowCntLst, toolArea.CreateConnectTags(toolArea.arrowCntLst));
        }
    }
}
