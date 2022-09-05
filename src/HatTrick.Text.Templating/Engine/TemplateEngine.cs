using System;
using System.Text;

namespace HatTrick.Text.Templating
{
    public class TemplateEngine
    {
        #region internals
        private int _index;
        private int _lineNum;
        private int _columnNum;
        private string _template;
        private ScopeChain _scopeChain;
        private LambdaRepository _lambdaRepo;
        private StringBuilder _result;
        private StringBuilder _tag;
        private int _maxStack = 32;
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
        #endregion

        #region constructors
        public TemplateEngine(string template)
        {
            _template = template ?? throw new ArgumentNullException(nameof(template));
            _index = 0;
            _tag = new StringBuilder(60);
            _result = new StringBuilder((int)(template.Length * 1.3));
            _scopeChain = new ScopeChain();
        }

        private TemplateEngine(string template, ScopeChain scopeChain, LambdaRepository lambdaRepo, int maxStack, bool trimWhiteSpace)
        {
            _template = template ?? throw new ArgumentNullException(nameof(template));
            _scopeChain = scopeChain;
            _lambdaRepo = lambdaRepo;
            _maxStack = maxStack > 0 ? maxStack : throw new InvalidOperationException($"Stack depth overflow...stack depth cannot exceed {_maxStack}"); ;
            _trimWhitespace = trimWhiteSpace;
            _index = 0;
            _tag = new StringBuilder(60);
            _result = new StringBuilder((int)(template.Length * 1.3));
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
            _tag.Clear();
            _index = 0;
            _lineNum = 1;
            _columnNum = 1;

            try
            {
                this.Scan();
                return _result.ToString();
            }
            catch (MergeException mex)
            {
                mex.Context.Push(this.ResolveExceptionContext());
                throw;
            }
            catch (Exception ex)
            {
                var mex = new MergeException("An error occurrred while merging the template.  See the inner exception for details.", ex);
                mex.Context.Push(this.ResolveExceptionContext());
                throw mex;
            }
        }
        #endregion

        #region scan
        private void Scan()
        {
            char eot = (char)3; //end of text....

            while (this.Peek() != eot)
            {
                if (this.MunchContent(ref _result, false))
                {
                    if (this.MunchTag(ref _tag))
                        this.HandleTag(new Tag(_tag.ToString(), _trimWhitespace));

                    else
                        throw new InvalidOperationException("Enountered un-closed tag; '}' (close tag) never found.");
                }
                _tag.Clear();
            }
        }
        #endregion

        #region resolve exception context
        private MergeExceptionContext ResolveExceptionContext()
        {
            string lastTag = (_tag.Length > 0) ? _tag.ToString() : null;
            return new MergeExceptionContext(_lineNum, _columnNum, _index, lastTag);
        }
        #endregion

        #region handle tag
        private void HandleTag(Tag tag)
        {
            switch (tag.Type)
            {
                case TagType.Simple:
                    this.HandleSimpleTag(tag);
                    break;
                case TagType.If:
                    this.HandleIfTag(tag);
                    break;
                case TagType.Each:
                    this.HandleEachTag(tag);
                    break;
                case TagType.With:
                    this.HandleWithTag(tag);
                    break;
                case TagType.VarDeclare:
                    this.HandleVariableDeclareTag(tag);
                    break;
                case TagType.VarAssign:
                    this.HandleVariableAssignTag(tag);
                    break;
                case TagType.Partial:
                    this.HandlePartialTag(tag);
                    break;
                case TagType.Comment:
                    this.HandleCommentTag(tag);
                    break;
            }
        }
        #endregion

        #region handle comment tag
        private void HandleCommentTag(Tag tag)
        {
            this.EnsureLeftTrim(_result, tag);
            this.EnsureRightTrim(tag);
        }
        #endregion

        #region handle simple tag
        private void HandleSimpleTag(Tag tag)
        {
            string bindAs = tag.BindAs();
            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            _result.Append(target ?? string.Empty);
        }
        #endregion

