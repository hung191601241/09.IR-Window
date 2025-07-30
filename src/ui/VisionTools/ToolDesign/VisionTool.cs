using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VisionTools.ToolEdit;

namespace VisionTools.ToolDesign
{
    public enum VisionToolType
    {
        NONE = -1,
        //Dafault Tool
        ACQUISITION = 0,
        TEMPLATEMATCH = 1,
        TEMPMATCHZERO = 2,
        FIXTURE = 3,
        EDITREGION = 4,
        IMAGEPROCESS = 5,
        CONTRASTnBRIGHTNESS = 6,
        BLOB = 7,
        SAVEIMAGE = 8,
        IMAGEBUFF = 9,

        //Key Tool
        SEGMENTNEURO = 50,
        VIDICOGNEX = 60,
        VISIONPRO = 61,

        //Output Tool
        OUTIMAGESUB = 100,
        OUTBLOBRES = 101,
        OUTACQUISRES = 102,
        OUTSEGNEURORES = 103,
        OUTVIDICOGRES = 104,
        OUTCHECKPRODUCT = 105,
    }
    public class VisionTool : Grid
    {
        public new virtual string Name { get; set; }
        public virtual VisionToolType ToolType { get; set; }
        public VisionTool(string name = "", VisionToolType toolType = VisionToolType.NONE)
        {
            this.Name = name;
            this.ToolType = toolType;
        }
        protected virtual void RegisEvent() { }
        public virtual bool RunToolInOut(List<ArrowConnector> arrowConnectLst, Dictionary<int, string[]> connectTags) { return true; }
        protected virtual void GetInput(List<ArrowConnector> arrowConnectLst, Dictionary<int, string[]> connectTags) { }
        protected virtual void SetOutput(List<ArrowConnector> arrowConnectLst, Dictionary<int, string[]> connectTags) { }
        protected void StatusChange(Ellipse elipSttRun, GridBase toolEdit)
        {
            elipSttRun.Fill = toolEdit.toolBase.BitStatus ? (Brush)new BrushConverter().ConvertFromString("#FF00F838") : (Brush)new BrushConverter().ConvertFromString("#FFE90E0E");
        }
    }

    public class ArrowConnector
    {
        public string name = string.Empty;
        public object data = new object();
        public Polyline arrowLine = new Polyline();
        public Polygon arrowHead = new Polygon();
        public Point startPoint = new Point();
        public Point endPoint = new Point();


        public ArrowConnector()
        {
            this.name = "";
            this.data = new object();
            this.arrowLine = new Polyline();
            this.arrowHead = new Polygon();
            this.startPoint = new Point();
            this.endPoint = new Point();
        }
        public ArrowConnector(string _name, Point _startPoint, Point _endPoint, Polyline _arrowLine, Polygon _arrowHead)
        {
            this.name = _name;
            this.data = new object();
            this.arrowLine = _arrowLine;
            this.arrowHead = _arrowHead;
            this.startPoint = _startPoint;
            this.endPoint = _endPoint;
        }
    }
}
