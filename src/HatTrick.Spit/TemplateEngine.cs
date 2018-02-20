using System;
using System.Collections.Generic;
using System.Text;
using HatTrick.Spit.Reflection;

namespace HatTrick.Spit
{
    public class TemplateEngine
    {
        #region internals
        private int _index;
        private string _template;
        private ScopeChain _scopeChain;
        private LambdaRepository _lambdaRepo;
        private StringBuilder _mergeResult;
        private int _allotedStackDepth = 15;
        #endregion

        #region interface
        public bool EndOfTemplate
        { get { return (_index == _template.Length); } }

        public bool SuppressNewline
        { get; set; }

        public LambdaRepository LambdaRepo
        { get { return (_lambdaRepo == null) ? _lambdaRepo = new LambdaRepository() : _lambdaRepo; } }
        #endregion

        #region constructors
        public TemplateEngine(string template) //TODO: JRod, allow stream as template...
        {
            _index = 0;
            _template = template;
            _mergeResult = new StringBuilder((int)(template.Length * 1.5));
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

        #region with alloted stack depth
        private TemplateEngine WithAllotedStackDepth(int depth)
        {
            if (depth < 0)
            {
                throw new MergeException("stack depth overflow.  partial (sub template) stack depth cannot exceed 15");
            }
            _allotedStackDepth = depth;
            return this;
        }
        #endregion

        #region merge
        public string Merge(object bindTo) //TODO, JRod, allow stream output...
        {
            _mergeResult.Clear();
            _index = 0;

            char eot = (char)3; //end of text....
            StringBuilder tokenBuilder = new StringBuilder(60);
            while (this.Peek() != eot)
            {
                this.RollForwardAndKeepTill('{');

                Action<char> captureTag = (c) =>
                {
                    if (c != ' ')
                    { tokenBuilder.Append(c); }
                };
                this.EmitCharToActionTill(captureTag, '}', true);

                if (!this.EndOfTemplate)
                {
                    string token = tokenBuilder.ToString();
                    if (this.IsIfTag(token)) //# if logic tag (boolean switch)
                    {
                        this.HandleIfTag(token, bindTo);
                    }
                    else if (this.IsEachTag(token)) //#each enumeration
                    {
                        this.HandleEachTag(token, bindTo);
                    }
                    else if (this.IsPartialTag(token)) //sub template tag
                    {
                        this.HandlePartialTag(token, bindTo);
                    }
                    else if (this.IsCommentTag(token)) //comment tag
                    {
                        this.HandleCommentToken(token);
                    }
                    else //basic token
                    {
                        this.HandleBasicToken(token, bindTo);
                    }
                }
                tokenBuilder.Clear();
            }

            return _mergeResult.ToString();
        }
        #endregion

        #region is if tag
        public bool IsIfTag(string tag)
        {
            return tag.StartsWith("{#if", StringComparison.CurrentCultureIgnoreCase);
        }
        #endregion

        #region is end if tag
        public bool IsEndIfTag(string tag)
        {
            return tag.StartsWith("{#/if", StringComparison.CurrentCultureIgnoreCase);
        }
        #endregion

        #region is each tag
        public bool IsEachTag(string tag)
        {
            return tag.StartsWith("{#each", StringComparison.CurrentCultureIgnoreCase);
        }
        #endregion

        #region is end each tag
        public bool IsEndEachTag(string tag)
        {
            return tag.StartsWith("{#/each", StringComparison.CurrentCultureIgnoreCase);
        }
        #endregion

        #region is comment tag
        public bool IsCommentTag(string tag)
        {
            return tag.StartsWith("{!", StringComparison.CurrentCultureIgnoreCase);
        }
        #endregion

        #region is partial tag
        public bool IsPartialTag(string tag)
        {
            return tag.StartsWith("{>", StringComparison.CurrentCultureIgnoreCase);
        }
        #endregion

        #region handle comment tag
        private void HandleCommentToken(string token)
        {
            //string endTag = new string(new char[] { _template[_index - 1], _template[_index] });
            //_index += 1;
            this.EnsureNewLineSuppression(token, out bool _);
        }
        #endregion

        #region handle basic tag
        private void HandleBasicToken(string token, object bindTo)
        {
            object target = this.ResolveTarget(token.Trim('{', '}'), bindTo);

            _mergeResult.Append(target ?? string.Empty);
        }
        #endregion

        #region handle if tag
        private void HandleIfTag(string token, object bindTo)
        {
            StringBuilder enclosedContentBuilder = new StringBuilder();

            Action<char> emitEnclosedTo = (s) => { enclosedContentBuilder.Append(s); };

            //roll and emit until proper #/if tag found (allowing nested #if #/if tags
            //this.EnsuredSubPatternCountEmitCharToActionTill(emitEnclosedTo, "{#/if", "{#if", false);
            string closeTag;
            this.EmitEnclosedContetToActionTill(emitEnclosedTo, this.IsEndIfTag, this.IsIfTag, out closeTag);
            this.EnsureNewLineSuppression(closeTag, out bool _);

            bool hasTrimMarker;
            this.EnsureNewLineSuppression(token, out hasTrimMarker);

            string bindAs = token.Substring(4, (hasTrimMarker) ? (token.Length - 6) : (token.Length - 5));
            bool isNegated = bindAs[0] == '!';

            object target = this.ResolveTarget(isNegated ? bindAs.Substring(1) : bindAs, bindTo);

            bool render = (target != null);

            if (render && target is bool)
            { render = ((bool)target == true); }

            if (isNegated)
            { render = !render; }

            if (render)
            {
                TemplateEngine subEngine = new TemplateEngine(enclosedContentBuilder.ToString())
                    .WithScopeChain(_scopeChain)
                    .WithAllotedStackDepth(_allotedStackDepth)
                    .WithLambdaRepository(_lambdaRepo);

                subEngine.SuppressNewline = this.SuppressNewline;
                _mergeResult.Append(subEngine.Merge(bindTo));
            }
        }
        #endregion

        #region handle each tag
        private void HandleEachTag(string token, object bindTo)
        {
            StringBuilder enclosedContentBuilder = new StringBuilder();

            Action<char> emitEnclosedTo = (s) => { enclosedContentBuilder.Append(s); };

            //roll and emit intil proper #/each tag found (allowing nested #each #/each tags
            string closeTag;
            this.EmitEnclosedContetToActionTill(emitEnclosedTo, this.IsEndEachTag, this.IsEachTag, out closeTag);
            this.EnsureNewLineSuppression(closeTag, out bool _);

            bool hasTrimMarker;
            this.EnsureNewLineSuppression(token, out hasTrimMarker);

            string bindAs = token.Substring(6, (hasTrimMarker) ? (token.Length - 8) : (token.Length - 7));

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
                subEngine = new TemplateEngine(enclosedContentBuilder.ToString())
                    .WithScopeChain(_scopeChain)
                    .WithAllotedStackDepth(_allotedStackDepth)
                    .WithLambdaRepository(_lambdaRepo);

                subEngine.SuppressNewline = this.SuppressNewline;
                foreach (var item in items)
                {
                    itemContent = subEngine.Merge(item);
                    _mergeResult.Append(itemContent);
                }
                _scopeChain.Pop();
            }
        }
        #endregion

