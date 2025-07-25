using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace VisionInspection
{
    class DbRead
    {
        public const String QR1_STATUS_KEY = "run";
        public const String SCANNER_STATUS_KEY = "scanner";

        private static MyLogger logger = new MyLogger("DbRead");

        //public static bool IsPcmExisting(String lotId, String qr) {
        //    var ret = false;

        //    using (var conn = Dba.GetConnection()) {
        //        var sql = "SELECT id FROM pcm_data WHERE lot_id = @lot AND qr = @qr";
        //        using (var sqlCmd = conn.CreateCommand()) {
        //            try {
        //                sqlCmd.CommandText = sql;
        //                sqlCmd.Parameters.AddWithValue("@lot", lotId);
        //                sqlCmd.Parameters.AddWithValue("@qr", qr);
        //                conn.Open();
        //                using (var reader = sqlCmd.ExecuteReader()) {
        //                    if (reader.Read()) {
        //                        ret = true;
        //                    }
        //                }
        //            } catch (Exception ex) {
        //                logger.Create("IsPcmExisting error:" + ex.Message);
        //            }
        //        }
        //    }

        //    return ret;
        //}

        public static string ReadAlarmImage(int id)
        {
            string nameImage = null;
            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT NameImage FROM AlarmImage_log WHERE id = @id";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@id", id);

                        conn.Open();
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                nameImage = reader.GetString(0); // Lấy giá trị cột NameImage từ kết quả truy vấn
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("ReadAlarmImage Error: " + ex.Message);
                    }
                }
            }
            return nameImage;
        }
        public static List<AlarmInfo> GetLatestAlarms(int limit)
        {
            var ret = new List<AlarmInfo>();

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT * FROM alarm_log ORDER BY created_time DESC LIMIT @limit";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@limit", limit);
                        conn.Open();
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var x = new AlarmInfo();
                                Object obj;

                                if ((obj = reader["id"]) != DBNull.Value)
                                {
                                    x.id = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["created_time"]) != DBNull.Value)
                                {
                                    x.createdTime = DateTime.Parse(obj.ToString());
                                }
                                if ((obj = reader["alarm_code"]) != DBNull.Value)
                                {
                                    x.alarmCode = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["message"]) != DBNull.Value)
                                {
                                    x.message = obj.ToString();
                                }
                                if ((obj = reader["solution"]) != DBNull.Value)
                                {
                                    x.solution = obj.ToString();
                                }
                                if ((obj = reader["mode"]) != DBNull.Value)
                                {
                                    x.mode = int.Parse(obj.ToString());
                                }
                                ret.Add(x);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("GetLatestAlarms error:" + ex.Message);
                    }
                }
            }

            return ret;
        }

        public static LotStatus GetLotStatus(String lotId)
        {
            LotStatus ret = null;

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT * FROM lot_status WHERE lot_id = @lot";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@lot", lotId);
                        conn.Open();
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Object obj;
                                ret = new LotStatus();

                                if ((obj = reader["id"]) != DBNull.Value)
                                {
                                    ret.Id = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["updated_time"]) != DBNull.Value)
                                {
                                    ret.UpdatedTime = DateTime.Parse(obj.ToString());
                                }
                                if ((obj = reader["input_count"]) != DBNull.Value)
                                {
                                    ret.InputCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["total_count"]) != DBNull.Value)
                                {
                                    ret.TotalCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["ng_count"]) != DBNull.Value)
                                {
                                    ret.NgCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["em_count"]) != DBNull.Value)
                                {
                                    ret.EmCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["total_time"]) != DBNull.Value)
                                {
                                    ret.totalSeconds = int.Parse(obj.ToString());
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("GetLotStatus error:" + ex.Message);
                    }
                }
            }

            return ret;
        }

        public static LotStatus GetCurrentLotStatus()
        {
            LotStatus ret = null;

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT * FROM lot_status ORDER BY updated_time DESC LIMIT 1";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        conn.Open();
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Object obj;
                                ret = new LotStatus();

                                if ((obj = reader["id"]) != DBNull.Value)
                                {
                                    ret.Id = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["lot_id"]) != DBNull.Value)
                                {
                                    ret.LotId = obj.ToString();
                                }
                                if ((obj = reader["updated_time"]) != DBNull.Value)
                                {
                                    ret.UpdatedTime = DateTime.Parse(obj.ToString());
                                }
                                if ((obj = reader["input_count"]) != DBNull.Value)
                                {
                                    ret.InputCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["total_count"]) != DBNull.Value)
                                {
                                    ret.TotalCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["ng_count"]) != DBNull.Value)
                                {
                                    ret.NgCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["em_count"]) != DBNull.Value)
                                {
                                    ret.EmCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["total_time"]) != DBNull.Value)
                                {
                                    ret.totalSeconds = int.Parse(obj.ToString());
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("GetCurrentLotStatus error:" + ex.Message);
                    }
                }
            }

            return ret;
        }

        public static VisionInspectionStatus GetQr1Status()
        {
            VisionInspectionStatus ret = null;

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT * FROM qr1_status WHERE key = @key";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@key", QR1_STATUS_KEY);
                        conn.Open();
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            ret = new VisionInspectionStatus();
                            if (reader.Read())
                            {
                                Object obj;

                                if ((obj = reader["value"]) != DBNull.Value)
                                {
                                    var js = obj.ToString();
                                    ret = VisionInspectionStatus.FromJSON(js);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("GetQr1Status error:" + ex.Message);
                    }
                }
            }

            return ret;
        }

        public static ScannerStatus GetScannerStatus()
        {
            ScannerStatus ret = null;

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT * FROM qr1_status WHERE key = @key";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@key", SCANNER_STATUS_KEY);
                        conn.Open();
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            ret = new ScannerStatus();
                            if (reader.Read())
                            {
                                Object obj;

                                if ((obj = reader["value"]) != DBNull.Value)
                                {
                                    var js = obj.ToString();
                                    ret = ScannerStatus.FromJSON(js);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("GetScannerStatus error:" + ex.Message);
                    }
                }
            }

            return ret;
        }

        public static List<EventLog> GetEvents(int pageIndex, int pageSize)
        {
            var ret = new List<EventLog>();

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT * FROM event_log LIMIT @limit OFFSET @offset";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@limit", pageSize);
                        sqlCmd.Parameters.AddWithValue("@offset", pageIndex * pageSize);
                        conn.Open();
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var x = new EventLog();
                                Object obj;

                                if ((obj = reader["id"]) != DBNull.Value)
                                {
                                    x.Id = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["created_time"]) != DBNull.Value)
                                {
                                    x.CreatedTime = obj.ToString();
                                }
                                if ((obj = reader["event_type"]) != DBNull.Value)
                                {
                                    x.EventType = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["message"]) != DBNull.Value)
                                {
                                    x.Message = obj.ToString();
                                }
                                ret.Add(x);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("GetEvents error:" + ex.Message);
                    }
                }
            }

            return ret;
        }

        public static int CountEvents()
        {
            int ret = 0;

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT COUNT(*) FROM event_log";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        conn.Open();
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Object obj;

                                if ((obj = reader[0]) != DBNull.Value)
                                {
                                    ret = int.Parse(obj.ToString());
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("CountEvents error:" + ex.Message);
                    }
                }
            }

            return ret;
        }

        public static List<UserLog> GetUserLogs(DateTime day, int pageIndex, int pageSize)
        {
            var ret = new List<UserLog>();

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT * FROM user_log WHERE date(created_time) = date(@date) LIMIT @limit OFFSET @offset";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@date", day.ToString("yyyy-MM-dd"));
                        sqlCmd.Parameters.AddWithValue("@limit", pageSize);
                        sqlCmd.Parameters.AddWithValue("@offset", pageIndex * pageSize);
                        conn.Open();
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var x = new UserLog();
                                Object obj;

                                if ((obj = reader["id"]) != DBNull.Value)
                                {
                                    x.Id = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["username"]) != DBNull.Value)
                                {
                                    x.Username = obj.ToString();
                                }
                                if ((obj = reader["created_time"]) != DBNull.Value)
                                {
                                    x.CreatedTime = obj.ToString();
                                }
                                if ((obj = reader["action"]) != DBNull.Value)
                                {
                                    x.Action = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["message"]) != DBNull.Value)
                                {
                                    x.Message = obj.ToString();
                                }
                                ret.Add(x);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("GetUserLogs error:" + ex.Message);
                    }
                }
            }

            return ret;
        }

        public static int CountUserLogs(DateTime day)
        {
            int ret = 0;

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT COUNT(*) FROM user_log WHERE date(created_time) = date(@date) ";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@date", day.ToString("yyyy-MM-dd"));
                        conn.Open();
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Object obj;

                                if ((obj = reader[0]) != DBNull.Value)
                                {
                                    ret = int.Parse(obj.ToString());
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("CountUserLogs error:" + ex.Message);
                    }
                }
            }

            return ret;
        }

        public static int GetSpcTotal(DateTime from, DateTime to)
        {
            int ret = 0;

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT COUNT(id) FROM pcm_data WHERE updated_time >= @from AND updated_time <= @to";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd HH:mm:ss"));
                        sqlCmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd HH:mm:ss"));
                        conn.Open();
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Object obj;

                                if ((obj = reader[0]) != DBNull.Value)
                                {
                                    ret = int.Parse(obj.ToString());
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("GetSpcTotal error:" + ex.Message);
                    }
                }
            }

            return ret;
        }

        public static int GetSpcCounter(int pcmResult, DateTime from, DateTime to)
        {
            int ret = 0;

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT COUNT(id) FROM pcm_data WHERE result = @result AND updated_time >= @from AND updated_time <= @to";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@result", pcmResult);
                        sqlCmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd HH:mm:ss"));
                        sqlCmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd HH:mm:ss"));
                        conn.Open();
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Object obj;

                                if ((obj = reader[0]) != DBNull.Value)
                                {
                                    ret = int.Parse(obj.ToString());
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("GetSpcCounter error:" + ex.Message);
                    }
                }
            }

            return ret;
        }

        public static int GetSpcTotalByLot(String lotId, DateTime from, DateTime to)
        {
            int ret = 0;

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT COUNT(id) FROM pcm_data WHERE lot_id = @lot AND updated_time >= @from AND updated_time <= @to";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@lot", lotId);
                        sqlCmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd HH:mm:ss"));
                        sqlCmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd HH:mm:ss"));
                        conn.Open();
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Object obj;

                                if ((obj = reader[0]) != DBNull.Value)
                                {
                                    ret = int.Parse(obj.ToString());
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("GetSpcTotalByLot error:" + ex.Message);
                    }
                }
            }

            return ret;
        }

        public static int GetSpcCounterByLot(String lotId, int pcmResult, DateTime from, DateTime to)
        {
            int ret = 0;

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT COUNT(id) FROM pcm_data WHERE lot_id = @lot AND result = @result AND updated_time >= @from AND updated_time <= @to";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@lot", lotId);
                        sqlCmd.Parameters.AddWithValue("@result", pcmResult);
                        sqlCmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd HH:mm:ss"));
                        sqlCmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd HH:mm:ss"));
                        conn.Open();
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Object obj;

                                if ((obj = reader[0]) != DBNull.Value)
                                {
                                    ret = int.Parse(obj.ToString());
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("GetSpcCounterByLot error:" + ex.Message);
                    }
                }
            }

            return ret;
        }

        public static List<LotStatus> GetAllLotStatus(String lotIdPattern = null)
        {
            var ret = new List<LotStatus>();

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT * FROM lot_status ORDER BY updated_time DESC";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        conn.Open();

                        var endTime = DateTime.Now;
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Object obj;
                                var lot = new LotStatus();

                                if ((obj = reader["id"]) != DBNull.Value)
                                {
                                    lot.Id = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["lot_id"]) != DBNull.Value)
                                {
                                    lot.LotId = obj.ToString();
                                }
                                if ((obj = reader["updated_time"]) != DBNull.Value)
                                {
                                    lot.UpdatedTime = DateTime.Parse(obj.ToString());
                                }
                                if ((obj = reader["input_count"]) != DBNull.Value)
                                {
                                    lot.InputCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["total_count"]) != DBNull.Value)
                                {
                                    lot.TotalCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["ng_count"]) != DBNull.Value)
                                {
                                    lot.NgCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["em_count"]) != DBNull.Value)
                                {
                                    lot.EmCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["total_time"]) != DBNull.Value)
                                {
                                    lot.totalSeconds = int.Parse(obj.ToString());
                                }
                                lot.EndTime = endTime;

                                if (String.IsNullOrEmpty(lotIdPattern) || lot.LotId.Contains(lotIdPattern))
                                {
                                    ret.Add(lot);
                                }

                                // The UpdatedTime of the newer is the previous's EndTime:
                                endTime = lot.UpdatedTime;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("GetAllLotStatus error:" + ex.Message);
                    }
                }
            }

            return ret;
        }

        public static List<LotStatus> GetLotStatusByTime(DateTime from, DateTime to)
        {
            var ret = new List<LotStatus>();

            using (var conn = Dba.GetConnection())
            {
                var sql = "SELECT * FROM lot_status WHERE updated_time >= @from AND updated_time <= @to ORDER BY updated_time DESC";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd HH:mm:ss"));
                        sqlCmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd HH:mm:ss"));
                        conn.Open();

                        var endTime = DateTime.Now;
                        using (var reader = sqlCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Object obj;
                                var lot = new LotStatus();

                                if ((obj = reader["id"]) != DBNull.Value)
                                {
                                    lot.Id = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["lot_id"]) != DBNull.Value)
                                {
                                    lot.LotId = obj.ToString();
                                }
                                if ((obj = reader["updated_time"]) != DBNull.Value)
                                {
                                    lot.UpdatedTime = DateTime.Parse(obj.ToString());
                                }
                                if ((obj = reader["input_count"]) != DBNull.Value)
                                {
                                    lot.InputCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["total_count"]) != DBNull.Value)
                                {
                                    lot.TotalCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["ng_count"]) != DBNull.Value)
                                {
                                    lot.NgCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["em_count"]) != DBNull.Value)
                                {
                                    lot.EmCount = int.Parse(obj.ToString());
                                }
                                if ((obj = reader["total_time"]) != DBNull.Value)
                                {
                                    lot.totalSeconds = int.Parse(obj.ToString());
                                }
                                lot.EndTime = endTime;

                                // The UpdatedTime of the newer is the previous's EndTime:
                                endTime = lot.UpdatedTime;

                                ret.Add(lot);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Create("GetLotStatusByTime error:" + ex.Message);
                    }
                }
            }

            return ret;
        }
    }
}