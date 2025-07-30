using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media.TextFormatting;
using AutoLaserCuttingInput;
using Development;
using MvCamCtrl.NET;
using MVSDK_Net;
using Newtonsoft.Json;
using OpenCvSharp;
using VisionInspection;
using VisionTools.ToolEdit;
using static VisionTools.ToolEdit.AcquisitionEdit;
using static VisionTools.ToolEdit.BlobEdit;
using static VisionTools.ToolEdit.ImageProcessEdit;
using static VisionTools.ToolEdit.TemplateMatchEdit;
using Point = System.Windows.Point;

namespace ITM_Semiconductor
{
    class AppSettings
    {
        //PLC
        public SettingDevice settingDevice;
        public SaveDevice selectDevice;

        //MES Vision
        public SettingDevice settingDeviceMesVs;
        public string currentModel { get; set; } // Machine Run Model

        public const string SETTING_FILE_NAME = "appsetting.json";
        private const String DEFAULT_PASSWORD = "itm";
        public String PassWordEN { get; set; }
        public String PassWordADM { get; set; }
        public String UseName { get; set; }
        public const String DEFAULT_USER_NAME = "Operator";
        public string Operator { get; set; }
        public ConnectionSettings connection { get; set; }

        // Setting Scanner

        private ModelSetting settingModel;

        public MesSettings MesSettings { get; set; } = new MesSettings();

        // Setting Scanner


        public RunSettings run { get; set; }
        public ModelSetting SettingModel { get => settingModel; set => settingModel = value; }

        public LotInData lotData { get; set; }

        public FTPClientSettings FTPClientSettings { get; set; }

        public MotorParameter Robot { get; set; }

        public Model CurrentModel { get; set; }
        public Model M01 { get; set; }
        public Model M02 { get; set; }
        public Model M03 { get; set; }

        public Model M04 { get; set; }
        public Model M05 { get; set; }
        public Model M06 { get; set; }
        public ROIProperty Property { get; set; }
        public bool caseShowDataMatrixRT { get; set; } = false;


        //Update User
        public UserID user { get; set; }

        public communication PLCTCP { get; set; }

        public MechanicalJig Jig { get; set; }

        //Vision Program
        public VisionProgram[] vsPrograms { get; set; }
        public CommProperty commProperty { get; set; }

        public AppSettings()
        {
            this.settingDevice = new SettingDevice();
            this.selectDevice = SaveDevice.Mitsubishi_MC_Protocol_Binary_TCP;
            this.settingDeviceMesVs = new SettingDevice();

            this.currentModel = "Default";
            this.Jig = new MechanicalJig();
            this.user = new UserID();
            this.PLCTCP = new communication();
            this.lotData = new LotInData();
            this.UseName = DEFAULT_USER_NAME;
            this.Operator = DEFAULT_USER_NAME;

            this.connection = new ConnectionSettings();

            this.SettingModel = new ModelSetting();

            this.run = new RunSettings();

            this.MesSettings = new MesSettings();

            this.FTPClientSettings = new FTPClientSettings();

            this.Robot = new MotorParameter();

            this.M01 = new Model();
            this.M02 = new Model();
            this.M03 = new Model();
            this.M04 = new Model();
            this.M05 = new Model();
            this.M06 = new Model();
            this.CurrentModel = new Model();
            this.Property = new ROIProperty();
            //Số Job
            this.vsPrograms = new VisionProgram[5];
            this.commProperty = new CommProperty();
        }
        public string TOJSON()
        {
            string retValue = "";
            retValue = JsonConvert.SerializeObject(this, Formatting.Indented);
            return retValue;
        }
        public static AppSettings FromJSON(String json)
        {

            var _appSettings = JsonConvert.DeserializeObject<AppSettings>(json);
            if (_appSettings.Jig == null)
            {
                _appSettings.Jig = new MechanicalJig();
            }
            if (String.IsNullOrEmpty(_appSettings.currentModel))
            {
                _appSettings.currentModel = "Default";
            }
            if (_appSettings.PLCTCP == null)
            {
                _appSettings.PLCTCP = new communication();
            }
            if (String.IsNullOrEmpty(_appSettings.PassWordEN))
            {
                _appSettings.PassWordEN = DEFAULT_PASSWORD;
            }
            if (String.IsNullOrEmpty(_appSettings.PassWordADM))
            {
                _appSettings.PassWordADM = DEFAULT_PASSWORD;
            }

            if (_appSettings.connection == null)
            {
                _appSettings.connection = new ConnectionSettings();
            }

            if (_appSettings.run == null)
            {
                _appSettings.run = new RunSettings();
            }
            if (_appSettings.SettingModel == null)
            {
                _appSettings.SettingModel = new ModelSetting();
            }
            if (_appSettings.MesSettings == null)
            {
                _appSettings.MesSettings = new MesSettings();
            }
            if (_appSettings.lotData == null)
            {
                _appSettings.lotData = new LotInData();
            }
            if (_appSettings.FTPClientSettings == null)
            {
                _appSettings.FTPClientSettings = new FTPClientSettings();
            }
            if (_appSettings.M01 == null)
            {
                _appSettings.M01 = new Model();
            }
            if (_appSettings.M02 == null)
            {
                _appSettings.M02 = new Model();
            }
            if (_appSettings.M03 == null)
            {
                _appSettings.M03 = new Model();
            }
            if (_appSettings.M04 == null)
            {
                _appSettings.M04 = new Model();
            }
            if (_appSettings.M05 == null)
            {
                _appSettings.M05 = new Model();
            }
            if (_appSettings.M06 == null)
            {
                _appSettings.M06 = new Model();
            }
            if (_appSettings.CurrentModel == null)
            {
                _appSettings.CurrentModel = new Model();
            }
            if (_appSettings.Property == null)
            {
                _appSettings.Property = new ROIProperty();
            }
            if (_appSettings.vsPrograms == null || _appSettings.vsPrograms.Length < 5)
            {
                _appSettings.vsPrograms = new VisionProgram[5];
            }
            for (int i = 0; i < _appSettings.vsPrograms.Length; i++)
            {
                if (_appSettings.vsPrograms[i] == null)
                    _appSettings.vsPrograms[i] = new VisionProgram() { NameJob = $"Job{i + 1}" };
            }
            return _appSettings;
        }
    }
    #region MotorParameter
    class MotorParameter
    {
        public int XaxisJogSpeed { get; set; }
        public int ZaxisJogSpeed { get; set; }
        public TeachDataXaxis TeachDataXaxis { get; set; }
        public TeachDataYaxis TeachDataYaxis { get; set; }
        public TeachDataYaxis1 TeachDataYaxis1 { get; set; }
        public TeachSpeedYaxis TeachSpeedYaxis { get; set; }
        public TeachSpeedYaxis1 TeachSpeedYaxis1 { get; set; }
        public TeachDataXaxis1 TeachDataXaxis1 { get; set; }
        public TeachDataZaxis TeachDataZaxis { get; set; }
        public TeachDataZaxis1 TeachDataZaxis1 { get; set; }
        public MotorParameter()
        {
            this.XaxisJogSpeed = 1000;
            this.ZaxisJogSpeed = 1000;
            this.TeachDataXaxis = new TeachDataXaxis();
            this.TeachDataZaxis = new TeachDataZaxis();
            this.TeachDataXaxis1 = new TeachDataXaxis1();
            this.TeachDataZaxis1 = new TeachDataZaxis1();
            this.TeachDataYaxis = new TeachDataYaxis();
            this.TeachDataYaxis1 = new TeachDataYaxis1();
            this.TeachSpeedYaxis = new TeachSpeedYaxis();
            this.TeachSpeedYaxis1 = new TeachSpeedYaxis1();
        }
        public MotorParameter Clone()
        {
            return new MotorParameter()
            {
                XaxisJogSpeed = this.XaxisJogSpeed,
                ZaxisJogSpeed = this.ZaxisJogSpeed,
                TeachDataXaxis = this.TeachDataXaxis.Clone(),
                TeachDataZaxis = this.TeachDataZaxis.Clone(),
                TeachDataXaxis1 = this.TeachDataXaxis1.Clone(),
                TeachDataZaxis1 = this.TeachDataZaxis1.Clone(),
                TeachDataYaxis = this.TeachDataYaxis.Clone(),
                TeachDataYaxis1 = this.TeachDataYaxis1.Clone(),
                TeachSpeedYaxis = this.TeachSpeedYaxis.Clone(),
                TeachSpeedYaxis1 = this.TeachSpeedYaxis1.Clone()
            };
        }

    }
    class TeachDataXaxis
    {

