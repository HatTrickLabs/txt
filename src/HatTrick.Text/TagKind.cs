﻿using System;

namespace HatTrick.Text
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