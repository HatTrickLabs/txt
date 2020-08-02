using System;

namespace HatTrick.Text.Templating
{
    #region tag kind enum
    public enum TagKind
    {
        Unknown,
        Simple,
        If,
        EndIf,
        Each,
        EndEach,
        With,
        EndWith,
        Partial,
        Comment,
        Variable
    }
    #endregion
}