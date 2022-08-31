using System;

namespace HatTrick.Text.Templating
{
    #region tag type enum
    public enum TagType
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

    #region block tag type
    public enum BlockTagOrientation
    {
        Begin,
        End
    }
    #endregion
}