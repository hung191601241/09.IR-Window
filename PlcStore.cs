using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLaserCuttingInput
{
    class PlcStore
    {
        #region PG Main
        public const string ALARM_01 = "200";
        public const string ALARM_02 = "201";
        public const string ALARM_03 = "202";
        public const string ALARM_04 = "203";
        public const string ALARM_05 = "204";
        public const string ALARM_06 = "205";
        public const string ALARM_07 = "206";
        public const string ALARM_08 = "207";
        public const string ALARM_09 = "208";
        public const string ALARM_10 = "209";
        public const string ALARM_11 = "210";
        public const string ALARM_12 = "211";
        public const string ALARM_13 = "212";
        public const string ALARM_14 = "213";
        public const string ALARM_15 = "214";
        public const string ALARM_16 = "215";
        public const string ALARM_17 = "216";
        public const string ALARM_18 = "217";
        public const string ALARM_19 = "218";
        public const string ALARM_20 = "219";


        public const string READ_BT_START = "102";
        public const string READ_BT_STOP = "103";
        public const string READ_BT_HOME = "105";
        public const string READ_BT_RESET = "107";

        public const string WRITE_BT_START = "2";
        public const string WRITE_BT_STOP = "3";
        public const string WRITE_BT_HOME = "5";
        public const string WRITE_BT_RESET = "7";

        public const string READ_RESET = "7";
        public const string READ_MACHINE_RUNING = "26";



        public const string READ_QR1_TRIG = "501";
        public const string READ_QR2_TRIG = "511";


        public const string READ_VISION1_TRIG = "521";
        public const string READ_VISION2_TRIG = "531";

        public const string READ_CLR_ALL = "540";

        public const string READ_CHECK_MES = "550";
        public const string WRITE_MES_OK = "551";
        public const string WRITE_MES_NG = "552";

        public const string WRITE_QR1_CLR_OK = "600";
        public const string WRITE_QR1_OK = "602";
        public const string WRITE_QR1_NG = "603";

        public const string WRITE_QR2_CLR_OK = "610";
        public const string WRITE_QR2_OK = "612";
        public const string WRITE_QR2_NG = "613";

        public const string WRITE_VISION1_CLR_OK = "620";
        public const string WRITE_VISION1_BUSY = "622";
        public const string WRITE_VISION1_OK = "623";
        public const string WRITE_VISION1_NG = "624";
        public const string WRITE_VISION1_EMTY = "625";

        public const string WRITE_VISION2_CLR_OK = "630";
        public const string WRITE_VISION2_BUSY = "632";
        public const string WRITE_VISION2_OK = "633";
        public const string WRITE_VISION2_NG = "634";
        public const string WRITE_VISION2_EMTY = "635";

        public const string WRITE_CLR_ALL_OK = "640";


        #endregion
        #region PG TEACHING
        public const string Servo_AX1_ON_OFF = "3702";
        public const string Servo_AX1_JOG_UP = "2002";
        public const string Servo_AX1_JOG_DOWN = "2003";
        public const string Servo_AX1_HOME = "2042";
        public const string Servo_AX1_BAKE_ON_OFF = "3742";

        public const string Servo_AX2_ON_OFF = "3704";
        public const string Servo_AX2_JOG_UP = "2004";
        public const string Servo_AX2_JOG_DOWN = "2005";
        public const string Servo_AX2_HOME = "2044";
        public const string Servo_AX2_BAKE_ON_OFF = "3744";


        public const string K_LAMP_AX1_LIMIT_DOWN = "8822";
        public const string K_LAMP_AX1_LIMIT_UP = "8862";
        public const string K_LAMP_AX1_HOME = "8902";
        public const string K_LAMP_AX1_DRIVER_ERROR = "8782";

        public const string K_LAMP_AX2_LIMIT_DOWN = "8824";
        public const string K_LAMP_AX2_LIMIT_UP = "8864";
        public const string K_LAMP_AX2_HOME = "8904";
        public const string K_LAMP_AX2_DRIVER_ERROR = "8784";

        public const string D_PLC_RUN_Time = "50";
        public const string D_ONE_CYCLE = "52";

        public const string D_AX1_POSITION_CURRENT = "2502";
        public const string D_AX1_SPEED_JOG = "2242";
        public const string D_AX1_ERROR_CODE = "2582";

        public const string D_AX2_POSITION_CURRENT = "2504";
        public const string D_AX2_SPEED_JOG = "2244";
        public const string D_AX2_ERROR_CODE = "2584";

        public const string K_LAMP_AX1_SERVO_ON_OFF = "8702";
        public const string K_LAMP_AX1_JOG_UP = "7002";
        public const string K_LAMP_AX1_JOG_DOWN = "7003";
        public const string K_LAMP_AX1_ORG = "7042";
        public const string K_LAMP_AX1_BAKE_ON_OFF = "8742";

        public const string K_LAMP_AX2_SERVO_ON_OFF = "8704";
        public const string K_LAMP_AX2_JOG_UP = "7004";
        public const string K_LAMP_AX2_JOG_DOWN = "7004";
        public const string K_LAMP_AX2_ORG = "7044";
        public const string K_LAMP_AX2_BAKE_ON_OFF = "8744";

        public const string K_JIG_CLAMP = "1002";
        public const string K_JIG_UNCLAMP = "1003";

        public const string K_LIGHT_MACHINE_ON = "1008";
        public const string K_LIGHT_MACHINE_OFF = "1009";

        public const string K_VACUUM_1_ON = "1004";
        public const string K_VACUUM_1_OFF = "1005";

        public const string K_VACUUM_2_ON = "1006";
        public const string K_VACUUM_2_OFF = "1007";

        public const string K_LIGHT_VISION_1_ON = "101A";
        public const string K_LIGHT_VISION_1_OFF = "101B";

        public const string K_LIGHT_VISION_2_ON = "101C";
        public const string K_LIGHT_VISION_2_OFF = "101D";



        public const string K_LAMP_JIG_CLAMP = "6002";
        public const string K_LAMP_JIG_UNCLAMP = "6003";

        public const string K_LAMP_LIGHT_MACHINE_ON = "6008";
        public const string K_LAMP_LIGHT_MACHINE_OFF = "6009";

        public const string K_LAMP_VACUUM_1_ON = "6004";
        public const string K_LAMP_VACUUM_1_OFF = "6005";

        public const string K_LAMP_VACUUM_2_ON = "6006";
        public const string K_LAMP_VACUUM_2_OFF = "6007";

        public const string K_LAMP_LIGHT_VISION_1_ON = "601A";
        public const string K_LAMP_LIGHT_VISION_1_OFF = "601B";

        public const string K_LAMP_LIGHT_VISION_2_ON = "601C";
        public const string K_LAMP_LIGHT_VISION_2_OFF = "601D";

        public const string K_CALL_MATRIX_VISION_POS_AX1 = "2102";
        public const string K_CALL_MATRIX_SCANNER_POS_AX1 = "2104";
        public const string K_CALL_MATRIX_VISION_POS_AX2 = "2202";
        public const string K_CALL_MATRIX_SCANNER_POS_AX2 = "2204";

        public const string K_LAMP_CALL_MATRIX_VISION_POS = "7102";
        public const string K_LAMP_CALL_MATRIX_SCANNER_POS = "7104";

        public const string K_WAIT_POS_0 = "2100";
        public const string K_X1_VISION_MATRIX_POS1 = "2101";
        public const string K_RUN_VISION_MATRIX_POS2 = "2102";
        public const string K_X1_QR_MATRIX_POS3 = "2103";
        public const string K_RUN_QR_MATRIX_POS4 = "2104";

        public const string K_Y_WAIT_POS_0 = "2200";
        public const string K_Y1_VISION_MATRIX_POS1 = "2201";
        public const string K_Y_RUN_VISION_MATRIX_POS2 = "2202";
        public const string K_Y1_QR_MATRIX_POS3 = "2203";
        public const string K_Y_RUN_QR_MATRIX_POS4 = "2204";

        public const string K_LAMP_WAIT_POS_0 = "7100";
        public const string K_LAMP_X1_VISION_MATRIX_POS1 = "7101";
        public const string K_LAMP_RUN_VISION_MATRIX_POS2 = "7102";
        public const string K_LAMP_X1_QR_MATRIX_POS3 = "7103";
        public const string K_LAMP_RUN_QR_MATRIX_POS4 = "7104";

        public const string K_LAMP_Y_WAIT_POS_0 = "7200";
        public const string K_LAMP_Y1_VISION_MATRIX_POS1 = "7201";
        public const string K_LAMP_Y_RUN_VISION_MATRIX_POS2 = "7202";
        public const string K_LAMP_Y1_QR_MATRIX_POS3 = "7203";
        public const string K_LAMP_Y_RUN_QR_MATRIX_POS4 = "7204";

        public const string D_CALL_POS_VISION = "526";
        public const string D_CALL_POS_QR = "566";


        public const string L_SAVE_POS_AX1 = "5716";
        public const string L_SAVE_POS_AX2 = "5726";

        public const string ZR_POS_AX1 = "5214";
        public const string ZR_POS_AX2 = "5224";

        public const string ZR_POS0 = "2000";
        public const string ZR_VISION_POS1 = "2002";
        public const string ZR_SCANNER_POS3 = "2006";

        public const string ZR_SPEED_POS0 = "12000";
        public const string ZR_SPEED_VISION_POS1 = "12002";
        public const string ZR_SPEED_VISION_POS2 = "12004";
        public const string ZR_SPEED_SCANNER_POS3 = "12006";
        public const string ZR_SPEED_SCANNER_POS4 = "12008";


        #endregion
        #region PG TEACHING 1
        public const string JIG_X_MAXTRIX_POINT = "502";
        public const string JIG_X_MAXTRIX_PITCH = "504";

        public const string JIG_Y_MAXTRIX_POINT = "512";
        public const string JIG_Y_MAXTRIX_PITCH = "514";
        #endregion

        #region PG_SYSTEM
        public const string K_DRYRUM_MODE = "500";
        public const string K_STEP_RUN_ON = "501";
        public const string K_DISABLE_ALARM = "502";
        public const string K_DISABLE_BUZZER = "503";
        public const string K_DISABLE_DOOR_SENSORS = "504";
        public const string K_DISABLE_SAFETY_SENSORS = "505";
        public const string K_BYPASS_DETECT_CHECK_JIG = "510";
        public const string K_BYPASS_VACUUMM_CHECK = "511";
        public const string K_DISABLE_VACUUMM = "512";

        public const string K_DISABLE_CH1 = "513";
        public const string K_DISABLE_CH2 = "514";
        public const string K_MANUAL_RUN = "515";


        public const string K_LAMP_DRYRUM_MODE = "700";
        public const string K_LAMP_STEP_RUN_ON = "701";
        public const string K_LAMP_DISABLE_ALARM = "702";
        public const string K_LAMP_DISABLE_BUZZER = "703";
        public const string K_LAMP_DISABLE_DOOR_SENSORS = "704";
        public const string K_LAMP_DISABLE_SAFETY_SENSORS = "705";
        public const string K_LAMP_BYPASS_DETECT_CHECK_JIG = "710";
        public const string K_LAMP_BYPASS_VACUUMM_CHECK = "711";
        public const string K_LAMP_DISABLE_VACUUMM = "712";

        public const string K_LAMP_DISABLE_CH1 = "713";
        public const string K_LAMP_DISABLE_CH2 = "714";
        public const string K_LAMP_MANUAL_RUN = "715";
        #endregion

        #region SuperUser
        public const string D_DOOR_SEN_ANTI_JAMMING = "300";
        public const string D_AREA_SEN_ANTI_JAMMING = "301";
        public const string D_DOOR_SENSOR_ERR_TIMEOUT = "302";
        public const string D_AREA_SENSOR_ERR_TIMEOUT = "303";
        public const string D_INPUT_DETECT_JIG_DELAY_TIME = "304";
        public const string D_INPUT_DETECT_CHECK_JIG_DELAY_TIME = "305";

        public const string D_ACC_TIME_AX1 = "2002";
        public const string D_DEC_TIME_AX1 = "2042";
        public const string D_SPEED_LIMIT_ALL_AX1 = "2082";
        public const string D_ORG_SPEED_AX1 = "2162";
        public const string D_JOG_SPEED_AX1 = "2242";

        public const string D_ACC_TIME_AX2 = "2004";
        public const string D_DEC_TIME_AX2 = "2044";
        public const string D_SPEED_LIMIT_ALL_AX2 = "2084";
        public const string D_ORG_SPEED_AX2 = "2164";
        public const string D_JOG_SPEED_AX2 = "2244";


        public const string D_JIG_CLAMP_ON = "1002";
        public const string D_VACUUM1_ON = "1004";
        public const string D_VACUUM2_ON = "1006";
        public const string D_LIGHT_MACHINE_ON = "1008";
        public const string D_LIGHT_VISION1_ON = "1010";
        public const string D_LIGHT_VISION2_ON = "1012";

        public const string D_JIG_CLAMP_OFF = "1003";
        public const string D_VACUUM1_OFF = "1005";
        public const string D_VACUUM2_OFF = "1007";
        public const string D_LIGHT_MACHINE_OFF = "1009";
        public const string D_LIGHT_VISION1_OFF = "1011";
        public const string D_LIGHT_VISION2_OFF = "1013";
        #endregion
    }
}
