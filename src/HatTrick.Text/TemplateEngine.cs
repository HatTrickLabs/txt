using System;
using System.Collections.Generic;
using System.Text;
using HatTrick.Text.Reflection;

namespace HatTrick.Text
{
    public class TemplateEngine
    {
        #region internals
        private int _index;
        private int _lineCount;

        private string _template;

        private ScopeChain _scopeChain;

        private LambdaRepository _lambdaRepo;

        private StringBuilder _result;
        private Action<char> _appendToResult;

        private StringBuilder _tag;
        private Action<char> _appendToTag;

        private int _maxStack = 15;

        private string _lastKnownTag;
        private int _lastTagLineNumber;
        #endregion

        #region interface
        public bool SuppressWhitespace
        { get; set; }

        public LambdaRepository LambdaRepo
        { get { return (_lambdaRepo == null) ? _lambdaRepo = new LambdaRepository() : _lambdaRepo; } }
        #endregion

        #region constructors
        public TemplateEngine(string template)
        {
            _index = 0;
            _lineCount = string.IsNullOrEmpty(_template) ? 0 : 1;
            _template = template;
            _tag = new StringBuilder(60);
            _result = new StringBuilder((int)(template.Length * 1.3));

            _appendToResult = (c) => { _result.Append(c); };

            _appendToTag = (c) => { if (c != ' ') { _tag.Append(c); } };

            _scopeChain = new ScopeChain();
        }
        #endregion

        #region with scope chain
        private TemplateEngine WithScopeChain(ScopeChain scopeChain)
        {
            _scopeChain = scopeChain;
            return this;
        }
        #endregion

        #region with lambda repository
        private TemplateEngine WithLambdaRepository(LambdaRepository repo)
        {
            _lambdaRepo = repo;
            return this;
        }
        #endregion

        #region with max stack depth
        private TemplateEngine WithMaxStack(int depth)
        {
            if (depth < 0)
            {
                throw new MergeException($"stack depth overflow.  partial (sub template) stack depth cannot exceed {_maxStack}");
            }
            _maxStack = depth;
            return this;
        }
        #endregion

        #region with white space suppression
        private TemplateEngine WithWhitespaceSuppression(bool suppress)
        {
            this.SuppressWhitespace = suppress;
            return this;
        }
        #endregion

        #region merge
        public string Merge(object bindTo)
        {
            _result.Clear();
            _index = 0;
            _lineCount = string.IsNullOrEmpty(_template) ? 0 : 1;

            char eot = (char)3; //end of text....

            _tag.Clear();

            while (this.Peek() != eot)
            {
                if (this.RollTill(_appendToResult, '{', false))
                {
                    if (this.RollTill(_appendToTag, '}', true))
                    {
                        string tag = _tag.ToString();
                        this.HandleTag(tag, bindTo);
                    }
                    //else
                    //TODO: JRod, encountered un-closed tag...
                }
                _tag.Clear();
            }

            return _result.ToString();
        }
        #endregion

        #region handle tag
        private void HandleTag(string tag, object bindTo)
        {
            _lastKnownTag = tag;
            _lastTagLineNumber = _lineCount;
            if (this.IsIfTag(tag)) //# if logic tag (boolean switch)
            {
                this.HandleIfTag(tag, bindTo);
            }
            else if (this.IsEachTag(tag)) //#each enumeration
            {
                this.HandleEachTag(tag, bindTo);
            }
            else if (this.IsPartialTag(tag)) //sub template tag
            {
                this.HandlePartialTag(tag, bindTo);
            }
            else if (this.IsCommentTag(tag)) //comment tag
            {
                this.HandleCommentTag(tag);
            }
            else //basic tag
            {
                this.HandleBasicTag(tag, bindTo);
            }
        }
        #endregion

        #region is if tag
        public bool IsIfTag(string tag)
        {
            return tag.Length > 3
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '#')
                && (tag[2] == '#' || tag[2] == 'i')
                && (tag[3] == 'i' || tag[3] == 'f');
        }
        #endregion

        #region is end if tag
        public bool IsEndIfTag(string tag)
        {
            return tag.Length > 4
                && tag[0] == '{'
                && (tag[1] == '-' || tag[1] == '/')
                && (tag[2] == '/' || tag[2] == 'i')
                && (tag[3] == 'i' || tag[3] == 'f');
        }
        #endregion

        #region is each tag
        public bool IsEachTag(string tag)
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
        public bool IsEndEachTag(string tag)
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
        public bool IsCommentTag(string tag)
        {
            return tag[0] == '{' 
                && (tag[1] == '!' || (tag[1] == '-' && tag[2] == '!'));
        }
        #endregion