        public int BendingPos { get; set; }
        public int readyPos { get; set; }
        public int QrPos { get; set; }
        public int QrScapPos { get; set; }
        public int MesScapPos { get; set; }
        public int MatingPos { get; set; }
        public int BendingPosSpeed { get; set; }
        public int readyPosSpeed { get; set; }
        public int QrPosSpeed { get; set; }
        public int QrScapPosSpeed { get; set; }
        public int MesScapPosSpeed { get; set; }
        public int MatingPosSpeed { get; set; }
        public TeachDataXaxis()
        {
            this.BendingPos = 0;
            this.readyPos = 0;
            this.QrPos = 0;
            this.QrScapPos = 0;
            this.MesScapPos = 0;
            this.MatingPos = 0;
            this.BendingPosSpeed = 0;
            this.readyPosSpeed = 0;
            this.QrPosSpeed = 0;
            this.QrScapPosSpeed = 0;
            this.MesScapPosSpeed = 0;
            this.MatingPosSpeed = 0;
        }
        public TeachDataXaxis Clone()
        {
            return new TeachDataXaxis
            {
                BendingPos = this.BendingPos,
                readyPos = this.readyPos,
                QrPos = this.QrPos,
                QrScapPos = this.QrScapPos,
                MesScapPos = this.MesScapPos,
                MatingPos = this.MatingPos
            };
        }
    }
    class TeachDataYaxis
    {

        public int BendingPos { get; set; }
        public int readyPos { get; set; }
        public int QrPos { get; set; }
        public int QrScapPos { get; set; }
        public int MesScapPos { get; set; }
        public int MatingPos { get; set; }
        public int BendingPosSpeed { get; set; }
        public int readyPosSpeed { get; set; }
        public int QrPosSpeed { get; set; }
        public int QrScapPosSpeed { get; set; }
        public int MesScapPosSpeed { get; set; }
        public int MatingPosSpeed { get; set; }
        public TeachDataYaxis()
        {
            this.BendingPos = 0;
            this.readyPos = 0;
            this.QrPos = 0;
            this.QrScapPos = 0;
            this.MesScapPos = 0;
            this.MatingPos = 0;
            this.BendingPosSpeed = 0;
            this.readyPosSpeed = 0;
            this.QrPosSpeed = 0;
            this.QrScapPosSpeed = 0;
            this.MesScapPosSpeed = 0;
            this.MatingPosSpeed = 0;
        }
        public TeachDataYaxis Clone()
        {
            return new TeachDataYaxis
            {
                BendingPos = this.BendingPos,
                readyPos = this.readyPos,
                QrPos = this.QrPos,
                QrScapPos = this.QrScapPos,
                MesScapPos = this.MesScapPos,
                MatingPos = this.MatingPos
            };
        }
    }
    class TeachDataYaxis1
    {

        public int BendingPos { get; set; }
        public int readyPos { get; set; }
        public int QrPos { get; set; }
        public int QrScapPos { get; set; }
        public int MesScapPos { get; set; }
        public int MatingPos { get; set; }
        public int BendingPosSpeed { get; set; }
        public int readyPosSpeed { get; set; }
        public int QrPosSpeed { get; set; }
        public int QrScapPosSpeed { get; set; }
        public int MesScapPosSpeed { get; set; }
        public int MatingPosSpeed { get; set; }
        public TeachDataYaxis1()
        {
            this.BendingPos = 0;
            this.readyPos = 0;
            this.QrPos = 0;
            this.QrScapPos = 0;
            this.MesScapPos = 0;
            this.MatingPos = 0;
            this.BendingPosSpeed = 0;
            this.readyPosSpeed = 0;
            this.QrPosSpeed = 0;
            this.QrScapPosSpeed = 0;
            this.MesScapPosSpeed = 0;
            this.MatingPosSpeed = 0;
        }
        public TeachDataYaxis1 Clone()
        {
            return new TeachDataYaxis1
            {
                BendingPos = this.BendingPos,
                readyPos = this.readyPos,
                QrPos = this.QrPos,
                QrScapPos = this.QrScapPos,
                MesScapPos = this.MesScapPos,
                MatingPos = this.MatingPos
            };
        }
    }
    class TeachDataXaxis1
    {

