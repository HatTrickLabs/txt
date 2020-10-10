using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Text.Templating
{
    public class TagBuilder
    {
        #region internals
        private char[] _tag;
        private int _length;

        private bool _inSingleQuote;
        private bool _inDoubleQuote;

        private char _previous;
        #endregion

        #region interface
        public char this[int index]
        { get { return _tag[index]; } }

        public int Length
        { get { return _length;/* _tag.Length; */ } }
        #endregion

        #region constructors
        public TagBuilder()
        {
            _tag = new char[256];
            this.Init();
        }
        #endregion

        #region init
        private void Init()
        {
            //_tag.Clear();
            _length = 0;
            _inSingleQuote = false;
            _inDoubleQuote = false;
            _previous = '\0';
        }
        #endregion

        #region append
        public void Append(char c)
        {
            char escape         = '\\';
            char singleQuote    = '\'';
            char doubleQuote    = '"';
            char tab            = '\t';
            char space          = ' ';

            //if double quote & not escaped & not already inside single quotes...
            if (c == doubleQuote && _previous != escape && !_inSingleQuote)
                _inDoubleQuote = !_inDoubleQuote;

            //if single quote & not escaped & not already inside double quotes...
            if (c == singleQuote && _previous != escape && !_inDoubleQuote)
                _inSingleQuote = !_inSingleQuote;

            //only append white space if inside double or single quotes...
            bool inQuotes = (_inDoubleQuote || _inSingleQuote);
            if (!(c == space || c == tab) || inQuotes)
                _tag[_length++] = c;

            _previous = c;
        }
        #endregion

        #region reset
        public void Reset()
        {
            this.Init();
        }
        #endregion

        #region to string
        public override string ToString()
        {
            return new string(_tag, 0, _length);
        }
        #endregion

        #region get as readonly span <char>
        public ReadOnlySpan<char> GetAsReadOnlySpan()
        {
            return new ReadOnlySpan<char>(_tag, 0, _length);
        }
		#endregion
	}
}
