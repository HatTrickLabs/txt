using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Text
{
    public struct Tag
    {
        #region internals
        private TagKind _kind;
        private string _tag;
        private TrimMark _markers;
        #endregion

        #region interface
        public TagKind Kind => _kind;
        #endregion

        #region constructors
        public Tag(string tag)
        {
            _tag = tag;
            _kind = TagKind.Unknown;
            _markers = TrimMark.None;
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
            string tag = _tag;
            if (Tag.IsIfTag(tag))          //# if logic tag (boolean switch)
            {
                _kind = TagKind.If;
            }
            else if (Tag.IsEachTag(tag))   //#each enumeration
            {
                _kind = TagKind.Each;
            }
            else if (Tag.IsPartialTag(tag)) //sub template tag
            {
                _kind = TagKind.Partial;
            }
            else if (Tag.IsCommentTag(tag)) //comment tag
            {
                _kind = TagKind.Comment;
            }
            else                            //simple tag
            {
                _kind = TagKind.Simple;
            }
        }
        #endregion

        #region resolve trim markers
        private void ResolveTrimMarkers()
        {
            if (_tag[1] == '-')             //has left trim mark...
            {
                _markers = TrimMark.Left;
            }
            if (_tag[_tag.Length - 2] == '-')//has right trim mark...
            {
                _markers |= TrimMark.Right;
            }
        }
        #endregion

        #region is if tag
        public static bool IsIfTag(string tag)
        {
            return tag.Length > 3
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '#')
                && (tag[2] == '#' || tag[2] == 'i')
                && (tag[3] == 'i' || tag[3] == 'f');
        }
        #endregion

        #region is end if tag
        public static bool IsEndIfTag(string tag)
        {
            return tag.Length > 4
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '/')
                && (tag[2] == '/' || tag[2] == 'i')
                && (tag[3] == 'i' || tag[3] == 'f');
        }
        #endregion

        #region is each tag
        public static bool IsEachTag(string tag)
        {
            return tag.Length > 5
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '#')
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
                && (tag[1] == '-' || tag[1] == '/')
                && (tag[2] == '/' || tag[2] == 'e')
                && (tag[3] == 'e' || tag[3] == 'a')
                && (tag[4] == 'a' || tag[4] == 'c')
                && (tag[5] == 'c' || tag[5] == 'h');
        }
        #endregion

        #region is comment tag
        public static bool IsCommentTag(string tag)
        {
            return tag[0] == '{'
                && (tag[1] == '!' || (tag[1] == '-' && tag[2] == '!'));
        }
        #endregion

        #region is partial tag
        public static bool IsPartialTag(string tag)
        {
            return tag[0] == '{'
                && (tag[1] == '>' || (tag[1] == '-' && tag[2] == '>'));
        }
        #endregion

        #region bind as
        public string BindAs()
        {
            string bindAs = null;
            TagKind kind = _kind;
            switch (kind)
            {
                case TagKind.Simple:
                    {
                        string tag = _tag;
                        bindAs = tag.Substring(1, (tag.Length - 2));
                    }
                    break;
                case TagKind.If:
                    {
                        string tag = _tag;

                        int start = this.Has(TrimMark.Left) ? 5 : 4;

                        int len = (this.Has(TrimMark.Left) && this.Has(TrimMark.Right))
                            ? (tag.Length - 7)
                            : (this.Has(TrimMark.Left) || this.Has(TrimMark.Right))
                                ? tag.Length - 6
                                : (tag.Length - 5);

                        bindAs = tag.Substring(start, len);
                    }
                    break;
                case TagKind.Each:
                    {
                        string tag = _tag;

                        int start = this.Has(TrimMark.Left) ? 7 : 6;

                        int len = (this.Has(TrimMark.Left) && this.Has(TrimMark.Right))
                            ? (tag.Length - 9)
                            : (this.Has(TrimMark.Left) || this.Has(TrimMark.Right))
                                ? tag.Length - 8
                                : (tag.Length - 7);
                        
                        bindAs = tag.Substring(start, len);
                    }
                    break;
                case TagKind.Partial:
                    {
                        string tag = _tag;
                        int start = this.Has(TrimMark.Left) ? 3 : 2;

                        int len = (this.Has(TrimMark.Left) && this.Has(TrimMark.Right)) 
                            ? (tag.Length - 5)
                            : (this.Has(TrimMark.Left) || this.Has(TrimMark.Right))
                                ? tag.Length - 4
                                : (tag.Length - 3);

                        bindAs = tag.Substring(start, len);
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

        #region has
        public bool Has(TrimMark marker)
        {
            return (_markers & marker) == marker; 
        }
        #endregion

        #region to string
        public override string ToString()
        {
            return _tag;
        }
        #endregion
    }
}
