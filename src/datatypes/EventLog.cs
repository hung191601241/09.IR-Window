using System;

namespace VisionInspection
{
    class EventLog
    {
        #region PLC LS
        #region EventLog Bit FENET Protocol
        public const int EV_FENETPROTOCOL_READ_BIT_ERROR = 0;
        public const int EV_FENETPROTOCOL_READ_MULTI_BITS_ERROR = 1;
        public const int EV_FENETPROTOCOL_WRITE_BIT_ERROR = 2;
        public const int EV_FENETPROTOCOL_WRITE_MULTI_BITS_ERROR = 3;
        #endregion

        #region EventLog Byte FENET Protocol
        public const int EV_FENETPROTOCOL_READ_BYTE_ERROR = 10;
        public const int EV_FENETPROTOCOL_READ_MULTI_BYTES_ERROR = 11;
        public const int EV_FENETPROTOCOL_WRITE_BYTE_ERROR = 12;
        public const int EV_FENETPROTOCOL_WRITE_MULTI_BYTES_ERROR = 13;
        #endregion

        #region EventLog Word FENET Protocol
        public const int EV_FENETPROTOCOL_READ_WORD_UINT16_ERROR = 20;
        public const int EV_FENETPROTOCOL_READ_WORD_INT16_ERROR = 21;
        public const int EV_FENETPROTOCOL_READ_MULTI_WORDS_UINT16_ERROR = 22;
        public const int EV_FENETPROTOCOL_READ_MULTI_WORDS_INT16_ERROR = 23;
        public const int EV_FENETPROTOCOL_WRITE_WORD_UINT16_ERROR = 24;
        public const int EV_FENETPROTOCOL_WRITE_WORD_INT16_ERROR = 25;
        public const int EV_FENETPROTOCOL_WRITE_MULTI_WORDS_UINT16_ERROR = 26;
        public const int EV_FENETPROTOCOL_WRITE_MULTI_WORDS_INT16_ERROR = 27;
        #endregion

        #region EventLog Double Word FENET Protocol
        public const int EV_FENETPROTOCOL_READ_DOUBLE_WORD_UINT32_ERROR = 30;
        public const int EV_FENETPROTOCOL_READ_DOUBLE_WORD_INT32_ERROR = 31;
        public const int EV_FENETPROTOCOL_READ_MULTI_DOUBLE_WORDS_UINT32_ERROR = 32;
        public const int EV_FENETPROTOCOL_READ_MULTI_DOUBLE_WORDS_INT32_ERROR = 33;
        public const int EV_FENETPROTOCOL_WRITE_DOUBLE_WORD_UINT32_ERROR = 34;
        public const int EV_FENETPROTOCOL_WRITE_DOUBLE_WORD_INT32_ERROR = 35;
        public const int EV_FENETPROTOCOL_WRITE_MULTI_DOUBLE_WORDS_UINT32_ERROR = 36;
        public const int EV_FENETPROTOCOL_WRITE_MULTI_DOUBLE_WORDS_INT32_ERROR = 37;
        #endregion

        #region EventLog Long Word FENET Protocol
        public const int EV_FENETPROTOCOL_READ_LONG_WORD_UINT64_ERROR = 40;
        public const int EV_FENETPROTOCOL_READ_LONG_WORD_INT64_ERROR = 41;
        public const int EV_FENETPROTOCOL_READ_MULTI_LONG_WORD_UINT64_ERROR = 42;
        public const int EV_FENETPROTOCOL_READ_MULTI_LONG_WORD_INT64_ERROR = 43;
        public const int EV_FENETPROTOCOL_WRITE_LONG_WORD_UINT64_ERROR = 44;
        public const int EV_FENETPROTOCOL_WRITE_LONG_WORD_INT64_ERROR = 45;
        public const int EV_FENETPROTOCOL_WRITE_MULTI_LONG_WORD_UINT64_ERROR = 46;
        public const int EV_FENETPROTOCOL_WRITE_MULTI_LONG_WORD_INT64_ERROR = 47;
        #endregion

        #region EventLog String FENET Protocol
        public const int EV_FENETPROTOCOL_READ_STRING_ERROR = 50;
        public const int EV_FENETPROTOCOL_WRITE_STRING_ERROR = 51;
        #endregion

