using System;
using System.Text;

namespace HatTrick.Text.Templating
{
    public class TemplateEngine
    {
        #region constants
        public const int MaxStack = 64;
        #endregion

        #region internals
        private int _index;
        private int _lineNum;
        private int _columnNum;
        private string _template;
        private ScopeChain _scopeChain;
        private LambdaRepository _lambdaRepo;
        private StringBuilder _result;
        private StringBuilder _tag;
        private int _maxStack;
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
            _maxStack = TemplateEngine.MaxStack;
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
                    this.MunchTag(ref _tag, false);
                    this.HandleTag(new Tag(_tag, _trimWhitespace));
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
                case TagType.Debug:
                    this.HandleDebugTag(tag);
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
            this.MunchBlockContent(ref block, TagType.If, out endTag);

            this.EnsureLeftTrim(block, endTag);

            string bindAs = tag.BindAs();
            bool negate = bindAs[0] == '!';

            object target = BindHelper.ResolveBindTarget(negate ? bindAs.Substring(1) : bindAs, _lambdaRepo, _scopeChain);

            bool render = BindHelper.IsTrue(target);

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

            this.EnsureRightTrim(endTag);
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
            this.MunchBlockContent(ref block, TagType.Each, out endTag);
            this.EnsureLeftTrim(block, endTag);

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

            this.EnsureRightTrim(endTag);
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
            this.MunchBlockContent(ref block, TagType.With, out endTag);
            this.EnsureLeftTrim(block, endTag);

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

            this.EnsureRightTrim(endTag);
        }
        #endregion

        #region handle variable tag
        private void HandleVariableTag(Tag tag, bool isDeclaration)
        {
            this.EnsureLeftTrim(_result, tag);

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
                    value = BindHelper.UnQuote(bindAs);                 //string literal

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

            this.EnsureRightTrim(tag);
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

            string bindAs = tag.BindAs();
            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            string template = (target as string) ?? throw new InvalidOperationException($"Sub template tag: {tag} reflected value is not typeof string: {target}");

            _scopeChain.ApplyVariableScopeMarker();
            TemplateEngine subEngine = new TemplateEngine(template, _scopeChain, _lambdaRepo, (_maxStack - 1), _trimWhitespace);
            string result = subEngine.Merge();
            _result.Append(result);
            _scopeChain.DereferenceVariableScope();

            this.EnsureRightTrim(tag);
        }
        #endregion

