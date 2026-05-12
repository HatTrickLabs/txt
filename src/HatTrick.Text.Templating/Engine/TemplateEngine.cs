using System;
using System.Buffers;
using System.Text;

namespace HatTrick.Text.Templating
{
    public class TemplateEngine
    {
        #region constants
        public const int MaxStack = 64;
        private static readonly SearchValues<char> RawBlockDelimiters;
        #endregion

        #region internals
        private int _index;
        private int _lineNum;
        private int _columnNum;
        private ReadOnlyMemory<char> _template;
        private ScopeChain _scopeChain;
        private LambdaRepository _lambdaRepo;
        private StringBuilder _result;
        private char[] _tagBuffer;
        private int _tagLen;
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
        static TemplateEngine()
        {
            RawBlockDelimiters = SearchValues.Create("{}\n\r");
        }

        public TemplateEngine(string template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            _template = template.AsMemory();
            _maxStack = TemplateEngine.MaxStack;
            _index = 0;
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
        }
        #endregion

        #region merge
        public string Merge(object bindTo)
        {
            _result.Clear();
            _scopeChain.Push(bindTo);
            this.MergeInto(_result);
            _scopeChain.Pop();
            return _result.ToString();
        }

        private void MergeInto(StringBuilder output, object bindTo)
        {
            _scopeChain.Push(bindTo);
            this.MergeInto(output);
            _scopeChain.Pop();
        }

        private void MergeInto(StringBuilder output)
        {
            _tagLen = 0;
            _index = 0;
            _lineNum = 1;
            _columnNum = 1;
            _result = output;

            bool ownsBuffer = _tagBuffer is null;
            if (ownsBuffer)
                _tagBuffer = ArrayPool<char>.Shared.Rent(64);
            try
            {
                this.Scan();
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
            finally
            {
                if (ownsBuffer)
                {
                    ArrayPool<char>.Shared.Return(_tagBuffer);
                    _tagBuffer = null;
                }
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
                    this.MunchTag(false);
                    this.HandleTag(new Tag(new ReadOnlyMemory<char>(_tagBuffer, 0, _tagLen), _trimWhitespace));
                    _tagLen = 0;
                }
            }
        }
        #endregion

        #region resolve exception context
        private MergeExceptionContext ResolveExceptionContext()
        {
            string lastTag = _tagLen > 0 ? new string(_tagBuffer, 0, _tagLen) : null;
            return new MergeExceptionContext(_lineNum, _columnNum, _index, lastTag);
        }
        #endregion

        #region handle tag
        private void HandleTag(in Tag tag)
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
        private void HandleCommentTag(in Tag tag)
        {
            this.EnsureLeftTrim(_result, tag);
            this.EnsureRightTrim(tag);
        }
        #endregion

        #region handle simple tag
        private void HandleSimpleTag(in Tag tag)
        {
            ReadOnlySpan<char> bindAs = tag.BindAs();
            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            _result.Append(target);
        }
        #endregion

        #region handle if tag
        private void HandleIfTag(in Tag tag)
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
                var subEngine = new TemplateEngine(_template.Slice(blockStart, blockEnd - blockStart), _scopeChain, _lambdaRepo, (_maxStack - 1), _trimWhitespace);
                subEngine.MergeInto(_result);
                _scopeChain.DereferenceVariableScope();
            }

