using DeviceSource;
using MvCamCtrl.NET;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Media3D;
using static MvCamCtrl.NET.MyCamera;

namespace VisionInspection
{
    public class HikCam : ICamDevice
    {
        public CameraOperator m_pOperator;
        public MyCamera.MV_CC_DEVICE_INFO_LIST m_pDeviceList;
        public MyCamera.MV_CC_DEVICE_INFO device;
        bool m_bGrabbing;

        UInt32 m_nBufSizeForDriver = 3072 * 2048 * 3;
        byte[] m_pBufForDriver = new byte[3072 * 2048 * 3];            // Buffer for getting image from driver

        UInt32 m_nBufSizeForSaveImage = 3072 * 2048 * 3 * 3 + 2048;
        byte[] m_pBufForSaveImage = new byte[3072 * 2048 * 3 * 3 + 2048];         // Buffer for saving image
        public ECamDevType eCamDevType { get; set; }
        public bool isOpen { get; set; }
        public DeviceList deviceList { get; set; }
        public string SerialNumber { get; set; }

        public HikCam()
        {
            InitializeCamera();
            eCamDevType = ECamDevType.Hikrobot;
        }
        public HikCam(MyCamera.MV_CC_DEVICE_INFO device)
        {
            InitializeCamera();
            this.device = device;
            eCamDevType = ECamDevType.Hikrobot;
            this.SerialNumber = GetSerialNumber();
        }

