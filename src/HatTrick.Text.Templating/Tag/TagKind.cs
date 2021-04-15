using System;

namespace HatTrick.Text.Templating
{
    #region tag kind enum
    public enum TagKind
    {
        Simple,
        If,
        EndIf,
        Each,
        EndEach,
        With,
        EndWith,
        Partial,
        Comment,
        VarDeclare,
        VarAssign,
    }
    #endregion
}