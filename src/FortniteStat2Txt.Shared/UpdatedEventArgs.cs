using System;
using System.Collections.Generic;
using System.Text;

namespace FortniteStat2Txt.Shared
{
    public class UpdatedEventArgs : EventArgs
    {
        public DateTime UpdatedTime { get; private set; } = DateTime.Now;
    }
}
