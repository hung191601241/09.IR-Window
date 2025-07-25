using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckNeurocleLicense
{
    internal class Program
    {
        static int Main(string[] args)
        {
            //Console.OutputEncoding = Encoding.UTF8;
            try
            {
                //int checkLicense = nrt.nrt.initialize_nrt(0);
                nrt.Model model = new nrt.Model();
                //Console.WriteLine("✅ License OK. Predictor khởi tạo thành công.");
                //Console.WriteLine();
                return 0;
            }
            catch (Exception ex)
            {
                //Console.WriteLine("❌ License lỗi hoặc không tìm thấy USB key.");
                //Console.WriteLine("Chi tiết: " + ex.Message);
                //Console.WriteLine();
                return -1;
            }
        }
    }
}
