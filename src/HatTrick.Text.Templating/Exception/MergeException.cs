using System;

namespace HatTrick.Text.Templating
{
    public class MergeException : Exception
    {
        #region internals
        private MergeExceptionContextStack _context;
        #endregion

        #region interface
        public MergeExceptionContextStack Context => _context;
        #endregion

        #region constructors
        public MergeException(string msg) : this(msg, null)
        {
        }

        public MergeException(string msg, Exception innerException) : base(msg, innerException)
        {
            _context = new MergeExceptionContextStack();
        }
        #endregion
    }
}
