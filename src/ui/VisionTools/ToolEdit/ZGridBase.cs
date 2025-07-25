using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace VisionTools.ToolEdit
{
    public class GridBase : System.Windows.Controls.Grid
    {
        public ToolEditBase toolBase = new ToolEditBase();
        public bool isEditMode = false;
        public int oldSelect = 0;
        public Stopwatch meaRunTime = new Stopwatch();

        #region Properties
        public Canvas CanvasImg { get => toolBase.canvasImg; set => toolBase.canvasImg = value; }
        public Image ImgView { get => toolBase.imgView; set => toolBase.imgView = value; }
        #endregion
        public GridBase()
        {

        }
        protected virtual void DisplayInit() { }
        protected virtual void RegisterEvent() { }
        public virtual void Run() { }
    }
    public class DeepResultObject
    {
        public int ID { get; set; }
        public int Judge { get; set; }
        public string Result { get; set; }
        public double Score { get; set; }

        public DeepResultObject()
        {
            this.ID = 0;
            this.Judge = 1;
            this.Result = "";
            this.Score = 0.0;
        }

        public DeepResultObject(int id, int judgeVal, string strResult, double score)
        {
            this.ID = id;
            this.Judge = judgeVal;
            this.Result = strResult;
            this.Score = score;
        }
        public DeepResultObject Clone()
        {
            return new DeepResultObject(this.ID, this.Judge, this.Result, this.Score);
        }
    }
}
