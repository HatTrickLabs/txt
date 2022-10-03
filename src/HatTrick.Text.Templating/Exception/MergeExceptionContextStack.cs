using System;
using System.Collections.Generic;
using System.Text;

namespace HatTrick.Text.Templating
{
    public class MergeExceptionContextStack : Stack<MergeExceptionContext>
    {
        #region constructors
        public MergeExceptionContextStack() : base()
        { }

        public MergeExceptionContextStack(int capacity) : base(capacity)
        { }
        #endregion

        #region to string
        public override string ToString()
        {
            return string.Join(Environment.NewLine, this);
        }
        #endregion
    }
}
