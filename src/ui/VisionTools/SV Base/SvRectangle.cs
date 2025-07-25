using System;
using System.Drawing;
using OpenCvSharp;

namespace VisionInspection
{
    [Serializable]
    public class SvRectangle : InteractDrawObject
    {
        public SvRectangle()
        {

        }
        public SvRectangle(float x, float y, float width, float height)
        {
            rectf.X = x;
            rectf.Y = y;
            Width = width;
            Height = height;
        }

        public SvRectangle(OpenCvSharp.Point2f location, float width, float height)
        {
            rectf.Location = location;
            Width = width;
            Height = height;
        }

        public SvRectangle(PointF location, float width, float height) : this(location.X, location.Y, width, height) { }
        public SvRectangle(Rect2f rect) { rectf = rect; }

        Rect2f rectf = new Rect2f();

        public Rect2f RectF
        {
            get { return rectf; }
            set { rectf = value; OnRefresh(this, null); }
        }

        public Rect Rect
        {
            get { return new Rect((int)Math.Round((double)rectf.X), (int)Math.Round((double)rectf.Y), (int)Math.Round((double)Width), (int)Math.Round((double)Height)); }
            set { rectf.Location = value.Location; Width = value.Width; Height = value.Height; OnRefresh(this, null); }
        }

        public RectangleF RoiRect
        {
            get { return new RectangleF(rectf.X, rectf.Y, Width, Height); }
            set { rectf.X = value.X; rectf.Y = value.Y; Width = value.Width; Height = value.Height; OnRefresh(this, null); }
        }

        PointF LeftTop
        {
            get { return new PointF(rectf.Left, rectf.Top); }
            set { Left = value; Top = value; }
        }

        PointF RightTop
        {
            get { return new PointF(rectf.Right, rectf.Top); }
            set { Right = value; Top = value; }
        }

        PointF LeftBottom
        {
            get { return new PointF(rectf.Left, rectf.Bottom); }
            set { Left = value; Bottom = value; }
        }

        PointF RightBottom
        {
            get { return new PointF(rectf.Right, rectf.Bottom); }
            set { Right = value; Bottom = value; }
        }

        public System.Drawing.PointF Left
        {
            get { return new PointF(rectf.Left, (rectf.Top + rectf.Bottom) / 2); }
            set { float f = rectf.Left; f = value.X - f; rectf.Left = value.X; Width -= f; }
        }

        public System.Drawing.PointF Right
        {
            get { return new PointF(rectf.Right, (rectf.Top + rectf.Bottom) / 2); }
            set { Width = value.X - rectf.Left + 1; }
        }

        public System.Drawing.PointF Top
        {
            get { return new PointF((rectf.Left + rectf.Right) / 2, rectf.Top); }
            set { float f = rectf.Top; f = value.Y - f; rectf.Top = value.Y; Height -= f; }
        }

        public System.Drawing.PointF Bottom
        {
            get { return new PointF((rectf.Left + rectf.Right) / 2, rectf.Bottom); }
            set { Height = value.Y - rectf.Top + 1; }
        }

        public float Width
        {
            get { return rectf.Width; }
            set
            {
                if (value < 0)
                {
                    rectf.Width = value;
                    float f = rectf.Left; rectf.Left = rectf.Right; rectf.Width = f - rectf.Left + 1;


                    switch (selectionPoint)
                    {
                        case SelectionPoint.LeftTop:
                            selectionPoint = SelectionPoint.RightTop;
                            //display.Cursor = System.Windows.Forms.Cursors.SizeNESW;
                            break;
                        case SelectionPoint.LeftBottom:
                            selectionPoint = SelectionPoint.RightBottom;
                            //display.Cursor = System.Windows.Forms.Cursors.SizeNWSE;
                            break;
                        case SelectionPoint.RightTop:
                            selectionPoint = SelectionPoint.LeftTop;
                            //display.Cursor = System.Windows.Forms.Cursors.SizeNWSE;
                            break;
                        case SelectionPoint.RightBottom:
                            selectionPoint = SelectionPoint.LeftBottom;
                            //display.Cursor = System.Windows.Forms.Cursors.SizeNESW;
                            break;
                        case SelectionPoint.Left:
                            selectionPoint = SelectionPoint.Right;
                            break;
                        case SelectionPoint.Right:
                            selectionPoint = SelectionPoint.Left;
                            break;
                    }
                }
                else
                    rectf.Width = value;
                OnRefresh(this, null);
            }
        }

