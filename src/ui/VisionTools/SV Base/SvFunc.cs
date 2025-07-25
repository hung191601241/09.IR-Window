using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VisionInspection
{
    public enum FindLinefromRefLineOption { Auto, Close, Far, Distance }
    public static class SvFunc
    {
        public static Point2d ImageToFixture2D(Point2d point, Mat mat)
        {
            if (mat == null) return point;

            Point2d transPt = new Point2d();
            Mat ptMat = new Mat(3, 1, MatType.CV_64FC1);
            ptMat.Set<double>(0, 0, (double)point.X);
            ptMat.Set<double>(1, 0, (double)point.Y);
            ptMat.Set<double>(2, 0, 1);

            //float[,] ff = SvFunc.DisplayMatF(mat);
            //float[,] fff = SvFunc.DisplayMatF(ptMat);

            Mat transPtMat = mat * ptMat;

            transPt.X = (double)transPtMat.Get<double>(0, 0) / (double)transPtMat.Get<double>(2, 0);
            transPt.Y = (double)transPtMat.Get<double>(1, 0) / (double)transPtMat.Get<double>(2, 0);
            return transPt;
        }

        static public Point2f ImageToFixture2F(Point2f point, Mat mat)
        {
            Point2d pointd = new Point2d(point.X, point.Y);
            Point2d transformedPointD = ImageToFixture2D(pointd, mat);
            return new Point2f((float)transformedPointD.X, (float)transformedPointD.Y);
        }
        static public Point ImageToFixture(Point point, Mat mat)
        {
            Point2d pointd = new Point2d(point.X, point.Y);
            Point2d transformedPointD = ImageToFixture2D(pointd, mat);
            return new Point((float)transformedPointD.X, (float)transformedPointD.Y);
        }

        static public Point2d FixtureToImage2D(Point2d point, Mat mat)
        {
            if (mat == null) return point;
            Mat invMat = mat.Inv();
            return ImageToFixture2D(point, invMat);
        }

        static public Point2f FixtureToImage2F(Point2f point, Mat mat)
        {
            if (mat == null) return point;
            Mat invMat = mat.Inv();
            return ImageToFixture2F(point, invMat);
        }
        static public Point FixtureToImage(Point point, Mat mat)
        {
            if (mat == null) return point;
            Mat invMat = mat.Inv();
            return ImageToFixture(point, invMat);
        }

        static public Point3d FixtureToImage3D(Point3d point, Mat mat)
        {
            Point2d ret = FixtureToImage2D(new Point2d(point.X, point.Y), mat);
            Point2d O = ImageToFixture2D(new Point2d(0, 0), mat);
            Point2d T = ImageToFixture2D(new Point2d(1, 0), mat);
            return new Point3d(ret.X, ret.Y, point.Z - Math.Atan2(T.Y - O.Y, T.X - O.X));
        }

        static public Point3d ImageToFixture3D(Point3d point, Mat mat)
        {
            Point2d ret = ImageToFixture2D(new Point2d(point.X, point.Y), mat);
            Point2d O = FixtureToImage2D(new Point2d(0, 0), mat);
            Point2d T = FixtureToImage2D(new Point2d(1, 0), mat);
            return new Point3d(ret.X, ret.Y, point.Z - Math.Atan2(T.Y - O.Y, T.X - O.X));
        }

        static public System.Drawing.PointF Multiply(System.Drawing.PointF P, float f)
        {
            return new System.Drawing.PointF(P.X * f, P.Y * f);
        }

        static public float getLength(System.Drawing.PointF P0, System.Drawing.PointF P1)
        {
            return (float)Math.Sqrt((P1.X - P0.X) * (P1.X - P0.X) + (P1.Y - P0.Y) * (P1.Y - P0.Y));
        }

        static public int Round(double d)
        {
            int iResult = (int)Math.Round(d);
            return iResult;
        }

        static public double Getlength(Point2d P)
        {
            return Math.Sqrt(P.X * P.X + P.Y * P.Y);
        }

        static public double Getlength(Point2d P0, Point2d P1)
        {
            Point2d P = P1 - P0;
            return Getlength(P);
        }

        static public double Getlength(System.Drawing.Point P0, System.Drawing.Point P1)
        {
            Point2d P = new Point2d(P1.X - P0.X, P1.Y - P0.Y);
            return Getlength(P);
        }

        static public double Getlength(System.Drawing.PointF P0, System.Drawing.PointF P1)
        {
            Point2d P = new Point2d(P1.X - P0.X, P1.Y - P0.Y);
            return Getlength(P);
        }

        /// <summary>
        /// Get Length along to Direction (P0->P1)
        /// </summary>
        /// <param name="VectorP0"></param>
        /// <param name="VectorP1"></param>
        /// <param name="P"></param>
        /// <returns></returns>
        static public double GetAlongDistance(Point2d VectorP0, Point2d VectorP1, Point2d P)
        {
            return (Point2d.DotProduct(VectorP1 - VectorP0, P - VectorP0) / Getlength(VectorP1 - VectorP0));
        }

        /// <summary>
        /// Get Normal Distance
        /// </summary>
        /// <param name="P0">Origine Point</param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <returns></returns>
        static public double GetNormalDistance(Point2d P0, Point2d P1, Point2d P2)
        {
            return Math.Abs(Point2d.CrossProduct(P1 - P0, P2 - P0) / Math.Max(Point2d.Distance(P0, P1), Point2d.Distance(P0, P2)));
        }

        static public double GetNormalDistance(Point2f P0, Point2f P1, Point2f P2)
        {
            return Math.Abs(Point2f.CrossProduct(P1 - P0, P2 - P0) / Math.Max(Point2f.Distance(P0, P1), Point2f.Distance(P0, P2)));
        }

        static public double GetNormalDistance(System.Drawing.PointF P0F, System.Drawing.PointF P1F, System.Drawing.PointF P2F)
        {
            Point2d P0 = new Point2d(P0F.X, P0F.Y);
            Point2d P1 = new Point2d(P1F.X, P1F.Y);
            Point2d P2 = new Point2d(P2F.X, P2F.Y);

            return Math.Abs(Point2d.CrossProduct(P1 - P0, P2 - P0) / Math.Max(Point2d.Distance(P0, P1), Point2d.Distance(P0, P2)));
        }

        static public void CopyROI(Mat src, out Mat result, Mat desROI, Rect ROI)
        {
            if (desROI.Width != ROI.Width || desROI.Height != ROI.Height)
            {
                result = new Mat(); // Trả về Mat rỗng nếu không hợp lệ
                return;
            }
            // Lấy ROI từ src
            Mat roiSrc = new Mat(src, ROI);

            // Copy dữ liệu vào desROI
            roiSrc.CopyTo(desROI);

            // Sao chép ROI vào result
            result = roiSrc.Clone();
        }

        static public Mat WarpImage(Mat src, Mat warp)
        {
            if (src == null) return null;

            double S = 1;
            Point2d P1 = WarpPoint(new Point2d(0, 0), warp);
            Point2d P2 = WarpPoint(new Point2d(src.Width, 0), warp);
            Point2d P3 = WarpPoint(new Point2d(0, src.Height), warp);
            Point2d P4 = WarpPoint(new Point2d(src.Width, src.Height), warp);

            double xmin = S * Math.Min(Math.Min(Math.Min((P1.X), (P2.X)), (P3.X)), (P4.X));
            double xmax = S * Math.Max(Math.Max(Math.Max((P1.X), (P2.X)), (P3.X)), (P4.X));
            double ymin = S * Math.Min(Math.Min(Math.Min((P1.Y), (P2.Y)), (P3.Y)), (P4.Y));
            double ymax = S * Math.Max(Math.Max(Math.Max((P1.Y), (P2.Y)), (P3.Y)), (P4.Y));

            Size dsize = new Size((int)Math.Round(xmax - xmin + 1, 0), (int)Math.Round(ymax - ymin + 1, 0));
            Mat dst = new Mat(dsize, src.Type());
            Cv2.WarpPerspective(src, dst, warp, dsize, InterpolationFlags.Linear);

            return dst.Clone();
        }

        static public Point2d WarpPoint(Point2d pt, Mat warp)
        {
            if (warp == null)
                throw new Exception("WarpMat is null");

            Mat result = new Mat(3, 1, MatType.CV_64FC1, new double[] { pt.X, pt.Y, 1.0 });
            result = warp * result;

            Point2d resultPt = new Point2d(double.NaN, double.NaN);

            if (result.At<double>(2, 0) != 0)
                return new Point2d(result.At<double>(0, 0) / result.At<double>(2, 0), result.At<double>(1, 0) / result.At<double>(2, 0));

            result.Dispose();

            return resultPt;
        }

        static public Point3d WarpPoint(Point3d pt, Mat warp)
        {
            if (warp == null)
                throw new Exception("WarpMat is null");

            Point3d resultPt = new Point3d(0, 0, 0);

            double theta = pt.Z;
            Point2d x0 = WarpPoint(SvFunc.Rotate(new Point2d(pt.X + 100, pt.Y), pt.Z), warp);
            //Point2d y0 = WarpPoint(new Point2d(pt.X + 100, pt.Y), warp);
            Point2d O = WarpPoint(new Point2d(pt.X, pt.Y), warp);

            if (O.X == double.NaN || O.Y == double.NaN)
                return new Point3d(double.NaN, double.NaN, double.NaN);

            theta = Math.Atan2((x0 - O).Y, (x0 - O).X);
            resultPt = new Point3d(O.X, O.Y, theta);

            return resultPt;
        }

        /// <summary>
        /// Moment Method를 이용한 중심(X, Y, T) 찾기
        /// </summary>
        /// <param name="Img"></param>
        /// <returns>Center(X, Y, Trad)</returns>
        static public Point3d CalMomentResult(Mat Img)
        {
            if (Img == null || Img.Channels() > 1) return new Point3d();

            Moments moment = Cv2.Moments(Img, true);
            double centroidX = moment.M10 / moment.M00;
            double centroidY = moment.M01 / moment.M00;
            double thetarad;

            if (moment.M00 == 0)
                return new Point3d();

            if ((moment.Nu20 - moment.Nu02) == 0) thetarad = 0;
            else
                thetarad = Math.Atan(2 * moment.Nu11 / (moment.Nu20 - moment.Nu02)) / 2;

            if (thetarad > 0)
            { if (moment.Mu20 < moment.Mu02) thetarad = thetarad - 90 * (Math.PI / 180); }
            else
            { if (moment.Mu20 < moment.Mu02) thetarad = 90 * (Math.PI / 180) + thetarad; }

            return new Point3d(centroidX, centroidY, thetarad);
        }

        /// <summary>
        /// Moment Method를 이용한 중심(X, Y, T) 찾기
        /// </summary>
        /// <param name="Img"></param>
        /// <returns>Center(X, Y, Trad)</returns>
        static public Point3d CalMomentResult(Point[] Contour)
        {
            if (Contour.Length < 3)
                return new Point3d(0, 0, 0);

            Point minP = new Point(int.MaxValue, int.MaxValue);
            Point maxP = new Point();

            for (int i = 0; i < Contour.Length; i++)
            {
                minP.X = Math.Min(minP.X, Contour[i].X);
                minP.Y = Math.Min(minP.Y, Contour[i].Y);
                maxP.X = Math.Max(maxP.X, Contour[i].X);
                maxP.Y = Math.Max(maxP.Y, Contour[i].Y);
            }

            Point[] resultPoints = new Point[Contour.Length];
            for (int i = 0; i < Contour.Length; i++)
            {
                resultPoints[i] = Contour[i];
                resultPoints[i] -= minP;
            }

            Size MomentSize = new Size(maxP.X - minP.X + 1, maxP.Y - minP.Y + 1);
            Mat MomentMat = new Mat(MomentSize, MatType.CV_8UC1, new Scalar(0));
            Cv2.DrawContours(MomentMat, new Point[][] { resultPoints }, 0, new Scalar(255), -1, LineTypes.AntiAlias);

            Point3d offsetPoint = CalMomentResult(MomentMat);

            offsetPoint.X = offsetPoint.X + minP.X;
            offsetPoint.Y = offsetPoint.Y + minP.Y;

            MomentMat.Dispose();

            return offsetPoint;
        }

        /// <summary>
        /// Moment Method를 이용한 중심(X, Y, T) 찾기
        /// </summary>
        /// <param name="Img"></param>
        /// <returns>Center(X, Y, Trad)</returns>
        static public Point3d CalMomentResult(Point2f[] Contour2f, int scale = 1)
        {
            if (Contour2f.Length < 3)
                return new Point3d(0, 0, 0);

            int N = Contour2f.Length;

            Point[] Contour = new Point[N];
            for (int i = 0; i < N; i++)
            {
                Contour[i] = (Point)Contour2f[i] * scale;
            }

            Point minP = new Point(int.MaxValue, int.MaxValue);
            Point maxP = new Point();

            for (int i = 0; i < Contour.Length; i++)
            {
                minP.X = Math.Min(minP.X, Contour[i].X);
                minP.Y = Math.Min(minP.Y, Contour[i].Y);
                maxP.X = Math.Max(maxP.X, Contour[i].X);
                maxP.Y = Math.Max(maxP.Y, Contour[i].Y);
            }

            Point[] resultPoints = new Point[Contour.Length];
            for (int i = 0; i < Contour.Length; i++)
            {
                resultPoints[i] = Contour[i];
                resultPoints[i] -= minP;
            }

            Size MomentSize = new Size(maxP.X - minP.X + 1, maxP.Y - minP.Y + 1);
            Mat MomentMat = new Mat(MomentSize, MatType.CV_8UC1, new Scalar(0));
            Cv2.DrawContours(MomentMat, new List<List<Point>> { resultPoints.ToList() }, 0, new Scalar(255), -1, LineTypes.AntiAlias);

            Point3d offsetPoint = CalMomentResult(MomentMat);

            offsetPoint.X = offsetPoint.X + minP.X;
            offsetPoint.Y = offsetPoint.Y + minP.Y;

            offsetPoint.X *= (1.0 / scale);
            offsetPoint.Y *= (1.0 / scale);

            MomentMat.Dispose();

            return offsetPoint;
        }

        static public void ShowTestImg(Mat src, double scale, string name = "Test")
        {
            if (src == null || src.IsDisposed || (int)(src.Width * scale) == 0 || (int)(src.Height * scale) == 0)
                return;
            Mat test = src.Clone();
            //test.Resize(Size.Zero, scale, scale);
            Cv2.Resize(test, test, new Size(src.Width * scale, src.Height * scale));
            Cv2.ImShow(name, test);
            Cv2.WaitKey();
            test.Dispose();
        }

        static public Mat HistoAnalysis(Mat src)
        {
            Mat[] mats = new Mat[1] { src };
            int[] channels = new int[1] { 0 };
            float[][] ranges = new float[1][] { new float[2] { 0, 255 } };
            int[] histoSize = new int[1] { 255 };

            Mat hist = new Mat();
            Cv2.CalcHist(mats, channels, new Mat(), hist, 1, histoSize, ranges);

            return hist;
        }

        static public double[,] DisplayMat(Mat src)
        {
            double[,] result = new double[src.Height, src.Width];
            for (int i = 0; i < src.Height; i++)
                for (int j = 0; j < src.Width; j++)
                    result[i, j] = src.At<double>(i, j);
            return result;
        }

        static public float[,] DisplayMatF(Mat src)
        {
            if (src == null) return new float[,] { };
            float[,] result = new float[src.Height, src.Width];
            for (int i = 0; i < src.Height; i++)
                for (int j = 0; j < src.Width; j++)
                    result[i, j] = src.At<float>(i, j);
            return result;
        }

        static public Point2d[] RotatePts(Point2d[] pts, Point3d center)
        {
            if (pts == null) return null;

            Point2d[] result = new Point2d[pts.Length];
            double T = center.Z;
            Point2d C = new Point2d(center.X, center.Y);

            for (int i = 0; i < pts.Length; i++)
            {
                result[i] = pts[i] - C;
                result[i] = C + Rotate(result[i], T);
            }

            return result;
        }


        /// <summary>
        /// Theta만큼 회전시킨 point를 반환
        /// </summary>
        /// <param name="point">회전시킬 포인트</param>
        /// <param name="thetaRad">회전량</param>
        /// <returns>새 포인트</returns>
        static public Point2d Rotate(Point2d point, double thetaRad)
        {
            Point2d result = new Point2d();
            result.X = Math.Cos(thetaRad) * point.X - Math.Sin(thetaRad) * point.Y;
            result.Y = Math.Sin(thetaRad) * point.X + Math.Cos(thetaRad) * point.Y;
            return result;
        }

        static public Point2f Rotate(Point2f point, double thetaRad)
        {
            Point2d result = Rotate(new Point2d(point.X, point.Y), thetaRad);
            return new Point2f((float)result.X, (float)result.Y);
        }

        /// <summary>
        /// Theta만큼 회전시킨 point를 반환
        /// </summary>
        /// <param name="point">회전시킬 포인트</param>
        /// <param name="thetaRad">회전량</param>
        /// <returns>새 포인트</returns>
        static public System.Drawing.PointF Rotate(System.Drawing.PointF point, double thetaRad)
        {
            System.Drawing.PointF result = new System.Drawing.PointF();
            result.X = (float)(Math.Cos(thetaRad) * point.X - Math.Sin(thetaRad) * point.Y);
            result.Y = (float)(Math.Sin(thetaRad) * point.X + Math.Cos(thetaRad) * point.Y);
            return result;
        }

        /// <summary>
        /// point를 중심으로 Theta만큼 회전시킨 point를 반환
        /// </summary>
        /// <param name="point">회전시킬 포인트</param>
        /// <param name="thetaRad">회전량</param>
        /// <returns>새 포인트</returns>
        static public Point2d RotateAtCenter(Point2d point, Point2d center, double thetaRad)
        {
            Point2d result = new Point2d();

            result.X = (Math.Cos(thetaRad) * (point.X - center.X) - Math.Sin(thetaRad) * (point.Y - center.Y)) + center.X;
            result.Y = (Math.Sin(thetaRad) * (point.X - center.X) + Math.Cos(thetaRad) * (point.Y - center.Y)) + center.Y;
            return result;
        }

        /// <summary>
        /// point를 중심으로 Theta만큼 회전시킨 point를 반환
        /// </summary>
        /// <param name="point">회전시킬 포인트</param>
        /// <param name="thetaRad">회전량</param>
        /// <returns>새 포인트</returns>
        static public System.Drawing.PointF RotateAtCenter(System.Drawing.PointF point, System.Drawing.PointF center, double thetaRad)
        {
            System.Drawing.PointF result = new System.Drawing.PointF();

            result.X = (float)((Math.Cos(thetaRad) * (point.X - center.X) - Math.Sin(thetaRad) * (point.Y - center.Y)) + center.X);
            result.Y = (float)((Math.Sin(thetaRad) * (point.X - center.X) + Math.Cos(thetaRad) * (point.Y - center.Y)) + center.Y);
            return result;
        }

        /// <summary>
        /// point를 중심으로 Theta만큼 회전시킨 point를 반환
        /// </summary>
        /// <param name="point">회전시킬 포인트</param>
        /// <param name="thetaRad">회전량</param>
        /// <returns>새 포인트</returns>
        static public Point2f RotateAtCenter(Point2f point, Point2f center, double thetaRad)
        {
            Point2f result = new Point2f();

            result.X = (float)((Math.Cos(thetaRad) * (point.X - center.X) - Math.Sin(thetaRad) * (point.Y - center.Y)) + center.X);
            result.Y = (float)((Math.Sin(thetaRad) * (point.X - center.X) + Math.Cos(thetaRad) * (point.Y - center.Y)) + center.Y);
            return result;
        }

        /// <summary>
        /// 행렬을 Theta 만큼 회전시킨 새로운 행렬을 반환
        /// </summary>
        /// <param name="mat">회전시킬 행렬</param>
        /// <param name="thetaRad">회전량(radian)</param>
        /// <returns>새 행렬</returns>
        static public Mat Rotate(Mat mat, double thetaRad)
        {
            if (mat.Depth() != 64)
                throw new Exception("double 형식 Mat만 됩니다.");

            Mat result = mat.Clone();
            result.Set<double>(0, 0, Math.Cos(thetaRad) * mat.At<double>(0, 0) - Math.Sin(thetaRad) * mat.At<double>(1, 0));
            result.Set<double>(1, 0, Math.Sin(thetaRad) * mat.At<double>(0, 0) + Math.Cos(thetaRad) * mat.At<double>(1, 0));
            return result;
        }

        static public Point2f Point2dTo2f(Point2d P2d)
        {
            return new Point2f((float)P2d.X, (float)P2d.Y);
        }

        static public System.Drawing.PointF Point2fToF(Point2f P2d)
        {
            return new System.Drawing.PointF((float)P2d.X, (float)P2d.Y);
        }

        static DateTime dateTime;
        static string watchName;
        static public void StartWatch(string name)
        {
            dateTime = DateTime.Now;
            watchName = name;
        }

        static public void StopWatch()
        {
            double timewatch = DateTime.Now.Subtract(dateTime).TotalMilliseconds;
            System.Diagnostics.Debug.WriteLine(string.Format("{0} : {1:0.000}", watchName, timewatch));
        }

        static public double TimeWatch(DateTime dt, string param)
        {
            double timewatch = DateTime.Now.Subtract(dt).TotalMilliseconds;
            System.Diagnostics.Debug.WriteLine(string.Format("{0} : {1:0.000}", param, timewatch));
            return timewatch;
        }

        static public Mat PtoMat(Point2d P)
        {
            Mat M = new Mat(2, 1, MatType.CV_64FC1, new double[] { P.X, P.Y });
            return M;
        }

        static public Mat PtoMat(Point3d P)
        {
            Mat M = new Mat(3, 1, MatType.CV_64FC1, new double[] { P.X, P.Y, 1 });
            return M;
        }

        static public Point3d MattoP(Mat M, bool isHomo = false)
        {
            Point3d P = new Point3d(0, 0, 1);
            P.X = M.At<double>(0);
            P.Y = M.At<double>(1);
            if (M.Height == 3)
                P.Z = M.At<double>(2);

            if (isHomo && P.Z != 0)
            {
                P.X /= P.Z;
                P.Y /= P.Z;
            }
            return P;
        }

        static public Rect2f Rect2Rectf(Rect rect)
        {
            return new Rect2f(rect.X, rect.Y, rect.Width, rect.Height);
        }

        static public Rect Rectf2Rect(Rect2f rectf)
        {
            return new Rect((int)Math.Round(rectf.X, 0), (int)Math.Round(rectf.Y, 0), (int)Math.Round(rectf.Width, 0), (int)Math.Round(rectf.Height, 0));
        }

        static public Rect GetRect(Rect Image, Rect rect)
        {
            Rect2f newRectf = GetRect(Rect2Rectf(Image), Rect2Rectf(rect));
            return Rectf2Rect(newRectf);
        }

        static public Rect2f GetRect(Rect2f Image, Rect2f rect)
        {
            Rect2f newRect = rect;
            if (rect.X < 0)
            {
                newRect.X = 0;
                newRect.Width += rect.X - 1;
            }
            if (rect.Y < 0)
            {
                newRect.Y = 0;
                newRect.Height += rect.Y - 1;                
            }
            if (rect.Right + 1 > Image.Width)
            {
                newRect.Width -= (rect.Right - Image.Width + 1);                
            }
            if (rect.Bottom + 1 > Image.Height)
            {
                newRect.Height -= (rect.Bottom - Image.Height + 1);
            }
            if (newRect.Width < 0)
                newRect.Width = 0;
            if (newRect.Height < 0)
                newRect.Height = 0;
            return newRect;
        }

        static public Rect GetRect(Mat Image, Rect rect)
        {
            if (Image == null)
                return rect;
            Rect ImageRect = new Rect(0, 0, Image.Width, Image.Height);
            return GetRect(ImageRect, rect);
        }

        static public Rect2f GetRect(Mat Image, Rect2f rect)
        {
            if (Image == null)
                return rect;
            Rect2f ImageRect = new Rect2f(0, 0, Image.Width, Image.Height);
            return GetRect(ImageRect, rect);
        }

        static public Rect ResizeRect(Rect rect, double scale)
        {
            return new Rect((int)Math.Round(rect.X * scale, 0), (int)Math.Round(rect.Y * scale, 0), (int)Math.Round(rect.Width * scale, 0), (int)Math.Round(rect.Height * scale, 0));
        }

        static public Size ResizeSize(Size rect, double scale)
        {
            return new Size((int)Math.Round(rect.Width * scale, 0), (int)Math.Round(rect.Height * scale, 0));
        }

        static public Rect GetRectFixCenter(Rect rect, int W, int H)
        {
            Point CP = (rect.TopLeft + rect.BottomRight) * 0.5;
            return new Rect(CP.X - W / 2, CP.Y - H / 2, W, H);
        }

        static public Rect MoveRect(Rect rect, Point2d move)
        {
            rect.Location += (Point)move;
            return rect;
        }

        static public Mat EMat
        {
            get { return new Mat(3, 3, MatType.CV_32FC1, new float[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 }); }
        }

        public static dynamic ChangeType(object inobj, Type inType, Type outType, int count = 0)
        {
            if (count >= 2) return null;

            dynamic newCalData = Activator.CreateInstance(outType) as dynamic;

            foreach (System.Reflection.PropertyInfo pinfo in inType.GetProperties())
            {
                try
                {
                    System.Reflection.PropertyInfo newpinfo = outType.GetProperty(pinfo.Name);

                    if (pinfo.PropertyType.IsClass)
                    {
                        dynamic d = ChangeType(pinfo.GetValue(inobj), pinfo.PropertyType, newpinfo.PropertyType, count + 1);
                        if (d == null) continue;
                        newpinfo.SetValue(newCalData, d);
                    }
                    else
                        newpinfo.SetValue(newCalData, pinfo.GetValue(inobj));
                }
                catch { continue; }
            }
            return newCalData;
        }

        public static dynamic Convert(dynamic source, Type dest)
        {
            return System.Convert.ChangeType(source, dest);
        }
        public static T GetInstance<T>(Type type) where T : class
        {
            try
            {
                return (T)Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        static public void WaitTime(int milisecond)
        {
            DateTime dt = DateTime.Now;
            while(milisecond < DateTime.Now.Subtract(dt).TotalMilliseconds)
            {}
        }

        static public bool IsPossibleImage(Mat img)
        {
            if (img == null || img.Width == 0 || img.Height == 0) return false;
            return true;
        }

        static public bool IsPossibleImage(SvImage img)
        {
            if (img == null || img.Mat == null || img.Width == 0 || img.Height == 0) return false;
            return true;
        }

        static public System.Drawing.Drawing2D.Matrix MatToMatrix(Mat mat)
        {
            if (mat.Width != 3 || mat.Height != 3) throw new Exception("Mat must be 3x3");
            if (mat.Type() != MatType.CV_32FC1) throw new Exception("Mat muyt be CV_32FC1");

            System.Drawing.Drawing2D.Matrix winMat = new System.Drawing.Drawing2D.Matrix
                (
                    mat.At<float>(0, 0), mat.At<float>(1, 0),
                    mat.At<float>(0, 1), mat.At<float>(1, 1),
                    mat.At<float>(0, 2), mat.At<float>(1, 2)
                );

            return winMat;
        }

        static public Mat MatrixToMat(System.Drawing.Drawing2D.Matrix matrix)
        {
            return new Mat(3, 3, MatType.CV_32FC1, new float[] { matrix.Elements[0], matrix.Elements[2], matrix.Elements[4],
                                                                 matrix.Elements[1], matrix.Elements[3], matrix.Elements[5],
                                                                 0, 0, 1 });
        }

        static public Mat ConvertMatTypeDtoF(Mat _d)
        {
            if (_d == null) return null;
            Mat _f = new Mat(_d.Rows, _d.Cols, MatType.CV_32FC1);
            for (int i = 0; i < _d.Rows; i++)
            {
                for (int j = 0; j < _d.Cols; j++)
                {
                    _f.Set(i, j, (float)_d.At<double>(i, j));
                }
            }
            return _f;
        }

        static public Point2f[] ChangeArray(System.Drawing.PointF[] _pts)
        {
            Point2f[] outPts = new Point2f[_pts.Length];
            for (int i = 0; i < _pts.Length; i++)
            {
                outPts[i] = new Point2f(_pts[i].X, _pts[i].Y);
            }
            return outPts;
        }

        static public System.Drawing.PointF[] ChangeArray(Point2f[] _pts)
        {
            System.Drawing.PointF[] outPts = new System.Drawing.PointF[_pts.Length];
            for (int i = 0; i < _pts.Length; i++)
            {
                outPts[i] = new System.Drawing.PointF(_pts[i].X, _pts[i].Y);
            }
            return outPts;
        }

        static public Mat CopyTo(Mat _src, Mat _des, Point2f[] _srcPts, Point2f[] _desPts)
        {
            Mat mask = new Mat(_src.Size(), _src.Type());

            mask.SetTo(1);
            _src.CopyTo(_des, mask);
            mask.Dispose();
            return _des;
        }


        #region FindLineRefLine
        static public bool FindLinefromRefLine(out Point3d _result, out List<int> _ExceptedIndex, double _lineX, double _lineY, double _lineTrad, List<Point2d> _lineResultPts, double _ignoreCntRate = 0.5, FindLinefromRefLineOption _option = FindLinefromRefLineOption.Auto, double _distance = 0)
        {
            // TestCode 만약 남아 있음 지울것!!
            //_ignoreCntRate = 0.8;
            //_lineX = 2000;
            //_lineY = 500;
            //_lineTrad = Math.PI / 4;
            //_distance = 0;
            // TestCode 끝
            List<KeyValuePair<int, Point2d>> indexedBestPts = new List<KeyValuePair<int, Point2d>>();
            List<Point2d> listbestPts = new List<Point2d>(_lineResultPts);
            
            listbestPts.RemoveAll(x => x.X == -1 && x.Y == -1); // 이건 IVA-A에서 못찾은 점에 대한 예외처리, IVA-B는 다른식으로 구성해야함

            for (int i = 0; i < _lineResultPts.Count; i++)
                indexedBestPts.Add(new KeyValuePair<int, Point2d>(i, _lineResultPts[i]));

            Point2d shiftpoint = new Point2d(_distance * Math.Cos(_lineTrad + Math.PI / 2), _distance * Math.Sin(_lineTrad + Math.PI / 2));
            if (((_lineResultPts[0] + _lineResultPts[_lineResultPts.Count - 1]) * 0.5 - new Point2d(_lineX, _lineY)).DotProduct(shiftpoint) < 0)
                shiftpoint *= -1;

            // -sin(T) * X + cos(T) * Y = C
            // Kx = -Sin(T), Ky = Cos(T)
            double Kx = -Math.Sin(_lineTrad);
            double Ky = Math.Cos(_lineTrad);

            Point3d fitLine;
            Point3d refLine = new Point3d(_lineX, _lineY, _lineTrad);
            Point3d distLine = new Point3d(_lineX + shiftpoint.X, _lineY + shiftpoint.Y, _lineTrad);

            _result = new Point3d();
            _ExceptedIndex = new List<int>();

            if (_lineResultPts == null || _lineResultPts.Count == 0) return false;
            int badCnt = 0;
            try
            {
                switch(_option)
                {
                    // FitLine에서 가장 먼 값을 없앤다.
                    case FindLinefromRefLineOption.Auto:
                        while (GetRMS(listbestPts, Kx, Ky) > 1 && listbestPts.Count > _lineResultPts.Count * (1 - _ignoreCntRate) && listbestPts.Count > 4)
                        {
                            // FitLine에서 먼 순서대로 정렬
                            fitLine = CalcFitLineFixedK(listbestPts, Kx, Ky);
                            indexedBestPts.Sort((x1, x2) => GetDistanceLine2Point(fitLine, x1.Value) > GetDistanceLine2Point(fitLine, x2.Value) ? -1 : 1);
                            listbestPts.Remove(indexedBestPts[badCnt].Value);
                            _ExceptedIndex.Add(indexedBestPts[badCnt].Key);
                            badCnt++;
                        }
                        break;

                    // OriginalLine에서 가장 먼 값을 없앤다.
                    case FindLinefromRefLineOption.Close:
                        // OriginalLine에서 먼 순서대로 정렬
                        indexedBestPts.Sort((x1, x2) => GetDistanceLine2Point(refLine, x1.Value) > GetDistanceLine2Point(refLine, x2.Value) ? -1 : 1);
                        while (GetRMS(listbestPts, Kx, Ky) > 1 && listbestPts.Count > _lineResultPts.Count * (1 - _ignoreCntRate) && listbestPts.Count > 4)
                        {
                            listbestPts.Remove(indexedBestPts[badCnt].Value);
                            _ExceptedIndex.Add(indexedBestPts[badCnt].Key);
                            badCnt++;
                        }
                        break;
                
                    // OriginalLine에서 가장 가까운 값을 없앤다.
                    case FindLinefromRefLineOption.Far:
                        indexedBestPts.Sort((x1, x2) => GetDistanceLine2Point(refLine, x1.Value) < GetDistanceLine2Point(refLine, x2.Value) ? -1 : 1);
                        while (GetRMS(listbestPts, Kx, Ky) > 1 && listbestPts.Count > _lineResultPts.Count * (1 - _ignoreCntRate) && listbestPts.Count > 4)
                        {
                            listbestPts.Remove(indexedBestPts[badCnt].Value);
                            _ExceptedIndex.Add(indexedBestPts[badCnt].Key);
                            badCnt++;
                        }
                        break;

                    // 거리가 L보다 가장 큰 차이가 나는 값을 없앤다.
                    case FindLinefromRefLineOption.Distance:
                        indexedBestPts.Sort((x1, x2) => GetDistanceLine2Point(distLine, x1.Value) > GetDistanceLine2Point(distLine, x2.Value) ? -1 : 1);
                        while (GetRMS(listbestPts, Kx, Ky) > 1 && listbestPts.Count > _lineResultPts.Count * (1 - _ignoreCntRate) && listbestPts.Count > 4)
                        {
                            listbestPts.Remove(indexedBestPts[badCnt].Value);
                            _ExceptedIndex.Add(indexedBestPts[badCnt].Key);
                            badCnt++;
                        }
                        break;
                }

                // 최종 Line에서 dist. 100 이상값은 버리고 다시 최종값을 구한다.
                while (GetRMS(listbestPts, Kx, Ky) > 1 && listbestPts.Count > 4)
                {
                    // FitLine에서 먼 순서대로 정렬
                    fitLine = CalcFitLineFixedK(listbestPts, Kx, Ky);
                    indexedBestPts.Sort((x1, x2) => GetDistanceLine2Point(fitLine, x1.Value) > GetDistanceLine2Point(fitLine, x2.Value) ? -1 : 1);
                    if (GetDistanceLine2Point(fitLine, listbestPts[badCnt]) > 100)
                    {
                        listbestPts.Remove(indexedBestPts[badCnt].Value);
                        _ExceptedIndex.Add(indexedBestPts[badCnt].Key);
                    }
                    else
                        break;
                    listbestPts.Remove(indexedBestPts[badCnt].Value);
                    _ExceptedIndex.Add(indexedBestPts[badCnt].Key);
                    badCnt++;
                }
                _result = CalcFitLineFixedK(listbestPts, Kx, Ky);
            }
            catch { return false; }
            

            return true;
        }

        static public Point3d CalcFitLineFixedK(List<Point2d> _listPts, double _Kx, double _Ky)
        {
            // -sin(T) * X + cos(T) * Y = C
            // Kx = -Sin(T), Ky = Cos(T)

            // Kx * Xs[0] + Ky * Ys[0] = C
            // Kx * Xs[1] + Ky * Ys[1] = C
            // 
            // Kx * Xs[n-1] + Ky * Ys[n-1] = C
            
            Point3d result = new Point3d();
            if (_listPts == null || _listPts.Count == 0) return result;
            double sum = 0;

            for (int i = 0; i < _listPts.Count; i++)
            {
                sum += _Kx * _listPts[i].X + _Ky * _listPts[i].Y;
            }

            double C = sum / _listPts.Count;

            result.Z = Math.Atan(-_Kx / _Ky);
            if (Math.Abs(result.Z) < Math.PI / 4)
            {
                result.X = 0;
                result.Y = C / _Ky;
            }
            else
            {
                result.Y = 0;
                result.X = C / _Kx;
            }
            
            return result;
        }

        static public double GetDistanceLine2Point(Point3d _line, Point2d _point)
        {
            Point2d vector1 = new Point2d(Math.Cos(_line.Z), Math.Sin(_line.Z));
            Point2d vector2 = new Point2d(_point.X - _line.X, _point.Y - _line.Y);
            double distance = vector1.CrossProduct(vector2);
            return Math.Abs(distance);
        }

        static public double GetRMS(List<Point2d> _listPts, double _Kx, double _Ky)
        {
            if (_listPts == null || _listPts.Count == 0) return 0;
            double sum = 0;
            for (int i = 0; i < _listPts.Count; i++)
            {
                sum += GetDistanceLine2Point(CalcFitLineFixedK(_listPts, _Kx, _Ky), _listPts[i]);
            }
            double result = Math.Sqrt(sum / _listPts.Count);
            return result;
        }
        #endregion
    }

}
