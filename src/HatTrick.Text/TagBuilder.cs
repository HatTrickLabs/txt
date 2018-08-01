﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Text
{
    public class TagBuilder
    {
        #region internals
        private StringBuilder _tag;

        private bool _inSingleQuote;
        private bool _inDoubleQuote;

        private char _previous;
        #endregion

        #region interface
        public char this[int index]
        { get { return _tag[index]; } }

        public int Length
        { get { return _tag.Length; } }
        #endregion

        #region constructors
        public TagBuilder()
        {
            _tag = new StringBuilder(60);
            this.Init();
        }
        #endregion

        #region init
        private void Init()
        {
            _tag.Clear();
            _inSingleQuote = false;
            _inDoubleQuote = false;
            _previous = '\0';
        }
        #endregion

        #region append
        public void Append(char c)
        {
            //if double quote & not escaped & not already inside single quotes...
            if (c == '"' && _previous != '\\' && !_inSingleQuote)
            {
                _inDoubleQuote = !_inDoubleQuote;
            }

            //if single quote & not escaped & not already inside double quotes...
            if (c == '\'' && _previous != '\\' && !_inDoubleQuote)
            {
                _inSingleQuote = !_inSingleQuote;
            }

            //only append a space if inside double or single quotes...
            if (c != ' ' || (_inDoubleQuote || _inSingleQuote))
            {
                _tag.Append(c);
            }

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
            return _tag.ToString();
        }
        #endregion
    }
}
