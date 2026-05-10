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
        private ReadOnlyMemory<char> _template;
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
            if (template == null) throw new ArgumentNullException(nameof(template));
            _template = template.AsMemory();
            _maxStack = TemplateEngine.MaxStack;
            _index = 0;
            _tag = new StringBuilder(60);
            _result = new StringBuilder((int)(template.Length * 1.3));
            _scopeChain = new ScopeChain();
        }

        private TemplateEngine(ReadOnlyMemory<char> template, ScopeChain scopeChain, LambdaRepository lambdaRepo, int maxStack, bool trimWhiteSpace)
        {
            _template = template;
            _scopeChain = scopeChain;
            _lambdaRepo = lambdaRepo;
            _maxStack = maxStack > 0 ? maxStack : throw new InvalidOperationException($"Stack depth overflow...stack depth cannot exceed {_maxStack}");
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
                //MunchContent returns true if a tag is encountered...
                if (this.MunchContent(_result))
                {
                    this.MunchTag(_tag, false);
                    this.HandleTag(new Tag(_tag, _trimWhitespace));
                    _tag.Clear();
                }
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
            ReadOnlySpan<char> bindAs = tag.BindAs();
            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            _result.Append(target);
        }
        #endregion

        #region handle if tag
        private void HandleIfTag(Tag tag)
        {
            this.EnsureLeftTrim(_result, tag);
            this.EnsureRightTrim(tag);

            ReadOnlySpan<char> bindAs = tag.BindAs();
            bool negate = bindAs[0] == '!';
            object target = BindHelper.ResolveBindTarget(negate ? bindAs.Slice(1) : bindAs, _lambdaRepo, _scopeChain);
            bool render = BindHelper.IsTrue(target);
            if (negate)
                render = !render;

            int blockStart = _index;
            Tag endTag;
            int blockEnd = this.MunchBlockContent(TagType.If, out endTag);

            if (endTag.ShouldTrimLeft())
                blockEnd = this.TrimBlockEnd(blockEnd);

            if (render)
            {
                _scopeChain.ApplyVariableScopeMarker();
                TemplateEngine subEngine = new TemplateEngine(_template.Slice(blockStart, blockEnd - blockStart), _scopeChain, _lambdaRepo, (_maxStack - 1), _trimWhitespace);
                _result.Append(subEngine.Merge());
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

            ReadOnlySpan<char> bindAs = tag.BindAs();
            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            int blockStart = _index;
            Tag endTag;
            int blockEnd = this.MunchBlockContent(TagType.Each, out endTag);

            if (endTag.ShouldTrimLeft())
                blockEnd = this.TrimBlockEnd(blockEnd);

            if (target != null)
            {
                if (!(target is System.Collections.IEnumerable))
                    throw new InvalidOperationException($"#each tag bound to non-enumerable object: {bindAs}");

                var items = (System.Collections.IEnumerable)target;
                TemplateEngine subEngine = new TemplateEngine(_template.Slice(blockStart, blockEnd - blockStart), _scopeChain, _lambdaRepo, (_maxStack - 1), _trimWhitespace);
                foreach (var item in items)
                {
                    _scopeChain.ApplyVariableScopeMarker();
                    _result.Append(subEngine.Merge(item));
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

            ReadOnlySpan<char> bindAs = tag.BindAs();
            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            int blockStart = _index;
            Tag endTag;
            int blockEnd = this.MunchBlockContent(TagType.With, out endTag);

            if (endTag.ShouldTrimLeft())
                blockEnd = this.TrimBlockEnd(blockEnd);

            _scopeChain.ApplyVariableScopeMarker();
            TemplateEngine subEngine = new TemplateEngine(_template.Slice(blockStart, blockEnd - blockStart), _scopeChain, _lambdaRepo, (_maxStack - 1), _trimWhitespace);
            _result.Append(subEngine.Merge(target));
            _scopeChain.DereferenceVariableScope();

            this.EnsureRightTrim(endTag);
        }
        #endregion

        #region handle variable tag
        private void HandleVariableTag(Tag tag, bool isDeclaration)
        {
            this.EnsureLeftTrim(_result, tag);

            ReadOnlySpan<char> expression = tag.BindAs(); //example:  :name=$.Name, or with no assign example:  :name

            int eqIdx = expression.IndexOf('=');
            bool assignment = eqIdx >= 0;
            string name = (assignment ? expression.Slice(0, eqIdx) : expression).ToString();
            ReadOnlySpan<char> bindAs = assignment ? expression.Slice(eqIdx + 1) : ReadOnlySpan<char>.Empty;

            object value = null;
            if (assignment)
            {
                if (BindHelper.IsSingleQuoted(bindAs) || BindHelper.IsDoubleQuoted(bindAs))
                    value = bindAs.Slice(1, bindAs.Length - 2).ToString();

                else if (MemoryExtensions.Equals(bindAs, "true", StringComparison.OrdinalIgnoreCase))
                    value = true;

                else if (MemoryExtensions.Equals(bindAs, "false", StringComparison.OrdinalIgnoreCase))
                    value = false;

                else
                {
                    value = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);
                }
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

            ReadOnlySpan<char> bindAs = tag.BindAs();
            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            string template = (target as string) ?? throw new InvalidOperationException($"Sub template tag: {tag} reflected value is not typeof string: {target}");

            _scopeChain.ApplyVariableScopeMarker();
            TemplateEngine subEngine = new TemplateEngine(template.AsMemory(), _scopeChain, _lambdaRepo, (_maxStack - 1), _trimWhitespace);
            _result.Append(subEngine.Merge());
            _scopeChain.DereferenceVariableScope();

            this.EnsureRightTrim(tag);
        }
        #endregion

        #region handle debug tag
        private void HandleDebugTag(Tag tag)
        {
            this.EnsureLeftTrim(_result, tag);

            ReadOnlySpan<char> bindAs = tag.BindAs();

            object output;
            if (BindHelper.IsDoubleQuoted(bindAs) || BindHelper.IsSingleQuoted(bindAs))
                output = BindHelper.Strip('\\', bindAs.Slice(1, bindAs.Length - 2));

            else if (MemoryExtensions.Equals(bindAs, "true", StringComparison.OrdinalIgnoreCase))
                output = true;

            else if (MemoryExtensions.Equals(bindAs, "false", StringComparison.OrdinalIgnoreCase))
                output = false;

            else
            {
                output = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);
            }

            System.Diagnostics.Trace.WriteLine(output);

            this.EnsureRightTrim(tag);
        }
        #endregion

        #region peek
        public char Peek()
        {
            ReadOnlySpan<char> span = _template.Span;
            return span.Length > _index ? span[_index] : (char)3;
        }

        public char Peek(int forward)
        {
            ReadOnlySpan<char> span = _template.Span;
            int at = _index + forward;
            return span.Length > at ? span[at] : (char)3;
        }

        private char PeekTagDesignator()
        {
            ReadOnlySpan<char> span = _template.Span;
            int i = _index;
            while (i < span.Length)
            {
                char c = span[i++];
                if (!(c == '{' || c == '-' || c == '+' || c == ' ' || c == '\t' || c == '\n' || c == '\r'))
                    return c;
            }
            return (char)3;
        }
        #endregion

        #region read
        private char Read()
        {
            char eot = (char)3;
            ReadOnlySpan<char> span = _template.Span;
            char c = span.Length > _index ? span[_index++] : eot;

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
            char c = _template.Span[_index];

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
        private bool MunchContent(StringBuilder output)
        {
            char c;
            char eot = (char)3;
            while ((c = this.Read()) != eot)
            {
                if (c == '{')
                {
                    if (this.Peek() == '{')
                    {
                        output.Append(this.Read()); //discard escape char, write the literal '{'
                        continue;
                    }
                    this.StepBack();
                    return true;
                }

                if (c == '}')
                {
                    if (this.Peek() == '}')
                    {
                        output.Append(this.Read()); //discard escape char, write the literal '}'
                        continue;
                    }
                    throw new InvalidOperationException("Encountered un-escaped close tag '}' within template content");
                }

                output.Append(c);
            }
            return false;
        }

        private bool MunchRawContentSkip()
        {
            char c;
            char eot = (char)3;
            while ((c = this.Read()) != eot)
            {
                if (c == '{')
                {
                    if (this.Peek() == '{')
                    {
                        this.Read(); // skip escaped brace
                        continue;
                    }
                    this.StepBack();
                    return true;
                }

                if (c == '}')
                {
                    if (this.Peek() == '}')
                    {
                        this.Read(); // skip escaped brace
                        continue;
                    }
                    throw new InvalidOperationException("Encountered un-escaped close tag '}' within template content");
                }
            }
            return false;
        }

        private void MunchTag(StringBuilder tag, bool verbatim)
        {
            char designator = this.PeekTagDesignator();
            switch (designator)
            {
                case '#':
                    this.MunchBlockTag(tag, verbatim);
                    break;
                case '/':
                    this.MunchEndBlockTag(tag, verbatim);
                    break;
                case '?':
                    this.MunchVariableTag(tag, verbatim);
                    break;
                case '>':
                    this.MunchParialTag(tag, verbatim);
                    break;
                case '@':
                    this.MunchDebugTag(tag, verbatim);
                    break;
                case '!':
                    this.MunchCommentTag(tag);
                    break;
                default:
                    this.MunchSimpleTag(tag, verbatim);
                    break;
            }
        }

        private void MunchTagDefault(StringBuilder tag, bool verbatim, out bool closed)
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

        private void MunchBlockTag(StringBuilder tag, bool verbatim)
        {
            this.MunchTagDefault(tag, verbatim, out bool closed);
            if (!closed)
                throw new InvalidOperationException($"Enountered un-closed {Tag.ResolveType(tag)} tag...'}}' never found.");
        }

        private void MunchEndBlockTag(StringBuilder tag, bool verbatim)
        {
            this.MunchTagDefault(tag, verbatim, out bool closed);
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

        private void MunchVariableTag(StringBuilder tag, bool verbatim)
        {
            this.MunchTagDefault(tag, verbatim, out bool closed);
            if (!closed)
            {
                TagType t = Tag.ResolveType(tag);
                string desc = t == TagType.VarAssign || t == TagType.VarDeclare ? t.ToString() : "Variable";
                throw new InvalidOperationException($"Enountered un-closed {desc}...'}}' never found.");
            }
        }

        private void MunchParialTag(StringBuilder tag, bool verbatim)
        {
            this.MunchTagDefault(tag, verbatim, out bool closed);
            if (!closed)
                throw new InvalidOperationException($"Enountered un-closed {TagType.Partial} tag...'}}' never found.");
        }

        private void MunchDebugTag(StringBuilder tag, bool verbatim)
        {
            this.MunchTagDefault(tag, verbatim, out bool closed);
            if (!closed)
                throw new InvalidOperationException($"Enountered un-closed {TagType.Debug} tag...'}}' never found.");
        }

        private void MunchSimpleTag(StringBuilder tag, bool verbatim)
        {
            this.MunchTagDefault(tag, verbatim, out bool closed);
            if (!closed)
                throw new InvalidOperationException($"Enountered un-closed {TagType.Simple} tag...'}}' never found.");
        }

        private void MunchCommentTag(StringBuilder tag)
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

        private int MunchBlockContent(TagType beginType, out Tag endTag)
        {
            char eot = (char)3;
            int offset = 1;
            var tag = new StringBuilder(60);
            TagType endType = Tag.ResolveEndTagType(beginType);
            endTag = null;
            int blockEnd = _index;

            while (this.Peek() != eot)
            {
                if (this.MunchRawContentSkip())
                {
                    blockEnd = _index; // _index is at '{' of the discovered tag
                    this.MunchTag(tag, true);

                    TagType type = Tag.ResolveType(tag);

                    if (type == beginType)
                        offset += 1;
                    else if (type == endType)
                        offset -= 1;

                    if (offset == 0)
                    {
                        endTag = new Tag(tag, _trimWhitespace);
                        break;
                    }

                    tag.Clear();
                }
            }

            return blockEnd;
        }
        #endregion

        #region trim block end
        private int TrimBlockEnd(int blockEnd)
        {
            ReadOnlySpan<char> span = _template.Span;
            int idx = blockEnd - 1;
            while (idx >= 0 && (span[idx] == ' ' || span[idx] == '\t'))
                idx--;
            return idx + 1;
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
                char lastChar;
                while ((lastChar = this.Peek()) == ' ' || lastChar == '\t')
                    _ = this.Read();

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
