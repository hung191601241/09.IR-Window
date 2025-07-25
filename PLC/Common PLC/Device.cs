using Mitsubishi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Development
{
    public enum DeviceCode
    {
        SM,
        SD,
        X,
        Y,
        M,
        L,
        F,
        V,
        B,
        D,
        W,
        SB,
        R,
        ZR,
        P_LS,
        K_LS,
    }
    public interface Device
    {
        bool isOpen();
        void Open();
        void Close();
        bool WriteWord(DeviceCode devCode, int _devNumber, int _writeValue);
        bool ReadWord(DeviceCode devCode, int _devNumber, out int _value);
        bool WriteDoubleWord(DeviceCode devCode, int _devNumber, int _writeValue);
        bool ReadDoubleWord(DeviceCode devCode, int _devNumber, out int _value);
        bool WriteBit(DeviceCode devCode, int _devNumber, bool _value);
        bool ReadBit(DeviceCode devCode, int _devNumber, out bool _value);
        bool ReadMultiBits(DeviceCode devCode, int _devNumber, int _count, out List<bool> _lstValue);
        bool ReadMultiWord(DeviceCode devCode, int _devNumber, int _count, out List<short> _value);
        bool ReadMultiDoubleWord(DeviceCode devCode, int _devNumber, int _count, out List<int> _value);
        bool ReadASCIIString(DeviceCode devCode, int _devNumber, int _count, out string result);
        bool WriteString(DeviceCode devCode, int _devNumber, string _writeString);

    }
}
