// SPDX-License-Identifier: Apache-2.0
// Copyright (c) HatTrick Labs, LLC

using System;

namespace HatTrick.Text.Templating
{
    public struct Tag
    {
        #region internals
        private TagType _type;
        private ReadOnlyMemory<char> _tag;
        private TrimMark _markers;
        private bool _forceTrim;
        #endregion

        #region interface
        public TagType Type => _type;
        #endregion

        #region constructors
        public Tag(ReadOnlyMemory<char> tag, bool forceTrim)
        {
            _tag = tag;
            _type = TagType.Unknown;
            _markers = TrimMark.None;
            _forceTrim = forceTrim;
            this.Init();
        }
        #endregion

        #region init
        private void Init()
        {
            _type = Tag.ResolveType(_tag.Span);
            this.ResolveTrimMarkers();
        }
        #endregion

        #region resolve type
        public static TagType ResolveType(ReadOnlySpan<char> tag)
        {
            if (Tag.IsIfTag(tag))
                return TagType.If;

            else if (Tag.IsEndIfTag(tag))
                return TagType.EndIf;

            else if (Tag.IsEachTag(tag))
                return TagType.Each;

            else if (Tag.IsEndEachTag(tag))
                return TagType.EndEach;

            else if (Tag.IsWithTag(tag))
                return TagType.With;

            else if (Tag.IsEndWithTag(tag))
                return TagType.EndWith;

            else if (Tag.IsPartialTag(tag))
                return TagType.Partial;

            else if (Tag.IsVariableDeclareTag(tag))
                return TagType.VarDeclare;

            else if (Tag.IsVariableAssignTag(tag))
                return TagType.VarAssign;

            else if (Tag.IsCommentTag(tag))
                return TagType.Comment;

            else if (Tag.IsDebugTag(tag))
                return TagType.Debug;

            else
                return TagType.Simple;
        }
        #endregion

        #region resolve end tag type
        public static TagType ResolveEndTagType(TagType type)
        {
            if (!Tag.IsBlockTag(type, out BlockTagOrientation orientation) || orientation != BlockTagOrientation.Begin)
                throw new ArgumentException($"Arg is not a valid begin block tag type: {type}... valid block begin tags are (If, Each, With)", nameof(type));

            if (type == TagType.If)
                return TagType.EndIf;

            else if (type == TagType.Each)
                return TagType.EndEach;

            else if (type == TagType.With)
                return TagType.EndWith;

            else
                throw new ArgumentException($"Encountered un-known TagType: {type}", nameof(type));
        }
        #endregion

        #region is block tag
        public static bool IsBlockTag(TagType type, out BlockTagOrientation orientation)
        {
            orientation = BlockTagOrientation.Unknown;

            bool isBlock = type == TagType.If
                        || type == TagType.Each
                        || type == TagType.With
                        || type == TagType.EndIf
                        || type == TagType.EndEach
                        || type == TagType.EndWith;

            if (isBlock)
            {
                orientation = (type == TagType.If || type == TagType.Each || type == TagType.With)
                    ? BlockTagOrientation.Begin
                    : BlockTagOrientation.End;
            }

            return isBlock;
        }
        #endregion

        #region resolve trim markers
        private void ResolveTrimMarkers()
        {
            if (_type == TagType.Simple)
                return;

            ReadOnlySpan<char> span = _tag.Span;

            if (span[1] == '-')
                _markers = TrimMark.DiscardLeft;

            else if (span[1] == '+')
                _markers = TrimMark.RetainLeft;


            if (span[span.Length - 2] == '-')
                _markers |= TrimMark.DiscardRight;

            else if (span[span.Length - 2] == '+')
                _markers |= TrimMark.RetainRight;
        }
        #endregion

        #region is if tag
        public static bool IsIfTag(ReadOnlySpan<char> tag)
        {
            return tag.Length > 4
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '+' || tag[1] == '#')
                && (tag[2] == '#' || tag[2] == 'i')
                && (tag[3] == 'i' || tag[3] == 'f');
        }
        #endregion

