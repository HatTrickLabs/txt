using System;

namespace HatTrick.Text.Templating
{
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
            return $"Ln: {_line}\tCol: {_column}\tChar Index: {_index}\tLastTag: {_lastTag}";
        }
        #endregion
    }
}