        public int BendingPos1 { get; set; }
        public int readyPos1 { get; set; }
        public int QrPos1 { get; set; }
        public int QrScapPos1 { get; set; }
        public int MesScapPos1 { get; set; }
        public int MatingPos1 { get; set; }
        public int BendingPosSpeed1 { get; set; }
        public int readyPosSpeed1 { get; set; }
        public int QrPosSpeed1 { get; set; }
        public int QrScapPosSpeed1 { get; set; }
        public int MesScapPosSpeed1 { get; set; }
        public int MatingPosSpeed1 { get; set; }
        public TeachDataXaxis1()
        {
            this.BendingPos1 = 0;
            this.readyPos1 = 0;
            this.QrPos1 = 0;
            this.QrScapPos1 = 0;
            this.MesScapPos1 = 0;
            this.MatingPos1 = 0;
            this.BendingPosSpeed1 = 0;
            this.readyPosSpeed1 = 0;
            this.QrPosSpeed1 = 0;
            this.QrScapPosSpeed1 = 0;
            this.MesScapPosSpeed1 = 0;
            this.MatingPosSpeed1 = 0;
        }
        public TeachDataXaxis1 Clone()
        {
            return new TeachDataXaxis1
            {
                BendingPos1 = this.BendingPos1,
                readyPos1 = this.readyPos1,
                QrPos1 = this.QrPos1,
                QrScapPos1 = this.QrScapPos1,
                MesScapPos1 = this.MesScapPos1,
                MatingPos1 = this.MatingPos1
            };
        }
    }
    class TeachDataZaxis
    {
        public int BendingPos { get; set; }
        public int readyPos { get; set; }
        public int QrPos { get; set; }
        public int QrScapPos { get; set; }
        public int MesScapPos { get; set; }
        public int MatingPos { get; set; }
        public int BendingPosSpeed { get; set; }
        public int readyPosSpeed { get; set; }
        public int QrPosSpeed { get; set; }
        public int QrScapPosSpeed { get; set; }
        public int MesScapPosSpeed { get; set; }
        public int MatingPosSpeed { get; set; }
        public TeachDataZaxis()
        {
            this.BendingPos = 0;
            this.readyPos = 0;
            this.QrPos = 0;
            this.QrScapPos = 0;
            this.MesScapPos = 0;
            this.MatingPos = 0;
            this.BendingPosSpeed = 0;
            this.readyPosSpeed = 0;
            this.QrPosSpeed = 0;
            this.QrScapPosSpeed = 0;
            this.MesScapPosSpeed = 0;
            this.MatingPosSpeed = 0;
        }
        public TeachDataZaxis Clone()
        {
            return new TeachDataZaxis
            {
                BendingPos = this.BendingPos,
                readyPos = this.readyPos,
                QrPos = this.QrPos,
                QrScapPos = this.QrScapPos,
                MesScapPos = this.MesScapPos,
                MatingPos = this.MatingPos
            };
        }
    }
    class TeachDataZaxis1
    {
        public int BendingPos1 { get; set; }
        public int readyPos1 { get; set; }
        public int QrPos1 { get; set; }
        public int QrScapPos1 { get; set; }
        public int MesScapPos1 { get; set; }
        public int MatingPos1 { get; set; }
        public int BendingPosSpeed1 { get; set; }
        public int readyPosSpeed1 { get; set; }
        public int QrPosSpeed1 { get; set; }
        public int QrScapPosSpeed1 { get; set; }
        public int MesScapPosSpeed1 { get; set; }
        public int MatingPosSpeed1 { get; set; }
        public TeachDataZaxis1()
        {
            this.BendingPos1 = 0;
            this.readyPos1 = 0;
            this.QrPos1 = 0;
            this.QrScapPos1 = 0;
            this.MesScapPos1 = 0;
            this.MatingPos1 = 0;
            this.BendingPosSpeed1 = 0;
            this.readyPosSpeed1 = 0;
            this.QrPosSpeed1 = 0;
            this.QrScapPosSpeed1 = 0;
            this.MesScapPosSpeed1 = 0;
            this.MatingPosSpeed1 = 0;
        }
        public TeachDataZaxis1 Clone()
        {
            return new TeachDataZaxis1
            {
                BendingPos1 = this.BendingPos1,
                readyPos1 = this.readyPos1,
                QrPos1 = this.QrPos1,
                QrScapPos1 = this.QrScapPos1,
                MesScapPos1 = this.MesScapPos1,
                MatingPos1 = this.MatingPos1
            };
        }
    }
    class TeachSpeedYaxis
    {
        public int BendingPos { get; set; }
        public int readyPos { get; set; }
        public int QrPos { get; set; }
        public int QrScapPos { get; set; }
        public int MesScapPos { get; set; }
        public int MatingPos { get; set; }
        public int BendingPosSpeed { get; set; }
        public int readyPosSpeed { get; set; }
        public int QrPosSpeed { get; set; }
        public int QrScapPosSpeed { get; set; }
        public int MesScapPosSpeed { get; set; }
        public int MatingPosSpeed { get; set; }
        public TeachSpeedYaxis()
        {
            this.BendingPos = 0;
            this.readyPos = 0;
            this.QrPos = 0;
            this.QrScapPos = 0;
            this.MesScapPos = 0;
            this.MatingPos = 0;
            this.BendingPosSpeed = 0;
            this.readyPosSpeed = 0;
            this.QrPosSpeed = 0;
            this.QrScapPosSpeed = 0;
            this.MesScapPosSpeed = 0;
            this.MatingPosSpeed = 0;
        }
        public TeachSpeedYaxis Clone()
        {
            return new TeachSpeedYaxis
            {
                BendingPos = this.BendingPos,
                readyPos = this.readyPos,
                QrPos = this.QrPos,
                QrScapPos = this.QrScapPos,
                MesScapPos = this.MesScapPos,
                MatingPos = this.MatingPos
            };
        }
    }
    class TeachSpeedYaxis1
    {
        public int BendingPos { get; set; }
        public int readyPos { get; set; }
        public int QrPos { get; set; }
        public int QrScapPos { get; set; }
        public int MesScapPos { get; set; }
        public int MatingPos { get; set; }
        public int BendingPosSpeed { get; set; }
        public int readyPosSpeed { get; set; }
        public int QrPosSpeed { get; set; }
        public int QrScapPosSpeed { get; set; }
        public int MesScapPosSpeed { get; set; }
        public int MatingPosSpeed { get; set; }
        public TeachSpeedYaxis1()
        {
            this.BendingPos = 0;
            this.readyPos = 0;
            this.QrPos = 0;
            this.QrScapPos = 0;
            this.MesScapPos = 0;
            this.MatingPos = 0;
            this.BendingPosSpeed = 0;
            this.readyPosSpeed = 0;
            this.QrPosSpeed = 0;
            this.QrScapPosSpeed = 0;
            this.MesScapPosSpeed = 0;
            this.MatingPosSpeed = 0;
        }
        public TeachSpeedYaxis1 Clone()
        {
            return new TeachSpeedYaxis1
            {
                BendingPos = this.BendingPos,
                readyPos = this.readyPos,
                QrPos = this.QrPos,
                QrScapPos = this.QrScapPos,
                MesScapPos = this.MesScapPos,
                MatingPos = this.MatingPos
            };
        }
    }
    #endregion
    class CamSettings
    {
        public String name { get; set; }
        public String fileConf { get; set; }

        public int ExposeTime { get; set; }

        //public MyCamera.MV_CC_DEVICE_INFO device { get; set; }
        public IMVDefine.IMV_DeviceInfo device { get; set; }

        public int OffsetAlignJigX { get; set; }
        public int OffsetAlignJigY { get; set; }
        public int mediumGrayVal { get; set; }
        public double scale { get; set; }



        public CamSettings()
        {
            this.name = "";
            this.fileConf = "";
            this.ExposeTime = 5000;
            this.OffsetAlignJigX = 10;
            this.OffsetAlignJigY = 10;
            this.mediumGrayVal = 10;
            this.scale = 0.12;
            //this.device = new MyCamera.MV_CC_DEVICE_INFO();
            this.device = new IMVDefine.IMV_DeviceInfo();

        }

        public CamSettings Clone()
        {
            return new CamSettings
            {
                OffsetAlignJigX = this.OffsetAlignJigX,
                OffsetAlignJigY = this.OffsetAlignJigY,
                mediumGrayVal = this.mediumGrayVal,
                name = String.Copy(this.name),
                fileConf = String.Copy(this.fileConf),
                scale = this.scale,
                device = this.device,
                ExposeTime = this.ExposeTime
            };
        }

    }
    class ImageSettings
    {
        public String CH1_path { get; set; }
        public String CH2_path { get; set; }