        public float Height
        {
            get { return rectf.Height; }
            set
            {
                if (value < 0)
                {
                    rectf.Height = value;
                    float f = rectf.Top; rectf.Top = rectf.Bottom; rectf.Height = f - rectf.Top + 1;


                    switch (selectionPoint)
                    {
                        case SelectionPoint.LeftTop:
                            selectionPoint = SelectionPoint.LeftBottom;
                            //display.Cursor = System.Windows.Forms.Cursors.SizeNESW;
                            break;
                        case SelectionPoint.LeftBottom:
                            selectionPoint = SelectionPoint.LeftTop;
                            //display.Cursor = System.Windows.Forms.Cursors.SizeNWSE;
                            break;
                        case SelectionPoint.RightTop:
                            selectionPoint = SelectionPoint.RightBottom;
                            //display.Cursor = System.Windows.Forms.Cursors.SizeNESW;
                            break;
                        case SelectionPoint.RightBottom:
                            selectionPoint = SelectionPoint.RightTop;
                            //display.Cursor = System.Windows.Forms.Cursors.SizeNWSE;
                            break;
                        case SelectionPoint.Top:
                            selectionPoint = SelectionPoint.Bottom;
                            break;
                        case SelectionPoint.Bottom:
                            selectionPoint = SelectionPoint.Top;
                            break;
                    }
                }
                else
                    rectf.Height = value;
                OnRefresh(this, null);
            }
        }

        PointF center;
        public System.Drawing.PointF Center
        {
            get { return new PointF((rectf.Right + rectf.Left) / 2, (rectf.Top + rectf.Bottom) / 2); }
            set { center = value; }
        }

        public Point2f Location
        {
            get { return rectf.Location; }
            set { rectf.Location = value; OnRefresh(this, null); }
        }

        Color color = Color.Red;
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
        //        if (Width == 0 || Height == 0)
        //        {
        //            rectf.Size = new OpenCvSharp.CPlusPlus.Size2f(display.Size.Width / 2, display.Size.Height / 2);
        //            rectf.Location = new Point2f((display.Size.Width - rectf.Size.Width) / 2, (display.Size.Height - rectf.Size.Height) / 2);
        //        }
        //    }
        //}

        enum SelectionPoint { None, LeftTop, RightTop, LeftBottom, RightBottom, Left, Right, Top, Bottom, Center };
        SelectionPoint selectionPoint = SelectionPoint.None;

