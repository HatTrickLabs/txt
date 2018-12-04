using System;
using System.Collections.Generic;
using System.Text;
using HatTrick.Reflection;

namespace HatTrick.Text.Templating
{
    public class TemplateEngine
    {
        #region internals
        private int _index;
        private int _lineNum;
        private Action<int, string> _progress;

        private string _template;

        private ScopeChain _scopeChain;

        private LambdaRepository _lambdaRepo;

        private StringBuilder _result;
        private Action<char> _appendToResult;

        private TagBuilder _tagBldr;

        private int _maxStack = 25;
        private bool _trimWhitespace;
        #endregion

        #region interface
        [Obsolete("This property is now obsolete and will be removed in the future, use 'TrimWhitespace' instead.")]
        public bool SuppressWhitespace
        { set { _trimWhitespace = value; } }

        public bool TrimWhitespace
        {
            get { return _trimWhitespace; }
            set { _trimWhitespace = true; }
        }

        public LambdaRepository LambdaRepo
        { get { return _lambdaRepo;  } }

        public Action<int, string> ProgressListener
        {
            get { return _progress; }
            set { _progress = value; }
        }
        #endregion

        #region constructors
        public TemplateEngine(string template)
        {
            _index = 0;

            _template = template;

            _tagBldr = new TagBuilder();

            _result = new StringBuilder((int)(template.Length * 1.3));

            _appendToResult = (c) => { _result.Append(c); };

            _scopeChain = new ScopeChain();
            _lambdaRepo = new LambdaRepository();
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
                //partial templates decrement the stack depth by 1 for each nested partial...
                //If we get below zero the max depth has been consumed...
                throw new MergeException($"stack depth overflow.  partial (sub template) stack depth cannot exceed {_maxStack}");
            }
            _maxStack = depth;
            return this;
        }
        #endregion

        #region with white space suppression
        private TemplateEngine WithWhitespaceSuppression(bool suppress)
        {
            _trimWhitespace = suppress;
            return this;
        }
        #endregion

        #region with progress listener
        private TemplateEngine WithProgressListener(Action<int, string> listener)
        {
            _progress = listener;
            return this; 
        }
        #endregion

        #region merge
        public string Merge(object bindTo)
        {
            _result.Clear();
            _index = 0;
            _lineNum = string.IsNullOrEmpty(_template) ? 0 : 1;

            char eot = (char)3; //end of text....

            _tagBldr.Reset();

            while (this.Peek() != eot)
            {
                if (this.RollTill(_appendToResult, '{', false, false))
                {
                    if (this.RollTill(_tagBldr.Append, '}', true, false))
                    {
                        Tag tag = new Tag(_tagBldr.ToString());
                        _progress?.Invoke(_lineNum, tag.ToString());
                        this.HandleTag(tag, bindTo);
                    }
                    else
                    { throw new MergeException("enountered un-closed tag; '}' never found"); }
                }
                _tagBldr.Reset();
            }
            return _result.ToString();
        }
        #endregion

        #region handle tag
        private void HandleTag(Tag tag, object bindTo)
        {
            switch (tag.Kind)
            {
                case TagKind.Simple:        //simple tag
                    this.HandleSimpleTag(in tag, bindTo);
                    break;
                case TagKind.If:            //# if logic tag (boolean switch)
                    this.HandleIfTag(in tag, bindTo);
                    break;
                case TagKind.Each:          //#each enumeration
                    this.HandleEachTag(in tag, bindTo);
                    break;
                case TagKind.With:
                    this.HandleWithTag(in tag, bindTo);
                    break;
                case TagKind.Partial:       //sub template tag
                    this.HandlePartialTag(in tag, bindTo);
                    break;
                case TagKind.Comment:       //comment tag
                    this.HandleCommentTag(in tag);
                    break;
            }
        }
        #endregion

        #region handle comment tag
        private void HandleCommentTag(in Tag tag)
        {
            this.EnsureLeftTrim(_result, in tag, false);
            this.EnsureRightTrim(in tag, false);
        }
        #endregion

        #region handle simple tag
        private void HandleSimpleTag(in Tag tag, object bindTo)
        {
            string bindAs = tag.BindAs();
            object target = this.ResolveTarget(bindAs, bindTo);

            _result.Append(target ?? string.Empty);
        }
        #endregion

        #region handle if tag
        private void HandleIfTag(in Tag tag, object bindTo)
        {
            bool forceTrim = _trimWhitespace;

            this.EnsureLeftTrim(_result, tag, forceTrim);
            this.EnsureRightTrim(tag, forceTrim);

            StringBuilder block = new StringBuilder();

            Action<char> emitEnclosedTo = (s) => { block.Append(s); };

            //roll and emit until proper #/if tag found (allowing nested #if #/if tags
            Tag closeTag;
            this.RollBlockedContentTill(emitEnclosedTo, Tag.IsEndIfTag, Tag.IsIfTag, out closeTag);

            this.EnsureLeftTrim(block, closeTag, forceTrim);
            this.EnsureRightTrim(closeTag, forceTrim);

            string bindAs = tag.BindAs();
            bool negate = bindAs[0] == '!';

            object target = this.ResolveTarget(negate ? bindAs.Substring(1) : bindAs, bindTo);

            bool render = this.IsTrue(target);

            if (negate)
            { render = !render; }

            if (render)
            {
                TemplateEngine subEngine = new TemplateEngine(block.ToString())
                    .WithProgressListener(_progress)
                    .WithWhitespaceSuppression(_trimWhitespace)
                    .WithScopeChain(_scopeChain)
                    .WithMaxStack(_maxStack)
                    .WithLambdaRepository(_lambdaRepo);

                _result.Append(subEngine.Merge(bindTo));
            }
        }
        #endregion

