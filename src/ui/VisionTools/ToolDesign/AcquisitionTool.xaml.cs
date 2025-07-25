using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using OpenCvSharp;
using VisionInspection;
using VisionTools.ToolEdit;
using Window = System.Windows.Window;

namespace VisionTools.ToolDesign
{
    /// <summary>
    /// Interaction logic for AcquisitionDesign.xaml
    /// </summary>
    public partial class AcquisitionTool : VisionTool
    {
        public AcquisitionEdit toolEdit = new AcquisitionEdit();
        public VisionToolType VsToolType { get => base.ToolType; }
        public AcquisitionTool()
        {
            base.Name = "AcquisitionTool";
            base.ToolType = VisionToolType.ACQUISITION;
            InitializeComponent(); 
            RegisEvent();
        }
        protected override void RegisEvent()
        {
            this.MouseMove += AcquisitionDesign_MouseMove;
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
            //Get Input Data
            toolEdit.Run();
            //Set Output Data
            SetOutput(arrowConnectLst, connectTags);
            if (!toolEdit.meaRunTime.IsRunning) return false;
            toolEdit.meaRunTime.Stop();
            toolEdit.toolBase.SetLbTime(true, toolEdit.meaRunTime.ElapsedMilliseconds);
            return true;
        }

        protected override void SetOutput(List<ArrowConnector> arrowConnectLst, Dictionary<int, string[]> connectTags)
        {
            if (connectTags == null) return;
            foreach (var cntTag in connectTags)
            {
                if (cntTag.Value[0] == this.Name)
                {
                    switch (cntTag.Value[1])
                    {
                        case "lbOutputImage":
                            arrowConnectLst[cntTag.Key].data = toolEdit.OutputImage;
                            break;
                    }
                }
            }
        }

        private void AcquisitionDesign_MouseMove(object sender, MouseEventArgs e)
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
                    Title = "Acquisition Window",
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
        private void WndBase_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ToolArea toolArea = this.Parent as ToolArea;
            RunToolInOut(toolArea.arrowCntLst, toolArea.CreateConnectTags(toolArea.arrowCntLst));
        }
    }
}