        [NonSerialized]
        Point2f[] selectionPoints = new Point2f[5];

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
                    case SelectionPoint.LeftTop:
                        LeftTop = mouseLocation;
                        break;
                    case SelectionPoint.RightTop:
                        RightTop = mouseLocation;
                        break;
                    case SelectionPoint.LeftBottom:
                        LeftBottom = mouseLocation;
                        break;
                    case SelectionPoint.RightBottom:
                        RightBottom = mouseLocation;
                        break;
                    case SelectionPoint.Left:
                        Left = mouseLocation;
                        break;
                    case SelectionPoint.Right:
                        Right = mouseLocation;
                        break;
                    case SelectionPoint.Top:
                        Top = mouseLocation;
                        break;
                    case SelectionPoint.Bottom:
                        Bottom = mouseLocation;
                        break;
                    case SelectionPoint.Center:
                        PointF Move = new PointF(mouseLocation.X - Center.X, mouseLocation.Y - Center.Y);
                        Center = mouseLocation;
                        rectf.Left += Move.X; rectf.Top += Move.Y;
                        break;
                }
                return true;
            }

            int index = -1; double min = double.MaxValue;

            Point2f mouse2f = SvFunc.FixtureToImage2F(new Point2f(mouseLocation.X, mouseLocation.Y), TransformMat);

            for (int i = 0; i < selectionPoints.Length; i++)
            {
                double distance = mouse2f.DistanceTo(selectionPoints[i]);
                if (distance <= min)
                {
                    index = i;
                    min = distance;
                }
            }

            if (min > SelectionSize)
            {
                for (int i = 0; i < 4; i++)
                {
                    Point2d baseline = new Point2d(selectionPoints[i].X - selectionPoints[(i + 1) % 4].X, selectionPoints[i].Y - selectionPoints[(i + 1) % 4].Y);
                    Point2d line1 = new Point2d(mouse2f.X - selectionPoints[i].X, mouse2f.Y - selectionPoints[i].Y);
                    Point2d line2 = new Point2d(mouse2f.X - selectionPoints[(i + 1) % 4].X, mouse2f.Y - selectionPoints[(i + 1) % 4].Y);

                    double distance = (Point2d.DotProduct(baseline, line1) * Point2d.DotProduct(baseline, line2) < 0) ?
                        SvFunc.GetNormalDistance(selectionPoints[i], selectionPoints[(i + 1) % 4], mouse2f) : double.MaxValue;

                    if (distance <= min)
                    {
                        index = i + 5;
                        min = distance;
                    }
                }
            }

            if (min > SelectionSize)
                index = -1;

            switch (index)
            {
                case -1:
                    //display.Cursor = System.Windows.Forms.Cursors.Arrow;
                    selectionPoint = SelectionPoint.None;
                    return false;
                case 0:
                    //display.Cursor = System.Windows.Forms.Cursors.SizeNWSE;
                    selectionPoint = SelectionPoint.LeftTop;
                    break;
                case 1:
                    //display.Cursor = System.Windows.Forms.Cursors.SizeNESW;
                    selectionPoint = SelectionPoint.RightTop;
                    break;
                case 2:
                    //display.Cursor = System.Windows.Forms.Cursors.SizeNWSE;
                    selectionPoint = SelectionPoint.RightBottom;
                    break;
                case 3:
                    //display.Cursor = System.Windows.Forms.Cursors.SizeNESW;
                    selectionPoint = SelectionPoint.LeftBottom;
                    break;
                case 4:
                    //display.Cursor = System.Windows.Forms.Cursors.SizeAll;
                    selectionPoint = SelectionPoint.Center;
                    break;
                case 5:
                    //display.Cursor = System.Windows.Forms.Cursors.SizeNS;
                    selectionPoint = SelectionPoint.Top;
                    break;
                case 6:
                    //display.Cursor = System.Windows.Forms.Cursors.SizeWE;
                    selectionPoint = SelectionPoint.Right;
                    break;
                case 7:
                    //display.Cursor = System.Windows.Forms.Cursors.SizeNS;
                    selectionPoint = SelectionPoint.Bottom;
                    break;
                case 8:
                    //display.Cursor = System.Windows.Forms.Cursors.SizeWE;
                    selectionPoint = SelectionPoint.Left;
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

        public override void Draw(System.Drawing.Graphics gdi)
        {
            if (rectf == null) return;
            if (color.IsEmpty == true)
                color = Color.Red;
            Pen p = new Pen(color);

            if (selectionPoints == null) selectionPoints = new Point2f[10];

            selectionPoints[0] = SvFunc.FixtureToImage2F(new Point2f(LeftTop.X, LeftTop.Y), TransformMat);
            selectionPoints[1] = SvFunc.FixtureToImage2F(new Point2f(RightTop.X, RightTop.Y), TransformMat);
            selectionPoints[2] = SvFunc.FixtureToImage2F(new Point2f(RightBottom.X, RightBottom.Y), TransformMat);
            selectionPoints[3] = SvFunc.FixtureToImage2F(new Point2f(LeftBottom.X, LeftBottom.Y), TransformMat);
            selectionPoints[4] = SvFunc.FixtureToImage2F(new Point2f(Center.X, Center.Y), TransformMat);

            for (int i = 0; i < 4; i++)
            {
                gdi.DrawLine(p, selectionPoints[i].X, selectionPoints[i].Y, selectionPoints[(i + 1) % 4].X, selectionPoints[(i + 1) % 4].Y);
            }
            //gdi.DrawRectangle(p, RoiRect.X, RoiRect.Y, RoiRect.Width, RoiRect.Height);
            //gdi.DrawEllipse(Pens.Yellow, Center.X - SelectionSize / 2, Center.Y - SelectionSize / 2, SelectionSize, SelectionSize);
        }

        public Point2f[] Pts2fImage
        {
            get
            {
                Point2f[] pts = new Point2f[4];

                pts[0] = SvFunc.FixtureToImage2F(new Point2f(rectf.Left, rectf.Top), TransformMat);
                pts[1] = SvFunc.FixtureToImage2F(new Point2f(rectf.Right, rectf.Top), TransformMat);
                pts[2] = SvFunc.FixtureToImage2F(new Point2f(rectf.Right, rectf.Bottom), TransformMat);
                pts[3] = SvFunc.FixtureToImage2F(new Point2f(rectf.Left, rectf.Bottom), TransformMat);

                return pts;
            }
        }

        public Point2f[] Pts2f
        {
            get
            {
                Point2f[] pts = new Point2f[4];

                pts[0] = new Point2f(0, 0);
                pts[1] = new Point2f(rectf.Right - rectf.Left, 0);
                pts[2] = new Point2f(rectf.Right - rectf.Left, rectf.Bottom - rectf.Top);
                pts[3] = new Point2f(0, rectf.Bottom - rectf.Top);

                return pts;
            }
        }

        public OpenCvSharp.Point2f[] PtsImage
        {
            get
            {
                OpenCvSharp.Point2f[] pts = new OpenCvSharp.Point2f[4];
                
                pts[0] = SvFunc.FixtureToImage2F(new Point2f(rectf.Left, rectf.Top), TransformMat);
                pts[1] = SvFunc.FixtureToImage2F(new Point2f(rectf.Right, rectf.Top), TransformMat);
                pts[2] = SvFunc.FixtureToImage2F(new Point2f(rectf.Right, rectf.Bottom), TransformMat);
                pts[3] = SvFunc.FixtureToImage2F(new Point2f(rectf.Left, rectf.Bottom), TransformMat);

                return pts;
            }
        }

        public Point2f[] Pts
        {
            get
            {
                Point2f[] pts = new Point2f[4];

                pts[0] = new Point2f(0, 0);
                pts[1] = new Point2f(rectf.Right - rectf.Left, 0);
                pts[2] = new Point2f(rectf.Right - rectf.Left, rectf.Bottom - rectf.Top);
                pts[3] = new Point2f(0, rectf.Bottom - rectf.Top);

                return pts;
            }
        }
    }
}