        public ImageSettings()
        {
            this.CH1_path = "CH1Path";
            this.CH2_path = "CH2Path";
        }

        public ImageSettings Clone()
        {
            return new ImageSettings
            {
                CH1_path = String.Copy(this.CH1_path),
                CH2_path = String.Copy(this.CH2_path)
            };
        }
    }
    class ConnectionSettings
    {
        public ScannerSettings scanner1 { get; set; }
        public ScannerSettings scanner2 { get; set; }
        public String VerUpDate { get; set; }
        public string DateUpdate { get; set; }
        public String modelName { get; set; }
        public String Recipe { get; set; }
        public String model { get; set; }
        public String model1 { get; set; }
        public String model2 { get; set; }
        public String model3 { get; set; }



        public CamSettings camera1 { get; set; }
        public CamSettings camera2 { get; set; }
        public CamSettings camera3 { get; set; }
        public CamSettings camera4 { get; set; }
        public ImageSettings image { get; set; }

        public string EquipmentName { get; set; }
        public ComSettings scanner { get; set; }
        public bool SelectModeCOM { get; set; }
        public ConnectionSettings()
        {
            this.scanner1 = new ScannerSettings();
            this.scanner2 = new ScannerSettings();

            this.DateUpdate = "2024-11-12";
            this.VerUpDate = "0";
            this.modelName = "Model Current";
            this.Recipe = "Machine";
            this.model = "X2833";
            this.model1 = "Model 01";
            this.model2 = "Model 02";
            this.model3 = "Model 03";

            this.EquipmentName = "MESITM001";
            this.scanner = new ComSettings();
            this.SelectModeCOM = false;

            this.camera1 = new CamSettings();
            this.camera2 = new CamSettings();
            this.camera3 = new CamSettings();
            this.camera4 = new CamSettings();
            this.image = new ImageSettings();

        }

        public ConnectionSettings Clone()
        {
            return new ConnectionSettings
            {
                scanner1 = this.scanner1.Clone(),
                scanner2 = this.scanner2.Clone(),

                modelName = String.Copy(this.modelName),
                model = String.Copy(this.model),

                camera1 = this.camera1.Clone(),
                camera2 = this.camera2.Clone(),
                camera3 = this.camera3.Clone(),
                camera4 = this.camera4.Clone(),
                image = this.image.Clone(),

                EquipmentName = string.Copy(EquipmentName),
                scanner = this.scanner.Clone(),
                SelectModeCOM = this.SelectModeCOM,
            };
        }
    }
    class RunSettings
    {
        public bool jamAction { get; set; } = true;
        public bool autoJigEnd { get; set; } = true;
        public bool qrCrossCheck { get; set; } = true;
        //public bool lotCheck { get; set; } = true;

        public bool mesOnline { get; set; } = true;

        public bool CheckMixLot { get; set; } = false;

        public bool ByPassVision { get; set; } = false;

        public bool CheckDoubleCode { get; set; } = true;
        public bool PackingOnline { get; set; } = false;
        public bool SortingMode { get; set; } = false;
        public bool PackingEnd { get; set; } = false;
        public bool PackingQROnline { get; set; } = false;
        //public bool testerOnline { get; set; } = true;
        public bool scannerOnline { get; set; } = true;
        public bool MachineQR { get; set; } = true;
        public int scannerNumberTrigger { get; set; } = 0;

        public RunSettings Clone()
        {
            return new RunSettings
            {
                MachineQR = this.MachineQR,
                jamAction = this.jamAction,
                autoJigEnd = this.autoJigEnd,
                PackingOnline = this.PackingOnline,
                PackingEnd = this.PackingEnd,
                qrCrossCheck = this.qrCrossCheck,
                mesOnline = this.mesOnline,
                scannerOnline = this.scannerOnline,
                scannerNumberTrigger = this.scannerNumberTrigger,
                CheckMixLot = this.CheckMixLot,
                CheckDoubleCode = this.CheckDoubleCode,
                ByPassVision = this.ByPassVision,
            };
        }


    }
    class MesSettings
    {
        public String localIp { get; set; }
        public int localPort { get; set; }

        public String MESName { get; set; }
        public bool Is_Enable_Log { get; set; } // Enable Write Log

        public MesSettings()
        {
            this.localIp = "192.168.1.2";
            this.localPort = 5010;
            this.MESName = "";
            this.Is_Enable_Log = true;
        }

        public MesSettings Clone()
        {
            return new MesSettings
            {
                localIp = String.Copy(this.localIp),
                localPort = this.localPort,
                MESName = string.Copy(this.MESName),
                Is_Enable_Log = this.Is_Enable_Log
            };
        }

    }
    public class LotInData
    {
        public String workGroup { get; set; } = "ITM";
        public String LotId { get; set; } = "";
        public String Config { get; set; } = "";
        public int lotQty { get; set; }


        public DateTime LotStart { get; set; }

        public String deviceId { get; set; } = "";
        public String lotId { get; set; } = "";

        public int lotCount { get; set; }
        public int QRNG { get; set; }

        public int QROK { get; set; }

        public LotInData Clone()
        {
            return new LotInData
            {
                workGroup = String.Copy(this.workGroup),
                LotId = String.Copy(this.LotId),
                Config = String.Copy(this.Config),
                lotQty = this.lotQty,


                deviceId = String.Copy(this.deviceId),
                lotId = String.Copy(this.lotId),

                lotCount = this.lotCount,
                QRNG = this.QRNG,
                QROK = this.QROK,
                LotStart = this.LotStart
            };
        }
    }
    public class FTPClientSettings
    {
        public string Image { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public String UserID { get; set; }
        public String PassWord { get; set; }
        public string FolderServer { get; set; }
        public FTPClientSettings()
        {
            this.Host = "192.168.54.217";
            this.Port = 38;
            this.UserID = "AUTOMATION_ITM";
            this.PassWord = "1";
            this.FolderServer = "/Project Hao";
            this.Image = "1.jpg";
        }
        public FTPClientSettings Clone()
        {
            return new FTPClientSettings()
            {
                Host = string.Copy(this.Host),
                Port = this.Port,
                UserID = string.Copy(this.UserID),
                PassWord = string.Copy(this.PassWord),
                FolderServer = string.Copy(this.FolderServer),
                Image = string.Copy(this.Image)
            };
        }
    }
    public class Model
    {
        public String Name { get; set; }
        public ROISettings ROI { get; set; }
        public int WhitePixels { get; set; }
        public int BlackPixels { get; set; }
        public int MatchingRate { get; set; }
        public int MatchingRateMin { get; set; }
        public int Threshol { get; set; }
        public int ThresholBl { get; set; }
        public bool CirWhCntEnb { get; set; }
        public bool RoiWhCntEnb { get; set; }
        public OpenCvSharp.Point BarCodeOffSet { get; set; }
        public Boolean OffSetJigEnb { get; set; }

