using System;

namespace HatTrick.Text.Templating
{
    #region trim mark enum
    [Flags]
    public enum TrimMark
    {
        None = 0x0,
        Left = 0x1,
        Right = 0x2,
    }
    #endregion
}