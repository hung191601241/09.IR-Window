using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionInspection
{
    public enum ECamDevType
    {
        None = -1,
        Hikrobot = 0,
        Irayple = 1,
    }
    public interface ICamDevice
    {
        ECamDevType eCamDevType { get; set; }
        bool isOpen { get; set; }
        DeviceList deviceList { get; set; }
        string SerialNumber { get; set; }
        bool InitializeCamera();
        DeviceList InitializeDeviceList();
        void DeviceListAcq();
        int Open();
        bool IsOpen();
        int Close();
        int DisPose();
        int StartGrab();
        int StopGrab();
        Mat CaptureImage();
        bool SetExposeTime(double exp);
        bool SetGain(double gain);
        bool SetHeight(long height);
        bool SetWidth(long width);
        bool GetDoubleMinValue(string strKey, out double dMinValue);
        bool GetDoubleMaxValue(string strKey, out double dMaxValue);
        bool GetDoubleValue(string strKey, out double dValue);
        bool GetIntMinValue(string strKey, out Int64 nMinValue);
        bool GetIntMaxValue(string strKey, out Int64 nMaxValue);
        bool GetIntValue(string strKey, out Int64 nValue);
        string GetserialNumber();
    }
    public class DeviceList
    {
        public uint devNum;

        public IntPtr[] devInfo;
        public DeviceList()
        {
            devNum = 0;
            devInfo = null;
        }
        public DeviceList(uint devNum, IntPtr[] devInfo)
        {
            this.devNum = devNum;
            this.devInfo = devInfo;
        }
    }
}
