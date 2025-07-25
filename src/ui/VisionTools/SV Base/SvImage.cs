using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using OpenCvSharp.CPlusPlus;
using System.Runtime.Serialization;
using System.Reflection;
using OpenCvSharp;

namespace VisionInspection
{
    [Serializable]
    public class SvImage : IDisposable, ISerializable, ICloneable
    {

        Mat transformMat;
        public SvImage() : this(new Mat())
        {
        }

        public SvImage(Mat mat)
        {
            regionRect = new SvRectangle();
            transformMat = Mat.Eye(3, 3, MatType.CV_64FC1);
            this.mat = mat;
            RegionRect = new SvRectangle(0, 0, mat.Width, mat.Height);
        }

        public object Clone()
        {
            return Clone(true);
        }

        public SvImage Clone(bool isMatCopy)
        {
            SvImage cloneImg = new SvImage();

            if (this.mat == null || this.mat.IsDisposed)
                cloneImg.mat = null;
            else if (isMatCopy)
                cloneImg.mat = this.mat.Clone();
            else
                cloneImg.mat = this.mat;

            cloneImg.regionRect.RectF = this.regionRect.RectF;
            cloneImg.transformMat = this.transformMat.Clone();
            cloneImg.CenterPoint = this.CenterPoint;
            cloneImg.RectPoint = this.RectPoint;
            cloneImg.AnglgeRoiRad = this.AnglgeRoiRad;

            return cloneImg;
        }

        Mat mat;

        public Mat Mat
        {
            get { return mat; }
            set
            {
                if (mat != null && !mat.IsDisposed && mat != value)
                    mat.Dispose();

                if (value == null || value.IsDisposed)
                    mat = new Mat();
                else
                    mat = value;
                if (regionRect.Width == 0 && regionRect.Height == 0)
                    RegionRect = new SvRectangle(0, 0, mat.Width, mat.Height);
            }
        }

        public int Width
        {
            get
            {
                if (IsNull)
                    return 0;
                else
                    return mat.Width;
            }
        }

        public int Height
        {
            get
            {
                if (IsNull)
                    return 0;
                else
                    return mat.Height;
            }
        }

        public bool IsNull
        {
            get
            {
                if (mat == null || mat.IsDisposed || mat.Width == 0 || mat.Height == 0)
                    return true;
                else
                    return false;
            }
        }

        SvRectangle regionRect;

        public SvRectangle RegionRect
        {
            get
            {
                //AdjustRegionRect();
                return regionRect;
            }
            set { regionRect = value; }
        }
        private Point3d centerPoint = new Point3d(0, 0, 0);
        public Point3d CenterPoint { get => centerPoint; set => centerPoint = value; }
        private Point2d[] rectPoint = new Point2d[4];
        public Point2d[] RectPoint { get => rectPoint; set => rectPoint = value; }
        public double AnglgeRoiRad { get; set; }

        void AdjustRegionRect()
        {
            Point2f transformedLocation = SvFunc.FixtureToImage2F(regionRect.Location, transformMat);
            if (mat != null && !mat.IsDisposed)
            {
                if (regionRect.Width < 0) regionRect.Width = 0;
                if (regionRect.Height < 0) regionRect.Height = 0;
                if (transformedLocation.X + regionRect.Width > mat.Width) regionRect.Width = mat.Width - transformedLocation.X;
                if (transformedLocation.Y + regionRect.Height > mat.Height) regionRect.Height = mat.Height - transformedLocation.Y;
            }
        }


        public Mat TransformMat
        {
            get { return transformMat; }
            set
            {
                if (transformMat != null && !transformMat.IsDisposed && transformMat.Width != 0 && transformMat.Height != 0)
                    transformMat.Dispose();

                if (value == null || value.IsDisposed)
                {
                    transformMat = Mat.Eye(3, 3, MatType.CV_64FC1);
                    return;
                }
                transformMat = value.Clone();
            }
        }

        public void Dispose()
        {
            if (mat != null && !mat.IsDisposed)
            {
                mat.Dispose();
                mat = new Mat();
            }

            transformMat.Dispose();
            transformMat = Mat.Eye(3, 3, MatType.CV_64FC1);

            regionRect = null;
            regionRect = new SvRectangle();
        }

        SvImage(SerializationInfo info, StreamingContext context)
        {
            //SvSerializer.Deserializeing(this, info, context);
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //SvSerializer.Serializeing(this, info, context);
        }

        public Point2f[] RegionRect2fPtsImage
        {
            get
            {
                Point2f[] pts = new Point2f[4];

                pts[0] = SvFunc.FixtureToImage2F(new Point2f(RegionRect.RectF.Left, RegionRect.RectF.Top), TransformMat);
                pts[1] = SvFunc.FixtureToImage2F(new Point2f(RegionRect.RectF.Right, RegionRect.RectF.Top), TransformMat);
                pts[2] = SvFunc.FixtureToImage2F(new Point2f(RegionRect.RectF.Right, RegionRect.RectF.Bottom), TransformMat);
                pts[3] = SvFunc.FixtureToImage2F(new Point2f(RegionRect.RectF.Left, RegionRect.RectF.Bottom), TransformMat);

                return pts;
            }
        }

        public Point2f[] RegionRect2fPts
        {
            get
            {
                Point2f[] pts = new Point2f[4];

                pts[0] = new Point2f(0, 0);
                pts[1] = new Point2f(RegionRect.RectF.Right - RegionRect.RectF.Left, 0);
                pts[2] = new Point2f(RegionRect.RectF.Right - RegionRect.RectF.Left, RegionRect.RectF.Bottom - RegionRect.RectF.Top);
                pts[3] = new Point2f(0, RegionRect.RectF.Bottom - RegionRect.RectF.Top);

                return pts;
            }
        }

        public Point2f[] RegionRectPtsImage
        {
            get
            {
                Point2f[] pts = new Point2f[4];

                pts[0] = SvFunc.FixtureToImage2F(new Point2f(RegionRect.RectF.Left, RegionRect.RectF.Top), TransformMat);
                pts[1] = SvFunc.FixtureToImage2F(new Point2f(RegionRect.RectF.Right, RegionRect.RectF.Top), TransformMat);
                pts[2] = SvFunc.FixtureToImage2F(new Point2f(RegionRect.RectF.Right, RegionRect.RectF.Bottom), TransformMat);
                pts[3] = SvFunc.FixtureToImage2F(new Point2f(RegionRect.RectF.Left, RegionRect.RectF.Bottom), TransformMat);

                return pts;
            }
        }

        public Point2f[] RegionRectPts
        {
            get
            {
                Point2f[] pts = new Point2f[4];

                pts[0] = new Point2f(0, 0);
                pts[1] = new Point2f(RegionRect.RectF.Right - RegionRect.RectF.Left, 0);
                pts[2] = new Point2f(RegionRect.RectF.Right - RegionRect.RectF.Left, RegionRect.RectF.Bottom - RegionRect.RectF.Top);
                pts[3] = new Point2f(0, RegionRect.RectF.Bottom - RegionRect.RectF.Top);

                return pts;
            }
        }

        public System.Drawing.PointF[] RegionRectFPts
        {
            get
            {
                System.Drawing.PointF[] pts = new System.Drawing.PointF[4];
                Point2f[] p2f = RegionRect2fPtsImage;
                for (int i = 0; i < 4; i++)
                    pts[i] = new System.Drawing.PointF(p2f[i].X, p2f[i].Y);

                return pts;
            }
        }

    }
}