        #region is partial tag
        public bool IsPartialTag(string tag)
        {
            return tag[0] == '{' 
                && (tag[1] == '>' || (tag[1] == '-' && tag[2] == '>'));
                
        }
        #endregion

        #region handle comment tag
        private void HandleCommentTag(string tag)
        {
            this.EnsureLeftTrim(_result, tag, out bool _, false);
            this.EnsureRightTrim(tag, out bool _, false);
        }
        #endregion

        #region handle basic tag
        private void HandleBasicTag(string tag, object bindTo)
        {
            string bindAs = tag.Substring(1, (tag.Length - 2));
            object target = this.ResolveTarget(bindAs, bindTo);

            _result.Append(target ?? string.Empty);
        }
        #endregion

        #region handle if tag
        private void HandleIfTag(string tag, object bindTo)
        {
            bool force = this.SuppressWhitespace;
            bool leftTrimMark;
            bool rightTrimMark;
            this.EnsureLeftTrim(_result, tag, out leftTrimMark, force);
            this.EnsureRightTrim(tag, out rightTrimMark, force);

            StringBuilder block = new StringBuilder();

            Action<char> emitEnclosedTo = (s) => { block.Append(s); };

            //roll and emit until proper #/if tag found (allowing nested #if #/if tags
            string closeTag;
            this.RollBlockedContentTill(emitEnclosedTo, this.IsEndIfTag, this.IsIfTag, out closeTag);

            this.EnsureLeftTrim(block, closeTag, out bool _, force);
            this.EnsureRightTrim(closeTag, out bool _, force);

            int start = leftTrimMark ? 5 : 4;

            int len = (leftTrimMark && rightTrimMark)
                ? (tag.Length - 7)
                : (leftTrimMark || rightTrimMark)
                    ? tag.Length - 6
                    : (tag.Length - 5);

            string bindAs = tag.Substring(start, len);
            bool negate = bindAs[0] == '!';

            object target = this.ResolveTarget(negate ? bindAs.Substring(1) : bindAs, bindTo);

            bool render = this.IsTrue(target);

            if (negate)
            { render = !render; }

            if (render)
            {
                TemplateEngine subEngine = new TemplateEngine(block.ToString())
                    .WithWhitespaceSuppression(this.SuppressWhitespace)
                    .WithScopeChain(_scopeChain)
                    .WithMaxStack(_maxStack)
                    .WithLambdaRepository(_lambdaRepo);

                _result.Append(subEngine.Merge(bindTo));
            }
        }
        #endregion

        #region is truth
        public bool IsTrue(object val)
        {
            bool? bit;
            int? i;
            uint? ui;
            long? l;
            ulong? ul;
            double? dbl;
            float? flt;
            decimal? dec;
            short? sht;
            ushort? usht;
            char? c;
            string s;
            System.Collections.IEnumerable col;

            bool isFalse = (val == null)
                       ||
                          ((bit = val as bool?) != null && bit == false
                       || (i = val as int?) != null && i == 0
                       || (dbl = val as double?) != null && dbl == 0
                       || (l = val as long?) != null && l == 0
                       || (flt = val as float?) != null && flt == 0
                       || (dec = val as decimal?) != null && dec == 0
                       || (c = val as char?) != null && c == '\0'
                       || (ui = val as uint?) != null && ui == 0
                       || (ul = val as ulong?) != null && ul == 0
                       || (sht = val as short?) != null && sht == 0
                       || (usht = val as ushort?) != null && usht == 0
                       || (col = val as System.Collections.IEnumerable) != null && !col.GetEnumerator().MoveNext() //NOTE: JRod, this will catch string.Empty
                       || (s = val as string) != null && (s.Length == 1 && s[0] == '\0'));

            return !isFalse;
        }
        #endregion