        public Model()
        {
            this.Name = "2833";
            this.ROI = new ROISettings();
            this.MatchingRate = 100;
            this.MatchingRateMin = 70;
            this.BarCodeOffSet = new OpenCvSharp.Point { };
            this.WhitePixels = 100;
            this.BlackPixels = 10;
            this.Threshol = 127;
            this.ThresholBl = 40;
            this.OffSetJigEnb = false;
            this.CirWhCntEnb = true;
            this.RoiWhCntEnb = false;

        }

        public Model Clone()
        {
            return new Model()
            {
                Name = String.Copy(this.Name),
                ROI = this.ROI,
                WhitePixels = this.WhitePixels,
                BlackPixels = this.BlackPixels,
                MatchingRate = this.MatchingRate,
                MatchingRateMin = this.MatchingRateMin,
                BarCodeOffSet = this.BarCodeOffSet,
                OffSetJigEnb = this.OffSetJigEnb,
                Threshol = this.Threshol,
                ThresholBl = this.ThresholBl,
                CirWhCntEnb = this.CirWhCntEnb,
                RoiWhCntEnb = this.RoiWhCntEnb
            };
        }
    }



    public class ScannerSettings
    {
        public String IpAddr { get; set; } = "192.168.1.2";
        public int TcpPort { get; set; } = 9004;

        public ScannerSettings Clone()
        {
            return new ScannerSettings
            {
                IpAddr = String.Copy(this.IpAddr),
                TcpPort = this.TcpPort
            };
        }
    }
    public class ROISettings
    {
        public List<OpenCvSharp.RotatedRect> listRectangle { get; set; }

        public ROISettings()
        {
            listRectangle = new List<OpenCvSharp.RotatedRect>() { };
        }
        public ROISettings Clone()
        {
            return new ROISettings
            {
                listRectangle = this.listRectangle,
            };
        }
    }

    public class ROIProperty
    {
        public int StrokeThickness { get; set; }
        public int labelFontSize { get; set; }
        public OpenCvSharp.Size rectSize { get; set; }

        public ROIProperty()
        {
            StrokeThickness = 7;
            labelFontSize = 25;
            rectSize = new OpenCvSharp.Size(10, 10);

        }
        public ROIProperty Clone()
        {
            return new ROIProperty
            {
                StrokeThickness = this.StrokeThickness,
                labelFontSize = this.labelFontSize,
                rectSize = this.rectSize,
            };
        }

    }

    public class UserID
    {
        public string UserName { get; set; }
        public string IDSuperuser { get; set; } = "ITM123";
        public string NameSuperuser { get; set; } = "ITM";
        //public string IDManager { get; set; } = "ITM123";
        public string IDManager { get; set; } = "1";
        public string NameManager { get; set; } = "ITM";

        public string IdOP { get; set; } = "123";
        public string NameIdOP { get; set; } = "ITM";
        public UserID Clone()
        {
            return new UserID()
            {
                UserName = this.UserName,
                IDSuperuser = this.IDSuperuser,
                NameSuperuser = this.NameSuperuser,
                IDManager = this.IDManager,
                NameManager = this.NameManager,
                IdOP = this.IdOP,
                NameIdOP = this.NameIdOP
            };
        }
    }
    public class communication
    {
        public String PLCip { get; set; }
        public int PLCport { get; set; }
        public int PLCSlot { get; set; }
        public communication()
        {
            this.PLCip = "192.168.3.39";
            this.PLCport = 2004;
            this.PLCSlot = 1;
        }

        public communication Clone()
        {
            return new communication
            {
                PLCip = this.PLCip,
                PLCport = this.PLCport,
                PLCSlot = this.PLCSlot,
            };

        }
    }

    //Vision Program