        #region handle if tag
        private void HandleIfTag(Tag tag)
        {
            this.EnsureLeftTrim(_result, tag);
            this.EnsureRightTrim(tag);

            StringBuilder block = new StringBuilder();

            //roll and emit until proper #/if tag found (allowing nested #if #/if tags
            Tag endTag;
            this.MunchBlock(ref block, TagType.If, out endTag);

            this.EnsureLeftTrim(block, endTag);
            this.EnsureRightTrim(endTag);

            string bindAs = tag.BindAs();
            bool negate = bindAs[0] == '!';

            object target = BindHelper.ResolveBindTarget(negate ? bindAs.Substring(1) : bindAs, _lambdaRepo, _scopeChain);

            bool render = this.IsTrue(target);

            if (negate)
            { render = !render; }

            if (render)
            {
                _scopeChain.ApplyVariableScopeMarker();
                string template = block.ToString();
                TemplateEngine subEngine = new TemplateEngine(template, _scopeChain, _lambdaRepo, (_maxStack - 1), _trimWhitespace);
                string result = subEngine.Merge();
                _result.Append(result);
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
                       || (col = val as System.Collections.IEnumerable) != null && !col.GetEnumerator().MoveNext() //NOTE: this will catch string.Empty
                       || (s = val as string) != null && (s.Length == 1 && s[0] == '\0');

            return !isFalse;
        }
        #endregion

        #region handle each tag
        private void HandleEachTag(Tag tag)
        {
            this.EnsureLeftTrim(_result, tag);
            this.EnsureRightTrim(tag);

            StringBuilder block = new StringBuilder();

            //roll and emit until proper #/each tag found (allowing nested #each #/each tags
            Tag endTag;
            this.MunchBlock(ref block, TagType.Each, out endTag);
            this.EnsureLeftTrim(block, endTag);
            this.EnsureRightTrim(endTag);

            string bindAs = tag.BindAs();

            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            if (!(target == null)) //if null just ignore
            {
                //if target is not enumerable, should not be bound to an #each tag
                if (!(target is System.Collections.IEnumerable))
                    throw new InvalidOperationException($"#each tag bound to non-enumerable object: {bindAs}");

                //cast to enumerable
                var items = (System.Collections.IEnumerable)target;
                string itemContent;
                TemplateEngine subEngine;
                string template = block.ToString();
                subEngine = new TemplateEngine(template, _scopeChain, _lambdaRepo, (_maxStack - 1), _trimWhitespace);

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
        private void HandleWithTag(Tag tag)
        {
            this.EnsureLeftTrim(_result, tag);
            this.EnsureRightTrim(tag);

            StringBuilder block = new StringBuilder();

            //roll and emit intil proper #/each tag found (allowing nested #each #/each tags
            Tag endTag;
            this.MunchBlock(ref block, TagType.With, out endTag);
            this.EnsureLeftTrim(block, endTag);
            this.EnsureRightTrim(endTag);

            string bindAs = tag.BindAs();

            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            string itemContent;
            TemplateEngine subEngine;
            _scopeChain.ApplyVariableScopeMarker();
            string template = block.ToString();
            subEngine = new TemplateEngine(template, _scopeChain, _lambdaRepo, (_maxStack - 1), _trimWhitespace);
            itemContent = subEngine.Merge(target);
            _result.Append(itemContent);

            _scopeChain.DereferenceVariableScope();
        }
        #endregion

        #region handle variable tag
        private void HandleVariableTag(Tag tag, bool isDeclaration)
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
		private void HandleVariableDeclareTag(Tag tag)
        {
            this.HandleVariableTag(tag, true);
        }
        #endregion

        #region handle variable assign tag
        private void HandleVariableAssignTag(Tag tag)
        {
            this.HandleVariableTag(tag, false);
        }
		#endregion

		#region handle partial tag (sub templates)
		private void HandlePartialTag(Tag tag)
        {
            this.EnsureLeftTrim(_result, tag);
            this.EnsureRightTrim(tag);

            string bindAs = tag.BindAs();
            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            string template = (target as string) ?? throw new InvalidOperationException($"Sub template tag: {tag} reflected value is not typeof string: {target}");

            _scopeChain.ApplyVariableScopeMarker();
            TemplateEngine subEngine = new TemplateEngine(template, _scopeChain, _lambdaRepo, (_maxStack - 1), _trimWhitespace);
            string result = subEngine.Merge();
            _result.Append(result);
            _scopeChain.DereferenceVariableScope();
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
        public char Peek(int forward)
        {
            int at = _index + forward;
            char c = (_template.Length > at)
                ? _template[at]
                : (char)3; //eot (end of text)

            return c;
        }
        #endregion

        #region read
        private char Read()
        {
            char c = (_template.Length > _index)
                 ? _template[_index++]
                 : (char)3; //eot (end of text)

            if (c == '\n')
            {
                _lineNum += 1;
                _columnNum = 0;
            }
            else if (c != '\r')
            {
                _columnNum += 1;
            }

            return c;
        }
        #endregion

        #region step back
        private void StepBack()
        {
            if (_index == 0)
                throw new InvalidOperationException("Cannot step backward, current template index is at 0.");

            _index -= 1;
            char c = _template[_index];

            if (c != '\n' && c != '\r')
                _columnNum -= 1;
        }
        #endregion

        #region munch
        private bool MunchContent(ref StringBuilder output, bool isSubBlock)
        {
            char c;
            char eot = (char)3; //eot (end of text)
            while ((c = this.Read()) != eot)
            {
                if (c == '{' && this.Peek() == '{')
                {
                    
                    if (isSubBlock) //if parsing blocked template content, maintain the escape char
                        output.Append(c).Append(this.Read());

                    else //discard the first one(the escape char) and write the second one
                        output.Append(this.Read());
                    
                    continue;
                }

                if (c == '{') //if open bracket that is not escaped, we found a tag
                {
                    this.StepBack();//back the index up 1 spot to basically pop the open tag char '{' back into the read queue
                    return true;
                }

                if (c == '}')
                {
                    if (this.Peek() == '}')
                    {
                        if (isSubBlock) //if parsing blocked template content, maintain the escape char
                            output.Append(c).Append(this.Read());

                        else //discard the first one(the escape char) and write the second one
                            output.Append(this.Read());

                        continue;
                    }
                    else
                    {
                        throw new InvalidOperationException("Encountered un-escaped close tag '}' within template content");
                    }
                }

                output.Append(c);
            }
            return false;
        }

        private bool MunchTag(ref StringBuilder tag)
        {
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            char escape = '\\';
            char singleQuote = '\'';
            char doubleQuote = '"';
            char tab = '\t';
            char space = ' ';
            char nl = '\n';
            char cr = '\r';
            char previous = '\0';
            char c = '\0';
            char eot = (char)3; //(end of text)

            int offset = 0;
            bool isComment = false;
            Func<StringBuilder, bool> isCommentTag = (StringBuilder sb) =>
            {
                return isComment ? isComment : (isComment = Tag.IsCommentTag(sb.ToString()));
            };

            while ((c = this.Read()) != eot)
            {
                if (c == '{')
                    offset += 1;

                else if (c == '}')
                    offset -= 1;

                //if double quote & not escaped & not already inside single quotes...
                if (c == doubleQuote && previous != escape && !inSingleQuote)
                    inDoubleQuote = !inDoubleQuote;

                //if single quote & not escaped & not already inside double quotes...
                if (c == singleQuote && previous != escape && !inDoubleQuote)
                    inSingleQuote = !inSingleQuote;

                //only append white space if inside double or single quotes...
                bool inQuotes = (inDoubleQuote || inSingleQuote);
                bool isWhiteSpace = c == space || c == tab || c == nl || c == cr;

                if (!isWhiteSpace || inQuotes)
                    tag.Append(c);

                if (c == '}' && (!inQuotes || isCommentTag(tag)) && !(offset > 0 && isCommentTag(tag)))
                    return true;

                previous = c;
            }
            return false;
        }

        public void MunchBlock(ref StringBuilder output, TagType beginType, out Tag endTag)
        {
            char c;
            char eot = (char)3; //(end of text)
            int offset = 1; // need to ensure we bypass any nested tags
            var tagBuffer = new StringBuilder(60);
            TagType endType = Tag.ResolveEndTagType(beginType);
            endTag = null;

            while ((c = this.Peek()) != eot)
            {
                if (this.MunchContent(ref output, true))
                {
                    if (!this.MunchTag(ref tagBuffer))
                        throw new InvalidOperationException($"Enountered un-closed tag...'{endType}' tag never found");

                    string tag = tagBuffer.ToString();
                    TagType type = Tag.ResolveType(tag);

                    if (type == beginType)
                        offset += 1;

                    else if (type == endType)
                        offset -= 1;

                    /**********************************************/

                    if (offset > 0)
                    {
                        output.Append(tag);
                    }
                    else if (offset == 0)
                    {
                        //we found the end tag...
                        endTag = new Tag(tag, _trimWhitespace);
                        break;
                    }

                    tagBuffer.Clear();
                }
            }
        }
        #endregion

        #region ensure left trim
        private void EnsureLeftTrim(StringBuilder from, Tag tag)
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
        private void EnsureRightTrim(Tag tag)
        {
            if (tag.ShouldTrimRight())
            {
                char lastChar = '\0';
                Func<char, bool> isWhitespace = (c) =>
                {
                    lastChar = c;
                    return c == ' ' || c == '\t';
                };

                //dispose of read char until we encounter something other than <space> or <tab>
                while (isWhitespace(this.Peek()))
                {
                    _ = this.Read(); //discard whitespace...
                }

                //must account for the removal of the 1 newline for both unix and windows based systems...could be 1 or two chars needing disposed
                if (lastChar == '\r' || lastChar == '\n')
                    _ = this.Read();

                lastChar = this.Peek();
                if (lastChar == '\r' || lastChar == '\n')
                    _ = this.Read();
            }
        }
        #endregion
    }
}