        #region handle each tag
        private void HandleEachTag(string tag, object bindTo)
        {
            bool force = this.SuppressWhitespace;
            bool leftTrimMark;
            bool rightTrimMark;
            this.EnsureLeftTrim(_result, tag, out leftTrimMark, force);
            this.EnsureRightTrim(tag, out rightTrimMark, force);

            StringBuilder contentBlock = new StringBuilder();

            Action<char> emitEnclosedTo = (s) => { contentBlock.Append(s); };

            //roll and emit intil proper #/each tag found (allowing nested #each #/each tags
            string closeTag;
            this.RollBlockedContentTill(emitEnclosedTo, this.IsEndEachTag, this.IsEachTag, out closeTag);
            this.EnsureLeftTrim(contentBlock, closeTag, out bool _, force);
            this.EnsureRightTrim(closeTag, out bool _, force);

            int len = (leftTrimMark && rightTrimMark)
                        ? (tag.Length - 9)
                        : (leftTrimMark || rightTrimMark)
                            ? tag.Length - 8
                            : (tag.Length - 7);

            int start = leftTrimMark ? 7 : 6;

            string bindAs = tag.Substring(start, len);

            object target = this.ResolveTarget(bindAs, bindTo);

            if (!(target == null)) //if null just ignore
            {
                //if target is not enumerable, should not be bound to an #each tag
                if (!(target is System.Collections.IEnumerable))
                { throw new MergeException($"#each tag bound to non-enumerable object: {bindAs}"); }

                //cast to enumerable
                var items = (System.Collections.IEnumerable)target;
                string itemContent;

                TemplateEngine subEngine;
                _scopeChain.Push(bindTo);
                subEngine = new TemplateEngine(contentBlock.ToString())
                    .WithWhitespaceSuppression(this.SuppressWhitespace)
                    .WithScopeChain(_scopeChain)
                    .WithMaxStack(_maxStack)
                    .WithLambdaRepository(_lambdaRepo);

                foreach (var item in items)
                {
                    itemContent = subEngine.Merge(item);
                    _result.Append(itemContent);
                }
                _scopeChain.Pop();
            }
        }
        #endregion

        #region handle partial tag (sub templates)
        private void HandlePartialTag(string tag, object bindTo)
        {
            bool leftTrimMarker;
            bool rightTrimMarker;
            this.EnsureLeftTrim(_result, tag, out leftTrimMarker, false);
            this.EnsureRightTrim(tag, out rightTrimMarker, false);

            int len = (leftTrimMarker && rightTrimMarker)
                ? (tag.Length - 5)
                : (leftTrimMarker || rightTrimMarker)
                    ? tag.Length - 4
                    : (tag.Length - 3);

            int start = leftTrimMarker ? 3 : 2;

            object target = this.ResolveTarget(tag.Substring(start, len), bindTo);

            string tgt = (target as string);
            if (tgt == null)
            { throw new MergeException($"#sub template tag / tag reflected value is not typeof string: {target}"); }

            TemplateEngine subEngine = new TemplateEngine(tgt)
                .WithWhitespaceSuppression(this.SuppressWhitespace)
                .WithScopeChain(_scopeChain)
                .WithMaxStack(_maxStack - 1) //decrement 1 unit for sub template...
                .WithLambdaRepository(_lambdaRepo);

            string result = subEngine.Merge(bindTo);

            _result.Append(result);
        }
        #endregion

        #region resolve target
        private object ResolveTarget(string tag, object localScope)
        {
            object target = null;
            if (tag.Length == 1 && tag[0] == '$') //append bindto obj
            {
                target = localScope;
            }
            else if (tag[0] == '$' && tag[1] == '.') //reflect from bindto object
            {
                string expression = tag.Substring(2, tag.Length - 2);//remove the $.
                target = ReflectionHelper.Expression.ReflectItem(localScope, expression);
            }
            else if (tag[0] == '.' && tag[1] == '.' && tag[2] == '\\')
            {
                int lastIdxOf;
                int cnt = this.CountPattern(tag, @"..\", out lastIdxOf);
                target = this.ResolveTarget(tag.Substring(lastIdxOf + 3, tag.Length - (cnt * 3)), _scopeChain.ReachBack(cnt));
            }
            else if (tag.Contains("=>")) //lambda expression
            {
                //{($.abc, $.xyz) => ConcatToValues}
                //{("keyVal") => GetSomething}
                //{(true) => GetSomething}

                string[] leftRight = tag.Split(new char[] { '=', '>' }, StringSplitOptions.RemoveEmptyEntries);

                //TODO: JRod, if params contains a string literal that contains a comma or open paren or close paren this BREAKS...
                string[] args = leftRight[0].Split(new char[] { '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);

                object[] parameters = new object[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i][0] == '\"' && args[i][args[i].Length - 1] == '\"') //double quoted string literal...
                    {
                        parameters[i] = args[i].Substring(1, (args[i].Length - 2));
                    }
                    else if (args[i][0] == '\'' && args[i][args[i].Length - 1] == '\'') //single quoted string literal...
                    {
                        parameters[i] = args[i].Substring(1, (args[i].Length - 2));
                    }
                    else if ((string.Compare(args[i], "true", true) == 0) || (string.Compare(args[i], "false", true) == 0))
                    {
                        parameters[i] = bool.Parse(args[i]);
                    }
                    else
                    {
                        parameters[i] = this.ResolveTarget(args[i], localScope); //recursive
                    }
                }

                target = this.LambdaRepo.Invoke(leftRight[1], parameters);
            }
            else
            {
                target = ReflectionHelper.Expression.ReflectItem(localScope, tag);
            }

            return target;
        }
        #endregion