        #region EventLog Common FENET Protocol
        public const int EV_FENETPROTOCOL_PLC_IS_NOT_CONNECT = 60;
        public const int EV_FENETPROTOCOL_DEVICE_NAME_NOT_CORRECT = 61;
        public const int EV_FENETPROTOCOL_ADDRESS_NOT_CORRECT = 62;
        public const int EV_FENETPROTOCOL_DATA_RESPONSE_NOT_CORRECT = 63;
        public const int EV_FENETPROTOCOL_CANNOT_TYPE_DIFFRENT_BIT_TYPE_IN_BIT = 64;
        public const int EV_FENETPROTOCOL_CANNOT_TYPE_DIFFRENT_BYTE_TYPE_IN_BYTE = 65;
        #endregion
        #endregion
        public const int Err_Sol_Hight_Cyl_Hold_Tray_01_02_Ch1 = 100;
        public const int Err_Sol_Low_Cyl_Hold_Tray_01_02_Ch1 = 101;
        public const int Err_Sol_Hight_Cyl_Hold_Tray_03_04_Ch1 = 102;
        public const int Err_Sol_Low_Cyl_Hold_Tray_03_04_Ch1 = 103;
        public const int Err_Sol_Hight_Cyl_Split_Tray_01_02_Ch1 = 104;
        public const int Err_Sol_Low_Cyl_Split_Tray_01_02_Ch1 = 105;
        public const int Err_Sol_Hight_Cyl_Split_Tray_03_04_Ch1 = 106;
        public const int Err_Sol_Low_Cyl_Split_Tray_03_04_Ch1 = 107;
        public const int Err_Sol_Low_Cyl_Up_Down_Tray_Ch1 = 108;
        public const int Err_Sol_Center_Cyl_Up_Down_Tray_Ch1 = 109;
        public const int Err_Sol_Hight_Cyl_Up_Down_Tray_Ch1 = 110;
        public const int Err_Sol_Hight_Cyl_Clamp_Pull_Tray_Ch1 = 111;
        public const int Err_Sol_Low_Cyl_Clamp_Pull_Tray_Ch1 = 112;
        public const int Err_Sol_Hight_Cyl_Keep_Center_Tray_01_02_Ch1 = 113;
        public const int Err_Sol_Low_Cyl_Keep_Center_Tray_01_02_Ch1 = 114;
        public const int Err_Sol_Hight_Cyl_Center_Tray_Ch1 = 115;
        public const int Err_Sol_Low_Cyl_Center_Tray_Ch1 = 116;
        public const int Err_Sol_Hight_Cyl_Up_Down_Out_Tray_Ch1 = 117;
        public const int Err_Sol_Low_Cyl_Up_Down_Out_Tray_Ch1 = 118;
        public const int Err_Sol_Hight_Cyl_In_Out_Jig_Ch1 = 119;
        public const int Err_Sol_Low_Cyl_In_Out_Jig_Ch1 = 120;
        public const int Err_Sol_Hight_Cyl_Up_Down_Lamp_Vision_Ch1 = 121;
        public const int Err_Sol_Low_Cyl_Up_Down_Lamp_Vision_Ch1 = 122;
        public const int Err_Sol_Hight_Cyl_Push_Carriage = 123;
        public const int Err_Sol_Low_Cyl_Push_Carriage = 124;
        public const int Err_Sol_Hight_Cyl_Clamp_Magazine = 125;
        public const int Err_Sol_Low_Cyl_Clamp_Magazine = 126;
        public const int Err_Sol_Hight_Cyl_Up_Down_Carrigae = 127;
        public const int Err_Sol_Low_Cyl_Up_Down_Carrigae = 128;
        public const int Alarm_Servo_Transfer_Tray_Ch1 = 129;
        public const int Err_Limit_add_Servo_Transfer_Tray_Ch1 = 130;
        public const int Err_Limit_minus_Servo_Transfer_Tray_Ch1 = 131;
        public const int Signal_Not_Ready_Servo_Transfer_Tray_Ch1 = 132;
        public const int Signal_Not_Ready_Servo_Up_Down_Magazine = 133;
        public const int Alarm_Servo_Up_Down_Magazine = 134;
        public const int Err_Limit_add_Servo_Up_Down_Magazine = 135;
        public const int Err_Limit_minus_Servo_Up_Down_Magazine = 136;
        public const int Signal_Not_Ready_Servo_Pull_Carrigae = 137;
        public const int Alarm_Servo_Pull_Carrigae = 138;
        public const int Err_Limit_add_Servo_Pull_Carrigae = 139;
        public const int Err_Limit_minus_Servo_Pull_Carrigae = 140;
        public const int Err_Part_Tray_Input_Empty_Tray_Ch1 = 141;
        public const int Err_Part_Tray_Input_Wrong_Position_Tray_Ch1 = 142;
        public const int Err_Part_Tray_Center_Wrong_Position_Tray_Ch1 = 143;
        public const int Err_Part_Tray_Output_Full_Tray_Ch1 = 144;
        public const int Err_Part_Tray_Output_Wrong_Position_Tray_Ch1 = 145;
        public const int Err_Not_Detect_Tray_In_Center_Tray_Ch1 = 146;
        public const int Err_Not_Detect_Jig_Ch1 = 147;
        public const int Err_Stuck_Push_Carriage_In_Magazine = 148;
        public const int Err_Position_Carriage_In_Magazine_Not_Safety = 149;
        public const int Err_Not_Detect_Carriage_Part_Pull_Cariage = 150;
        public const int Err_Stuck_Hang_Carriage_In_Part_Pull_Cariage = 151;
        public const int Err_Ping_IP_Plc = 152;
        public const int Err_Check_Tray_CH1 = 153;
        public const int Err_Check_Megarin_CH1 = 154;
        public const int Err_Check_Jig_Ch1 = 155;
        public const int Err_Connect_Robot_Ch1 = 156;
        public const int Err_Empty_Oil_AxisZ_Robot_Ch1 = 158;
        public const int Err_Current_Pos_AxisZ_Higher_Limit_Robot_Ch1 = 159;
        public const int Err_Collision_Robot_Ch1 = 160;
        public const int Err_Vaccum_Frame_Robot_Ch1 = 161;
        public const int Err_Vaccum_Tool_1_Robot_Ch1 = 162;
        public const int Err_Vaccum_Tool_2_Robot_Ch1 = 163;
        public const int Err_Vaccum_Tool_3_Robot_Ch1 = 164;
        public const int Err_Down_Frame_Robot_Ch1 = 165;
        public const int Err_Up_Frame_Robot_Ch1 = 166;
        public const int Err_Down_Tool_1_Robot_Ch1 = 167;
        public const int Err_Up_Tool_1_Robot_Ch1 = 168;
        public const int Err_Down_Tool_2_Robot_Ch1 = 169;
        public const int Err_Up_Tool_2_Robot_Ch1 = 170;
        public const int Err_Down_Tool_3_Robot_Ch1 = 171;
        public const int Err_Up_Tool_3_Robot_Ch1 = 172;
        public const int Err_Pallet_Point_Out_Of_Range_Robot_Ch1 = 173;
        public const int Err_Open_Door = 174;
        public const int Err_Emergency_Button_Active = 175;
        public const int Err_Safety_Sensor_Outtray_Ch1_Pause_Rb1 = 176;
        public const int Err_Safety_Sensor_Supply_Magazine_Pause_Rb1 = 177;
        public const int Err_Safety_Sensor_Intray_Ch1_Pause_Rb1 = 178;
        public const int Err_Safety_Sensor_Intray_Ch1 = 179;
        public const int Err_Safety_Sensor_Outtray_Ch1 = 180;
        public const int Err_Safety_Sensor_Supply_Magazine = 181;
        public const int Err_Safety_Sensor_Intray_Ch2 = 182;
        public const int Err_Safety_Sensor_Outtray_Ch2 = 183;


