using OpenCvSharp;
using System;
using System.Drawing;
using OpenCvSharp.Extensions;
using System.Drawing.Imaging;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace VisionInspection
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SvMask : InteractDrawObject, ISerializable, IDisposable
    {
        int height;
        int width;

        public int PenSize { get; set; }

        public SvMask(int width, int height)
        {
            this.height = height;
            this.width = width;
            this.DrawMask = true;
            this.OnMask = true;
            this.DisplayMask = true;

            maskMat = new Mat(height, width, MatType.CV_8UC1, new Scalar(255));
            displayMaskMat = new Mat(height, width, MatType.CV_8UC4, new Scalar(0, 0, 0, 0));
        }

        [JsonIgnore]
        Mat displayMaskMat { get; set; }
        [JsonProperty]
        public bool OnMask { get; set; } // Mask를 그린다/지운다
        [JsonProperty]
        public bool DrawMask { get; set; } // Mask 만든다/안만든다
        [JsonProperty]
        public bool DisplayMask { get; set; } // Mask를 보인다/안보인다 

        //bool mDrawMask = false;
        //public bool DrawMask { get { return mDrawMask; } set { mDrawMask = value; } }

        Mat maskMat;

        [JsonIgnore]
        public Mat MaskMat
        {
            get
            {
                return maskMat;
            }
        }

        public void ResetMask()
        {
            this.Dispose();

            maskMat = new Mat(height, width, MatType.CV_8UC1, new Scalar(255));
            displayMaskMat = new Mat(height, width, MatType.CV_8UC4, new Scalar(0, 0, 0, 0));
            OnRefresh(this, null);
        }

        //public override SvMip.Display.SvDisplayViewInteract Display
        //{
        //    get { return display; }
        //    set { display = value; }
        //}


        [JsonIgnore]
        System.Drawing.PointF oldmousePt { get; set; }

        public override bool FindPoint(System.Drawing.PointF mouseLocation)
        {
            if (!IsPositionChange || !DrawMask) return false;

            if (oldmousePt == new System.Drawing.Point(-10000, -10000))
            {
                oldmousePt = mouseLocation;
                return true;
            }

            Point2d oldPt = SvFunc.FixtureToImage2D(new Point2d((int)Math.Round(oldmousePt.X), (int)Math.Round(oldmousePt.Y)), TransformMat);
            Point2d newPt = SvFunc.FixtureToImage2D(new Point2d((int)Math.Round(mouseLocation.X), (int)Math.Round(mouseLocation.Y)), TransformMat);

            Scalar gray, color;
            if (OnMask)
            {
                gray = new Scalar(0);
                //color = new Scalar(255, 150, 0, 50);
            }
            else
            {
                gray = new Scalar(255);
                //color = new Scalar(0, 0, 0, 0);
            }
            if (PenSize == 0)
                PenSize = SelectionSize;
            maskMat.Line((OpenCvSharp.Point)oldPt, (OpenCvSharp.Point)newPt, gray, PenSize);
            //displayMaskMat.Line(oldPt, newPt, color, SelectionSize);

            oldmousePt = mouseLocation;
            return true;
        }

        public override void SelectPoint()
        {
            if (!IsPositionChange && DrawMask)
            {
                IsPositionChange = true;
                oldmousePt = new System.Drawing.PointF(-10000, -10000);
            }
        }

        public override void ResetSelectedPoint()
        {
            IsPositionChange = false;
        }

        public override void Draw(System.Drawing.Graphics gdi)
        {
            if (DisplayMask == false) return;
            if (displayMaskMat == null || displayMaskMat.IsDisposed || displayMaskMat.Width == 0) return;
            if (maskMat == null || maskMat.IsDisposed || maskMat.Width == 0) return;

            Mat resizeMask = new Mat();
            OpenCvSharp.Size dsize = new OpenCvSharp.Size(maskMat.Width / 5, maskMat.Height / 5);
            Mat nullMask = new Mat(dsize, MatType.CV_8UC1, 0);
            Mat resizedisplay = new Mat();
            Cv2.Resize(255 - maskMat, resizeMask, dsize);
            Cv2.Merge(new Mat[] { resizeMask, resizeMask, nullMask, resizeMask * 0.3 }, resizedisplay);
            Cv2.Resize(resizedisplay, displayMaskMat, displayMaskMat.Size());
            Bitmap img = displayMaskMat.ToBitmap(PixelFormat.Format32bppArgb);
            gdi.DrawImageUnscaled(img, 0, 0);

            resizeMask.Dispose();
            nullMask.Dispose();
            resizedisplay.Dispose();
            img.Dispose();
        }

        public bool IsPossible(SvImage inputImage)
        {
            if (inputImage == null) return false;
            if (inputImage.Width == 0 || inputImage.Height == 0) return false;
            if (this.MaskMat.Width != inputImage.Width || this.MaskMat.Height != inputImage.Height) return false;
            return true;
        }

        #region Serialize
        protected SvMask(SerializationInfo info, StreamingContext context)
        {
            //SvSerializer.Deserializeing(this, info, context);
            if (displayMaskMat == null || displayMaskMat.IsDisposed || displayMaskMat.Width == 0)
                displayMaskMat = new Mat(height, width, MatType.CV_8UC4, new Scalar(0, 0, 0, 0));
        }
        public bool IsDisposed
        {
            get
            {
                if (maskMat != null || displayMaskMat != null) return false;
                if (!maskMat.IsDisposed || !displayMaskMat.IsDisposed) return false;
                return true;
            }
        }

        public void Dispose()
        {
            if (maskMat != null && !maskMat.IsDisposed)
                maskMat.Dispose();

            if (displayMaskMat != null && !displayMaskMat.IsDisposed)
                displayMaskMat.Dispose();
        }

        public SvMask Clone()
        {
            if (this.maskMat == null)
                return null;

            Mat maskMat = this.maskMat.Clone();
            Mat displaymaskMat = this.displayMaskMat.Clone();

            SvMask cloneMask = new SvMask(this.width, this.height);
            cloneMask.maskMat = maskMat;
            cloneMask.displayMaskMat = displaymaskMat;
            return cloneMask;
        }
        #endregion
    }
}