        #region handle debug tag
        private void HandleDebugTag(Tag tag)
        {
            this.EnsureLeftTrim(_result, tag);

            string bindAs = tag.BindAs();

            object output = null;
            if (BindHelper.IsDoubleQuoted(bindAs) || BindHelper.IsSingleQuoted(bindAs))
                output = BindHelper.Strip('\\', BindHelper.UnQuote(bindAs));

            else if (BindHelper.IsNumericLiteral(bindAs))
                output = bindAs;

            else
                output = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            System.Diagnostics.Trace.WriteLine(output);

            this.EnsureRightTrim(tag);
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

        public char Peek(int forward)
        {
            int at = _index + forward;
            char c = (_template.Length > at)
                ? _template[at]
                : (char)3; //eot (end of text)

            return c;
        }

        private char Peek(Predicate<char> till)
        {
            char c;
            int i = _index;
            while (i < _template.Length)
            {
                c = _template[i++];
                if (till(c))
                    return c;
            }

            return (char)3; //eot (end of text)
        }
        #endregion

        #region read
        private char Read()
        {
            char eot = (char)3;
            char c = (_template.Length > _index)
                 ? _template[_index++]
                 : eot; //eot (end of text)

            if (c != eot)
            {
                if (c == '\n')
                {
                    _lineNum += 1;
                    _columnNum = 1;
                }
                else if (c != '\r')
                {
                    _columnNum += 1;
                }
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

            if (c != '\n')
            {
                if (c != '\r')
                    _columnNum -= 1;
            }
            else
            {
                _lineNum -= 1;
            }
        }
        #endregion

        #region munch
        private bool MunchContent(ref StringBuilder output, bool verbatim)
        {
            char c;
            char eot = (char)3; //eot (end of text)
            while ((c = this.Read()) != eot)
            {
                if (c == '{')
                {
                    if (this.Peek() == '{')
                    {
                        if (verbatim) //if parsing blocked template content, maintain the escape char
                            output.Append(c).Append(this.Read());

                        else //discard the first one(the escape char) and write the second one
                            output.Append(this.Read());

                        continue;
                    }
                    //if the open bracket is not escaped, we found a tag
                    this.StepBack();//back the index up 1 spot to basically pop the open tag char '{' back on to the read queue
                    return true;
                }

                if (c == '}')
                {
                    if (this.Peek() == '}')
                    {
                        if (verbatim) //if parsing blocked template content, maintain the escape char
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

        private void MunchTag(ref StringBuilder tag, bool verbatim)
        {
            Predicate<char> isTagDesignator = (c) => !(c == '{' || c == '-' || c == '+' || c == ' ' || c == '\t' || c == '\n' || c == '\r');
            char designator = this.Peek(isTagDesignator);
            switch (designator)
            {
                case '#':
                    this.MunchBlockTag(ref tag, verbatim);
                    break;
                case '/':
                    this.MunchEndBlockTag(ref tag, verbatim);
                    break;
                case '?':
                    this.MunchVariableTag(ref tag, verbatim);
                    break;
                case '>':
                    this.MunchParialTag(ref tag, verbatim);
                    break;
                case '@':
                    this.MunchDebugTag(ref tag, verbatim);
                    break;
                case '!':
                    this.MunchCommentTag(ref tag);
                    break;
                default:
                    this.MunchSimpleTag(ref tag, verbatim);
                    break;
            }
        }

        private void MunchTagDefault(ref StringBuilder tag, bool verbatim, out bool closed)
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

            bool inQuotes = false;
            while ((c = this.Read()) != eot)
            {
                //if double quote & not escaped & not already inside single quotes...
                if (c == doubleQuote && previous != escape && !inSingleQuote)
                    inDoubleQuote = !inDoubleQuote;

                //if single quote & not escaped & not already inside double quotes...
                if (c == singleQuote && previous != escape && !inDoubleQuote)
                    inSingleQuote = !inSingleQuote;

                //only append white space if inside double or single quotes...
                inQuotes = (inDoubleQuote || inSingleQuote);
                bool isWhiteSpace = c == space || c == tab || c == nl || c == cr;

                if (!isWhiteSpace || verbatim || inQuotes)
                    tag.Append(c);

                if (c == '}' && !inQuotes)
                {
                    closed = true;
                    return;
                }

                previous = c;
            }
            closed = false;
        }

        private void MunchBlockTag(ref StringBuilder tag, bool verbatim)
        {
            this.MunchTagDefault(ref tag, verbatim, out bool closed);
            if (!closed)
                throw new InvalidOperationException($"Enountered un-closed {Tag.ResolveType(tag)} tag...'}}' never found.");
        }

        private void MunchEndBlockTag(ref StringBuilder tag, bool verbatim)
        {
            this.MunchTagDefault(ref tag, verbatim, out bool closed);
            if (!closed)
            {
                TagType type = TagType.Unknown;
                if (_tag.Length > 0) //last parsed tag, SHOULD be an open block tag...
                {
                    TagType t = Tag.ResolveType(_tag);
                    if (Tag.IsBlockTag(t, out BlockTagOrientation orientation) && orientation == BlockTagOrientation.Begin)
                    {
                        type = Tag.ResolveEndTagType(t);
                    }
                }
                string desc = type == TagType.Unknown ? "end block" : type.ToString();
                throw new InvalidOperationException($"Enountered un-closed {desc} tag...'}}' never found.");
            }
        }

        private void MunchVariableTag(ref StringBuilder tag, bool verbatim)
        {
            this.MunchTagDefault(ref tag, verbatim, out bool closed);
            if (!closed)
            {
                TagType t = Tag.ResolveType(tag);
                string desc = t == TagType.VarAssign || t == TagType.VarDeclare ? t.ToString() : "Variable";
                throw new InvalidOperationException($"Enountered un-closed {desc}...'}}' never found.");
            }
        }

        private void MunchParialTag(ref StringBuilder tag, bool verbatim)
        {
            this.MunchTagDefault(ref tag, verbatim, out bool closed);
            if (!closed)
                throw new InvalidOperationException($"Enountered un-closed {TagType.Partial} tag...'}}' never found.");
        }

        private void MunchDebugTag(ref StringBuilder tag, bool verbatim)
        {
            this.MunchTagDefault(ref tag, verbatim, out bool closed);
            if (!closed)
                throw new InvalidOperationException($"Enountered un-closed {TagType.Debug} tag...'}}' never found.");
        }

        private void MunchCommentTag(ref StringBuilder tag)
        {
            char escape = '\\';
            char previous = '\0';
            char c = '\0';
            char eot = (char)3; //(end of text)

            int offset = 0;
            while ((c = this.Read()) != eot)
            {
                offset += (c == '{') ? 1 : (c == '}' && previous != escape) ? -1 : 0;

                tag.Append(c);

                if (c == '}')
                {
                    if (offset == 0 && previous != escape)
                        return;
                }

                previous = c;
            }

            throw new InvalidOperationException($"Enountered un-closed {TagType.Comment} tag...'}}' never found.");
        }

        private void MunchSimpleTag(ref StringBuilder tag, bool verbatim)
        {
            this.MunchTagDefault(ref tag, verbatim, out bool closed);
            if (!closed)
                throw new InvalidOperationException($"Enountered un-closed {TagType.Simple} tag...'}}' never found.");
        }

        private void MunchBlockContent(ref StringBuilder output, TagType beginType, out Tag endTag)
        {
            char c;
            char eot = (char)3; //(end of text)
            int offset = 1; //need to ensure we bypass any nested tags
            var tag = new StringBuilder(60);
            TagType endType = Tag.ResolveEndTagType(beginType);
            endTag = null;

            while ((c = this.Peek()) != eot)
            {
                if (this.MunchContent(ref output, true))
                {
                    this.MunchTag(ref tag, true);

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

                    tag.Clear();
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
