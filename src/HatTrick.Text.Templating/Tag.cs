using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Text.Templating
{
    public ref struct Tag
    {
        #region internals
        private TagKind _kind;
        private ReadOnlySpan<char> _tag;
        private TrimMark _markers;
        private bool _forceTrim;
        #endregion

        #region interface
        public TagKind Kind => _kind;
        #endregion

        #region constructors
        public Tag(ReadOnlySpan<char> tag, bool forceTrim)
        {
            _tag = tag;
            _kind = TagKind.Unknown;
            _markers = TrimMark.None;
            _forceTrim = forceTrim;
            this.Init();
        }
        #endregion

        #region init
        private void Init()
        {
            this.ResolveKind();
            this.ResolveTrimMarkers();
        }
        #endregion

        #region resolve kind
        private void ResolveKind()
        {
            ReadOnlySpan<char> tag = _tag;
            if (Tag.IsIfTag(tag))               //# if logic tag (boolean switch)
            {
                _kind = TagKind.If;
            }
            else if (Tag.IsEndIfTag(tag))
            {
                _kind = TagKind.EndIf;
            }
            else if (Tag.IsEachTag(tag))        //#each enumeration
            {
                _kind = TagKind.Each;
            }
            else if (Tag.IsEndEachTag(tag))
            {
                _kind = TagKind.EndEach;
            }
            else if (Tag.IsWithTag(tag))        //#with tag
            {
                _kind = TagKind.With;
            }
            else if (Tag.IsEndWithTag(tag))
            {
                _kind = TagKind.EndWith;
            }
            else if (Tag.IsPartialTag(tag))     //sub template tag
            {
                _kind = TagKind.Partial;
            }
            else if (Tag.IsVariableTag(tag))
            {
                _kind = TagKind.Variable;
            }
            else if (Tag.IsCommentTag(tag))     //comment tag
            {
                _kind = TagKind.Comment;
            }
            else                                //simple tag
            {
                _kind = TagKind.Simple;
            }
        }
        #endregion

        #region resolve trim markers
        private void ResolveTrimMarkers()
        {
            if (_kind == TagKind.Simple)
                return;

            if (_tag[1] == '-')                     //has discard left trim mark...
            {
                _markers = TrimMark.DiscardLeft;
            }
            else if (_tag[1] == '+')                //has retain left trim mark...
            {
                _markers = TrimMark.RetainLeft;
            }

            if (_tag[_tag.Length - 2] == '-')       //has discard right trim mark...
            {
                _markers |= TrimMark.DiscardRight;
            }
            else if (_tag[_tag.Length - 2] == '+')   //has retain right trim mark...
            {
                _markers |= TrimMark.RetainRight;
            }
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
            return tag[0] == '{'
                && (tag[1] == '!' || (tag[1] == '-' && tag[2] == '!') || (tag[1] == '+' && tag[2] == '!'));
        }
        #endregion

        #region is partial tag
        public static bool IsPartialTag(ReadOnlySpan<char> tag)
        {
            return tag[0] == '{'
                && (tag[1] == '>' || (tag[1] == '-' && tag[2] == '>') || (tag[1] == '+' && tag[2] == '>'));
        }
        #endregion

        #region is variable tag
        public static bool IsVariableTag(ReadOnlySpan<char> tag)
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

		#region bind as
		public ReadOnlySpan<char> BindAs()
        {
            ReadOnlySpan<Char> bindAs = null;
            TagKind kind = _kind;
            switch (kind)
            {
                case TagKind.Simple:
                    {
                        ReadOnlySpan<char> tag = _tag;
                        bindAs = tag.Slice(1, (tag.Length - 2));
                    }
                    break;
                case TagKind.If:
                    {
                        ReadOnlySpan<char> tag = _tag;
                        bool left = this.HasTrimMark(TrimMark.DiscardLeft) || this.HasTrimMark(TrimMark.RetainLeft);
                        bool right = this.HasTrimMark(TrimMark.DiscardRight) || this.HasTrimMark(TrimMark.RetainRight);
                        int start = left ? 5 : 4;

                        int len = (left && right)
                            ? (tag.Length - 7)
                            : (left || right)
                                ? (tag.Length - 6)
                                : (tag.Length - 5);

                        bindAs = tag.Slice(start, len);
                    }
                    break;
                case TagKind.Each:
                    {
                        ReadOnlySpan<char> tag = _tag;
                        bool left = this.HasTrimMark(TrimMark.DiscardLeft) || this.HasTrimMark(TrimMark.RetainLeft);
                        bool right = this.HasTrimMark(TrimMark.DiscardRight) || this.HasTrimMark(TrimMark.RetainRight);
                        int start = left ? 7 : 6;

                        int len = (left && right)
                            ? (tag.Length - 9)
                            : (left || right)
                                ? (tag.Length - 8)
                                : (tag.Length - 7);
                        
                        bindAs = tag.Slice(start, len);
                    }
                    break;
                case TagKind.Variable:
                    {
                        ReadOnlySpan<char> tag = _tag;
                        bool left = this.HasTrimMark(TrimMark.DiscardLeft) || this.HasTrimMark(TrimMark.RetainLeft);
                        bool right = this.HasTrimMark(TrimMark.DiscardRight) || this.HasTrimMark(TrimMark.RetainRight);
                        int start = left ? 6 : 5;

                        int len = (left && right)
                            ? (tag.Length - 8)
                            : (left || right)
                                ? (tag.Length - 7)
                                : (tag.Length - 6);

                        bindAs = tag.Slice(start, len);
                    }
                    break;
                case TagKind.With:
                    {
                        ReadOnlySpan<char> tag = _tag;
                        bool left = this.HasTrimMark(TrimMark.DiscardLeft) || this.HasTrimMark(TrimMark.RetainLeft);
                        bool right = this.HasTrimMark(TrimMark.DiscardRight) || this.HasTrimMark(TrimMark.RetainRight);
                        int start = left ? 7 : 6;

                        int len = (left && right)
                            ? (tag.Length - 9)
                            : (left || right)
                                ? (tag.Length - 8)
                                : (tag.Length - 7);

                        bindAs = tag.Slice(start, len);
                    }
                    break;
                case TagKind.Partial:
                    {
                        ReadOnlySpan<char> tag = _tag;
                        bool left = this.HasTrimMark(TrimMark.DiscardLeft) || this.HasTrimMark(TrimMark.RetainLeft);
                        bool right = this.HasTrimMark(TrimMark.DiscardRight) || this.HasTrimMark(TrimMark.RetainRight);
                        int start = left ? 3 : 2;

                        int len = (left && right) 
                            ? (tag.Length - 5)
                            : (left || right)
                                ? (tag.Length - 4)
                                : (tag.Length - 3);

                        bindAs = tag.Slice(start, len);
                    }
                    break;
                case TagKind.Comment:
                case TagKind.Unknown:
                    bindAs = null;
                    break;
            }
            return bindAs;
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
            bool result = HasTrimMark(TrimMark.DiscardLeft) || (_forceTrim && !HasTrimMark(TrimMark.RetainLeft));
            return result;
        }
        #endregion

        #region should trim right
        public bool ShouldTrimRight()
        {
            bool result = HasTrimMark(TrimMark.DiscardRight) || (_forceTrim && !HasTrimMark(TrimMark.RetainRight));
            return result;
        }
		#endregion

		#region to string
		public override string ToString()
        {
            return _tag.ToString();
        }
        #endregion
    }
}
