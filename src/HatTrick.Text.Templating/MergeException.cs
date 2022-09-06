using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Text.Templating
{
    #region merge exception
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
    #endregion

    #region merge exception context
    public class MergeExceptionContext
    {
        #region internals
        private int _line;
        private int _column;
        private int _index;
        private string _lastTag;
        #endregion

        #region interface
        public int Line => _line;

        public int Column => _column;

        public int CharIndex => _index;

        public string LastTag => _lastTag;
        #endregion

        #region constructors
        public MergeExceptionContext(int line, int column, int index, string lastTag)
        {
            _line = line;
            _column = column;
            _index = index;
            _lastTag = lastTag ?? string.Empty;
        }
        #endregion

        #region to string
        public override string ToString()
        {
            return $"Ln: {_line}  Col: {_column}  Char Index: {_index}  LastTag: {_lastTag}";
        }
        #endregion
    }
    #endregion

    #region merge exception context stack
    public class MergeExceptionContextStack : Stack<MergeExceptionContext>
    {
        #region constructors
        public MergeExceptionContextStack() : base()
        {
        }

        public MergeExceptionContextStack(int capacity) : base(capacity)
        {
        }
        #endregion
    }
    #endregion
}
