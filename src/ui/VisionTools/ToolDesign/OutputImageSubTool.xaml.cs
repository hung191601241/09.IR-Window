using System;
using System.Collections.Generic;
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
using VisionTools.ToolEdit;

namespace VisionTools.ToolDesign
{
    /// <summary>
    /// Interaction logic for OutputImageSub.xaml
    /// </summary>
    public partial class OutputImageSubTool : VisionTool
    {
        public VisionToolType VsToolType { get => base.ToolType; }
        public SvImage InputImage { get; set; } = new SvImage();
        public SvImage OutputImage { get; set; } = new SvImage();
        public SvImage OriginImage { get; set; } = new SvImage();
        public OutputImageSubTool()
        {
            base.Name = "OutputImageSubTool";
            base.ToolType = VisionToolType.OUTIMAGESUB;
            InitializeComponent();
            RegisEvent();
        }
        protected override void RegisEvent()
        {
            this.MouseMove += OutputImageSub_MouseMove;
        }
        public override bool RunToolInOut(List<ArrowConnector> arrowConnectLst, Dictionary<int, string[]> connectTags)
        {
            //Get Input Data

            if(InputImage == null || InputImage.Mat == null || InputImage.Mat.Height <= 0 || InputImage.Mat.Width <= 0)
            {
                elipSttRun.Fill = (Brush)new BrushConverter().ConvertFromString("#FFE90E0E");
                return false;
            }

            OutputImage = InputImage.Clone(true);
            //Set Output Data
            SetOutput(arrowConnectLst, connectTags);
            elipSttRun.Fill = (Brush)new BrushConverter().ConvertFromString("#FF00F838");
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
                        case "lbOriginImage":
                            arrowConnectLst[cntTag.Key].data = OriginImage;
                            break;
                        case "lbOutputImage":
                            arrowConnectLst[cntTag.Key].data = OutputImage;
                            break;
                    }
                }
            }
        }

        private void OutputImageSub_MouseMove(object sender, MouseEventArgs e)
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
        }
    }
}