        public const int PLC_COMM_FAILED = 184;





        // Alarm Codes : Ch2

        public const int Err_Sol_Hight_Cyl_Hold_Tray_01_02_Ch2 = 350;
        public const int Err_Sol_Low_Cyl_Hold_Tray_01_02_Ch2 = 351;
        public const int Err_Sol_Hight_Cyl_Hold_Tray_03_04_Ch2 = 352;
        public const int Err_Sol_Low_Cyl_Hold_Tray_03_04_Ch2 = 353;
        public const int Err_Sol_Hight_Cyl_Split_Tray_01_02_Ch2 = 354;
        public const int Err_Sol_Low_Cyl_Split_Tray_01_02_Ch2 = 355;
        public const int Err_Sol_Hight_Cyl_Split_Tray_03_04_Ch2 = 356;
        public const int Err_Sol_Low_Cyl_Split_Tray_03_04_Ch2 = 357;
        public const int Err_Sol_Low_Cyl_Up_Down_Tray_Ch2 = 358;
        public const int Err_Sol_Center_Cyl_Up_Down_Tray_Ch2 = 359;
        public const int Err_Sol_Hight_Cyl_Up_Down_Tray_Ch2 = 360;
        public const int Err_Sol_Hight_Cyl_Clamp_Pull_Tray_Ch2 = 361;
        public const int Err_Sol_Low_Cyl_Clamp_Pull_Tray_Ch2 = 362;
        public const int Err_Sol_Hight_Cyl_Keep_Center_Tray_01_02_Ch2 = 363;
        public const int Err_Sol_Low_Cyl_Keep_Center_Tray_01_02_Ch2 = 364;
        public const int Err_Sol_Hight_Cyl_Center_Tray_Ch2 = 365;
        public const int Err_Sol_Low_Cyl_Center_Tray_Ch2 = 366;
        public const int Err_Sol_Hight_Cyl_Up_Down_Out_Tray_Ch2 = 367;
        public const int Err_Sol_Low_Cyl_Up_Down_Out_Tray_Ch2 = 368;
        public const int Err_Sol_Hight_Cyl_In_Out_Jig_Ch2 = 369;
        public const int Err_Sol_Low_Cyl_In_Out_Jig_Ch2 = 370;
        public const int Err_Sol_Hight_Cyl_Up_Down_Lamp_Vision_Ch2 = 371;
        public const int Err_Sol_Low_Cyl_Up_Down_Lamp_Vision_Ch2 = 372;
        public const int Alarm_Servo_Transfer_Tray_Ch2 = 373;
        public const int Err_Limit_add_Servo_Transfer_Tray_Ch2 = 374;
        public const int Err_Limit_minus_Servo_Transfer_Tray_Ch2 = 375;
        public const int Signal_Not_Ready_Servo_Transfer_Tray_Ch2 = 376;
        public const int Err_Part_Tray_Input_Empty_Tray_Ch2 = 377;
        public const int Err_Part_Tray_Input_Wrong_Position_Tray_Ch2 = 378;
        public const int Err_Part_Tray_Center_Wrong_Position_Tray_Ch2 = 379;
        public const int Err_Part_Tray_Output_Full_Tray_Ch2 = 380;
        public const int Err_Part_Tray_Output_Wrong_Position_Tray_Ch2 = 381;
        public const int Err_Not_Detect_Tray_In_Center_Tray_Ch2 = 382;
        public const int Err_Not_Detect_Jig_Ch2 = 383;
        public const int Err_Check_Tray_CH2 = 384;
        public const int Err_Check_Jig_Ch2 = 385;


