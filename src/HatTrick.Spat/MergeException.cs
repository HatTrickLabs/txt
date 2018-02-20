using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Spat
{
    public class MergeException : Exception
    {
        public MergeException(string msg) : base(msg)
        {
        }
    }
}
