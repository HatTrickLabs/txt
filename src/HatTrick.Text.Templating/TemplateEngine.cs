using System;
using System.Collections.Generic;
using System.Text;

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

        private TagBuilder _nextTag;

        private int _maxStack = 25;
        private bool _trimWhitespace;
        #endregion

        #region interface
        public bool TrimWhitespace
        {
            get { return _trimWhitespace; }
            set { _trimWhitespace = value; }
        }

        public LambdaRepository LambdaRepo
        { 
            get { return (_lambdaRepo == null) ? _lambdaRepo = new LambdaRepository() : _lambdaRepo;  }
            set { _lambdaRepo = value; }
        }

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
            _nextTag = new TagBuilder();
            _result = new StringBuilder((int)(template.Length * 1.3));
            _appendToResult = (c) => { _result.Append(c); };
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
            _scopeChain.Push(bindTo);
            string result = this.Merge();
            _scopeChain.Pop();
            return result;
        }

        private string Merge()
        {
            _result.Clear();
            _nextTag.Reset();
            _index = 0;
            _lineNum = string.IsNullOrEmpty(_template) ? 0 : 1;

            char eot = (char)3; //end of text....

            while (this.Peek() != eot)
            {
                if (this.RollTill(_appendToResult, '{', false, false))
                {
                    if (this.RollTill(_nextTag.Append, '}', true, false))
                    {
                        Tag tag = new Tag(_nextTag.ToString(), _trimWhitespace);
                        _progress?.Invoke(_lineNum, tag.ToString());
                        this.HandleTag(tag);
                    }
                    else
                    { throw new MergeException("enountered un-closed tag; '}' never found"); }
                }
                _nextTag.Reset();
            }

            return _result.ToString();
        }
        #endregion

        #region handle tag
        private void HandleTag(Tag tag)
        {
            switch (tag.Kind)
            {
                case TagKind.Simple:
                    this.HandleSimpleTag(in tag);
                    break;
                case TagKind.If:
                    this.HandleIfTag(in tag);
                    break;
                case TagKind.Each:
                    this.HandleEachTag(in tag);
                    break;
                case TagKind.With:
                    this.HandleWithTag(in tag);
                    break;
                case TagKind.VarDeclare:
                    this.HandleVariableDeclareTag(in tag);
                    break;
                case TagKind.VarAssign:
                    this.HandleVariableAssignTag(in tag);
                    break;
                case TagKind.Partial:
                    this.HandlePartialTag(in tag);
                    break;
                case TagKind.Comment:
                    this.HandleCommentTag(in tag);
                    break;
            }
        }
        #endregion

        #region handle comment tag
        private void HandleCommentTag(in Tag tag)
        {
            this.EnsureLeftTrim(_result, in tag);
            this.EnsureRightTrim(in tag);
        }
        #endregion

        #region handle simple tag
        private void HandleSimpleTag(in Tag tag)
        {
            string bindAs = tag.BindAs();
            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            _result.Append(target ?? string.Empty);
        }
        #endregion

        #region handle if tag
        private void HandleIfTag(in Tag tag)
        {
            this.EnsureLeftTrim(_result, tag);
            this.EnsureRightTrim(tag);

            StringBuilder block = new StringBuilder();

            Action<char> emitEnclosedTo = (s) => { block.Append(s); };

            //roll and emit until proper #/if tag found (allowing nested #if #/if tags
            Tag closeTag;
            this.RollBlockedContentTill(emitEnclosedTo, Tag.IsEndIfTag, Tag.IsIfTag, out closeTag);

            this.EnsureLeftTrim(block, closeTag);
            this.EnsureRightTrim(closeTag);

            string bindAs = tag.BindAs();
            bool negate = bindAs[0] == '!';

            object target = BindHelper.ResolveBindTarget(negate ? bindAs.Substring(1) : bindAs, _lambdaRepo, _scopeChain);

            bool render = this.IsTrue(target);

            if (negate)
            { render = !render; }

            if (render)
            {
                _scopeChain.ApplyVariableScopeMarker();

                TemplateEngine subEngine = new TemplateEngine(block.ToString())
                    .WithProgressListener(_progress)
                    .WithWhitespaceSuppression(_trimWhitespace)
                    .WithScopeChain(_scopeChain)
                    .WithMaxStack(_maxStack)
                    .WithLambdaRepository(_lambdaRepo);

                _result.Append(subEngine.Merge());

                _scopeChain.DereferenceVariableScope();
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
                       || (bit = val as bool?) != null && bit == false
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
                       || (s = val as string) != null && (s.Length == 1 && s[0] == '\0');

            return !isFalse;
        }
        #endregion

        #region handle each tag
        private void HandleEachTag(in Tag tag)
        {
            this.EnsureLeftTrim(_result, tag);
            this.EnsureRightTrim(tag);

            StringBuilder block = new StringBuilder();

            Action<char> emitEnclosedTo = (s) => { block.Append(s); };

            //roll and emit intil proper #/each tag found (allowing nested #each #/each tags
            Tag closeTag;
            this.RollBlockedContentTill(emitEnclosedTo, Tag.IsEndEachTag, Tag.IsEachTag, out closeTag);
            this.EnsureLeftTrim(block, closeTag);
            this.EnsureRightTrim(closeTag);

            string bindAs = tag.BindAs();

            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            if (!(target == null)) //if null just ignore
            {
                //if target is not enumerable, should not be bound to an #each tag
                if (!(target is System.Collections.IEnumerable))
                { throw new MergeException($"#each tag bound to non-enumerable object: {bindAs}"); }

                //cast to enumerable
                var items = (System.Collections.IEnumerable)target;
                string itemContent;

                TemplateEngine subEngine;
                subEngine = new TemplateEngine(block.ToString())
                    .WithProgressListener(_progress)
                    .WithWhitespaceSuppression(_trimWhitespace)
                    .WithScopeChain(_scopeChain)
                    .WithMaxStack(_maxStack)
                    .WithLambdaRepository(_lambdaRepo);

                foreach (var item in items)
                {
                    _scopeChain.ApplyVariableScopeMarker();
                    itemContent = subEngine.Merge(item);
                    _result.Append(itemContent);
                    _scopeChain.DereferenceVariableScope();
                }
            }
        }
        #endregion

        #region handle with tag
        private void HandleWithTag(in Tag tag)
        {
            this.EnsureLeftTrim(_result, tag);
            this.EnsureRightTrim(tag);

            StringBuilder block = new StringBuilder();

            Action<char> emitEnclosedTo = (s) => { block.Append(s); };

            //roll and emit intil proper #/each tag found (allowing nested #each #/each tags
            Tag closeTag;
            this.RollBlockedContentTill(emitEnclosedTo, Tag.IsEndWithTag, Tag.IsWithTag, out closeTag);
            this.EnsureLeftTrim(block, closeTag);
            this.EnsureRightTrim(closeTag);

            string bindAs = tag.BindAs();

            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            string itemContent;
            TemplateEngine subEngine;

            _scopeChain.ApplyVariableScopeMarker();

            subEngine = new TemplateEngine(block.ToString())
                .WithProgressListener(_progress)
                .WithWhitespaceSuppression(_trimWhitespace)
                .WithScopeChain(_scopeChain)
                .WithMaxStack(_maxStack)
                .WithLambdaRepository(_lambdaRepo);

            itemContent = subEngine.Merge(target);
            _result.Append(itemContent);

            _scopeChain.DereferenceVariableScope();
        }
        #endregion

        #region handle variable tag
        private void HandleVariableTag(in Tag tag, bool isDeclaration)
        {
            this.EnsureLeftTrim(_result, tag);
            this.EnsureRightTrim(tag);

            string expression = tag.BindAs(); //example:  :name=$.Name, or with no assign example:  :name

            StringBuilder sb = new StringBuilder();
            string name = null;
            bool assignment = false;
            for (int i = 0; i < expression.Length; i++)
            {
                if (!assignment && expression[i] == '=')
                {
                    name = sb.ToString();
                    sb.Clear();
                    assignment = true;
                    continue;
                }
                sb.Append(expression[i]);
            }

            if (!assignment)
            {
                name = sb.ToString();
            }

            string bindAs = assignment ? sb.ToString() : null;

            object value = null;
            if (assignment)
            {
                if (BindHelper.IsSingleQuoted(bindAs) || BindHelper.IsDoubleQuoted(bindAs))
                    value = bindAs.Substring(1, (bindAs.Length - 2));   //string literal
                else if (BindHelper.IsNumericLiteral(bindAs))
                    value = BindHelper.ParseNumericLiteral(bindAs);     //numeric literal
                else if (string.Compare(bindAs, "true", true) == 0)
                    value = true;
                else if (string.Compare(bindAs, "false", true) == 0)
                    value = false;
                else
                    value = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);
            }

            if (isDeclaration)
                _scopeChain.SetVariable(name, value);
            else
                _scopeChain.UpdateVariable(name, value);
        }
		#endregion

		#region handle variable declare tag
		private void HandleVariableDeclareTag(in Tag tag)
        {
            this.HandleVariableTag(in tag, true);
        }
        #endregion

        #region handle variable assign tag
        private void HandleVariableAssignTag(in Tag tag)
        {
            this.HandleVariableTag(in tag, false);
        }
		#endregion

		#region handle partial tag (sub templates)
		private void HandlePartialTag(in Tag tag)
        {
            this.EnsureLeftTrim(_result, tag);
            this.EnsureRightTrim(tag);

            string bindAs = tag.BindAs();
            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            string tgt = (target as string);
            if (tgt == null)
            { throw new MergeException($"#sub template tag: {tag} reflected value is not typeof string: {target}"); }

            _scopeChain.ApplyVariableScopeMarker();

            TemplateEngine subEngine = new TemplateEngine(tgt)
                .WithProgressListener(_progress)
                .WithWhitespaceSuppression(_trimWhitespace)
                .WithScopeChain(_scopeChain)
                .WithMaxStack(_maxStack - 1) //decrement 1 unit for sub template...
                .WithLambdaRepository(_lambdaRepo);

            string result = subEngine.Merge();

            _result.Append(result);

            _scopeChain.DereferenceVariableScope();
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

        private bool RollTill(Action<char> emitTo, Func<char, bool> till, bool greedy, bool isSubBlock, bool breakOnEscape = false)
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
                        if (breakOnEscape)
                            break;

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

                _nextTag.Reset();
                tagIsContent = false;

                this.RollTill(_nextTag.Append, '}', true, true);

                if (_nextTag.Length == 0)
                { throw new MergeException($"enountered un-closed tag; 'till' condition never found"); }

                if (!till(_nextTag.ToString()))
                {
                    tagIsContent = true;
                    if (ensuring(_nextTag.ToString()))
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
                        endTag = new Tag(_nextTag.ToString(), _trimWhitespace);
                    }
                }
                if (tagIsContent)
                {
                    for (int i = 0; i < _nextTag.Length; i++)
                    {
                        emitTo(_nextTag[i]);
                    }
                }

            } while (offset > 0);
        }
        #endregion

        #region ensure left trim
        private void EnsureLeftTrim(StringBuilder from, in Tag tag)
        {
            if (tag.ShouldTrimLeft())
            {
                int idx = from.Length - 1;
                while (idx > -1 && (from[idx] == '\t' || from[idx] == ' '))
                {
                    idx -= 1;
                }
                from.Length = (idx + 1);
            }
        }
        #endregion

        #region ensure right trim
        private void EnsureRightTrim(in Tag tag)
        {
            if (tag.ShouldTrimRight())
            {
                Action<char> emitTo = (c) => { }; //just throw away the whitespace...

                char lastChar = '\0';
                Func<char, bool> isNotWhitespace = (c) =>
                {
                    lastChar = c;
                    return !(c == ' ' || c == '\t');
                };

                bool found = this.RollTill(emitTo, isNotWhitespace, false, false, true);

                if (lastChar == '\r' && this.PeekAt(_index + 1) == '\n')
                    _index += 2;
                else if (lastChar == '\n' || lastChar == '\r') //the second condition will eliminate rogue \r chars...
                    _index += 1;
            }
        }
        #endregion
    }
}