    public class VisionProgram
    {
        public string NameJob { get; set; }
        public string NameDisp { get; set; }
        public List<VisionProgramN> vsProgramNs { get; set; }
        public VisionProgram()
        {
            this.NameJob = "Job1";
            this.NameDisp = "Vision Program";
            this.vsProgramNs = new List<VisionProgramN>();
        }
        public VisionProgram Clone()
        {
            return new VisionProgram()
            {
                NameJob = String.Copy(this.NameJob),
                NameDisp = String.Copy(this.NameDisp),
                vsProgramNs = this.vsProgramNs
            };
        }
    }
    public class CommProperty
    {
        public string addrJigPos { get; set; }
        public string addrJob { get; set; }
        public DeviceCode selectDevJigPos { get; set; }
        public DeviceCode selectDevJob { get; set; }
        public Dictionary<int, Point> mtrxPoint { get; set; }
        public CommProperty()
        {
            this.addrJob = "3";
            this.addrJigPos = "4";
            this.selectDevJob = DeviceCode.D;
            this.selectDevJigPos = DeviceCode.D;
            this.mtrxPoint = new Dictionary<int, Point>();
        }
        public CommProperty Clone()
        {
            return new CommProperty()
            {
                addrJob = this.addrJob,
                addrJigPos = this.addrJigPos,
                selectDevJob = this.selectDevJob,
                selectDevJigPos = this.selectDevJigPos,
                mtrxPoint = this.mtrxPoint,
            };
        }

    }
    public class VisionProgramN
    {
        public string ContentVP { get; set; }
        public string addrIn { get; set; }
        public DeviceCode selectDevIn { get; set; }
        public VisionMainSub vsMain { get; set; }
        public List<VisionMainSub> vsSubs { get; set; }
        public VisionProgramN()
        {
            this.ContentVP = "Vision Program 1";
            this.selectDevIn = DeviceCode.M;
            this.addrIn = "10";
            this.vsMain = new VisionMainSub();
            this.vsSubs = new List<VisionMainSub>();
        }
        public VisionProgramN Clone()
        {
            return new VisionProgramN()
            {
                ContentVP = this.ContentVP,
                selectDevIn = this.selectDevIn,
                addrIn = this.addrIn,
                vsMain = this.vsMain,
                vsSubs = this.vsSubs,
            };
        }

    }
    public class VisionMainSub
    {
        public string ContentSub { get; set; }
        public List<string> nameTools { get; set; }
        public List<double> heightToolLst { get; set; }
        public double heightAllTool { get; set; }
        public bool isOutTool { get; set; }
        public bool isBlockOut { get; set; }
        public List<ArrowConnectSetting> arrowConnect { get; set; }
        public List<AcquisitionSetting> aquisitionSettings { get; set; }
        public List<ImageBuffSetting> imageBuffSettings { get; set; }
        public List<SaveImageSetting> saveImageSettings { get; set; }
        public List<TemplateMatchSetting> templateMatchSettings { get; set; }
        public List<FixtureSetting> fixtureSettings { get; set; }
        public List<EditRegonSetting> editRegonSettings { get; set; }
        public List<ContrastNBrightnessSetting> contrastNBrightnessSettings { get; set; }
        public List<ImageProcessSetting> imageProcessSettings { get; set; }
        public List<BlobSetting> blobSettings { get; set; }
        public List<TempMatchZeroSetting> tempMatchZeroSettings { get; set; }
        public List<SegmentNeuroSetting> segmentNeuroSettings { get; set; }
        public List<VidiCognexSetting> vidiCognexSettings { get; set; }
        public List<VisionProSetting> visionProSettings { get; set; }
        public List<OutBlobResSetting> outBlobResSettings { get; set; }    
        public List<OutAcquisResSetting> outAcquisResSettings { get; set; } 
        public List<OutCheckProductSetting> outCheckProductSettings { get; set; }
        public List<OutSegNeuroResSetting> outSegNeuroResSettings { get; set; }
        public List<OutVidiCogResSetting> outVidiCogResSettings { get; set; }
        public VisionMainSub()
        {
            this.ContentSub = "";
            this.nameTools = new List<string>();
            this.heightToolLst = new List<double>();
            this.heightAllTool = 1;
            this.isOutTool = false;
            this.isBlockOut = false;
            this.arrowConnect = new List<ArrowConnectSetting>();
            this.aquisitionSettings = new List<AcquisitionSetting>();
            this.imageBuffSettings = new List<ImageBuffSetting>();
            this.saveImageSettings = new List<SaveImageSetting>();
            this.templateMatchSettings = new List<TemplateMatchSetting>();
            this.fixtureSettings = new List<FixtureSetting>();
            this.editRegonSettings = new List<EditRegonSetting>();
            this.contrastNBrightnessSettings = new List<ContrastNBrightnessSetting>();
            this.imageProcessSettings = new List<ImageProcessSetting>();
            this.blobSettings = new List<BlobSetting>();
            this.tempMatchZeroSettings = new List<TempMatchZeroSetting>();
            this.segmentNeuroSettings = new List<SegmentNeuroSetting>();
            this.vidiCognexSettings = new List<VidiCognexSetting>();
            this.visionProSettings = new List<VisionProSetting>();
            this.outBlobResSettings = new List<OutBlobResSetting>();
            this.outAcquisResSettings = new List<OutAcquisResSetting>();
            this.outCheckProductSettings = new List<OutCheckProductSetting>();
            this.outSegNeuroResSettings = new List<OutSegNeuroResSetting>();
            this.outVidiCogResSettings = new List<OutVidiCogResSetting>();
        }
        public VisionMainSub Clone()
        {
            return new VisionMainSub()
            {
                ContentSub = this.ContentSub,
                nameTools = this.nameTools,
                heightToolLst = this.heightToolLst,
                heightAllTool = this.heightAllTool,
                isOutTool = this.isOutTool,
                isBlockOut = this.isBlockOut,
                arrowConnect = this.arrowConnect,
                aquisitionSettings = this.aquisitionSettings,
                imageBuffSettings = this.imageBuffSettings,
                saveImageSettings = this.saveImageSettings,
                templateMatchSettings = this.templateMatchSettings,
                fixtureSettings = this.fixtureSettings,
                editRegonSettings = this.editRegonSettings,
                contrastNBrightnessSettings = this.contrastNBrightnessSettings,
                imageProcessSettings = this.imageProcessSettings,
                blobSettings = this.blobSettings,
                tempMatchZeroSettings = this.tempMatchZeroSettings,
                segmentNeuroSettings = this.segmentNeuroSettings,
                vidiCognexSettings = this.vidiCognexSettings,
                visionProSettings = this.visionProSettings,
                outBlobResSettings = this.outBlobResSettings,
                outAcquisResSettings= this.outAcquisResSettings,
                outCheckProductSettings = this.outCheckProductSettings,
                outSegNeuroResSettings = this.outSegNeuroResSettings,
                outVidiCogResSettings = this.outVidiCogResSettings,
            };
        }
    }
    public class ArrowConnectSetting
    {
        public string name { get; set; }
        public object data { get; set; }
        public Point startPoint { get; set; }
        public Point endPoint { get; set; }
        public ArrowConnectSetting()
        {
            this.name = "";
            this.data = new object();
            this.startPoint = new Point();
            this.endPoint = new Point();
        }
        public ArrowConnectSetting Clone()
        {
            return new ArrowConnectSetting()
            {
                name = this.name,
                data = this.data,
                startPoint = this.startPoint,
                endPoint = this.endPoint
            };
        }
    }
    public class AcquisitionSetting
    {
        public ECamDevType eCamDevType { get; set; }
        public string serialNumber { get; set; }
        public bool isCmaera { get; set; }
        public bool isFolder { get; set; }
        public bool isVerticalFlip { get; set; }
        public bool isHorizontalFlip { get; set; }
        public RotateMode rotateMode { get; set; }
        public bool isGrayMode { get; set; }
        public GrayMode grayMode { get; set; }

        public double ExposureTime { get; set; }
        public double Gain { get; set; }
        public long WidthCam { get; set; }
        public long HeightCam { get; set; }
        public AcquisitionSetting()
        {
            this.eCamDevType = ECamDevType.Hikrobot;
            this.serialNumber = "";
            this.isCmaera = true;
            this.isFolder = false;
            this.isVerticalFlip = false;
            this.isHorizontalFlip = false;
            this.rotateMode = RotateMode.Rotate0;
            this.isGrayMode = false;
            this.grayMode = GrayMode.BGR2Gray;

            this.ExposureTime = 5000d;
            this.Gain = 0d;
            this.WidthCam = 500;
            this.HeightCam = 500;
        }
        public AcquisitionSetting Clone()
        {
            return new AcquisitionSetting()
            {
                eCamDevType = this.eCamDevType,
                serialNumber = this.serialNumber,
                isCmaera = this.isCmaera,
                isFolder = this.isFolder,
                isVerticalFlip = this.isVerticalFlip,
                isHorizontalFlip = this.isHorizontalFlip,
                rotateMode = this.rotateMode,
                isGrayMode = this.isGrayMode,
                grayMode = this.grayMode,
                ExposureTime = this.ExposureTime,
                Gain = this.Gain,
                WidthCam = this.WidthCam,
                HeightCam = this.HeightCam,
            };
        }
    }
    public class ImageBuffSetting
    {
        public DeviceCode selectDevReset { get; set; }
        public string addrReset { get; set; }
        public int cacheQuantity { get; set; }
        public ImageBuffSetting()
        {
            selectDevReset = DeviceCode.M;
            addrReset = "0";
            cacheQuantity = 1;
        }
        public ImageBuffSetting Clone()
        {
            return new ImageBuffSetting
            {
                addrReset = this.addrReset,
                cacheQuantity = this.cacheQuantity,
                selectDevReset = this.selectDevReset,
            };
        }
    }
    public class SaveImageSetting
    {
        public string fileName { get; set; }
        public string folderPath { get; set; }
        public string imageFormat { get; set; }
        public bool isAddDateTime { get; set; }
        public bool isAddCounter { get; set; }
        public int counter { get; set; }
        public double imageStorage { get; set; }
        public SaveImageSetting()
        {
            this.fileName = "ImageName";
            this.folderPath = @"C:\";
            this.imageFormat = "BMP";
            this.isAddDateTime = false;
            this.isAddCounter = false;
            this.counter = 0;
            this.imageStorage = 5d;
        }
        public SaveImageSetting Clone()
        {
            return new SaveImageSetting()
            {
                fileName = this.fileName,
                folderPath = this.folderPath,
                imageFormat = this.imageFormat,
                isAddDateTime = this.isAddDateTime,
                isAddCounter = this.isAddCounter,
                counter = this.counter,
                imageStorage = this.imageStorage,
            };
        }
    }
    public class TemplateMatchSetting
    {
        public bool isUseEdge { get; set; }
        public bool isAutoMatchPara { get; set; }
        public double scaleFirst { get; set; }
        public double scaleLast { get; set; }
        public double degMin { get; set; }
        public double degMax { get; set; }
        public double firstStep { get; set; }
        public double precision { get; set; }
        public Priority priority { get; set; }
        public double priorityCreteria { get; set; }
        public int maxCount { get; set; }
        public double tempScaleMin { get; set; }
        public double tempScaleMax { get; set; }
        public Point3d cpPattern { get; set; }
        public PatternData patternDataSetting { get; set; }

