using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionInspection
{
    class DbWrite
    {
        private static MyLogger logger = new MyLogger("DbWrite");

        public static bool createAlarmImage(int id, string alarm)
        {
            var ret = false;
            using (var conn = Dba.GetConnection())
            {
                var sql = @"INSERT INTO AlarmImage_log (id, NameImage) VALUES (@id, @solution) ON CONFLICT(id) DO UPDATE SET NameImage = excluded.NameImage;";
                //var sql = "INSERT INTO AlarmImage_log (NameImage) VALUES (@solution)";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@id", id);
                        sqlCmd.Parameters.AddWithValue("@solution", alarm.ToString());

                        conn.Open();
                        ret = sqlCmd.ExecuteNonQuery() > 0;
                    }
                    catch (Exception ex)
                    {

                        logger.Create("CreateAlarm Error: " + ex.Message);

                    }
                }
            }
            return ret;
        }
        public static bool createAlarm(AlarmInfo alarm)
        {
            var ret = false;
            using (var conn = Dba.GetConnection())
            {
                var sql = "INSERT INTO alarm_log (created_time, alarm_code, message, solution, mode) VALUES (@time, @code, @message, @solution, @mode)";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@time", alarm.createdTime.ToString("yyyy-MM-dd HH:mm:ss.ff"));
                        sqlCmd.Parameters.AddWithValue("@code", alarm.alarmCode);
                        sqlCmd.Parameters.AddWithValue("@message", alarm.message);
                        sqlCmd.Parameters.AddWithValue("@solution", alarm.solution);
                        sqlCmd.Parameters.AddWithValue("@mode", alarm.mode);

                        conn.Open();
                        ret = sqlCmd.ExecuteNonQuery() > 0;
                    }
                    catch (Exception ex)
                    {
                        logger.Create("createAlarm error:" + ex.Message);
                    }
                }
            }
            return ret;
        }

        //public static bool updatePcm(PcmInfo pcm) {
        //    var ret = false;
        //    using (var conn = Dba.GetConnection()) {
        //        var sql = "UPDATE pcm_data SET result = @result, updated_time = @time WHERE qr = @qr AND lot_id = @lot";
        //        using (var sqlCmd = conn.CreateCommand()) {
        //            try {
        //                sqlCmd.CommandText = sql;
        //                sqlCmd.Parameters.AddWithValue("@result", pcm.result);
        //                sqlCmd.Parameters.AddWithValue("@time", pcm.updatedTime.ToString("yyyy-MM-dd HH:mm:ss.ff"));
        //                sqlCmd.Parameters.AddWithValue("@lot", pcm.lotId);
        //                sqlCmd.Parameters.AddWithValue("@qr", pcm.qr);

        //                conn.Open();
        //                ret = sqlCmd.ExecuteNonQuery() > 0;
        //            } catch (Exception ex) {
        //                logger.Create("updatePcm error:" + ex.Message);
        //            }
        //        }
        //    }
        //    return ret;
        //}

        public static bool createPcm(PcmInfo pcm)
        {
            var ret = false;
            using (var conn = Dba.GetConnection())
            {
                var sql = "INSERT INTO pcm_data (lot_id, qr, result, updated_time) VALUES (@lot, @qr, @result, @time)";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@lot", pcm.lotId);
                        sqlCmd.Parameters.AddWithValue("@qr", pcm.qr);
                        sqlCmd.Parameters.AddWithValue("@result", pcm.result);
                        sqlCmd.Parameters.AddWithValue("@time", pcm.updatedTime.ToString("yyyy-MM-dd HH:mm:ss.ff"));

                        conn.Open();
                        ret = sqlCmd.ExecuteNonQuery() > 0;
                    }
                    catch (Exception ex)
                    {
                        logger.Create("createPcm error:" + ex.Message);
                    }
                }
            }
            return ret;
        }

        public static bool insertLot(LotStatus lot)
        {
            var ret = false;
            using (var conn = Dba.GetConnection())
            {
                var sql = "INSERT INTO lot_status (lot_id, updated_time, input_count, total_count, ng_count, em_count, total_time) " +
                          "VALUES (@lotId, @updatedTime, @inputCount, @totalCount, @ngCount, @emCount, @totalTime)";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@lotId", lot.LotId);
                        sqlCmd.Parameters.AddWithValue("@updatedTime", lot.UpdatedTime.ToString("yyyy-MM-dd HH:mm:ss.ff"));
                        sqlCmd.Parameters.AddWithValue("@inputCount", lot.InputCount);
                        sqlCmd.Parameters.AddWithValue("@totalCount", lot.TotalCount);
                        sqlCmd.Parameters.AddWithValue("@ngCount", lot.NgCount);
                        sqlCmd.Parameters.AddWithValue("@emCount", lot.EmCount);
                        sqlCmd.Parameters.AddWithValue("@totalTime", lot.totalSeconds);

                        conn.Open();
                        ret = sqlCmd.ExecuteNonQuery() > 0;
                    }
                    catch (Exception ex)
                    {
                        logger.Create("insertLot error:" + ex.Message);
                    }
                }
            }
            return ret;
        }

        public static bool updateLotStatus(LotStatus lot)
        {
            var ret = false;
            using (var conn = Dba.GetConnection())
            {
                var sql = "UPDATE lot_status SET updated_time = @updatedTime, total_count = @totalCount, ng_count = @ngCount, em_count = @emCount, total_time = @totalTime " +
                          "WHERE id = @id";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@updatedTime", lot.UpdatedTime.ToString("yyyy-MM-dd HH:mm:ss.ff"));
                        sqlCmd.Parameters.AddWithValue("@totalCount", lot.TotalCount);
                        sqlCmd.Parameters.AddWithValue("@ngCount", lot.NgCount);
                        sqlCmd.Parameters.AddWithValue("@emCount", lot.EmCount);
                        sqlCmd.Parameters.AddWithValue("@totalTime", lot.totalSeconds);
                        sqlCmd.Parameters.AddWithValue("@id", lot.Id);

                        conn.Open();
                        ret = sqlCmd.ExecuteNonQuery() > 0;
                    }
                    catch (Exception ex)
                    {
                        logger.Create("updateLotStatus error:" + ex.Message);
                    }
                }
            }
            return ret;
        }

        public static bool updateQr1Status(VisionInspectionStatus x)
        {
            logger.Create(String.Format("updateQr1Status: total={0},loss={1},stop={2}",
                x.totalTime, x.lossTime, x.stopTime));

            var ret = false;
            using (var conn = Dba.GetConnection())
            {
                var sql = "REPLACE INTO qr1_status (key, value) VALUES (@key, @value)";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@key", DbRead.QR1_STATUS_KEY);
                        var js = x.ToJSON();
                        sqlCmd.Parameters.AddWithValue("@value", js);

                        conn.Open();
                        ret = sqlCmd.ExecuteNonQuery() > 0;
                    }
                    catch (Exception ex)
                    {
                        logger.Create("updateQr1Status error:" + ex.Message);
                    }
                }
            }
            return ret;
        }

        public static bool updateScannerStatus(ScannerStatus x)
        {
            logger.Create(String.Format("updateScannerStatus: cycle={0}, calib={1}", x.cycleCount, x.calibCount));

            var ret = false;
            using (var conn = Dba.GetConnection())
            {
                var sql = "REPLACE INTO qr1_status (key, value) VALUES (@key, @value)";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@key", DbRead.SCANNER_STATUS_KEY);
                        var js = x.ToJSON();
                        sqlCmd.Parameters.AddWithValue("@value", js);

                        conn.Open();
                        ret = sqlCmd.ExecuteNonQuery() > 0;
                    }
                    catch (Exception ex)
                    {
                        logger.Create("updateScannerStatus error:" + ex.Message);
                    }
                }
            }
            return ret;
        }

        public static bool createEvent(EventLog ev)
        {
            var ret = false;
            using (var conn = Dba.GetConnection())
            {
                var sql = "INSERT INTO event_log (created_time, event_type, message) VALUES (@time, @type, @message)";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@time", ev.CreatedTime);
                        sqlCmd.Parameters.AddWithValue("@type", ev.EventType);
                        sqlCmd.Parameters.AddWithValue("@message", ev.Message);

                        conn.Open();
                        ret = sqlCmd.ExecuteNonQuery() > 0;
                    }
                    catch (Exception ex)
                    {
                        logger.Create("createEvent error:" + ex.Message);
                    }
                }
            }
            return ret;
        }

        public static bool createUserLog(UserLog log)
        {
            var ret = false;
            using (var conn = Dba.GetConnection())
            {
                var sql = "INSERT INTO user_log (username, created_time, action, message) VALUES (@username, @time, @action, @message)";
                using (var sqlCmd = conn.CreateCommand())
                {
                    try
                    {
                        sqlCmd.CommandText = sql;
                        sqlCmd.Parameters.AddWithValue("@username", log.Username);
                        sqlCmd.Parameters.AddWithValue("@time", log.CreatedTime);
                        sqlCmd.Parameters.AddWithValue("@action", log.Action);
                        sqlCmd.Parameters.AddWithValue("@message", log.Message);

                        conn.Open();
                        ret = sqlCmd.ExecuteNonQuery() > 0;
                    }
                    catch (Exception ex)
                    {
                        logger.Create("createUserLog error:" + ex.Message);
                    }
                }
            }
            return ret;
        }
    }
}