        public const int Err_Connect_Robot_Ch2 = 386;
        public const int Err_Empty_Oil_AxisZ_Robot_Ch2 = 388;
        public const int Err_Current_Pos_AxisZ_Higher_Limit_Robot_Ch2 = 389;
        public const int Err_Collision_Robot_Ch2 = 390;
        public const int Err_Vaccum_Frame_Robot_Ch2 = 391;
        public const int Err_Vaccum_Tool_1_Robot_Ch2 = 392;
        public const int Err_Vaccum_Tool_2_Robot_Ch2 = 393;
        public const int Err_Vaccum_Tool_3_Robot_Ch2 = 394;
        public const int Err_Down_Frame_Robot_Ch2 = 395;
        public const int Err_Up_Frame_Robot_Ch2 = 396;
        public const int Err_Down_Tool_1_Robot_Ch2 = 397;
        public const int Err_Up_Tool_1_Robot_Ch2 = 398;
        public const int Err_Down_Tool_2_Robot_Ch2 = 399;
        public const int Err_Up_Tool_2_Robot_Ch2 = 400;
        public const int Err_Down_Tool_3_Robot_Ch2 = 401;
        public const int Err_Up_Tool_3_Robot_Ch2 = 402;
        public const int Err_Pallet_Point_Out_Of_Range_Robot_Ch2 = 403;
        public const int Err_Safety_Sensor_Outtray_Ch2_Pause_Rb2 = 404;
        public const int Err_Safety_Sensor_Supply_Magazine_Pause_Rb2 = 405;
        public const int Err_Safety_Sensor_Intray_Ch2_Pause_Rb2 = 406;

        public const int Err_Camera_Machine = 30006;

        public const int NEW_LOT_REQUIRE = 31157;
        public const int LOT_END = 31212;

        public const int EV_PLC_READ_BIT_ERROR = 10;
        public const int EV_PLC_WRITE_BIT_ERROR = 11;
        public const int EV_PLC_READ_WORD_ERROR = 12;
        public const int EV_PLC_WRITE_WORD_ERROR = 13;

        public const int EV_SCANNER_READ_ERROR = 20;

        public const int EV_MES_READY_TIMEOUT = 30;
        public const int EV_MES_CHECK_TIMEOUT = 31;

        public const int EV_TESTER_NODATA = 40;
        public const int EV_TESTER_CONFIRM_ERROR = 41;

        public const int EV_AUTO_START_FAILED = 50;
        public const int EV_AUTO_STOP_FAILED = 51;

        public int Id { get; set; }
        public String CreatedTime { get; set; }
        public String Message { get; set; }
        public int EventType { get; set; }

        public EventLog()
        {
        }

        public EventLog(int ev) {
            this.Id = 0;
            this.EventType = ev;
            this.CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff");
            this.Message = getMessageFromEvent(ev);
        }

        public EventLog(int ev, String detail = null) {
            this.Id = 0;
            this.EventType = ev;
            this.CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff");
            this.Message = getMessageFromEvent(ev);
            if (detail != null) {
                this.Message += detail;
            }
        }

