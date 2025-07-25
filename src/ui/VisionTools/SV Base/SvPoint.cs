using System;
using System.Drawing;
using OpenCvSharp;
using Newtonsoft.Json;

namespace VisionInspection
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SvPoint : InteractDrawObject
    {
        public SvPoint()
        {
            this.point = new Point2f();
        }

        public SvPoint(float x, float y)
        {
            point.X = x;
            point.Y = y;
        }

        public SvPoint(double x, double y)
        {
            point.X = (float)x;
            point.Y = (float)y;
        }

        public SvPoint(double x, double y, double z)
        {
            point.X = (float)x;
            point.Y = (float)y;
            ThetaRad = z;
        }

        public SvPoint(Point3d p3d)
        {
            point.X = (float)p3d.X;
            point.Y = (float)p3d.Y;
            ThetaRad = p3d.Z;
        }

        private Point2f point = new Point2f();

        [JsonIgnore]
        public double ThetaRad { get; set; }

        [JsonIgnore]
        public Point2f Point
        {
            get { return point; }
            set { point = value; OnRefresh(this, null); }
        }

        [JsonIgnore]
        public PointF PointF
        {
            get { return new PointF(point.X, point.Y); }
            set { point.X = value.X; point.Y = value.Y; OnRefresh(this, null); }
        }

        [JsonIgnore]
        public Point2d Point2d
        {
            get { return new Point2d(point.X, point.Y); }
            set { point.X = (float)value.X; point.Y = (float)value.Y; OnRefresh(this, null); }
        }
        [JsonProperty]
        public Point3d Point3d
        {
            get { return new Point3d(point.X, point.Y, ThetaRad); }
            set { point.X = (float)value.X; point.Y = (float)value.Y; ThetaRad = value.Z; OnRefresh(this, null); }
        }

        [JsonIgnore]
        public float X
        {
            get { return point.X; }
            set { point.X = value; OnRefresh(this, null); }
        }

        [JsonIgnore]
        public float Y
        {
            get { return point.Y; }
            set { point.Y = value; OnRefresh(this, null); }
        }

        Color color = Color.Red;

        [JsonIgnore]
        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        //[NonSerialized]
        //protected SvMip.Display.SvDisplayViewInteract display;

        //public override SvMip.Display.SvDisplayViewInteract Display
        //{
        //    get
        //    {
        //        return display;
        //    }
        //    set
        //    {
        //        display = value;
        //        if (point.X == 0 && point.Y == 0)
        //            point = new Point2f(display.Size.Width / 2, display.Size.Height / 2);
        //    }
        //}

        enum SelectionPoint { None, Ref };
        SelectionPoint selectionPoint = SelectionPoint.None;

        /// <summary>
        /// 마우스 커서가 선택한 포인트로 변경한다(MouseMove이벤트에서 사용)
        /// </summary>
        /// <param name="mouseLocation">마우스 위치</param>
        public override bool FindPoint(PointF mouseLocation)
        {
            if (IsPositionChange)
            {
                switch (selectionPoint)
                {
                    case SelectionPoint.None:
                        ResetSelectedPoint();
                        return false;
                    case SelectionPoint.Ref:
                        PointF = mouseLocation;
                        break;
                }
            }

            double distance = Math.Sqrt(Math.Pow((point.X - mouseLocation.X), 2) + Math.Pow((point.Y - mouseLocation.Y), 2));

            int index = 0;

            if (distance > SelectionSize)
                index = -1;

            switch (index)
            {
                case -1:
                    //display.Cursor = System.Windows.Forms.Cursors.Arrow;
                    selectionPoint = SelectionPoint.None;
                    return false;
                case 0:
                    //display.Cursor = System.Windows.Forms.Cursors.SizeAll;
                    selectionPoint = SelectionPoint.Ref;
                    break;
            }
            return true;
        }

        /// <summary>
        /// 포인트 모드를 선택한다(MouseDown이벤트에서 사용)
        /// </summary>
        public override void SelectPoint()
        {
            if (selectionPoint != SelectionPoint.None)
                IsPositionChange = true;
        }

        /// <summary>
        /// 포인트 선택을 취소한다(MouseUp이벤트에서 사용)
        /// </summary>
        public override void ResetSelectedPoint()
        {
            IsPositionChange = false; ;
            selectionPoint = SelectionPoint.None;
        }

        [JsonIgnore]
        public int CrossSize { get { return mCrossSize; } set { mCrossSize = value; } }
        int mCrossSize = 20;
        public override void Draw(System.Drawing.Graphics gdi)
        {
            //if (transform != null)
            //    gdi.Transform = transform;//.Clone();
            //float[,] ff = SvFunc.DisplayMatF(this.display.Transform2D);
            if (color.IsEmpty == true)
                color = Color.Red;
            Pen p = new Pen(color);
            Point2f H = new Point2f(2 * mCrossSize, 0);
            Point2f V = new Point2f(0, 2 * mCrossSize);
            H = SvFunc.Rotate(H, ThetaRad);
            V = SvFunc.Rotate(V, ThetaRad);

            // size 조절
            float width = mCrossSize; float height = mCrossSize;

            Point2f p2f = new Point2f(point.X, point.Y);
            Point2f fixtureC = SvFunc.FixtureToImage2F(p2f, TransformMat);
            Point2f point_X = SvFunc.FixtureToImage2F(p2f + new Point2f(width, 0), transformMat);
            float newWidth = 2 * (float)fixtureC.DistanceTo(point_X);

            float scale = 1;// width / newWidth * display.ZoomRatio;


            Point2f CvP1_0 = (fixtureC + SvFunc.Rotate(new Point2f(-width * scale / 2, 0), ThetaRad));
            Point2f CvP1_1 = (fixtureC + SvFunc.Rotate(new Point2f(+width * scale / 2, 0), ThetaRad));
            Point2f CvP1_2 = (fixtureC + SvFunc.Rotate(new Point2f(0, -height * scale / 2), ThetaRad));
            Point2f CvP1_3 = (fixtureC + SvFunc.Rotate(new Point2f(0, +height * scale / 2), ThetaRad));


            //DrawLine(p, CvP1_0, CvP1_1);
            //DrawLine(p, CvP1_2, CvP1_3);
            gdi.DrawLine(p, SvFunc.Point2fToF(CvP1_0), SvFunc.Point2fToF(CvP1_1));
            gdi.DrawLine(p, SvFunc.Point2fToF(CvP1_2), SvFunc.Point2fToF(CvP1_3));
        }

        public SvPoint Clone()
        {
            if (this == null) return null;
            SvPoint newPoint = new SvPoint();
            newPoint.Point3d = this.Point3d;
            newPoint.TransformMat = this.TransformMat;
            return newPoint;
        }
    }
}