        #region is true
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
                       || val == DBNull.Value 
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
        private void HandleEachTag(in Tag tag, object bindTo)
        {
            bool forceTrim = _trimWhitespace;
            this.EnsureLeftTrim(_result, tag, forceTrim);
            this.EnsureRightTrim(tag, forceTrim);

            StringBuilder block = new StringBuilder();

            Action<char> emitEnclosedTo = (s) => { block.Append(s); };

            //roll and emit intil proper #/each tag found (allowing nested #each #/each tags
            Tag closeTag;
            this.RollBlockedContentTill(emitEnclosedTo, Tag.IsEndEachTag, Tag.IsEachTag, out closeTag);
            this.EnsureLeftTrim(block, closeTag, forceTrim);
            this.EnsureRightTrim(closeTag, forceTrim);

            string bindAs = tag.BindAs();

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
                subEngine = new TemplateEngine(block.ToString())
                    .WithProgressListener(_progress)
                    .WithWhitespaceSuppression(_trimWhitespace)
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

        #region handle with tag
        private void HandleWithTag(in Tag tag, object bindTo)
        {
            bool forceTrim = _trimWhitespace;
            this.EnsureLeftTrim(_result, tag, forceTrim);
            this.EnsureRightTrim(tag, forceTrim);

            StringBuilder block = new StringBuilder();

            Action<char> emitEnclosedTo = (s) => { block.Append(s); };

            //roll and emit intil proper #/each tag found (allowing nested #each #/each tags
            Tag closeTag;
            this.RollBlockedContentTill(emitEnclosedTo, Tag.IsEndWithTag, Tag.IsWithTag, out closeTag);
            this.EnsureLeftTrim(block, closeTag, forceTrim);
            this.EnsureRightTrim(closeTag, forceTrim);

            string bindAs = tag.BindAs();

            object target = this.ResolveTarget(bindAs, bindTo);

            string itemContent;
            TemplateEngine subEngine;
            _scopeChain.Push(bindTo);
            subEngine = new TemplateEngine(block.ToString())
                .WithProgressListener(_progress)
                .WithWhitespaceSuppression(_trimWhitespace)
                .WithScopeChain(_scopeChain)
                .WithMaxStack(_maxStack)
                .WithLambdaRepository(_lambdaRepo);

            itemContent = subEngine.Merge(target);
            _result.Append(itemContent);
            _scopeChain.Pop();
        }
        #endregion

        #region handle partial tag (sub templates)
        private void HandlePartialTag(in Tag tag, object bindTo)
        {
            this.EnsureLeftTrim(_result, tag, false);
            this.EnsureRightTrim(tag, false);

            string bindAs = tag.BindAs();
            object target = this.ResolveTarget(bindAs, bindTo);

            string tgt = (target as string);
            if (tgt == null)
            { throw new MergeException($"#sub template tag: {tag} reflected value is not typeof string: {target}"); }

            TemplateEngine subEngine = new TemplateEngine(tgt)
                .WithProgressListener(_progress)
                .WithWhitespaceSuppression(_trimWhitespace)
                .WithScopeChain(_scopeChain)
                .WithMaxStack(_maxStack - 1) //decrement 1 unit for sub template...
                .WithLambdaRepository(_lambdaRepo);

            string result = subEngine.Merge(bindTo);

            _result.Append(result);
        }
        #endregion

        #region resolve target
        private object ResolveTarget(string bindAs, object localScope)
        {
            object target = null;
            if (bindAs.Length == 1 && bindAs[0] == '$') //append bindto obj
            {
                target = localScope;
            }
            else if (bindAs[0] == '$' && bindAs[1] == '.') //reflect from bindto object
            {
                string expression = bindAs.Substring(2, bindAs.Length - 2);//remove the $.
                target = ReflectionHelper.Expression.ReflectItem(localScope, expression);
            }
            else if (bindAs[0] == '.' && bindAs[1] == '.' && bindAs[2] == '\\')
            {
                int lastIdxOf;
                int cnt = this.CountPattern(bindAs, @"..\", out lastIdxOf);
                target = this.ResolveTarget(bindAs.Substring(lastIdxOf + 3, bindAs.Length - (cnt * 3)), _scopeChain.ReachBack(cnt));
            }
            else if (bindAs.Contains("=>")) //lambda expression
            {
                //{($.abc, $.xyz) => ConcatToValues}
                //{("keyVal") => GetSomething}
                //{(true) => GetSomething}
                string name;
                object[] arguments;
                this.ExtractLambda(bindAs, localScope, out name, out arguments);
                target = this.LambdaRepo.Invoke(name, arguments);
            }
            else
            {
                target = ReflectionHelper.Expression.ReflectItem(localScope, bindAs);
            }
            return target;
        }
        #endregion

        #region extract lambda
        private void ExtractLambda(string bindAs, object localScope, out string name, out object[] arguments)
        {
            string[] args;
            _lambdaRepo.Parse(bindAs, out name, out args);

            arguments = this.ParseLambdaArguments(localScope, args);
        }
        #endregion

        #region parse lambda arguments
        private object[] ParseLambdaArguments(object localScope, string[] args)
        {
            object[] arguments = new object[args.Length];

            //TODO: JRod, refactor this to lex proper without the SubString calls... 
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i][0] == '\"' && args[i][args[i].Length - 1] == '\"')      //double quoted string literal...
                {
                    arguments[i] = args[i].Substring(1, (args[i].Length - 2));
                }
                else if (args[i][0] == '\'' && args[i][args[i].Length - 1] == '\'') //single quoted string literal...
                {
                    arguments[i] = args[i].Substring(1, (args[i].Length - 2));
                }
                else if ((string.Compare(args[i], "true", true) == 0) || (string.Compare(args[i], "false", true) == 0))
                {
                    arguments[i] = bool.Parse(args[i]);
                }
                else if (this.TextContains(args[i], ':', out int at))               //numeric literal...
                {
                    string value = args[i].Substring(0, at);
                    string suffix = args[i].Substring(at + 1);

                    arguments[i] = this.LambdaRepo.ParseNumericLiteral(value, suffix);
                }
                else
                {
                    arguments[i] = this.ResolveTarget(args[i], localScope);         //recursive
                }
            }
            return arguments;
        }
        #endregion

        #region text contains
        private bool TextContains(string text, char value, out int at)
        {
            if (string.IsNullOrEmpty(text))
            {
                at = -1;
            }
            else
            {
                at = text.IndexOf(value);
            }
            return at > 0;
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
        private bool RollTill(Action<char> emitTo, char till, bool greedy, bool isSubBlock)
        {
            return this.RollTill(emitTo, (c) => c == till, greedy, isSubBlock);
        }

        private bool RollTill(Action<char> emitTo, Func<char, bool> till, bool greedy, bool isSubBlock)
        {
            bool found = false;
            char eot = (char)3;
            char c;
            while((c = this.Peek()) != eot)
            {
                if (c == '\n')
                {
                    _lineNum += 1;
                }
                if (c == '{' || c == '}')
                {
                    char next = this.PeekAt(_index + 1);
                    if (c == next)
                    {
                        emitTo(c);
                        if (isSubBlock)
                        {
                            emitTo(next);
                        }
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
        private void RollBlockedContentTill(Action<char> emitTo, Func<string, bool> till, Func<string, bool> ensuring, out Tag endTag)
        {
            endTag = default;
            int offset = 1;// i.e. we are inside 1 #if tag and looking for its /if tag but must account for contained #if tags...
            bool tagIsContent;
            do
            {
                //look for the next tag...
                this.RollTill(emitTo, '{', false, true);

                _tagBldr.Reset();
                tagIsContent = false;

                this.RollTill(_tagBldr.Append, '}', true, true);

                if (_tagBldr.Length == 0)
                { throw new MergeException($"enountered un-closed tag; 'till' condition never found"); }

                if (!till(_tagBldr.ToString()))
                {
                    tagIsContent = true;
                    if (ensuring(_tagBldr.ToString()))
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
                        endTag = new Tag(_tagBldr.ToString());
                    }
                }
                if (tagIsContent)
                {
                    for (int i = 0; i < _tagBldr.Length; i++)
                    {
                        emitTo(_tagBldr[i]);
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
        private void EnsureLeftTrim(StringBuilder from, in Tag tag, bool force)
        {
            if (tag.Has(TrimMark.Left) || force)
            {
                int len = Environment.NewLine.Length; //new line len
                bool found = false;//line end found
                int idx = from.Length - 1;
                while (idx > -1 && (from[idx] == '\t' || from[idx] == ' ' || from[idx] == '\n'))
                {
                    if (from[idx] == '\n')
                    {
                        if (found)
                        {
                            break;
                        }
                        found = true;
                        idx -= len;
                    }
                    else
                    {
                        idx -= 1;
                    }
                }
                from.Length = (idx + 1);
            }
        }
        #endregion

        #region ensure right trim
        private void EnsureRightTrim(in Tag tag, bool force)
        {
            //if global trim trailing newline || tag has the newline trim marker..
            if (tag.Has(TrimMark.Right) || force)
            {
                Action<char> emitTo = (c) => { }; //just throw the whitespace away...

                Func<char, bool> isNotWhitespace = (c) =>
                {
                    return !(c == ' ' || c == '\t');
                };
                bool found = this.RollTill(emitTo, isNotWhitespace, false, false);
            }
        }
        #endregion
    }
}