        public TemplateMatchSetting()
        {
            this.isUseEdge = false;
            this.isAutoMatchPara = true;
            this.scaleFirst = 1;
            this.scaleLast = 2;
            this.degMin = -10;
            this.degMax = -10;
            this.firstStep = 5;
            this.precision = 0.02;
            this.priority = Priority.None;
            this.priorityCreteria = 0.75;
            this.maxCount = 1;
            this.tempScaleMin = 1;
            this.tempScaleMax = 1;
            this.cpPattern = new Point3d();
            this.patternDataSetting = new PatternData();
        }

        public TemplateMatchSetting Clone()
        {
            return new TemplateMatchSetting()
            {
                isUseEdge = this.isUseEdge,
                isAutoMatchPara = this.isAutoMatchPara,
                scaleFirst = this.scaleFirst,
                scaleLast = this.scaleLast,
                degMin = this.degMin,
                degMax = this.degMax,
                firstStep = this.firstStep,
                precision = this.precision,
                priority = this.priority,
                priorityCreteria = this.priorityCreteria,
                maxCount = this.maxCount,
                tempScaleMin = this.tempScaleMin,
                tempScaleMax = this.tempScaleMax,
                cpPattern = this.cpPattern,
                patternDataSetting = this.patternDataSetting,
            };
        }
    }
    public class FixtureSetting
    {
        public double inTranslateX { get; set; }
        public double inTranslateY { get; set; }
        public double inScale { get; set; }
        public double inRotation { get; set; }
        public FixtureSetting()
        {
            this.inTranslateX = 0;
            this.inTranslateY = 0;
            this.inScale = 1;
            this.inRotation = 0;
        }
        public FixtureSetting Clone()
        {
            return new FixtureSetting()
            {
                inTranslateX = this.inTranslateX,
                inTranslateY = this.inTranslateY,
                inScale = this.inScale,
                inRotation = this.inRotation,
            };
        }
    }
    public class EditRegonSetting
    {
        public List<RotatedRect> ROIList { get; set; }
        public Point3d centInputImage { get; set; }
        public int numberSub { get; set; }
        public List<List<Tuple<int, bool>>> resultCkbList { get; set; }
        public EditRegonSetting()
        {
            this.ROIList = new List<RotatedRect>();
            this.centInputImage = new Point3d(0, 0, 0);
            this.numberSub = 0;
            this.resultCkbList = new List<List<Tuple<int, bool>>>();
        }
        public EditRegonSetting Clone()
        {
            return new EditRegonSetting()
            {
                ROIList = this.ROIList,
                centInputImage = this.centInputImage,
                numberSub = this.numberSub,
                resultCkbList = this.resultCkbList
            };
        }
    }
    public class ContrastNBrightnessSetting
    {
        public double gammaValue { get; set; }
        public double alphaValue { get; set; }
        public double betaValue { get; set; }
        public ContrastNBrightnessSetting()
        {
            this.gammaValue = 0;
            this.alphaValue = 0;
            this.betaValue = 0;
        }
        public ContrastNBrightnessSetting Clone()
        {
            return new ContrastNBrightnessSetting()
            {
                gammaValue = this.gammaValue,
                alphaValue = this.alphaValue,
                betaValue = this.betaValue,
            };
        }
    }
    public class ImageProcessSetting
    {
        public ImageProcessMode selectedImageProcessMode { get; set; }
        public ThresholdMode selectedThresholdMode { get; set; }
        public int thresholdValue { get; set; }
        public ImageProcessSetting()
        {
            this.selectedImageProcessMode = ImageProcessMode.Threshold;
            this.selectedThresholdMode = ThresholdMode.White;
            this.thresholdValue = 0;
        }
        public ImageProcessSetting Clone()
        {
            return new ImageProcessSetting()
            {
                selectedImageProcessMode = this.selectedImageProcessMode,
                selectedThresholdMode = this.selectedThresholdMode,
                thresholdValue = this.thresholdValue,
            };
        }
    }
    public class BlobSetting
    {
        public BlobMode selectBlobMode { get; set; }
        public BlobType selectBlobType { get; set; }
        public BlobPolarity selectBlobPolarity { get; set; }
        public BlobBinary selectBlobBinary { get; set; }
        public BlobPriority selectBlobPriority { get; set; }
        public SortType selectSort { get; set; }
        public bool isCalBlob { get; set; }
        public bool isExceptBound { get; set; }
        public bool isFillHole { get; set; }
        public bool isAscend { get; set; }
        public int range { get; set; }
        public int lowRange { get; set; }
        public int highRange { get; set; }
        public int blockSize { get; set; }
        public int coeff { get; set; }
        public int coeffR { get; set; }
        public int constantSubtract { get; set; }
        public int constantMin { get; set; }
        public List<BlobFilter> blobFilters { get; set; }
        public BlobSetting()
        {
            selectBlobMode = BlobMode.Threshold;
            selectBlobType = BlobType.Binary;
            selectBlobPolarity = BlobPolarity.White;
            selectBlobBinary = BlobBinary.ToBlack;
            selectBlobPriority = BlobPriority.None;
            selectSort = SortType.Area;
            isCalBlob = false;
            isExceptBound = false;
            isFillHole = false;
            isAscend = false;
            range = 0;
            lowRange = 0;
            highRange = 255;
            blockSize = 3;
            coeff = 0;
            coeffR = 0;
            constantSubtract = 0;
            constantMin = 0;
            blobFilters = new List<BlobFilter>();
        }
        public BlobSetting Clone()
        {
            return new BlobSetting
            {
                selectBlobMode = this.selectBlobMode,
                selectBlobBinary = this.selectBlobBinary,
                selectBlobType = this.selectBlobType,
                selectBlobPolarity = this.selectBlobPolarity,
                selectBlobPriority = this.selectBlobPriority,
                selectSort = this.selectSort,
                isCalBlob = this.isCalBlob,
                isExceptBound = this.isExceptBound,
                isFillHole = this.isFillHole,
                isAscend = this.isAscend,
                range = this.range,
                lowRange = this.lowRange,
                highRange = this.highRange,
                blockSize = this.blockSize,
                coeff = this.coeff,
                coeffR = this.coeffR,
                constantSubtract = this.constantSubtract,
                constantMin = this.constantMin,
                blobFilters = this.blobFilters,
            };
        }
    }
    public class TempMatchZeroSetting
    {
        public double priorityCreteria { get; set; }
        public int maxCount { get; set; }
        public Point3d cpPattern { get; set; }
        public PatternData patternDataSetting { get; set; }
        public List<PatternData> patternLst { get; set; }
        public int numPattern {  get; set; }
        public bool isUseROI { get; set; }
        public Rect rectSearch { get; set; }
        public Rect rectTrain { get; set; }
        public TempMatchZeroSetting()
        {
            this.priorityCreteria = 0.75;
            this.maxCount = 1;
            this.cpPattern = new Point3d();
            this.patternDataSetting = new PatternData();
            this.patternLst = new List<PatternData>();
            numPattern = 0;
            isUseROI = true;
            rectSearch = new Rect(10, 10, 100, 100);
            rectTrain = new Rect(10, 10, 100, 100);
        }

