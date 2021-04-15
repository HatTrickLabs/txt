using System;

namespace HatTrick.Text.Templating
{
    #region trim mark enum
    [Flags]
    public enum TrimMark
    {
        None =          0x0,
        DiscardLeft =   0x1,
        DiscardRight =  0x2,
        RetainLeft =    0x4,
        RetainRight =   0x8
    }
    #endregion
}