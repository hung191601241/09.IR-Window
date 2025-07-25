using System;

namespace VisionInspection
{
    class UserManager
    {
        private static MyLogger logger = new MyLogger("UserManager");

        public static Boolean isLogOnSuper = false;
        public static Boolean isLogOnManager = false;
        public static Boolean isLogOnOperater = false;

        public static void createUserLog(UserActions action, String detail = null)
        {
            try
            {
                var log = new UserLog(action);
                DbWrite.createUserLog(log);
                if (detail != null)
                {
                    log.Message += ": " + detail;
                }
            }
            catch (Exception ex)
            {
                logger.Create("createUserLog error:" + ex.Message);
            }
        }

        public static Boolean LogOn(String username, String password)
        {
            bool KQ = false;
            try
            {
                var User = UiManager.appSettings.user;
                if (username == "SuperUser")
                {
                    if (User.IDSuperuser == password)
                    {
                        isLogOnSuper = true;
                        KQ = true;
                    }
                    else
                    {
                        isLogOnSuper = false;
                    }
                }
                else if (username == "Manager")
                {
                    if (User.IDManager == password)
                    {
                        isLogOnManager = true;
                        KQ = true;
                    }
                    else
                    {
                        isLogOnManager = false;
                    }
                }
                else if (username == "Operator")
                {
                    if (User.IdOP == password)
                    {
                        isLogOnOperater = true;
                        KQ = true;
                    }
                    else
                    {
                        isLogOnOperater = false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Create("LogOn error:" + ex.Message);
            }
            return KQ;
        }

        //public static void LogOut() {
        //    logger.Create("LogOut");
        //    isLogOn = false;
        //}

        //public static Boolean IsLogOn() {
        //    return isLogOn;
        //}

        public static Boolean ChangePassword(String passOld, String passNew)
        {
            try
            {

            }
            catch (Exception ex)
            {
                logger.Create("ChangePassword error:" + ex.Message);
            }
            return false;
        }
    }
}
