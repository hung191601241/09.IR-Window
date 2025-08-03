using nrt;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VisionInspection;

namespace VisionTools.ToolDesign
{
    /// <summary>
    /// Interaction logic for ToolList.xaml
    /// </summary>
    /// 

    public partial class ToolList : Grid
    {
        public ToolList()
        {
            InitializeComponent();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Grid parent = this.Parent as Grid;
            parent.Children.Remove(this);
            GC.Collect();
        }

        private void Lb_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is Label label)
            {
                DataObject data = new DataObject();
                VisionTool tool = new VisionTool();
                switch (label.Name)
                {
                    case "lbAcquisitionTool":
                        tool.ToolType = VisionToolType.ACQUISITION;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbImageBuffTool":
                        tool.ToolType = VisionToolType.IMAGEBUFF;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbSaveImageTool":
                        tool.ToolType = VisionToolType.SAVEIMAGE;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbEditRegionTool":
                        tool.ToolType = VisionToolType.EDITREGION;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbTemplateMatchTool":
                        tool.ToolType = VisionToolType.TEMPLATEMATCH;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbFixtureTool":
                        tool.ToolType = VisionToolType.FIXTURE;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbContrastNBrightnessTool":
                        tool.ToolType = VisionToolType.CONTRASTnBRIGHTNESS;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbImageProcessTool":
                        tool.ToolType = VisionToolType.IMAGEPROCESS;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbBlobTool":
                        tool.ToolType = VisionToolType.BLOB;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbTempMatchZeroTool":
                        tool.ToolType = VisionToolType.TEMPMATCHZERO;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbSegmentNeuroTool":
                        tool.ToolType = VisionToolType.SEGMENTNEURO;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbVisionProTool":
                        tool.ToolType = VisionToolType.VISIONPRO;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbVidiCognexTool":
                        tool.ToolType = VisionToolType.VIDICOGNEX;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbOutBlobResTool":
                        tool.ToolType = VisionToolType.OUTBLOBRES;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbOutCheckProductTool":
                        tool.ToolType = VisionToolType.OUTCHECKPRODUCT;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbOutSegNeuroResTool":
                        tool.ToolType = VisionToolType.OUTSEGNEURORES;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                    case "lbOutVidiCogResTool":
                        tool.ToolType = VisionToolType.OUTVIDICOGRES;
                        data.SetData(typeof(VisionTool), tool); // đăng theo kiểu cha
                        break;
                }
                DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Copy);
            }
        }

        private void Lb_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var scVw = this.Children.OfType<ScrollViewer>().FirstOrDefault();
            var stPn = scVw.Content as StackPanel;
            List<Label> lbs = stPn.Children.OfType<Label>().ToList();
            foreach (var lb in lbs)
            {
                lb.Background = Brushes.White;
                switch (lb.Name)
                {
                    //Tool Key
                    case "lbSegmentNeuroTool":
                    case "lbVidiCognexTool":
                    case "lbVisionProTool":
                        lb.Foreground = (Brush)new BrushConverter().ConvertFromString("#FFDD798B");
                        break;
                    //Tool Out
                    case "lbOutBlobResTool":
                    case "lbOutAcquisResTool":
                    case "lbOutCheckProductTool":
                    case "lbOutSegNeuroResTool":
                    case "lbOutVidiCogResTool":
                        lb.Foreground = (Brush)new BrushConverter().ConvertFromString("#FF0C88DA");
                        break;
                    //Tool Default
                    default:
                        lb.Foreground = Brushes.Black;
                        break;
                }    
                
            }
            // Đổi màu label được nhấn
            if (sender is Label clickedLabel)
            {
                clickedLabel.Background = Brushes.Blue;
                clickedLabel.Foreground = Brushes.White;
            }
        }
    }
}
