using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Text
{
    public class Breadcrumb
    {
        #region internals
        private int _maxDepth;
        private int _index;
        private int[] _line;
        private string[] _blockTag;
        #endregion

        #region constructors
        public Breadcrumb(int maxDepth)
        {
            _maxDepth = maxDepth;
            _index = 0;
            _line = new int[maxDepth];
            _blockTag = new string[maxDepth];
        }
        #endregion

        #region push
        public void Push(int lineNumber, string blockTag)
        {
            _line[_index] = lineNumber;
            _blockTag[_index] = blockTag;
            _index += 1;
        }
        #endregion

        #region pop
        public void Pop(out int lineNumber, out string blockTag)
        {
            lineNumber = _line[_index];
            blockTag = _blockTag[_index];
            _index -= 1;
        }
        #endregion
    }
}
