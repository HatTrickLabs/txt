using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Text.Templating
{
    public class Tag
    {
        #region internals
        private TagType _type;
        private string _tag;
        private int _tagLength;
        private TrimMark _markers;
        private bool _forceTrim;
        #endregion

        #region interface
        public TagType Type => _type;
        #endregion

        #region constructors
        public Tag(string tag, bool forceTrim)
        {
            _tag = tag;
            _tagLength = tag.Length;
            _type = TagType.Simple;
            _markers = TrimMark.None;
            _forceTrim = forceTrim;
            this.Init();
        }
        #endregion

        #region init
        private void Init()
        {
            _type = Tag.ResolveType(_tag);
            this.ResolveTrimMarkers();
        }
        #endregion

        #region resolve type
        public static TagType ResolveType(string tag)
        {
            if (Tag.IsIfTag(tag))                       //# if logic tag (boolean switch)
                return TagType.If;

            else if (Tag.IsEndIfTag(tag))               //end if
                return TagType.EndIf;

            else if (Tag.IsEachTag(tag))                //#each enumeration
                return TagType.Each;

            else if (Tag.IsEndEachTag(tag))             //end each
                return TagType.EndEach;

            else if (Tag.IsWithTag(tag))                //#with tag
                return TagType.With;

            else if (Tag.IsEndWithTag(tag))             //end with
                return TagType.EndWith;

            else if (Tag.IsPartialTag(tag))             //sub template tag
                return TagType.Partial;

            else if (Tag.IsVariableDeclareTag(tag))     //variable declaratino 
                return TagType.VarDeclare;

            else if (Tag.IsVariableAssignTag(tag))      //variable assignment
                return TagType.VarAssign;

            else if (Tag.IsCommentTag(tag))             //comment tag
                return TagType.Comment;

            else                                        //simple tag
                return TagType.Simple;
        }
        #endregion

        #region resolve end tag type
        public static TagType ResolveEndTagType(TagType type)
        {
            if (!Tag.IsBlockTag(type, out BlockTagOrientation? orientation) || orientation.Value != BlockTagOrientation.Begin)
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
        public static bool IsBlockTag(TagType type, out BlockTagOrientation? orientation)
        {
            orientation = null;

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

            if (_tag[1] == '-')                     //has discard left trim mark...
                _markers = TrimMark.DiscardLeft;

            else if (_tag[1] == '+')                //has retain left trim mark...
                _markers = TrimMark.RetainLeft;


            if (_tag[_tag.Length - 2] == '-')       //has discard right trim mark...
                _markers |= TrimMark.DiscardRight;

            else if (_tag[_tag.Length - 2] == '+')   //has retain right trim mark...
                _markers |= TrimMark.RetainRight;
        }
        #endregion

        #region is if tag
        public static bool IsIfTag(string tag)
        {
            return tag.Length > 4
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '+' || tag[1] == '#')
                && (tag[2] == '#' || tag[2] == 'i')
                && (tag[3] == 'i' || tag[3] == 'f');
        }
        #endregion

        #region is end if tag
        public static bool IsEndIfTag(string tag)
        {
            return tag.Length > 4
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '+' || tag[1] == '/')
                && (tag[2] == '/' || tag[2] == 'i')
                && (tag[3] == 'i' || tag[3] == 'f');
        }
        #endregion

        #region is each tag
        public static bool IsEachTag(string tag)
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
        public static bool IsEndEachTag(string tag)
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
        public static bool IsWithTag(string tag)
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
        public static bool IsEndWithTag(string tag)
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
        public static bool IsCommentTag(string tag)
        {
            return tag[0] == '{'
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
        public static bool IsPartialTag(string tag)
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
        private static bool IsVariableDeclareTag(string tag)
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

        #region is variable declare tag
        private static bool IsVariableAssignTag(string tag)
        {
            return tag.Length > 4
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '+' || tag[1] == '?')
                && (tag[2] == '?' || tag[2] == ':');
		}
        #endregion

		#region bind as
		public string BindAs()
        {
            string bindAs = null;
            TagType type = _type;

            int start = 0;
            int len = 0;
            int maxLen = 0;

            bool left = this.HasTrimMark(TrimMark.DiscardLeft) || this.HasTrimMark(TrimMark.RetainLeft);
            bool right = this.HasTrimMark(TrimMark.DiscardRight) || this.HasTrimMark(TrimMark.RetainRight);

            switch (type)
            {
                case TagType.Simple:
                    start = 1;
                    maxLen = _tagLength - 2;
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
                case TagType.Comment:
                default:
                    throw new InvalidOperationException($"Encountered un-expected TagType: {type} ... tag type cannot be bound");
            }

            if (type == TagType.Simple) //simple tags cannot have trim markers...
                len = maxLen;

            else if (left && right)
                len = (_tagLength - maxLen);

            else if (left || right)
                len = _tagLength - (maxLen - 1);

            else
                len = _tagLength - (maxLen - 2);


			bindAs = (len > 0) ? new string(_tag.ToCharArray(), start, len) : null;

            return bindAs;
        }
        #endregion

        #region has trim mark
        public bool HasTrimMark(TrimMark marker)
        {
            bool exists = (_markers & marker) == marker;
            return exists;
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
        public override string ToString() => _tag;
        #endregion
    }
}
