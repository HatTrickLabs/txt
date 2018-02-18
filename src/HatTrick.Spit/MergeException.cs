using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Spit
{
    public class MergeException : Exception
    {
        public MergeException(string msg) : base(msg)
        {
        }
    }
}