        public bool InitializeCamera()
        {
            m_pOperator = new CameraOperator();
            m_bGrabbing = false;
            //DeviceListAcq();
            return true;
        }
        public DeviceList InitializeDeviceList()
        {
            DeviceList deviceList = new DeviceList();
            deviceList.devNum = m_pDeviceList.nDeviceNum;
            deviceList.devInfo = new IntPtr[m_pDeviceList.nDeviceNum];
            for(int i = 0; i< deviceList.devNum; i++)
            {
                deviceList.devInfo[i] = m_pDeviceList.pDeviceInfo[i];
            }
            return deviceList;
        }
        public void DeviceListAcq()
        {
            int nRet;
            /*Create Device List*/
            System.GC.Collect();
            nRet = CameraOperator.EnumDevices(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_pDeviceList);

            deviceList = InitializeDeviceList();
            if (0 != nRet)
            {
                return;
            }
        }
        private string GetSerialNumber()
        {
            if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
                MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                return gigeInfo.chSerialNumber;
            }
            else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
            {
                IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stUsb3VInfo, 0);
                MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                return usbInfo.chSerialNumber;
            }
            else 
                return null;
        }
        public int Open()
        {
            int ret = m_pOperator.Open(ref device);
            //m_pOperator.SetEnumValue("AcquisitionMode", 2);
            //m_pOperator.SetEnumValue("TriggerMode", 0);

            ret = StartGrab();
            if (ret == MyCamera.MV_OK)
            {
                isOpen = true;
            }
            else
            {
                isOpen = false;
            }
            return ret;
        }
        public bool IsOpen()
        {
            return this.isOpen;
        }
        public int Close()
        {
            int ret = m_pOperator.Close();
            return ret;
        }

        public int DisPose()
        {
            int ret = m_pOperator.DisPose();
            return ret;
        }

        public int StartGrab()
        {
            int nRet1;

            //Start Grabbing
            nRet1 = m_pOperator.StartGrabbing();
            if (MyCamera.MV_OK != nRet1)
            {
                return nRet1;
            }

            //nRet1 = m_pOperator.Display(img.Handle);

            //HwndSource hwndSource = PresentationSource.FromVisual(img) as HwndSource;

            //if (hwndSource != null)
            //{
            //    nRet1 = m_pOperator.Display(hwndSource.Handle);
            //}
            if (MyCamera.MV_OK != nRet1)
            {
                return nRet1;
            }
            return nRet1;
        }

        public int StopGrab()
        {
            int nRet;

            //Start Grabbing
            nRet = m_pOperator.StopGrabbing();
            return nRet;
        }
        public Mat CaptureImageOld1()
        {
            int nRet;
            UInt32 nPayloadSize = 0;
            nRet = m_pOperator.GetIntValue("PayloadSize", ref nPayloadSize);
            if (MyCamera.MV_OK != nRet)
            {
                //MessageBox.Show("Get PayloadSize failed");
                return null;
            }
            if (nPayloadSize + 2048 > m_nBufSizeForDriver)
            {
                m_nBufSizeForDriver = nPayloadSize + 2048;
                m_pBufForDriver = new byte[m_nBufSizeForDriver];

                // Determine the buffer size to save image
                // BMP image size: width * height * 3 + 2048 (Reserved for BMP header)
                m_nBufSizeForSaveImage = m_nBufSizeForDriver * 3 + 2048;
                m_pBufForSaveImage = new byte[m_nBufSizeForSaveImage];
            }

            IntPtr pData = Marshal.UnsafeAddrOfPinnedArrayElement(m_pBufForDriver, 0);
            UInt32 nDataLen = 0;
            MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();

            //Get one frame timeout, timeout is 1 sec
            nRet = m_pOperator.GetOneFrameTimeout(pData, ref nDataLen, m_nBufSizeForDriver, ref stFrameInfo, 1000);
            if (MyCamera.MV_OK != nRet)
            {
                //MessageBox.Show("No Data!");
                return null;
            }

            IntPtr pImage = Marshal.UnsafeAddrOfPinnedArrayElement(m_pBufForSaveImage, 0);
            MyCamera.MV_SAVE_IMAGE_PARAM_EX stSaveParam = new MyCamera.MV_SAVE_IMAGE_PARAM_EX();
            stSaveParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Bmp;
            stSaveParam.enPixelType = stFrameInfo.enPixelType;
            stSaveParam.pData = pData;
            stSaveParam.nDataLen = stFrameInfo.nFrameLen;
            stSaveParam.nHeight = stFrameInfo.nHeight;
            stSaveParam.nWidth = stFrameInfo.nWidth;
            stSaveParam.pImageBuffer = pImage;
            stSaveParam.nBufferSize = m_nBufSizeForSaveImage;
            stSaveParam.nJpgQuality = 80;
            nRet = m_pOperator.SaveImage(ref stSaveParam);
            if (MyCamera.MV_OK != nRet)
            {
                //MessageBox.Show("Save Fail!");
                return null;
            }
            //FileStream file = new FileStream("image.bmp", FileMode.Create, FileAccess.Write);
            //file.Write(m_pBufForSaveImage, 0, (int)stSaveParam.nImageLen);
            //file.Close();
            byte[] imageData = m_pBufForSaveImage;
            Bitmap bmp;
            using (var ms = new MemoryStream(imageData))
            {
                bmp = new Bitmap(ms);
            }
            Mat src = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);

            return src;
        }
        public Mat CaptureImage()
        {
            int nRet = m_pOperator.ExcuteTrigger();
            if (MyCamera.MV_OK != nRet)
            {
                return null;
            }
            UInt32 nPayloadSize = 0;
            // Lấy kích thước payload
            if (m_pOperator.GetIntValue("PayloadSize", ref nPayloadSize) != MyCamera.MV_OK)
                return null;

            // Cấp phát bộ đệm nếu cần
            if (nPayloadSize > m_nBufSizeForDriver)
            {
                m_nBufSizeForDriver = nPayloadSize;
                m_pBufForDriver = new byte[m_nBufSizeForDriver];
            }

            IntPtr pData = Marshal.UnsafeAddrOfPinnedArrayElement(m_pBufForDriver, 0);
            UInt32 nDataLen = 0;
            MyCamera.MV_FRAME_OUT_INFO_EX frameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();

            // Lấy 1 khung hình với timeout 1000ms
            if (m_pOperator.GetOneFrameTimeout(pData, ref nDataLen, m_nBufSizeForDriver, ref frameInfo, 1000) != MyCamera.MV_OK)
                return null;

            int width = (int)frameInfo.nWidth;
            int height = (int)frameInfo.nHeight;
            MvGvspPixelType pixelType = frameInfo.enPixelType;

            // Xử lý từng định dạng PixelType cụ thể
            switch (pixelType)
            {
                case MvGvspPixelType.PixelType_Gvsp_Mono8:
                    return new Mat(height, width, MatType.CV_8UC1, pData);

                case MvGvspPixelType.PixelType_Gvsp_BGR8_Packed:
                    return new Mat(height, width, MatType.CV_8UC3, pData);

                case MvGvspPixelType.PixelType_Gvsp_BGRA8_Packed:
                    return new Mat(height, width, MatType.CV_8UC4, pData);

                default:
                    // Với các định dạng chưa xử lý, chuyển sang BMP tạm thời (có thể nặng hơn)
                    int saveBufferSize = width * height * 3 + 2048;
                    if (m_pBufForSaveImage == null || m_nBufSizeForSaveImage < saveBufferSize)
                    {
                        m_nBufSizeForSaveImage = (uint)saveBufferSize;
                        m_pBufForSaveImage = new byte[saveBufferSize];
                    }

                    IntPtr pImage = Marshal.UnsafeAddrOfPinnedArrayElement(m_pBufForSaveImage, 0);

                    var saveParam = new MyCamera.MV_SAVE_IMAGE_PARAM_EX
                    {
                        enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Bmp,
                        enPixelType = frameInfo.enPixelType,
                        pData = pData,
                        nDataLen = frameInfo.nFrameLen,
                        nWidth = frameInfo.nWidth,
                        nHeight = frameInfo.nHeight,
                        pImageBuffer = pImage,
                        nBufferSize = (uint)m_nBufSizeForSaveImage,
                        nJpgQuality = 90
                    };

                    if (m_pOperator.SaveImage(ref saveParam) != MyCamera.MV_OK)
                        return null;

                    using (var ms = new MemoryStream(m_pBufForSaveImage, 0, (int)saveParam.nImageLen))
                    using (var bmp = new Bitmap(ms))
                    {
                        return OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
                    }
            }
        }

        public bool SetExposeTime(double Exp)
        {
            int nRet = m_pOperator.SetFloatValue("ExposureTime", (float)Exp);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool SetGain(double Gain)
        {
            int nRet = m_pOperator.SetFloatValue("Gain", (float)Gain);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool SetHeight(long Height)
        {
            if (!CheckAccessMode("Height", out MV_XML_AccessMode accessMode) || accessMode == MV_XML_AccessMode.AM_RO)
                return false;
            int nRet = m_pOperator.SetIntValue("Height", (uint)Height);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool SetWidth(long Width)
        {
            if (!CheckAccessMode("Width", out MV_XML_AccessMode accessMode) || accessMode == MV_XML_AccessMode.AM_RO)
                return false;
            int nRet = m_pOperator.SetIntValue("Width", (uint)Width);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool CheckAccessMode(string para, out MV_XML_AccessMode accessMode)
        {
            int nRet = m_pOperator.AccessMode(para, out accessMode);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool GetDoubleMinValue(string strKey, out double dMinValue)
        {
            dMinValue = 0d;
            if (!CheckAccessMode(strKey, out MV_XML_AccessMode accessMode) || accessMode == MV_XML_AccessMode.AM_WO)
                return false;
            float temp = 0f;
            int nRet = m_pOperator.GetFloatMinValue(strKey, ref temp);
            dMinValue = temp;
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool GetDoubleMaxValue(string strKey, out double dMaxValue)
        {
            dMaxValue = 0d;
            float temp = 0f;
            if (!CheckAccessMode(strKey, out MV_XML_AccessMode accessMode) || accessMode == MV_XML_AccessMode.AM_WO)
                return false;
            int nRet = m_pOperator.GetFloatMaxValue(strKey, ref temp);
            dMaxValue = temp;
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool GetDoubleValue(string strKey, out double dValue)
        {
            dValue = 0d;
            float temp = 0f;
            if (!CheckAccessMode(strKey, out MV_XML_AccessMode accessMode) || accessMode == MV_XML_AccessMode.AM_WO)
                return false;
            int nRet = m_pOperator.GetFloatValue(strKey, ref temp);
            dValue = temp;
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool GetIntMinValue(string strKey, out Int64 nMinValue)
        {
            nMinValue = 0;
            if (!CheckAccessMode(strKey, out MV_XML_AccessMode accessMode) || accessMode == MV_XML_AccessMode.AM_WO)
                return false;
            uint temp = 0;
            int nRet = m_pOperator.GetIntMinValue(strKey, ref temp);
            nMinValue = (uint)temp;
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool GetIntMaxValue(string strKey, out Int64 nMaxValue)
        {
            nMaxValue = 0;
            if (!CheckAccessMode(strKey, out MV_XML_AccessMode accessMode) || accessMode == MV_XML_AccessMode.AM_WO)
                return false;
            uint temp = 0;
            int nRet = m_pOperator.GetIntMaxValue(strKey, ref temp);
            nMaxValue = (uint)temp;
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool GetIntValue(string strKey, out Int64 nValue)
        {
            nValue = 0;
            if (!CheckAccessMode(strKey, out MV_XML_AccessMode accessMode) || accessMode == MV_XML_AccessMode.AM_WO)
                return false;
            uint temp = 0;
            int nRet = m_pOperator.GetIntValue(strKey, ref temp);
            nValue = (uint)temp;
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public string GetserialNumber()
        {
            string SerialNumber = "";
            if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(this.device.SpecialInfo.stGigEInfo, 0);
                MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));

                SerialNumber = gigeInfo.chSerialNumber;

            }
            else if (this.device.nTLayerType == MyCamera.MV_USB_DEVICE)

            {
                IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(this.device.SpecialInfo.stUsb3VInfo, 0);
                MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                SerialNumber = usbInfo.chSerialNumber;


            }
            return SerialNumber;

        }
        public string GetserialNumber(MyCamera.MV_CC_DEVICE_INFO deviceT)
        {
            string SerialNumber = "";
            if (deviceT.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(deviceT.SpecialInfo.stGigEInfo, 0);
                MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));

                SerialNumber = gigeInfo.chSerialNumber;

            }
            else if (deviceT.nTLayerType == MyCamera.MV_USB_DEVICE)
            {
                IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(deviceT.SpecialInfo.stUsb3VInfo, 0);
                MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                SerialNumber = usbInfo.chSerialNumber;

            }
            return SerialNumber;

        }

    }
}