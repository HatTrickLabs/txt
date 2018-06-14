using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Text
{
    public class LambdaInvocationException : Exception
    {
        #region constructors
        public LambdaInvocationException(string message) : base(message)
        {

        }
        #endregion
    }
}
