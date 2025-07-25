using System;

namespace VisionInspection
{
    class AlarmInfo
    {
        // Alarm Codes : Ch1
        public const int SYSTEM_NOT_READY = 30000;
        public const int PLC_COMM_FAILED = 30001;
        public const int CAMERA_COMM_FAILED = 30002;
        public const int VISION_FAILED = 30003;
        public const int QR_READ_ERROR= 1000;

        public static String getMessage(int code)
        {
            var ret = "";
            switch (code)
            {
                case QR_READ_ERROR:
                    ret = "Lỗi đọc Scanner";
                    break;
                case PLC_COMM_FAILED:
                    ret = "Lỗi kết nối PLC";
                    break;
                case CAMERA_COMM_FAILED:
                    ret = "Lỗi kết nối Camera";
                    break;
                case VISION_FAILED:
                    ret = "Lỗi kiểm tra Vision";
                    break;
                case SYSTEM_NOT_READY:
                    ret = "Lôi Hệ thống đang khởi tạo, chưa sẵn sàng chạy Auto";
                    break;

            }
            return ret;
        }

        public static String getSolution(int code)
        {
            var ret = "";
            switch (code)
            {
                case QR_READ_ERROR:
                    ret = String.Format("Kiểm tra lại Đầu đọc QR");
                    break;
                case PLC_COMM_FAILED:
                    ret = String.Format(@"- B1: Tắt toàn bộ chương trình hiện tại và mở lại phần mềm (Tránh trường hợp mở 2 chương trình cùng 1 lúc)
- B2: Nếu đã thực hiện bước 1 chương trình vẫn báo lỗi tương tự thì kiểm tra lại dây kết nối PLC và PC xem có bị tuột không.Nếu cable tuột thì nối / lắp lại và restart lại chương trình.
- B3: Sau khi kiểm tra bước 2 chương trình vẫn báo lỗi thì mở tab PLC Settings xem có đúng cấu hình mặc định như trong manual không, Nếu không sửa lại cho đúng và restart lại chương trình.");
                    break;
                case CAMERA_COMM_FAILED:
                    ret = String.Format(@"- B1: Kiểm tra xem phần mềm cài đặt Camera bên ngoài chương trình có đang bật hay không, nếu có tắt tất cả chương trình đi và bật lại.
- B2: Nếu đã thực hiện bước 1 chương trình vẫn báo lỗi tương tự thì kiểm tra lại dây kết nối Camera và PC xem có bị tuột không.Nếu cable tuột thì nối / lắp lại và restart lại chương trình.
- B3: Sau khi kiểm tra bước 2 chương trình vẫn báo lỗi thì mở tab Settings xem có đúng cấu hình mặc định theo như phần cứng của Camera hay không.");
                    break;
                case VISION_FAILED:
                    ret = String.Format(@"Không bắt được Partern Drop cho ảnh");
                    break;
                case SYSTEM_NOT_READY:
                    ret = "Chờ hệ thống khởi tạo xong trong vài giây và Start lại";
                    break;
            }
            return ret;
        }

        public int id { get; set; }
        public int alarmCode { get; set; }
        public DateTime createdTime { get; set; }
        public int mode { get; set; }
        public String message { get; set; }
        public String solution { get; set; }


        public AlarmInfo() { }

        public AlarmInfo(int code, String msg, String sol)
        {
            // this.id = 0;
            // this.mode = mode;
            this.alarmCode = code;
            this.createdTime = DateTime.Now;
            this.message = msg;
            this.solution = sol;
        }
        public AlarmInfo(Int32 msg, String sol)
        {
            this.createdTime = DateTime.Now;

        }
    }
}

