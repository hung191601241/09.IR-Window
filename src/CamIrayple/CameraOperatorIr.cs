/***************************************************************************************************
***************************************************************************************************/
using MVSDK_Net;
using nrt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;


namespace DeviceSource
{
    using static MVSDK_Net.IMVDefine;
    using static MVSDK_Net.MyCamera;
    //using ExceptionCallBack = MyCamera.cbExceptiondelegate;
    using ImageCallBack = IMVDefine.IMV_FrameCallBack;

    public class CameraOperatorIr
    {
        public const int CO_FAIL = -1;
        public const int CO_OK = 0;
        private MyCamera m_pCSI;
        //private delegate void ImageCallBack(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO pFrameInfo, IntPtr pUser);

        public CameraOperatorIr()
        {
            // m_pDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
            m_pCSI = new MyCamera();
        }

        /****************************************************************************
         * @fn           EnumDevices
         * @brief        Enumerate devices
         * @param        nLayerType       IN         Transport layer protocol: 1-GigE; 4-USB; Can be stacked
         * @param        stDeviceList     OUT        Device List
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public static int EnumDevices(uint nLayerType, ref IMVDefine.IMV_DeviceList stDeviceList)
        {
            return MyCamera.IMV_EnumDevices(ref stDeviceList, nLayerType);
        }
        /****************************************************************************
         * @fn           Open
         * @brief        Open Device
         * @param        stDeviceInfo       IN       Device Information Structure
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int Open(ref IMVDefine.IMV_DeviceInfo stDeviceInfo)
        {
            if (null == m_pCSI)
            {
                m_pCSI = new MyCamera();
                if (null == m_pCSI)
                {
                    return CO_FAIL;
                }
            }

            int nRet;
            //nRet = m_pCSI.IMV_CreateHandle(ref stDeviceInfo);
            nRet = m_pCSI.IMV_CreateHandle(IMVDefine.IMV_ECreateHandleMode.modeByCameraKey, cameraStr: stDeviceInfo.cameraKey);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            nRet = m_pCSI.IMV_Open();
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            nRet = m_pCSI.IMV_SetEnumFeatureSymbol("TriggerMode", "Off");
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }


        /****************************************************************************
         * @fn           Close
         * @brief        Close Device
         * @param        none
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int Close()
        {
            int nRet;
            nRet = m_pCSI.IMV_Close();
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }

            nRet = m_pCSI.IMV_DestroyHandle();
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }


        /****************************************************************************
         * @fn           StartGrabbing
         * @brief        Start Grabbing
         * @param        none
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int StartGrabbing()
        {
            int nRet;
            //Start Grabbing
            nRet = m_pCSI.IMV_StartGrabbing();
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }



        /****************************************************************************
         * @fn           StopGrabbing
         * @brief        Stop Grabbing
         * @param        none
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int StopGrabbing()
        {
            int nRet;
            nRet = m_pCSI.IMV_StopGrabbing();
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }


        /****************************************************************************
         * @fn           RegisterImageCallBack
         * @brief        Register Image CallBack Function
         * @param        CallBackFunc          IN        CallBack Function
         * @param        pUser                 IN        User Parameters
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int RegisterImageCallBack(ImageCallBack CallBackFunc, IntPtr pUser)
        {
            int nRet;
            nRet = m_pCSI.IMV_AttachGrabbing(CallBackFunc, pUser);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }


        /****************************************************************************
         * @fn           RegisterExceptionCallBack
         * @brief        Register Exception CallBack Function
         * @param        CallBackFunc          IN        CallBack Function
         * @param        pUser                 IN        User Parameters
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        //public int RegisterExceptionCallBack(ExceptionCallBack CallBackFunc, IntPtr pUser)
        //{
        //    int nRet;
        //    nRet = m_pCSI.MV_CC_RegisterExceptionCallBack_NET(CallBackFunc, pUser);
        //    if (IMVDefine.IMV_OK != nRet)
        //    {
        //        return CO_FAIL;
        //    }
        //    return CO_OK;
        //}


        /****************************************************************************
         * @fn           GetOneFrame
         * @brief        Get one frame image data
         * @param        pData                 IN-OUT            Data Array Pointer
         * @param        pnDataLen             IN                Date Size
         * @param        nDataSize             IN                Array Buffer Size
         * @param        pFrameInfo            OUT               Data Information
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int ExecuteCommand(string command)
        {
            int nRet = m_pCSI.IMV_ExecuteCommandFeature(command);
            if (IMVDefine.IMV_OK != nRet)
            {
                return nRet;
            }


            return nRet;
        }
        public int GetOneFrame(ref IMVDefine.IMV_Frame pFrameInfo, uint timeout)
        {
            int nRet = m_pCSI.IMV_GetFrame(ref pFrameInfo, timeout);
            if (IMVDefine.IMV_OK != nRet)
            {
                return nRet;
            }


            return nRet;
        }
        public int PixelConvert(ref IMVDefine.IMV_PixelConvertParam pstPixelConvertParam)
        {
            int nRet = m_pCSI.IMV_PixelConvert(ref pstPixelConvertParam);
            if (IMVDefine.IMV_OK != nRet)
            {
                return nRet;
            }

            return nRet;
        }
        public int ReleaseFrame(ref IMVDefine.IMV_Frame pFrameInfo)
        {
            int nRet = m_pCSI.IMV_ReleaseFrame(ref pFrameInfo);
            nRet += m_pCSI.IMV_ClearFrameBuffer();
            if (IMVDefine.IMV_OK != nRet)
            {
                return nRet;
            }

            return nRet;
        }

        /****************************************************************************
         * @fn           Display
         * @brief        Display Image
         * @param        hWnd                  IN        Windows Handle
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        private static void displayDeviceInfo(IMVDefine.IMV_DeviceList deviceInfoList)
        {

            Console.WriteLine("Enum device size : {0}\n", deviceInfoList.nDevNum);
            for (int i = 0; i < deviceInfoList.nDevNum; i++)
            {
                IMVDefine.IMV_DeviceInfo deviceInfo =
                    (IMVDefine.IMV_DeviceInfo)
                        Marshal.PtrToStructure(
                            deviceInfoList.pDevInfo + Marshal.SizeOf(typeof(IMVDefine.IMV_DeviceInfo)) * i,
                            typeof(IMVDefine.IMV_DeviceInfo));

                // 相机设备列表的索引
                // Device index in device list
                Console.WriteLine("Camera index : {0}", i);
                // 接口类型（GigE，U3V，CL，PCIe）
                // Interface type 
                Console.WriteLine("nInterfaceType : {0}", deviceInfo.nInterfaceType);
                // 设备ID信息
                // Device ID
                Console.WriteLine("cameraKey : {0}", deviceInfo.cameraKey);
                // 设备的型号信息
                // Device model name
                Console.WriteLine("modelName : {0}", deviceInfo.modelName);
                // 设备的序列号
                // Device serial number
                Console.WriteLine("serialNumber : {0}", deviceInfo.serialNumber);
                // 设备的自定义名称
                // DeviceUserID 
                Console.WriteLine("DeviceUserID : {0}", deviceInfo.cameraName);
                if (deviceInfo.nCameraType == IMVDefine.IMV_ECameraType.typeGigeCamera)
                {

                    IMVDefine.IMV_GigEDeviceInfo gigEDeviceInfo =
                        (IMVDefine.IMV_GigEDeviceInfo)
                            ByteToStruct(deviceInfo.deviceSpecificInfo.gigeDeviceInfo,
                                typeof(IMVDefine.IMV_GigEDeviceInfo));

                    Console.WriteLine("ipAddress : {0}", gigEDeviceInfo.ipAddress);
                }
                Console.WriteLine();
            }

        }
        public static object ByteToStruct(Byte[] bytes, Type type)
        {
            int size = Marshal.SizeOf(type);
            if (size > bytes.Length)
            {
                return null;
            }

            // 分配结构体内存空间
            IntPtr structPtr = Marshal.AllocHGlobal(size);

            // 将byte数组拷贝到分配好的内存空间
            Marshal.Copy(bytes, 0, structPtr, size);

            // 将内存空间转换为目标结构体
            object obj = Marshal.PtrToStructure(structPtr, type);

            // 释放内存空间
            Marshal.FreeHGlobal(structPtr);

            return obj;
        }


        /****************************************************************************
         * @fn           GetIntValue
         * @brief        Get Int Type Paremeters Value
         * @param        strKey                IN        Parameters key value, for detail value name please refer to HikCameraNode.xls
         * @param        pnValue               OUT       Return Value
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int GetIntValue(string strKey, ref Int64 pnValue)
        {
            int nRet = m_pCSI.IMV_GetIntFeatureValue(strKey, ref pnValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }
        public int GetIntMinValue(string strKey, ref Int64 nMinValue)
        {
            int nRet = m_pCSI.IMV_GetIntFeatureMin(strKey, ref nMinValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }
        public int GetIntMaxValue(string strKey, ref Int64 nMaxValue)
        {
            int nRet = m_pCSI.IMV_GetIntFeatureMax(strKey, ref nMaxValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }

            return CO_OK;
        }


        /****************************************************************************
         * @fn           SetIntValue
         * @brief        Set Int Type Paremeters Value
         * @param        strKey                IN        Parameters key value, for detail value name please refer to HikCameraNode.xls
         * @param        nValue                IN        Set parameters value, for specific value range please refer to HikCameraNode.xls
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int SetIntValue(string strKey, Int64 nValue)
        {
            int nRet = m_pCSI.IMV_SetIntFeatureValue(strKey, nValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }



        /****************************************************************************
         * @fn           GetFloatValue
         * @brief        Get Floot Type Paremeters Value
         * @param        strKey                IN        Parameters key value, for detail value name please refer to HikCameraNode.xls
         * @param        pValue                OUT       Return Value
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int GetDoubleValue(string strKey, ref double pdValue)
        {
            int nRet = m_pCSI.IMV_GetDoubleFeatureValue(strKey, ref pdValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }
        public int GetDoubleMinValue(string strKey, ref double dMinValue)
        {
            int nRet = m_pCSI.IMV_GetDoubleFeatureMin(strKey, ref dMinValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }
        public int GetDoubleMaxValue(string strKey, ref double dMaxValue)
        {
            int nRet = m_pCSI.IMV_GetDoubleFeatureMax(strKey, ref dMaxValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }


        /****************************************************************************
         * @fn           SetFloatValue
         * @brief        Set Floot Type Paremeters Value
         * @param        strKey                IN        Parameters key value, for detail value name please refer to HikCameraNode.xls
         * @param        fValue                IN        Set parameters value, for specific value range please refer to HikCameraNode.xls
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int SetDoubleValue(string strKey, double dValue)
        {
            int nRet = m_pCSI.IMV_SetDoubleFeatureValue(strKey, dValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }


        /****************************************************************************
         * @fn           GetEnumValue
         * @brief        Get Enum Type Paremeters Value
         * @param        strKey                IN        Parameters key value, for detail value name please refer to HikCameraNode.xls
         * @param        pnValue               OUT       Return Value
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int GetEnumValue(string strKey, ref UInt64 pnValue)
        {
            int nRet = m_pCSI.IMV_GetEnumFeatureValue(strKey, ref pnValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }
        public int GetEnumSymbol(string strKey, ref IMV_String strValue)
        {
            int nRet = m_pCSI.IMV_GetEnumFeatureSymbol(strKey, ref strValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }


        /****************************************************************************
         * @fn           SetEnumValue
         * @brief        Set Enum Type Paremeters Value
         * @param        strKey                IN        Parameters key value, for detail value name please refer to HikCameraNode.xls
         * @param        nValue                IN        Set parameters value, for specific value range please refer to HikCameraNode.xls
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int SetEnumValue(string strKey, UInt64 nValue)
        {
            int nRet = m_pCSI.IMV_SetEnumFeatureValue(strKey, nValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }
        public int SetEnumSymbol(string strKey, string strValue)
        {
            int nRet = m_pCSI.IMV_SetEnumFeatureSymbol(strKey, strValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }



        /****************************************************************************
         * @fn           GetBoolValue
         * @brief        Get Bool Type Paremeters Value
         * @param        strKey                IN        Parameters key value, for detail value name please refer to HikCameraNode.xls
         * @param        pbValue               OUT       Return Value
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int GetBoolValue(string strKey, ref bool pbValue)
        {
            int nRet = m_pCSI.IMV_GetBoolFeatureValue(strKey, ref pbValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }

            return CO_OK;
        }


        /****************************************************************************
         * @fn           SetBoolValue
         * @brief        Set Bool Type Paremeters Value
         * @param        strKey                IN        Parameters key value, for detail value name please refer to HikCameraNode.xls
         * @param        bValue                IN        Set parameters value, for specific value range please refer to HikCameraNode.xls
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int SetBoolValue(string strKey, bool bValue)
        {
            int nRet = m_pCSI.IMV_SetBoolFeatureValue(strKey, bValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }


        /****************************************************************************
         * @fn           GetStringValue
         * @brief        Get String Type Paremeters Value
         * @param        strKey                IN        Parameters key value, for detail value name please refer to HikCameraNode.xls
         * @param        strValue              OUT       Return Value
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int GetStringValue(string strKey, ref IMVDefine.IMV_String strValue)
        {
            int nRet = m_pCSI.IMV_GetStringFeatureValue(strKey, ref strValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }


        /****************************************************************************
         * @fn           SetStringValue
         * @brief        Set String Type Paremeters Value
         * @param        strKey                IN        Parameters key value, for detail value name please refer to HikCameraNode.xls
         * @param        strValue              IN        Set parameters value, for specific value range please refer to HikCameraNode.xls
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int SetStringValue(string strKey, string strValue)
        {
            int nRet = m_pCSI.IMV_SetStringFeatureValue(strKey, strValue);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }


        /****************************************************************************
         * @fn           CommandExecute
         * @brief        Command
         * @param        strKey                IN        Parameters key value, for detail value name please refer to HikCameraNode.xls
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int CommandExecute(string strKey)
        {
            int nRet = m_pCSI.IMV_ExecuteCommandFeature(strKey);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }


        /****************************************************************************
         * @fn           SaveImage
         * @brief        Save Image
         * @param        pSaveParam            IN        Save image configure parameters structure 
         * @return       Success:0; Fail:-1
         ****************************************************************************/
        public int SaveImage(ref IMVDefine.IMV_SaveImageToFileParam pSaveParam)
        {
            int nRet;
            nRet = m_pCSI.IMV_SaveImageToFile(ref pSaveParam);
            return nRet;
        }
        public int DisPose()
        {
            int nRet;
            nRet = m_pCSI.IMV_DestroyHandle();
            return nRet;
        }

        public int AccessMode(out IMVDefine.IMV_ECameraAccessPermission accessMode)
        {
            accessMode = IMV_ECameraAccessPermission.accessPermissionUndefined;
            int nRet = m_pCSI.IMV_GIGE_GetAccessPermission(ref accessMode);
            if (IMVDefine.IMV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }
    }
}