        #region is end if tag
        public static bool IsEndIfTag(ReadOnlySpan<char> tag)
        {
            return tag.Length > 4
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '+' || tag[1] == '/')
                && (tag[2] == '/' || tag[2] == 'i')
                && (tag[3] == 'i' || tag[3] == 'f');
        }
        #endregion

        #region is each tag
        public static bool IsEachTag(ReadOnlySpan<char> tag)
        {
            return tag.Length > 6
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '+' || tag[1] == '#')
                && (tag[2] == '#' || tag[2] == 'e')
                && (tag[3] == 'e' || tag[3] == 'a')
                && (tag[4] == 'a' || tag[4] == 'c')
                && (tag[5] == 'c' || tag[5] == 'h');
        }
        #endregion

        #region is end each tag
        public static bool IsEndEachTag(ReadOnlySpan<char> tag)
        {
            return tag.Length > 6
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '+' || tag[1] == '/')
                && (tag[2] == '/' || tag[2] == 'e')
                && (tag[3] == 'e' || tag[3] == 'a')
                && (tag[4] == 'a' || tag[4] == 'c')
                && (tag[5] == 'c' || tag[5] == 'h');
        }
        #endregion

        #region is with tag
        public static bool IsWithTag(ReadOnlySpan<char> tag)
        {
            return tag.Length > 6
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '+' || tag[1] == '#')
                && (tag[2] == '#' || tag[2] == 'w')
                && (tag[3] == 'w' || tag[3] == 'i')
                && (tag[4] == 'i' || tag[4] == 't')
                && (tag[5] == 't' || tag[5] == 'h');
        }
        #endregion

        #region is end with tag
        public static bool IsEndWithTag(ReadOnlySpan<char> tag)
        {
            return tag.Length > 6
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '+' || tag[1] == '/')
                && (tag[2] == '/' || tag[2] == 'w')
                && (tag[3] == 'w' || tag[3] == 'i')
                && (tag[4] == 'i' || tag[4] == 't')
                && (tag[5] == 't' || tag[5] == 'h');
        }
        #endregion

        #region is comment tag
        public static bool IsCommentTag(ReadOnlySpan<char> tag)
        {
            return tag.Length > 2
                && tag[0] == '{'
                &&
                (
                    tag[1] == '!'
                    ||
                    (tag[1] == '-' && tag[2] == '!')
                    ||
                    (tag[1] == '+' && tag[2] == '!')
                );
        }
        #endregion

        #region is partial tag
        public static bool IsPartialTag(ReadOnlySpan<char> tag)
        {
            return tag[0] == '{'
                &&
                (
                    tag[1] == '>'
                    ||
                    (tag[1] == '-' && tag[2] == '>')
                    ||
                    (tag[1] == '+' && tag[2] == '>')
                );
        }
        #endregion

        #region is variable declare tag
        private static bool IsVariableDeclareTag(ReadOnlySpan<char> tag)
        {
            return tag.Length > 6
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '+' || tag[1] == '?')
                && (tag[2] == '?' || tag[2] == 'v')
                && (tag[3] == 'v' || tag[3] == 'a')
                && (tag[4] == 'a' || tag[4] == 'r')
                && (tag[5] == 'r' || tag[5] == ':');
        }
        #endregion

        #region is variable assign tag
        private static bool IsVariableAssignTag(ReadOnlySpan<char> tag)
        {
            return tag.Length > 4
                && tag[0] == '{'
                && ((tag[1] == '-' || tag[1] == '+') && tag[2] == '?' && tag[3] == ':')
                    || (tag[1] == '?' && tag[2] == ':');
        }
        #endregion

        #region is debug tag
        public static bool IsDebugTag(ReadOnlySpan<char> tag)
        {
            return tag.Length > 2
                && tag[0] == '{'
                &&
                (
                    tag[1] == '@'
                    ||
                    (tag[1] == '-' && tag[2] == '@')
                    ||
                    (tag[1] == '+' && tag[2] == '@')
                );
        }
        #endregion

        #region bind as
        public ReadOnlySpan<char> BindAs()
        {
            TagType type = _type;

            int start, len, maxLen;

            bool left = this.HasTrimMark(TrimMark.DiscardLeft) || this.HasTrimMark(TrimMark.RetainLeft);
            bool right = this.HasTrimMark(TrimMark.DiscardRight) || this.HasTrimMark(TrimMark.RetainRight);

            switch (type)
            {
                case TagType.Simple:
                    start = 1;
                    maxLen = _tag.Length - 2;
                    break;
                case TagType.If:
                    start = left ? 5 : 4;
                    maxLen = 7;
                    break;
                case TagType.Each:
                    start = left ? 7 : 6;
                    maxLen = 9;
                    break;
                case TagType.VarDeclare:
                    start = left ? 6 : 5;
                    maxLen = 8;
                    break;
                case TagType.VarAssign:
                    start = left ? 3 : 2;
                    maxLen = 5;
                    break;
                case TagType.With:
                    start = left ? 7 : 6;
                    maxLen = 9;
                    break;
                case TagType.Partial:
                    start = left ? 3 : 2;
                    maxLen = 5;
                    break;
                case TagType.Debug:
                    start = left ? 3 : 2;
                    maxLen = 5;
                    break;
                case TagType.Comment:
                default:
                    throw new InvalidOperationException($"Encountered un-expected TagType: {type} ... tag type cannot be bound");
            }

            if (type == TagType.Simple)
                len = maxLen;

            else if (left && right)
                len = (_tag.Length - maxLen);

            else if (left || right)
                len = _tag.Length - (maxLen - 1);

            else
                len = _tag.Length - (maxLen - 2);

            return len > 0 ? _tag.Span.Slice(start, len) : ReadOnlySpan<char>.Empty;
        }
        #endregion

        #region has trim mark
        public bool HasTrimMark(TrimMark marker)
        {
            return (_markers & marker) == marker;
        }
        #endregion

        #region should trim left
        public bool ShouldTrimLeft()
        {
            return HasTrimMark(TrimMark.DiscardLeft) || (_forceTrim && !HasTrimMark(TrimMark.RetainLeft));
        }
        #endregion

        #region should trim right
        public bool ShouldTrimRight()
        {
            return HasTrimMark(TrimMark.DiscardRight) || (_forceTrim && !HasTrimMark(TrimMark.RetainRight));
        }
        #endregion

        #region to string
        public override string ToString()
        {
            return new string(_tag.Span);
        }
        #endregion
    }
}
