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
        private List<MergeExceptionContext> _context;
        #endregion

        #region interface
        public List<MergeExceptionContext> Context => _context;
        #endregion

        #region constructors
        public MergeException(string msg) : this(msg, null)
        {
        }

        public MergeException(string msg, Exception innerException) : base(msg, innerException)
        {
            _context = new List<MergeExceptionContext>();
        }
        #endregion
    }
    #endregion

    #region merge exception context
    public class MergeExceptionContext
    {
        #region internals
        private int _lineNum;
        private string _surroundings;
        #endregion

        #region interface
        public int LineNumber => _lineNum;
        public string Surroundings => _surroundings;
        #endregion

        #region constructors
        public MergeExceptionContext(int lineNumber, string surroundings)
        {
            _lineNum = lineNumber;
            _surroundings = surroundings ?? throw new ArgumentNullException(nameof(surroundings));
        }
        #endregion
    }
    #endregion
}
