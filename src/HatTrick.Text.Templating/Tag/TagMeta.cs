using System;

namespace HatTrick.Text.Templating
{
    public struct TagMeta
    {
        #region interface
        public int Start;
        public int End;
        public int Length;
        #endregion

        #region constructors
        public TagMeta(int start, int end, int length)
        {
            this.Start = start;
            this.End = end;
            this.Length = length;
        }
        #endregion
    }
}