            this.EnsureRightTrim(endTag);
        }
        #endregion

        #region handle each tag
        private void HandleEachTag(in Tag tag)
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
                subEngine._tagBuffer = ArrayPool<char>.Shared.Rent(64);
                try
                {
                    foreach (var item in items)
                    {
                        _scopeChain.ApplyVariableScopeMarker();
                        subEngine.MergeInto(_result, item);
                        _scopeChain.DereferenceVariableScope();
                    }
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(subEngine._tagBuffer);
                    subEngine._tagBuffer = null;
                }
            }

            this.EnsureRightTrim(endTag);
        }
        #endregion

        #region handle with tag
        private void HandleWithTag(in Tag tag)
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
            var subEngine = new TemplateEngine(_template.Slice(blockStart, blockEnd - blockStart), _scopeChain, _lambdaRepo, (_maxStack - 1), _trimWhitespace);
            subEngine.MergeInto(_result, target);
            _scopeChain.DereferenceVariableScope();

            this.EnsureRightTrim(endTag);
        }
        #endregion

        #region handle variable tag
        private void HandleVariableTag(in Tag tag, bool isDeclaration)
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
        private void HandleVariableDeclareTag(in Tag tag)
        {
            this.HandleVariableTag(tag, true);
        }
        #endregion

        #region handle variable assign tag
        private void HandleVariableAssignTag(in Tag tag)
        {
            this.HandleVariableTag(tag, false);
        }
        #endregion

        #region handle partial tag (sub templates)
        private void HandlePartialTag(in Tag tag)
        {
            this.EnsureLeftTrim(_result, tag);

            ReadOnlySpan<char> bindAs = tag.BindAs();
            object target = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

            string template = (target as string) ?? throw new InvalidOperationException($"Sub template tag: {tag} reflected value is not typeof string: {target}");

            _scopeChain.ApplyVariableScopeMarker();
            var subEngine = new TemplateEngine(template.AsMemory(), _scopeChain, _lambdaRepo, (_maxStack - 1), _trimWhitespace);
            subEngine.MergeInto(_result);
            _scopeChain.DereferenceVariableScope();

            this.EnsureRightTrim(tag);
        }
        #endregion

        #region handle debug tag
        private void HandleDebugTag(in Tag tag)
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
                output = BindHelper.ResolveBindTarget(bindAs, _lambdaRepo, _scopeChain);

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
            ReadOnlySpan<char> span = _template.Span;
            char eot = (char)3;
            int chunkStart = _index;
            char c;

            while ((c = this.Read()) != eot)
            {
                if (c == '{' || c == '}')
                {
                    if (this.Peek() == c)//is escaped
                    {//this is part of the self inflicted pain we deal with for my bad decision to make acual tags single brackets
                     //and force users to escape actual template contant brackets. In hindsight, everything OUTSIDE a tag should be 
                     //copied verbatime (less white space) into the output. Requiring the removal of the escape char causes us pain.
                        output.Append(span.Slice(chunkStart, (_index - 1) - chunkStart));
                        this.Read(); // consume escape, second '{' or '}'
                        output.Append(c);
                        chunkStart = _index;
                    }
                    else if (c == '{')//is open tag, step back 1 char and return so tag can be munched in whole
                    {
                        output.Append(span.Slice(chunkStart, (_index - 1) - chunkStart));
                        this.StepBack();
                        return true;
                    }
                    else//found a close tag that isn't escaped an is NOT closing an actual tag, template error...
                    {
                        throw new InvalidOperationException("Encountered un-escaped close tag '}' within template content");
                    }
                }
            }

            output.Append(span.Slice(chunkStart, _index - chunkStart));
            return false;
        }

        private bool MunchRawBlockContent()
        {
            ReadOnlySpan<char> span = _template.Span;

            while (_index < span.Length)
            {
                int next = span.Slice(_index).IndexOfAny(RawBlockDelimiters);
                if (next < 0)
                {
                    _columnNum += span.Length - _index;
                    _index = span.Length;
                    return false;
                }

                _columnNum += next;
                _index += next;

                char c = span[_index++];

                if (c == '\n') 
                { 
                    _lineNum++; 
                    _columnNum = 1; 
                    continue; 
                }

                if (c == '\r') 
                    continue;

                _columnNum++;

                if (c == '{')
                {
                    if (_index < span.Length && span[_index] == '{')
                    {
                        _index++; _columnNum++;
                        continue;
                    }
                    _index--; 
                    _columnNum--;
                    return true;
                }

                if (_index < span.Length && span[_index] == '}')
                {
                    _index++; 
                    _columnNum++;
                    continue;
                }
                throw new InvalidOperationException("Encountered un-escaped close tag '}' within template content");
            }
            return false;
        }

        private void MunchTag(bool verbatim)
        {
            char designator = this.PeekTagDesignator();
            switch (designator)
            {
                case '#':
                    this.MunchBlockTag(verbatim);
                    break;
                case '/':
                    this.MunchEndBlockTag(verbatim);
                    break;
                case '?':
                    this.MunchVariableTag(verbatim);
                    break;
                case '>':
                    this.MunchParialTag(verbatim);
                    break;
                case '@':
                    this.MunchDebugTag(verbatim);
                    break;
                case '!':
                    this.MunchCommentTag();
                    break;
                default:
                    this.MunchSimpleTag(verbatim);
                    break;
            }
        }

        private void MunchTagDefault(bool verbatim, out bool closed)
        {
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            const char escape = '\\';
            const char singleQuote = '\'';
            const char doubleQuote = '"';
            const char tab = '\t';
            const char space = ' ';
            const char nl = '\n';
            const char cr = '\r';
            const char eot = (char)3;
            char previous = '\0';
            char c = '\0';

            bool inQuotes = false;
            while ((c = this.Read()) != eot)
            {
                if (c == doubleQuote && previous != escape && !inSingleQuote)
                    inDoubleQuote = !inDoubleQuote;

                if (c == singleQuote && previous != escape && !inDoubleQuote)
                    inSingleQuote = !inSingleQuote;

                inQuotes = (inDoubleQuote || inSingleQuote);
                bool isWhiteSpace = c == space || c == tab || c == nl || c == cr;

                if (!isWhiteSpace || verbatim || inQuotes)
                {
                    if (_tagLen == _tagBuffer.Length) 
                        this.GrowTagBuffer();

                    _tagBuffer[_tagLen++] = c;
                }

                if (c == '}' && !inQuotes)
                {
                    closed = true;
                    return;
                }

                previous = c;
            }
            closed = false;
        }

        private void MunchBlockTag()
        {
            if (this.PeekTagDesignator() == '!')
            {
                // Comment tags use brace-offset counting; quotes have no special meaning inside them
                int offset = 0;
                char prev = '\0';
                char c;
                const char eot = (char)3;
                while ((c = this.Read()) != eot)
                {
                    offset += c == '{' ? 1 : (c == '}' && prev != '\\') ? -1 : 0;
                    if (c == '}' && offset == 0 && prev != '\\')
                        return;

                    prev = c;
                }
                return;
            }

            // All other tags: scan to closing } respecting string literals
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            char previous = '\0';
            char ch;
            const char eot2 = (char)3;
            while ((ch = this.Read()) != eot2)
            {
                if (ch == '"' && previous != '\\' && !inSingleQuote)
                    inDoubleQuote = !inDoubleQuote;

                if (ch == '\'' && previous != '\\' && !inDoubleQuote)
                    inSingleQuote = !inSingleQuote;

                if (ch == '}' && !(inDoubleQuote || inSingleQuote))
                    return;

                previous = ch;
            }
        }

        private void MunchBlockTag(bool verbatim)
        {
            this.MunchTagDefault(verbatim, out bool closed);
            if (!closed)
                throw new InvalidOperationException($"Enountered un-closed {Tag.ResolveType(new ReadOnlySpan<char>(_tagBuffer, 0, _tagLen))} tag...'}}' never found.");
        }

        private void MunchEndBlockTag(bool verbatim)
        {
            this.MunchTagDefault(verbatim, out bool closed);
            if (!closed)
            {
                TagType type = TagType.Unknown;
                if (_tagLen > 0)
                {
                    TagType t = Tag.ResolveType(new ReadOnlySpan<char>(_tagBuffer, 0, _tagLen));
                    if (Tag.IsBlockTag(t, out BlockTagOrientation orientation) && orientation == BlockTagOrientation.Begin)
                        type = Tag.ResolveEndTagType(t);
                }
                string desc = type == TagType.Unknown ? "end block" : type.ToString();
                throw new InvalidOperationException($"Enountered un-closed {desc} tag...'}}' never found.");
            }
        }

        private void MunchVariableTag(bool verbatim)
        {
            this.MunchTagDefault(verbatim, out bool closed);
            if (!closed)
            {
                TagType t = Tag.ResolveType(new ReadOnlySpan<char>(_tagBuffer, 0, _tagLen));
                string desc = t == TagType.VarAssign || t == TagType.VarDeclare ? t.ToString() : "Variable";
                throw new InvalidOperationException($"Enountered un-closed {desc}...'}}' never found.");
            }
        }

        private void MunchParialTag(bool verbatim)
        {
            this.MunchTagDefault(verbatim, out bool closed);
            if (!closed)
                throw new InvalidOperationException($"Enountered un-closed {TagType.Partial} tag...'}}' never found.");
        }

        private void MunchDebugTag(bool verbatim)
        {
            this.MunchTagDefault(verbatim, out bool closed);
            if (!closed)
                throw new InvalidOperationException($"Enountered un-closed {TagType.Debug} tag...'}}' never found.");
        }

        private void MunchSimpleTag(bool verbatim)
        {
            this.MunchTagDefault(verbatim, out bool closed);
            if (!closed)
                throw new InvalidOperationException($"Enountered un-closed {TagType.Simple} tag...'}}' never found.");
        }

        private void MunchCommentTag()
        {
            char escape = '\\';
            char previous = '\0';
            char c = '\0';
            const char eot = (char)3;

            int offset = 0;
            while ((c = this.Read()) != eot)
            {
                offset += (c == '{') ? 1 : (c == '}' && previous != escape) ? -1 : 0;

                if (_tagLen == _tagBuffer.Length) 
                    this.GrowTagBuffer();

                _tagBuffer[_tagLen++] = c;

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
            const char eot = (char)3;
            int offset = 1;
            TagType endType = Tag.ResolveEndTagType(beginType);
            endTag = default;
            int blockEnd = _index;

            while (this.Peek() != eot)
            {
                if (this.MunchRawBlockContent())
                {
                    blockEnd = _index; // _index is at '{' of the discovered tag
                    this.MunchBlockTag();

                    ReadOnlySpan<char> tagSpan = _template.Span.Slice(blockEnd, _index - blockEnd);
                    TagType type = Tag.ResolveType(tagSpan);

                    if (type == beginType)
                        offset += 1;

                    else if (type == endType)
                        offset -= 1;

                    if (offset == 0)
                    {
                        endTag = new Tag(_template.Slice(blockEnd, _index - blockEnd), _trimWhitespace);
                        break;
                    }
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
            {
                idx--;
            }
            return idx + 1;
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

        #region grow tag buffer
        private void GrowTagBuffer()
        {
            char[] current = _tagBuffer;
            _tagBuffer = ArrayPool<char>.Shared.Rent(current.Length * 2);
            current.AsSpan(0, _tagLen).CopyTo(_tagBuffer);
            ArrayPool<char>.Shared.Return(current);
        }
        #endregion
    }
}