        public TempMatchZeroSetting Clone()
        {
            return new TempMatchZeroSetting()
            {
                priorityCreteria = this.priorityCreteria,
                maxCount = this.maxCount,
                cpPattern = this.cpPattern,
                patternDataSetting = this.patternDataSetting,
                patternLst = this.patternLst,
                numPattern = this.numPattern,
                isUseROI = this.isUseROI,
                rectSearch = this.rectSearch,
                rectTrain = this.rectTrain,
            };
        }
    }
    public class SegmentNeuroSetting
    {
        public List<NeuroModel> modelNeuroes { get; set; }
        public SegmentNeuroSetting()
        {
            this.modelNeuroes = new List<NeuroModel>();
        }
        public SegmentNeuroSetting Clone()
        {
            return new SegmentNeuroSetting()
            {
                modelNeuroes = this.modelNeuroes,
            };
        }
    }
    public class VidiCognexSetting
    {
        public string deviceName { get; set; }
        public int deviceIdx { get ; set; }
        public List<VidiModel> vidiModels { get; set; }
        public VidiCognexSetting()
        {
            this.deviceName = "";
            this.deviceIdx = 0;
            this.vidiModels = new List<VidiModel>();
        }
        public VidiCognexSetting Clone()
        {
            return new VidiCognexSetting()
            {
                deviceName = this.deviceName,
                deviceIdx = this.deviceIdx,
                vidiModels = this.vidiModels,
            };
        }
    }
    public class VisionProSetting
    {
        public string path { get; set; }
        public ObservableCollection<VppInOutInfo> vppOutputInfos { get ; set; }
        public VisionProSetting()
        {
            this.path = "";
            this.vppOutputInfos = new ObservableCollection<VppInOutInfo>();
        }
        public VisionProSetting Clone()
        {
            return new VisionProSetting()
            {
                path = this.path,
                vppOutputInfos = this.vppOutputInfos,
            };
        }
    }
    public class OutBlobResSetting
    {
        public string addrOutOK { get; set; }
        public string addrOutNG { get; set; }
        public DeviceCode selectDevOutOK { get; set; }
        public DeviceCode selectDevOutNG { get; set; }
        public List<int> indexImgs { get; set; }
        public List<string> addrOKLst { get; set; }
        public List<string> addrNGLst { get; set; }
        public double distanceSet { get; set; }
        public OutBlobResSetting()
        {
            this.addrOutOK = "0";
            this.addrOutNG = "1";
            this.selectDevOutOK = DeviceCode.M;
            this.selectDevOutNG = DeviceCode.M;
            this.addrOKLst = new List<string>();
            this.addrNGLst = new List<string>();
            this.indexImgs = new List<int>();
            this.distanceSet = 0d;
        }

        public OutBlobResSetting Clone()
        {
            return new OutBlobResSetting()
            {
                addrOutOK = this.addrOutOK,
                addrOutNG = this.addrOutNG,
                selectDevOutOK = this.selectDevOutOK,
                selectDevOutNG = this.selectDevOutNG,
                addrOKLst = this.addrOKLst,
                addrNGLst = this.addrNGLst,
                indexImgs = this.indexImgs,
                distanceSet = this.distanceSet,
            };
        }
    }
    public class OutAcquisResSetting
    {
        public string addrOutOK { get; set; }
        public string addrOutNG { get; set; }
        public DeviceCode selectDevOutOK { get; set; }
        public DeviceCode selectDevOutNG { get; set; }
        public List<string> addrOKLst { get; set; }
        public List<string> addrNGLst { get; set; }
        public OutAcquisResSetting()
        {
            this.addrOutOK = "0";
            this.addrOutNG = "1";
            this.selectDevOutOK = DeviceCode.M;
            this.selectDevOutNG = DeviceCode.M;
            this.addrOKLst = new List<string>();
            this.addrNGLst = new List<string>();
        }

        public OutAcquisResSetting Clone()
        {
            return new OutAcquisResSetting()
            {
                addrOutOK = this.addrOutOK,
                addrOutNG = this.addrOutNG,
                selectDevOutOK = this.selectDevOutOK,
                selectDevOutNG = this.selectDevOutNG,
                addrOKLst = this.addrOKLst,
                addrNGLst = this.addrNGLst,
            };
        }
    }
    public class OutCheckProductSetting
    {
        public string addrOut { get; set; }
        public DeviceCode selectDevOut { get; set; }
        public string[] arrAddr { get; set; }
        public OutCheckProductSetting()
        {
            this.addrOut = "0";
            this.selectDevOut = DeviceCode.M;
            this.arrAddr = new string[3];
        }

        public OutCheckProductSetting Clone()
        {
            return new OutCheckProductSetting()
            {
                addrOut = this.addrOut,
                selectDevOut = this.selectDevOut,
                arrAddr = this.arrAddr,
            };
        }
    }
    public class OutSegNeuroResSetting
    {
        public string addrOut { get; set; }
        public DeviceCode selectDevOut { get; set; }
        public int numberPos { get; set; }
        public List<string> addrLst { get; set; }
        public OutSegNeuroResSetting()
        {
            this.addrOut = "0";
            this.selectDevOut = DeviceCode.D;
            this.numberPos = 0;
            this.addrLst = new List<string>();
        }

        public OutSegNeuroResSetting Clone()
        {
            return new OutSegNeuroResSetting()
            {
                addrOut = this.addrOut,
                selectDevOut = this.selectDevOut,
                numberPos = this.numberPos,
                addrLst = this.addrLst,
            };
        }
    }
    public class OutVidiCogResSetting
    {
        public string addrOut { get; set; }
        public DeviceCode selectDevOut { get; set; }
        public string[] arrAddr { get; set; }
        public OutVidiCogResSetting()
        {
            this.addrOut = "0";
            this.selectDevOut = DeviceCode.M;
            this.arrAddr = new string[3];
        }

        public OutVidiCogResSetting Clone()
        {
            return new OutVidiCogResSetting()
            {
                addrOut = this.addrOut,
                selectDevOut = this.selectDevOut,
                arrAddr = this.arrAddr,
            };
        }
    }
}
