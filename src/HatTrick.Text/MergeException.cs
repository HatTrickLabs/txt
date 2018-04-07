using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Text
{
    public class MergeException : Exception
    {
        public MergeException(string msg) : base(msg)
        {
        }
    }
}