        #region peek
        public char Peek()
        {
            char c = (_template.Length > _index) 
                 ? _template[_index] 
                 : (char)3; //eot (end of text)

            return c;
        }
        #endregion

        #region peek at
        public char PeekAt(int indexOf)
        {
            char c = (_template.Length > indexOf)
                ? _template[indexOf]
                : (char)3; //eot (end of text)

            return c;
        }
        #endregion

        #region roll till
        private bool RollTill(Action<char> emitTo, char till, bool greedy)
        {
            return this.RollTill(emitTo, (c) => c == till, greedy);
        }

        private bool RollTill(Action<char> emitTo, Func<char, bool> till, bool greedy)
        {
            bool found = false;
            char eot = (char)3;
            char c;
            while((c = this.Peek()) != eot)
            {
                if (c == '\n')
                {
                    _lineCount += 1;
                }
                if (c == '{' || c == '}')
                {
                    char next = this.PeekAt(_index + 1);
                    if (c == next)
                    {
                        emitTo(c);
                        _index += 2;
                        continue;
                    }
                }
                if (till(c))
                {
                    if (greedy)
                    {
                        emitTo(c);
                        _index += 1;
                    }
                    found = true;
                    break;
                }
                else
                {
                    emitTo(c);
                    _index += 1;
                }
            }
            return found;
        }
        #endregion

        #region roll blocked content to action till
        private void RollBlockedContentTill(Action<char> emitTo, Func<string, bool> till, Func<string, bool> ensuring, out string endTag)
        {
            endTag = null;
            int offset = 1;// i.e. we are inside 1 if tag and looking for its /if tag but must account for contained if tags...

            StringBuilder tag = new StringBuilder(30);
            bool tagIsContent;
            do
            {
                //look for the next tag...
                this.RollTill(emitTo, '{', false);

                tag.Clear();
                tagIsContent = false;

                Action<char> emitTagTo = (c) =>
                {
                    if (c != ' ') { tag.Append(c); }
                };

                this.RollTill(emitTagTo, '}', true);

                if (tag.Length == 0)
                { throw new MergeException($"enountered un-closed tag > 'till' condition never found{Environment.NewLine}Last Open Tag:{_lastKnownTag}{Environment.NewLine}Last Open Tag Line #:{_lastTagLineNumber}{Environment.NewLine}"); }

                if (!till(tag.ToString()))
                {
                    tagIsContent = true;
                    if (ensuring(tag.ToString()))
                    {
                        offset += 1;
                    }
                }
                else
                {
                    offset -= 1;
                    if (offset > 0)
                    {
                        tagIsContent = true;
                    }
                    else
                    {
                        endTag = tag.ToString();
                    }
                }
                if (tagIsContent)
                {
                    for (int i = 0; i < tag.Length; i++)
                    {
                        emitTo(tag[i]);
                    }
                }

            } while (offset > 0);
        }
        #endregion

        #region count instances of pattern
        public int CountPattern(string content, string pattern, out int lastIndexOf)
        {
            lastIndexOf = -1;
            int cnt = 0;
            int idx = 0;

            while ((idx = content.IndexOf(pattern, idx)) != -1)
            {
                lastIndexOf = idx;
                idx += pattern.Length;
                cnt += 1;
            }

            return cnt;
        }
        #endregion

        #region ensure left  trim
        private void EnsureLeftTrim(StringBuilder from, string tag, out bool leftTrimMark, bool force)
        {
            leftTrimMark = tag[1] == '-';

            if (leftTrimMark || force)
            {
                int idx = from.Length - 1;
                while (idx > -1 && (from[idx] == '\t' || from[idx] == ' '))
                {
                    idx -= 1;
                }
                from.Length = idx + 1;
            }
        }
        #endregion

        #region ensure right trim
        private void EnsureRightTrim(string tag, out bool rightTrimMark, bool force)
        {
            rightTrimMark = tag[tag.Length - 2] == '-';

            //if global trim trailing newline || tag has the newline trim marker..
            if (rightTrimMark || force)
            {
                Action<char> emitTo = (c) => { }; //just throw the whitespace away...

                Func<char, bool> isNotWhitespace = (c) =>
                {
                    return !(c == ' ' || c == '\t');
                };

                bool found = this.RollTill(emitTo, isNotWhitespace, false);

                int newLineLength = Environment.NewLine.Length;

                string lookFor = newLineLength == 1
                    ? Char.ToString(this.Peek())
                    : Char.ToString(this.Peek()) + Char.ToString(this.PeekAt(_index + 1));

                if (lookFor == Environment.NewLine)
                {
                    _index += newLineLength;
                    _lineCount += 1;
                }
            }
        }
        #endregion
    }
}