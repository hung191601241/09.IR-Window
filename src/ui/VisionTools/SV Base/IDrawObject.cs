using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionInspection
{
    public delegate void RefreshHandler(object sender, EventArgs arg);

    public delegate void Drawing(System.Drawing.Graphics gdi);

    public interface IDrawObject
    {
        /// <summary>
        /// 그림을 그린다.
        /// </summary>
        /// <param name="gdi">GDI 객체</param>
        void Draw(System.Drawing.Graphics gdi);
        
        event RefreshHandler Refresh;

        /// <summary>
        /// 그림을 다시그린다(Invalidate)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        void OnRefresh(object sender, EventArgs arg);
    }
}
