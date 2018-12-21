using System;

namespace HatTrick.Text.Templating
{
    #region tag kind enum
    public enum TagKind
    {
        Unknown,
        Simple,
        If,
        Each,
        With,
        Partial,
        Comment
    }
    #endregion
}