        #region handle partial tag (sub templates)
        private void HandlePartialTag(string token, object bindTo)
        {
            //this.EnsureNewLineSuppression(token); //TODO: JRod, test this!!!!
            object target = this.ResolveTarget(token.Trim('{', '>', '}'), bindTo);

            if (!(target is string))
            { throw new MergeException($"#sub template tag / token reflected value is not typeof string: {target}"); }

            TemplateEngine subEngine = new TemplateEngine(target as string)
                .WithScopeChain(_scopeChain)
                .WithAllotedStackDepth(_allotedStackDepth - 1) //decrement 1 unit for sub template...
                .WithLambdaRepository(_lambdaRepo);

            subEngine.SuppressNewline = this.SuppressNewline;
            string result = subEngine.Merge(bindTo);

            _mergeResult.Append(result);
        }
        #endregion

        #region resolve target
        private object ResolveTarget(string token, object localScope)
        {
            object target = null;
            if (token == "$") //append bindto obj
            {
                target = localScope;
            }
            else if (token[0] == '$') //reflect from bindto object
            {
                string expression = token.Substring(2, token.Length - 2);//remove the $.
                target = ReflectionHelper.Expression.ReflectItem(localScope, expression);
            }
            else if (token[0] == '.')
            {
                int back = this.CountInstanceOfPattern(token, @"..\");
                target = this.ResolveTarget(token.Replace(@"..\", string.Empty), _scopeChain.Reach(back));

            }
            else if (token.Contains("=>")) //lambda expression
            {
                //{($.abc, $.xyz) => ConcatToValues}

                string[] leftRight = token.Split(new char[] { '=', '>' }, StringSplitOptions.RemoveEmptyEntries);

                string[] args = leftRight[0].Split(new char[] { '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);

                object[] parameters = new object[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    parameters[i] = this.ResolveTarget(args[i], localScope); //recursive
                }

                target = this.LambdaRepo.Invoke(leftRight[1], parameters);
            }
            else //reflect from root context (inside #each tag)
            {
                target = ReflectionHelper.Expression.ReflectItem(localScope, token);
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

        public string Peek(int length)
        {
            string peek = (_template.Length >= (_index + length))
                ? _template.Substring(_index, length) //TODO: JRod, refactor out the substring...
                : null;

            return peek;
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

        #region roll forward and keep till
        public void RollForwardAndKeepTill(char till)
        {
            this.RollForwardTill(till, true);
        }
        #endregion

        #region roll forward and discard till
        public void RollForwardAndDiscardTill(char till)
        {
            this.RollForwardTill(till, false);
        }
        #endregion

        #region roll forward till
        private void RollForwardTill(char till, bool keepChars)
        {
            char c;
            while (_index < _template.Length)
            {
                c = this.Peek();

                if (c == '{' || c == '}')
                {
                    char next = this.PeekAt(_index + 1);
                    if (c == next)
                    {
                        if (keepChars)
                        { _mergeResult.Append(c); }
                        _index += 2;
                        continue;
                    }
                }

                if (c == till)
                {
                    break;
                }
                else
                {
                    if (keepChars)
                    { _mergeResult.Append(c); }
                    _index += 1;
                }
            }
        }
        #endregion

        #region emit char to action till
        public void EmitCharToActionTill(Action<char> emitTo, char till, bool greedy)
        {
            char c;
            while (_index < _template.Length)
            {
                c = this.Peek();
                if (c == till)
                {
                    if (greedy)
                    {
                        _index += 1;
                        emitTo(c);
                    }
                    break;
                }
                else
                {
                    _index += 1;
                    emitTo(c);
                }
            }
        }

        public void EmitCharToActionTill(Action<char> emitTo, string till, bool greedy)
        {
            char c;
            while (_index < _template.Length)
            {
                string peek = this.Peek(till.Length);
                if (peek == till)
                {
                    if (greedy)
                    {
                        for (int i = 0; i < till.Length; i++)
                        {
                            c = _template[_index++];
                            emitTo(c);
                        }
                    }
                    break;
                }
                else
                {
                    c = _template[_index++];
                    emitTo(c);
                }
            }
        }
        #endregion

        #region emit enclosed content to action till
        private void EmitEnclosedContetToActionTill(Action<char> emitContentTo, Func<string, bool> till, Func<string, bool> ensuring, out string endTag)
        {
            endTag = null;
            int offset = 1;// we are inside 1 if and looking for its /if

            string tag = string.Empty;
            bool emitTagAsContent;
            do
            {
                //look for the next tag...
                this.EmitCharToActionTill(emitContentTo, '{', false);

                tag = string.Empty;
                emitTagAsContent = false;

                Action<char> emitTagTo = (c) =>
                {
                    if (c != ' ') { tag += c; }
                };

                this.EmitCharToActionTill(emitTagTo, '}', true);

                if (!till(tag))
                {
                    emitTagAsContent = true;
                    if (ensuring(tag))
                    {
                        offset += 1;
                    }
                }
                else
                {
                    offset -= 1;
                    if (offset > 0)
                    {
                        emitTagAsContent = true;
                    }
                    else
                    {
                        endTag = tag;
                    }
                }

                if (emitTagAsContent)
                {
                    for (int i = 0; i < tag.Length; i++)
                    {
                        emitContentTo(tag[i]);
                    }
                }

            } while (offset > 0);
        }
        #endregion

        #region count instances of pattern
        public int CountInstanceOfPattern(string content, string pattern)
        {
            int cnt = 0;
            int idx = 0;

            while ((idx = content.IndexOf(pattern, idx)) != -1)
            {
                idx += pattern.Length;
                cnt += 1;
            }

            return cnt;
        }
        #endregion

        #region ensure new line suppression
        private void EnsureNewLineSuppression(string tag, out bool hasTrimMarker)
        {
            //if global suppress newline or tag has right side trim marker..
            hasTrimMarker = false;
            if ((hasTrimMarker = tag[tag.Length - 2] == '-') || this.SuppressNewline)
            {
                int newLineLength = Environment.NewLine.Length;
                if (this.Peek(newLineLength) == Environment.NewLine)
                { _index += newLineLength; }
            }
        }
        #endregion

        #region scope chain
        public class ScopeChain
        {
            #region internals
            private List<object> _items;
            #endregion

            #region constructors
            public ScopeChain()
            {
                _items = new List<object>();
            }
            #endregion

            #region push
            public void Push(object item)
            {
                _items.Add(item);
            }
            #endregion

            #region pop
            public object Pop()
            {
                int lastIndex = _items.Count - 1;
                object item = _items[lastIndex];
                _items.RemoveAt(lastIndex);
                return item;
            }
            #endregion

            #region get
            public object Reach(int back)
            {
                int count = _items.Count;
                object item = _items[count - back];
                return item;
            }
            #endregion
        }
        #endregion

        #region lambda repository
        public class LambdaRepository
        {
            #region internals
            Dictionary<string, Delegate> _lambdas;
            #endregion

            #region constructors
            public LambdaRepository()
            {
                _lambdas = new Dictionary<string, Delegate>();
            }
            #endregion

            #region add
            public void Add(string name, Delegate lambda)
            {
                if (_lambdas.ContainsKey(name))
                {
                    throw new ArgumentException($"A lambda with the provided name: {name} has already been added");
                }
                _lambdas.Add(name, lambda);
            }
            #endregion

            #region remove
            public void Remove(string name)
            {
                if (!_lambdas.ContainsKey(name))
                {
                    throw new ArgumentException($"No lambda exists for the provided name: {name}");
                }
                _lambdas.Remove(name);
            }
            #endregion

            #region invoke
            public object Invoke(string name, params object[] parms)
            {
                if (!_lambdas.ContainsKey(name))
                { throw new MergeException($"Encountered lambda that does not exist in lambda repo: {name}"); }

                System.Reflection.MethodInfo mi = _lambdas[name].Method;

                int paramsCount = mi.GetParameters().Length;

                if (paramsCount != parms.Length)
                {
                    string msg = $"Attempted lambda invocation with invalid number of parameters.  Actual count: {paramsCount} Provided count: {parms.Length}";
                    throw new MergeException(msg);
                }

                return _lambdas[name].DynamicInvoke(parms);
            }
            #endregion
        }
        #endregion
    }
}