        private static String getMessageFromEvent(int ev) {
            switch (ev) {
                case EV_PLC_READ_BIT_ERROR:
                    return "PLC.READ_BIT failed";

                case EV_PLC_WRITE_BIT_ERROR:
                    return "PLC.WRITE_BIT failed";

                case EV_PLC_READ_WORD_ERROR:
                    return "PLC.READ_WORD failed";

                case EV_PLC_WRITE_WORD_ERROR:
                    return "PLC.WRITE_WORD failed";

                case EV_SCANNER_READ_ERROR:
                    return "SCANNER.Read error.";

                case EV_MES_READY_TIMEOUT:
                    return "MES.CHECK_READY ERROR";

                case EV_MES_CHECK_TIMEOUT:
                    return "MES.CHECK_QR error.";

                case EV_TESTER_NODATA:
                    return "TESTER.REQUEST: NO JIG DATA";

                case EV_TESTER_CONFIRM_ERROR:
                    return "TESTER.CONFIRM FAILED";

                case EV_AUTO_START_FAILED:
                    return "AUTO.Start failed";

                case EV_AUTO_STOP_FAILED:
                    return "AUTO.Stop failed";


                // Nam Add log
                case Err_Sol_Hight_Cyl_Hold_Tray_01_02_Ch1:
                    return "Lỗi xy lanh giữ tray số 1 và 2 kênh 1 không đi ra";
                case Err_Sol_Low_Cyl_Hold_Tray_01_02_Ch1:
                    return "Lỗi xy lanh giữ tray số 1 và 2 kênh 1 không đi về";
                case Err_Sol_Hight_Cyl_Hold_Tray_03_04_Ch1:
                    return "Lỗi xy lanh giữ tray số 3 và 4 kênh 1 không đi ra";
                case Err_Sol_Low_Cyl_Hold_Tray_03_04_Ch1:
                    return "Lỗi xy lanh giữ tray số 3 và 4 kênh 1 không đi về";
                case Err_Sol_Hight_Cyl_Split_Tray_01_02_Ch1:
                    return "Lỗi xy lanh tách tray số 1 và 2 kênh 1 không đi ra";
                case Err_Sol_Low_Cyl_Split_Tray_01_02_Ch1:
                    return "Lỗi xy lanh tách tray số 1 và 2 kênh 1 không đi về";
                case Err_Sol_Hight_Cyl_Split_Tray_03_04_Ch1:
                    return "Lỗi xy lanh tách tray số 3 và 4 kênh 1 không đi ra";
                case Err_Sol_Low_Cyl_Split_Tray_03_04_Ch1:
                    return "Lỗi xy lanh tách tray số 3 và 4 kênh 1 không đi về";
                case Err_Sol_Low_Cyl_Up_Down_Tray_Ch1:
                    return "Lỗi xy lanh nâng hạ tray cụm In Tray kênh 1 không hạ xuống";
                case Err_Sol_Center_Cyl_Up_Down_Tray_Ch1:
                    return "Lỗi xy lanh nâng hạ tray cụm In Tray kênh 1 không đến vị trí giữa";
                case Err_Sol_Hight_Cyl_Up_Down_Tray_Ch1:
                    return "Lỗi xy lanh nâng hạ tray cụm In Tray kênh 1 không đi lên";
                case Err_Sol_Hight_Cyl_Clamp_Pull_Tray_Ch1:
                    return "Lỗi xy lanh kẹp kéo tray cụm Center Tray kênh 1 không đi ra";
                case Err_Sol_Low_Cyl_Clamp_Pull_Tray_Ch1:
                    return "Lỗi xy lanh kẹp kéo tray cụm Center Tray kênh 1 không đi về";
                case Err_Sol_Hight_Cyl_Keep_Center_Tray_01_02_Ch1:
                    return "Lỗi xy lanh kẹp tray số 1 và 2 cụm Center Tray kênh 1 không đi ra";
                case Err_Sol_Low_Cyl_Keep_Center_Tray_01_02_Ch1:
                    return "Lỗi xy lanh kẹp tray số 1 và 2 cụm Center Tray kênh 1 không đi về";
                case Err_Sol_Hight_Cyl_Center_Tray_Ch1:
                    return "Lỗi xy lanh Center tray số 1 và 2 cụm Center Tray kênh 1 không đi ra";
                case Err_Sol_Low_Cyl_Center_Tray_Ch1:
                    return "Lỗi xy lanh Center tray số 1 và 2 cụm Center Tray kênh 1 không đi về";
                case Err_Sol_Hight_Cyl_Up_Down_Out_Tray_Ch1:
                    return "Lỗi xy lanh nâng hạ cụm Out tray kênh 1 không đi lên";
                case Err_Sol_Low_Cyl_Up_Down_Out_Tray_Ch1:
                    return "Lỗi xy lanh nâng hạ cụm Out tray kênh 1 không đi xuống";
                case Err_Sol_Hight_Cyl_In_Out_Jig_Ch1:
                    return "Lỗi xy lanh vào ra cụm Jig kênh 1 không đi ra";
                case Err_Sol_Low_Cyl_In_Out_Jig_Ch1:
                    return "Lỗi xy lanh vào ra cụm Jig kênh 1 không đi vào";
                case Err_Sol_Hight_Cyl_Up_Down_Lamp_Vision_Ch1:
                    return "Lỗi xy lanh nâng hạ đèn Vision kênh 1 không đi ra ";
                case Err_Sol_Low_Cyl_Up_Down_Lamp_Vision_Ch1:
                    return "Lỗi xy lanh nâng hạ đèn Vision kênh 1 không đi về";
                case Err_Sol_Hight_Cyl_Push_Carriage:
                    return "Lỗi xy lanh đẩy Carriage không đi ra";
                case Err_Sol_Low_Cyl_Push_Carriage:
                    return "Lỗi xy lanh đẩy Carriage không đi về";
                case Err_Sol_Hight_Cyl_Clamp_Magazine:
                    return "Lỗi xy lanh kẹp Magazine không đi ra";
                case Err_Sol_Low_Cyl_Clamp_Magazine:
                    return "Lỗi xy lanh kẹp Magazine không đi về";
                case Err_Sol_Hight_Cyl_Up_Down_Carrigae:
                    return "Lỗi xy lanh nâng hạ Carriage không đi ra";
                case Err_Sol_Low_Cyl_Up_Down_Carrigae:
                    return "Lỗi xy lanh nâng hạ Carriage không đi về";
                case Alarm_Servo_Transfer_Tray_Ch1:
                    return "Lỗi động cơ Servo cụm di chuyển tray kênh 1";
                case Err_Limit_add_Servo_Transfer_Tray_Ch1:
                    return "Lỗi động cơ Servo chạm giới hạn trên cụm di chuyển tray kênh 1";
                case Err_Limit_minus_Servo_Transfer_Tray_Ch1:
                    return "Lỗi động cơ Servo chạm giới hạn dưới cụm di chuyển tray kênh 1";
                case Signal_Not_Ready_Servo_Transfer_Tray_Ch1:
                    return "Lỗi động cơ Servo chưa sẵn sàng chạy cụm di chuyển tray kênh 1";
                case Signal_Not_Ready_Servo_Up_Down_Magazine:
                    return "Lỗi động cơ Servo chưa sẵn sàng chạy cụm nâng hạ Magazine";
                case Alarm_Servo_Up_Down_Magazine:
                    return "Lỗi động cơ Servo cụm nâng hạ Magazine";
                case Err_Limit_add_Servo_Up_Down_Magazine:
                    return "Lỗi động cơ Servo chạm giới hạn trên cụm nâng hạ Magazine";
                case Err_Limit_minus_Servo_Up_Down_Magazine:
                    return "Lỗi động cơ Servo chạm giới dưới trên cụm nâng hạ Magazine";
                case Signal_Not_Ready_Servo_Pull_Carrigae:
                    return "Lỗi động cơ Servo chưa sẵn sàng chạy cụm nâng hạ kéo Carriage";
                case Alarm_Servo_Pull_Carrigae:
                    return "Lỗi động cơ Servo cụm nâng hạ kéo Carriage";
                case Err_Limit_add_Servo_Pull_Carrigae:
                    return "Lỗi động cơ Servo chạm giới hạn trên cụm nâng hạ kéo Carriage";
                case Err_Limit_minus_Servo_Pull_Carrigae:
                    return "Lỗi động cơ Servo chạm giới hạn dưới cụm nâng hạ kéo Carriage";
                case Err_Part_Tray_Input_Empty_Tray_Ch1:
                    return "Lỗi hết tray cụm In Tray kênh 1";
                case Err_Part_Tray_Input_Wrong_Position_Tray_Ch1:
                    return "Lỗi tray nằm sai vị trí cụm In Tray kênh 1";
                case Err_Part_Tray_Center_Wrong_Position_Tray_Ch1:
                    return "Lỗi tray nằm sai vị trí cụm Center Tray kênh 1";
                case Err_Part_Tray_Output_Full_Tray_Ch1:
                    return "Lỗi đầy tray cụm Out Tray kênh 1";
                case Err_Part_Tray_Output_Wrong_Position_Tray_Ch1:
                    return "Lỗi tray nằm sai vị trí cụm Out Tray kênh 1";
                case Err_Not_Detect_Tray_In_Center_Tray_Ch1:
                    return "Lỗi không phát hiện tray ở cụm Center Tray kênh 1";
                case Err_Not_Detect_Jig_Ch1:
                    return "Lỗi không phát hiện Jig ở cụm cấp Jig kênh 1";
                case Err_Stuck_Push_Carriage_In_Magazine:
                    return "Lỗi kẹt khi đẩy Carrigae trong Magazine";
                case Err_Position_Carriage_In_Magazine_Not_Safety:
                    return "Lỗi Carriage ở vị trí không an toàn";
                case Err_Not_Detect_Carriage_Part_Pull_Cariage:
                    return "Lỗi không phát hiện Carriage cụm kéo Carriage";
                case Err_Stuck_Hang_Carriage_In_Part_Pull_Cariage:
                    return "Lỗi kẹt khi móc Carriage cụm kéo Carriage";
                case Err_Ping_IP_Plc:
                    return "Lỗi mất kết nối PLC";
                case Err_Check_Tray_CH1:
                    return "Lỗi có tray trên cụm cấp Tray Kênh 1";
                case Err_Check_Megarin_CH1:
                    return "Lỗi check Megarin Kênh 1";
                case Err_Check_Jig_Ch1:
                    return "Lỗi check Jig Kênh 1";
                case Err_Connect_Robot_Ch1:
                    return "Lỗi kết nối Etherner Robot Kênh 1";
                case Err_Empty_Oil_AxisZ_Robot_Ch1:
                    return "Lỗi khô dầu trục Z của Robot kênh 1";
                case Err_Current_Pos_AxisZ_Higher_Limit_Robot_Ch1:
                    return "Lỗi vị trí hiện tại của Robot kênh 1 đang quá giới hạn trục Z";
                case Err_Collision_Robot_Ch1:
                    return "Lỗi va chạm của Robot kênh 1 ";
                case Err_Vaccum_Frame_Robot_Ch1:
                    return "Lỗi khí hút Frame của Robot kênh 1 ";
                case Err_Vaccum_Tool_1_Robot_Ch1:
                    return "Lỗi khí hút Tool 1 của Robot kênh 1 ";
                case Err_Vaccum_Tool_2_Robot_Ch1:
                    return "Lỗi khí hút Tool 2 của Robot kênh 1 ";
                case Err_Vaccum_Tool_3_Robot_Ch1:
                    return "Lỗi khí hút Tool 3 của Robot kênh 1 ";
                case Err_Down_Frame_Robot_Ch1:
                    return "Lỗi cảm biến khi hạ Frame xuống của Robot kênh 1 ";
                case Err_Up_Frame_Robot_Ch1:
                    return "Lỗi cảm biến khi nâng Frame lên của Robot kênh 1 ";
                case Err_Down_Tool_1_Robot_Ch1:
                    return "Lỗi cảm biến khi hạ Tool 1 xuống của Robot kênh 1 ";
                case Err_Up_Tool_1_Robot_Ch1:
                    return "Lỗi cảm biến khi nâng Tool 1 lên của Robot kênh 1 ";
                case Err_Down_Tool_2_Robot_Ch1:
                    return "Lỗi cảm biến khi hạ Tool 2 xuống của Robot kênh 1 ";
                case Err_Up_Tool_2_Robot_Ch1:
                    return "Lỗi cảm biến khi nâng Tool 2 lên của Robot kênh 1 ";
                case Err_Down_Tool_3_Robot_Ch1:
                    return "Lỗi cảm biến khi hạ Tool 3 xuống của Robot kênh 1 ";
                case Err_Up_Tool_3_Robot_Ch1:
                    return "Lỗi cảm biến khi nâng Tool 3 lên của Robot kênh 1 ";
                case Err_Pallet_Point_Out_Of_Range_Robot_Ch1:
                    return "Lỗi điểm Palet của Robot kênh 1 ngoài dải";
                case Err_Camera_Machine:
                    return "Lỗi mở CAMERA";
                case Err_Open_Door:
                    return "Lỗi mở cửa";
                case Err_Emergency_Button_Active:
                    return "Lỗi nút Emergency bị tác động";
                case Err_Safety_Sensor_Outtray_Ch1_Pause_Rb1:
                    return "Lỗi cảm biến an toàn cụm Outtray kênh 1 tác động lâu làm dừng Robot 1 ";
                case Err_Safety_Sensor_Supply_Magazine_Pause_Rb1:
                    return "Lỗi cảm biến an toàn cụm cấp magazine tác động lâu làm dừng Robot 1   ";
                case Err_Safety_Sensor_Intray_Ch1_Pause_Rb1:
                    return "Lỗi cảm biến an toàn cụm Intray kênh 1 tác động lâu làm dừng Robot 1  ";
                case Err_Safety_Sensor_Intray_Ch1:
                    return "Lỗi cảm biến an toàn cụm Intray kênh 1   ";
                case Err_Safety_Sensor_Outtray_Ch1:
                    return "Lỗi cảm biến an toàn cụm Outtray kênh 1   ";
                case Err_Safety_Sensor_Supply_Magazine:
                    return "Lỗi cảm biến an toàn cụm cấp magazine  ";
                case Err_Safety_Sensor_Intray_Ch2:
                    return "Lỗi cảm biến an toàn cụm Intray kênh 2  ";
                case Err_Safety_Sensor_Outtray_Ch2:
                    return "Lỗi cảm biến an toàn cụm Outtray kênh 2   ";



                case Err_Sol_Hight_Cyl_Hold_Tray_01_02_Ch2:
                    return "Lỗi xy lanh giữ tray số 1 và 2 kênh 2 không đi ra";
                case Err_Sol_Low_Cyl_Hold_Tray_01_02_Ch2:
                    return "Lỗi xy lanh giữ tray số 1 và 2 kênh 2 không đi về";
                case Err_Sol_Hight_Cyl_Hold_Tray_03_04_Ch2:
                    return "Lỗi xy lanh giữ tray số 3 và 4 kênh 2 không đi ra";
                case Err_Sol_Low_Cyl_Hold_Tray_03_04_Ch2:
                    return "Lỗi xy lanh giữ tray số 3 và 4 kênh 2 không đi về";
                case Err_Sol_Hight_Cyl_Split_Tray_01_02_Ch2:
                    return "Lỗi xy lanh tách tray số 1 và 2 kênh 2 không đi ra";
                case Err_Sol_Low_Cyl_Split_Tray_01_02_Ch2:
                    return "Lỗi xy lanh tách tray số 1 và 2 kênh 2 không đi về";
                case Err_Sol_Hight_Cyl_Split_Tray_03_04_Ch2:
                    return "Lỗi xy lanh tách tray số 3 và 4 kênh 2 không đi ra";
                case Err_Sol_Low_Cyl_Split_Tray_03_04_Ch2:
                    return "Lỗi xy lanh tách tray số 3 và 4 kênh 2 không đi về";
                case Err_Sol_Low_Cyl_Up_Down_Tray_Ch2:
                    return "Lỗi xy lanh nâng hạ tray cụm In Tray kênh 2 không hạ xuống";
                case Err_Sol_Center_Cyl_Up_Down_Tray_Ch2:
                    return "Lỗi xy lanh nâng hạ tray cụm In Tray kênh 2 không đến vị trí giữa";
                case Err_Sol_Hight_Cyl_Up_Down_Tray_Ch2:
                    return "Lỗi xy lanh nâng hạ tray cụm In Tray kênh 2 không đi lên";
                case Err_Sol_Hight_Cyl_Clamp_Pull_Tray_Ch2:
                    return "Lỗi xy lanh kẹp kéo tray cụm Center Tray kênh 2 không đi ra";
                case Err_Sol_Low_Cyl_Clamp_Pull_Tray_Ch2:
                    return "Lỗi xy lanh kẹp kéo tray cụm Center Tray kênh 2 không đi về";
                case Err_Sol_Hight_Cyl_Keep_Center_Tray_01_02_Ch2:
                    return "Lỗi xy lanh kẹp tray số 1 và 2 cụm Center Tray kênh 2 không đi ra";
                case Err_Sol_Low_Cyl_Keep_Center_Tray_01_02_Ch2:
                    return "Lỗi xy lanh kẹp tray số 1 và 2 cụm Center Tray kênh 2 không đi về";
                case Err_Sol_Hight_Cyl_Center_Tray_Ch2:
                    return "Lỗi xy lanh Center tray số 1 và 2 cụm Center Tray kênh 2 không đi ra";
                case Err_Sol_Low_Cyl_Center_Tray_Ch2:
                    return "Lỗi xy lanh Center tray số 1 và 2 cụm Center Tray kênh 2 không đi về";
                case Err_Sol_Hight_Cyl_Up_Down_Out_Tray_Ch2:
                    return "Lỗi xy lanh nâng hạ cụm Out tray kênh 2 không đi lên";
                case Err_Sol_Low_Cyl_Up_Down_Out_Tray_Ch2:
                    return "Lỗi xy lanh nâng hạ cụm Out tray kênh 2 không đi xuống";
                case Err_Sol_Hight_Cyl_In_Out_Jig_Ch2:
                    return "Lỗi xy lanh vào ra cụm Jig kênh2 không đi ra";
                case Err_Sol_Low_Cyl_In_Out_Jig_Ch2:
                    return "Lỗi xy lanh vào ra cụm Jig kênh 1 không đi vào";
                case Err_Sol_Hight_Cyl_Up_Down_Lamp_Vision_Ch2:
                    return "Lỗi xy lanh nâng hạ đèn Vision kênh 2 không đi ra ";
                case Err_Sol_Low_Cyl_Up_Down_Lamp_Vision_Ch2:
                    return "Lỗi xy lanh nâng hạ đèn Vision kênh 2 không đi về";
                case Alarm_Servo_Transfer_Tray_Ch2:
                    return "Lỗi động cơ Servo cụm di chuyển tray kênh 2";
                case Err_Limit_add_Servo_Transfer_Tray_Ch2:
                    return "Lỗi động cơ Servo chạm giới hạn trên cụm di chuyển tray kênh 2";
                case Err_Limit_minus_Servo_Transfer_Tray_Ch2:
                    return "Lỗi động cơ Servo chạm giới hạn dưới cụm di chuyển tray kênh 2";
                case Signal_Not_Ready_Servo_Transfer_Tray_Ch2:
                    return "Lỗi động cơ Servo chưa sẵn sàng chạy cụm di chuyển tray kênh 2";
                case Err_Part_Tray_Input_Empty_Tray_Ch2:
                    return "Lỗi hết tray cụm In Tray kênh 2";
                case Err_Part_Tray_Input_Wrong_Position_Tray_Ch2:
                    return "Lỗi tray nằm sai vị trí cụm In Tray kênh 2";
                case Err_Part_Tray_Center_Wrong_Position_Tray_Ch2:
                    return "Lỗi tray nằm sai vị trí cụm Center Tray kênh 2";
                case Err_Part_Tray_Output_Full_Tray_Ch2:
                    return "Lỗi đầy tray cụm Out Tray kênh 2";
                case Err_Part_Tray_Output_Wrong_Position_Tray_Ch2:
                    return "Lỗi tray nằm sai vị trí cụm Out Tray kênh 2";
                case Err_Not_Detect_Tray_In_Center_Tray_Ch2:
                    return "Lỗi không phát hiện tray ở cụm Center Tray kênh 2";
                case Err_Not_Detect_Jig_Ch2:
                    return "Lỗi không phát hiện Jig ở cụm cấp Jig kênh 2";
                case Err_Check_Tray_CH2:
                    return "Lỗi có tray trên cụm cấp Tray Kênh 2";

                case Err_Check_Jig_Ch2:
                    return "Lỗi check Jig Kênh 2";
                case Err_Connect_Robot_Ch2:
                    return "Lỗi kết nối Etherner Robot Kênh 2";
                case Err_Empty_Oil_AxisZ_Robot_Ch2:
                    return "Lỗi khô dầu trục Z của Robot kênh 2";
                case Err_Current_Pos_AxisZ_Higher_Limit_Robot_Ch2:
                    return "Lỗi vị trí hiện tại của Robot kênh 2 đang quá giới hạn trục Z";
                case Err_Collision_Robot_Ch2:
                    return "Lỗi va chạm của Robot kênh 2 ";
                case Err_Vaccum_Frame_Robot_Ch2:
                    return "Lỗi khí hút Frame của Robot kênh 2 ";
                case Err_Vaccum_Tool_1_Robot_Ch2:
                    return "Lỗi khí hút Tool 1 của Robot kênh 2 ";
                case Err_Vaccum_Tool_2_Robot_Ch2:
                    return "Lỗi khí hút Tool 2 của Robot kênh 2 ";
                case Err_Vaccum_Tool_3_Robot_Ch2:
                    return "Lỗi khí hút Tool 3 của Robot kênh 2 ";
                case Err_Down_Frame_Robot_Ch2:
                    return "Lỗi cảm biến khi hạ Frame xuống của Robot kênh 2 ";
                case Err_Up_Frame_Robot_Ch2:
                    return "Lỗi cảm biến khi nâng Frame lên của Robot kênh 2 ";
                case Err_Down_Tool_1_Robot_Ch2:
                    return "Lỗi cảm biến khi hạ Tool 1 xuống của Robot kênh 2 ";
                case Err_Up_Tool_1_Robot_Ch2:
                    return "Lỗi cảm biến khi nâng Tool 1 lên của Robot kênh 2 ";
                case Err_Down_Tool_2_Robot_Ch2:
                    return "Lỗi cảm biến khi hạ Tool 2 xuống của Robot kênh 2 ";
                case Err_Up_Tool_2_Robot_Ch2:
                    return "Lỗi cảm biến khi nâng Tool 2 lên của Robot kênh 2 ";
                case Err_Down_Tool_3_Robot_Ch2:
                    return "Lỗi cảm biến khi hạ Tool 3 xuống của Robot kênh 2 ";
                case Err_Up_Tool_3_Robot_Ch2:
                    return "Lỗi cảm biến khi nâng Tool 3 lên của Robot kênh 2 ";
                case Err_Pallet_Point_Out_Of_Range_Robot_Ch2:
                    return "Lỗi điểm Palet của Robot kênh 2 ngoài dải";
                case Err_Safety_Sensor_Outtray_Ch2_Pause_Rb2:
                    return "Lỗi cảm biến an toàn cụm Outtray kênh 2 tác động lâu làm dừng Robot 2   ";
                case Err_Safety_Sensor_Supply_Magazine_Pause_Rb2:
                    return "Lỗi cảm biến an toàn cụm cấp magazine tác động lâu làm dừng Robot 2  ";
                case Err_Safety_Sensor_Intray_Ch2_Pause_Rb2:
                    return "Lỗi cảm biến an toàn cụm Outtray kênh 2 tác động lâu làm dừng Robot 2   ";


                case NEW_LOT_REQUIRE:
                    return "The new Lot will ba changed";
                case LOT_END:
                    return "The Lot Input count is reached. Take a new Lot";
            }
            return "";
        }
    }
}
