using DeviceSource;
using MVSDK_Net;
using nrt;
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
using System.Windows.Threading;
using VisionInspection.src.comm;
using static MvCamCtrl.NET.MyCamera;
using static MVSDK_Net.IMVDefine;
using static MVSDK_Net.MyCamera;

namespace VisionInspection
{
    public class IraypleCam : ICamDevice
    {
        public CameraOperatorIr m_pOperator;
        public MVSDK_Net.IMVDefine.IMV_DeviceList m_pDeviceList;
        public MVSDK_Net.IMVDefine.IMV_DeviceInfo device;
        bool m_bGrabbing;
        private static IMVDefine.IMV_FrameCallBack frameCallBack;
        public ECamDevType eCamDevType { get; set; }
        public bool isOpen { get; set; }
        public DeviceList deviceList { get; set; }
        public string SerialNumber { get; set; }

        public IraypleCam()
        {
            InitializeCamera();
            eCamDevType = ECamDevType.Irayple;
        }
        public IraypleCam(IMVDefine.IMV_DeviceInfo device)
        {
            InitializeCamera();
            this.device = device;
            eCamDevType = ECamDevType.Irayple;
            SerialNumber = device.serialNumber;
        }


        public bool InitializeCamera()
        {
            m_pOperator = new CameraOperatorIr();
            m_bGrabbing = false;
            return true;
        }
        public DeviceList InitializeDeviceList()
        {
            IMV_DeviceInfo[] devices = new IMV_DeviceInfo[m_pDeviceList.nDevNum];
            int size = Marshal.SizeOf(typeof(IMV_DeviceInfo));

            DeviceList deviceList = new DeviceList();
            deviceList.devNum = m_pDeviceList.nDevNum;
            deviceList.devInfo = new IntPtr[m_pDeviceList.nDevNum];
            for (int i = 0; i < devices.Length; i++)
            {
                IntPtr ptr = IntPtr.Add(m_pDeviceList.pDevInfo, i * size);
                deviceList.devInfo[i] = ptr;
            }
            return deviceList;
        }
        public void DeviceListAcq()
        {
            int nRet;
            /*Create Device List*/
            System.GC.Collect();
            nRet = CameraOperatorIr.EnumDevices((uint)IMVDefine.IMV_EInterfaceType.interfaceTypeGige | (uint)IMVDefine.IMV_EInterfaceType.interfaceTypeUsb3, ref m_pDeviceList);

            deviceList = InitializeDeviceList();
            if (0 != nRet)
            {
                return;
            }
        }
        //public int Open(IMVDefine.IMV_DeviceInfo device)
        //{
        //    this.device = device;
        //    int ret = m_pOperator.Open(ref device);
        //    //m_pOperator.SetEnumValue("AcquisitionMode", 2);
        //    //m_pOperator.SetEnumValue("TriggerMode", 0);
        //    ret = StartGrab();
        //    if (ret == IMVDefine.IMV_OK)
        //    {
        //        isOpen = true;
        //    }
        //    else
        //    {
        //        isOpen = false;
        //    }
        //    return ret;
        //}
        public int Open()
        {
            int ret = m_pOperator.Open(ref this.device);
            ret += m_pOperator.SetEnumSymbol("TriggerSource", "Software");
            ret += m_pOperator.SetEnumSymbol("TriggerSelector", "FrameStart");
            ret += m_pOperator.SetEnumSymbol("TriggerMode", "On");
            ret += StartGrab();
            if (ret == IMVDefine.IMV_OK)
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
            if (IMVDefine.IMV_OK != nRet1)
            {
                return nRet1;
            }

            //nRet1 = m_pOperator.Display(img.Handle);

            //HwndSource hwndSource = PresentationSource.FromVisual(img) as HwndSource;

            //if (hwndSource != null)
            //{
            //    nRet1 = m_pOperator.Display(hwndSource.Handle);
            //}
            if (IMVDefine.IMV_OK != nRet1)
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
        public Mat CaptureImage()
        {
            int nRet;
            Mat imgMat = new Mat();
            IMVDefine.IMV_Frame frame = new IMVDefine.IMV_Frame();
            nRet = m_pOperator.ExecuteCommand("TriggerSoftware");
            if (IMVDefine.IMV_OK != nRet)
            {
                return null;
            }
            nRet = m_pOperator.GetOneFrame(ref frame, 1000);
            if (IMVDefine.IMV_OK != nRet)
            {
                return null;
            }
            int width = (int)frame.frameInfo.width;
            int height = (int)frame.frameInfo.height;
            IntPtr pData = frame.pData;
            IMVDefine.IMV_EPixelType pixelType = frame.frameInfo.pixelFormat;
            switch (pixelType)
            {
                case IMVDefine.IMV_EPixelType.gvspPixelMono8:
                    imgMat = new Mat(height, width, MatType.CV_8UC1, pData);
                    break;
                case IMVDefine.IMV_EPixelType.gvspPixelBGR8:
                //case IMVDefine.IMV_EPixelType.gvspPixelBayGB8:
                    imgMat = new Mat(height, width, MatType.CV_8UC3, pData);
                    break;
                case IMVDefine.IMV_EPixelType.gvspPixelRGBA8:
                    imgMat = new Mat(height, width, MatType.CV_8UC4, pData);
                    break;
                case IMV_EPixelType.gvspPixelBayGB8:
                    Mat bayerMat = new Mat(height, width, MatType.CV_8UC1, pData);
                    Cv2.CvtColor(bayerMat, imgMat, ColorConversionCodes.BayerGB2RGB);
                    break;
                default:
                    IntPtr pImage = Marshal.AllocHGlobal(width * height * 3);
                    IMVDefine.IMV_PixelConvertParam stPixelConvertParam = new IMVDefine.IMV_PixelConvertParam
                    {
                        nWidth = frame.frameInfo.width,
                        nHeight = frame.frameInfo.height,
                        ePixelFormat = frame.frameInfo.pixelFormat,
                        pSrcData = frame.pData,
                        nSrcDataLen = frame.frameInfo.size,
                        nPaddingX = frame.frameInfo.paddingX,
                        nPaddingY = frame.frameInfo.paddingY,
                        eBayerDemosaic = IMVDefine.IMV_EBayerDemosaic.demosaicNearestNeighbor,
                        eDstPixelFormat = IMVDefine.IMV_EPixelType.gvspPixelBGR8,
                        pDstBuf = pImage,
                        nDstBufSize = frame.frameInfo.width * frame.frameInfo.height * 3
                    };
                    int res = m_pOperator.PixelConvert(ref stPixelConvertParam);
                    imgMat = (res == IMVDefine.IMV_OK) ? new Mat(height, width, MatType.CV_8UC3, pImage) : null;
                    break;
            }
            int a = m_pOperator.ReleaseFrame(ref frame);
            return imgMat;
        }

        public bool SetExposeTime(double Exp)
        {
            int nRet = m_pOperator.SetDoubleValue("ExposureTime", Exp);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool SetGain(double Gain)
        {
            int nRet = m_pOperator.SetDoubleValue("Gain", Gain);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool SetHeight(long Height)
        {
            int nRet = m_pOperator.SetIntValue("Height", Height);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool SetWidth(long Width)
        {
            int nRet = m_pOperator.SetIntValue("Width", Width);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool GetDoubleMinValue(string strKey, out double dMinValue)
        {
            dMinValue = 0d;
            int nRet = m_pOperator.GetDoubleMinValue(strKey, ref dMinValue);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool GetDoubleMaxValue(string strKey, out double dMaxValue)
        {
            dMaxValue = 0d;
            int nRet = m_pOperator.GetDoubleMaxValue(strKey, ref dMaxValue);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool GetDoubleValue(string strKey, out double dValue)
        {
            dValue = 0f;
            int nRet = m_pOperator.GetDoubleValue(strKey, ref dValue);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool GetIntMinValue(string strKey, out Int64 nMinValue)
        {
            nMinValue = 0;
            int nRet = m_pOperator.GetIntMinValue(strKey, ref nMinValue);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool GetIntMaxValue(string strKey, out Int64 nMaxValue)
        {
            nMaxValue = 0;
            int nRet = m_pOperator.GetIntMaxValue(strKey, ref nMaxValue);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public bool GetIntValue(string strKey, out Int64 nValue)
        {
            nValue = 0;
            int nRet = m_pOperator.GetIntValue(strKey, ref nValue);
            if (nRet != CameraOperator.CO_OK)
            {
                return false;
            }
            return true;
        }
        public string GetserialNumber()
        {
            return device.serialNumber;
        }
        public string GetserialNumber(IMVDefine.IMV_DeviceInfo deviceT)
        {
            return deviceT.serialNumber;
        }

    }
}