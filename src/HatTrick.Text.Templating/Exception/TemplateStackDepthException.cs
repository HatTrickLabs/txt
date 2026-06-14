// SPDX-License-Identifier: Apache-2.0
// Copyright (c) HatTrick Labs, LLC

using System;

namespace HatTrick.Text.Templating
{
    public class TemplateStackDepthException : InvalidOperationException
    {
        #region ctors
        public TemplateStackDepthException(string message) : base(message)
        { }
        #endregion
    }
}
