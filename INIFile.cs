using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VisionInspection;

namespace AutoLaserCuttingInput
{
    public class INIFile
    {
        // Add Mylooger
        private MyLogger Logger = new MyLogger("INIFile");

        #region Add Dll
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
        string key,
        string val,
        string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
        string key,
        string def,
        StringBuilder retVal,
        int size,
        string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string Section,
        int Key,
        string Value,
        [MarshalAs(UnmanagedType.LPArray)] byte[] Result,
        int Size, string FileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern uint GetPrivateProfileSectionNames(IntPtr lpszReturnBuffer,
        uint nSize,
        string lpFileName);
        #endregion

        private static object LockObject = new object();
        private string filePath = "";
        private string EXE = "";
        public INIFile(string filePath)
        {
            this.filePath = filePath;
            this.EXE = Assembly.GetExecutingAssembly().GetName().Name;
        }
        public void Write(string Key, string Value, string Section = null)
        {
            lock (LockObject)
            {
                try
                {
                    WritePrivateProfileString(Section ?? this.EXE, Key, Value, this.filePath);
                }
                catch (Exception ex)
                {
                    Logger.Create(String.Format("INI FIle Write File Error: {0}.", ex.Message));
                }
            }
        }
        public void DeleteKeyName(string Key, string Section = null)
        {
            lock (LockObject)
            {
                try
                {
                    Write(Key, null, Section ?? this.EXE);
                }
                catch (Exception ex)
                {
                    Logger.Create(String.Format("INI FIle Delete Key Error: {0}.", ex.Message));
                }
            }
        }
        public void DeleteSectionName(string Section = null)
        {
            lock (LockObject)
            {
                try
                {
                    Write(null, null, Section ?? this.EXE);
                }
                catch (Exception ex)
                {
                    Logger.Create(String.Format("INI FIle Delete Section Error: {0}.", ex.Message));
                }
            }
        }
        public bool CheckKeyExists(string Key, string Section = null)
        {
            bool ret = false;
            lock (LockObject)
            {
                try
                {
                    return GetValue(Key, Section).Length > 0;
                }
                catch (Exception ex)
                {
                    Logger.Create(String.Format("INI FIle Key Exits Error: {0}.", ex.Message));
                }
            }
            return ret;
        }
        public string GetValue(string Key, string Section = null)
        {
            string ret = "";
            lock (LockObject)
            {
                try
                {
                    var RetVal = new StringBuilder(255);
                    GetPrivateProfileString(Section ?? this.EXE, Key, "", RetVal, 255, this.filePath);
                    return RetVal.ToString();
                }
                catch (Exception ex)
                {
                    Logger.Create(String.Format("INI FIle Get Value Error: {0}", ex.Message));
                }
                return ret;
            }
        }
        public string[] GetSectionNames()
        {
            string[] ret = null;
            lock (LockObject)
            {
                try
                {
                    uint MAX_BUFFER = 32767;
                    IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER);
                    uint bytesReturned = GetPrivateProfileSectionNames(pReturnedString, MAX_BUFFER, this.filePath);
                    if (bytesReturned == 0)
                        return null;
                    string result = Marshal.PtrToStringAnsi(pReturnedString, (int)(bytesReturned * 2));
                    Marshal.FreeCoTaskMem(pReturnedString);

                    // convert ASCII string to Unicode string
                    byte[] bytes = Encoding.ASCII.GetBytes(result);
                    string local = Encoding.Unicode.GetString(bytes);

                    //use of Substring below removes terminating null for split
                    return ret = local.Substring(0, local.Length - 1).Split('\0');
                }
                catch (Exception ex)
                {
                    Logger.Create(String.Format("INI FIle Get Section Name Error: {0}.", ex.Message));
                }
                return ret;
            }
        }
        public string[] GetKeyName(string section)
        {
            string[] ret = null;
            lock (LockObject)
            {
                try
                {
                    for (int maxsize = 500; true; maxsize *= 2)
                    {
                        byte[] bytes = new byte[maxsize];
                        int size = GetPrivateProfileString(section, 0, "", bytes, maxsize, this.filePath);
                        if (size < maxsize - 2)
                        {
                            string entries = Encoding.ASCII.GetString(bytes, 0, size - (size > 0 ? 1 : 0));
                            return ret = entries.Split(new char[] { '\0' });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Create(String.Format("INI FIle Get Key Name Error: {0}.", ex.Message));
                }
                return ret;
            }
        }
    }
}
