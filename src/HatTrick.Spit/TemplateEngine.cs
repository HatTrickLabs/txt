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

        #region merge
        public string Merge(object bindTo) //TODO, JRod, allow stream output...
        {
            _mergeResult.Clear();
            _index = 0;

            char eot = (char)3; //end of text....
            while (this.Peek() != eot)
            {
                this.RollForwardAndKeepTill('{');

                if (!this.EndOfTemplate)
                {
                    //TODO: JRod, optimize... eliminate the peek(int count) func..
                    //--->: we need to be able to handle dirty tags (spaces) i.e. {# if or { #if or {# each etc...

                    if (this.Peek(4) == "{#if") //# if logic tag (boolean switch)
                    {
                        this.HandleIfTag(bindTo);
                    }
                    else if (this.Peek(6) == "{#each") //#each enumeration
                    {
                        this.HandleEachTag(bindTo);
                    }
                    else if (this.Peek(2) == "{>") //sub template tag
                    {
                        this.HandleSubTemplateTag(bindTo);
                    }
                    else if (this.Peek(2) == "{!") //comment tag
                    {
                        this.HandleCommentTag();
                    }
                    else //basic token
                    {
                        this.HandleBasicTag(bindTo);
                    }
                }
            }

            return _mergeResult.ToString();
        }
        #endregion

        #region handle comment tag
        private void HandleCommentTag()
        {
            //bypass and exclude from output
            this.RollForwardAndDiscardTill('}');
            string endTag = new string(new char[] { _template[_index - 1], _template[_index] });
            _index += 1;
            this.EnsureNewLineSuppression(endTag);
        }
        #endregion

        #region handle basic tag
        private void HandleBasicTag(object bindTo)
        {
            string token = string.Empty;

            Action<char> emitTo = (s) => 
            {
                if (s != ' ' && s != '{' && s != '}')
                { token += s; }
            };

            this.EmitCharToActionTill(emitTo, '}', true);

            object target = this.ResolveTarget(token, bindTo);

            _mergeResult.Append(target ?? string.Empty);
        }
        #endregion

        #region handle if tag
        private void HandleIfTag(object bindTo)
        {
            StringBuilder openTagBuilder = new StringBuilder();
            StringBuilder enclosedContentBuilder = new StringBuilder();
            StringBuilder closeTagBuilder = new StringBuilder();

            Action<char> emitOpenTo = (s) => 
            {
                if (s != ' ')
                { openTagBuilder.Append(s); }
            };
            Action<char> emitEnclosedTo = (s) => { enclosedContentBuilder.Append(s); };
            Action<char> emitCloseTo = (s) => { closeTagBuilder.Append(s); };

            //roll throug the open each tag and emit to open builder
            this.EmitCharToActionTill(emitOpenTo, '}', true);
            string openTag = openTagBuilder.ToString();
            this.EnsureNewLineSuppression(openTag);

            //roll and emit intil proper #/if tag found (allowing nested #if #/if tags
            this.EnsuredSubPatternCountEmitCharToActionTill(emitEnclosedTo, "{#/if", "{#if", false);

            //roll through the close tag and emit to close builder
            this.EmitCharToActionTill(emitCloseTo, '}', true);
            string closeTag = closeTagBuilder.ToString();
            this.EnsureNewLineSuppression(closeTag);

            bool isNegated = false;
            for (int i = 0; i < openTag.Length; i++)
            {
                //TODO: JRod, optimize... no need to iterate the entire tag contents... ! should be before token
                if (openTag[i] == '!')
                {
                    isNegated = true;
                    break;
                }
            }

            string[] bindAs = openTag.Split(new string[] { "{", "#", "if", "!", "-", "}" }, StringSplitOptions.RemoveEmptyEntries);

            object target = this.ResolveTarget(bindAs[0], bindTo);

            bool render = (target != null);

            if (render && target is bool)
            { render = ((bool)target == true); }

            if (isNegated)
            { render = !render; }

            if (render)
            {
                TemplateEngine subEngine = new TemplateEngine(enclosedContentBuilder.ToString())
                    .WithScopeChain(_scopeChain)
                    .WithLambdaRepository(_lambdaRepo);

                subEngine.SuppressNewline = this.SuppressNewline;
                _mergeResult.Append(subEngine.Merge(bindTo));
            }
        }
        #endregion

        #region handle each tag
        private void HandleEachTag(object bindTo)
        {
            StringBuilder openTagBuilder = new StringBuilder();
            StringBuilder enclosedContentBuilder = new StringBuilder();
            StringBuilder closeTagBuilder = new StringBuilder();

            Action<char> emitOpenTo = (s) => 
            {
                if (s != ' ')
                { openTagBuilder.Append(s); }
            };
            Action<char> emitEnclosedTo = (s) => { enclosedContentBuilder.Append(s); };
            Action<char> emitCloseTo = (s) => { closeTagBuilder.Append(s); };

            //roll through the open each tag and emit to open builder
            this.EmitCharToActionTill(emitOpenTo, '}', true);
            string openTag = openTagBuilder.ToString();
            this.EnsureNewLineSuppression(openTag);

            //roll and emit intil proper #/each tag found (allowing nested #each #/each tags
            this.EnsuredSubPatternCountEmitCharToActionTill(emitEnclosedTo, "{#/each", "{#each", false);

            //roll through the close tag and emit to close builder
            this.EmitCharToActionTill(emitCloseTo, '}', true);
            string closeTag = closeTagBuilder.ToString();
            this.EnsureNewLineSuppression(closeTag);

            //open tag {#each Person.Details.Addresses}
            string[] bindAs = openTagBuilder.ToString().Split(new string[] { "{", "#", "each", "-", "}" }, StringSplitOptions.RemoveEmptyEntries);

            object target = this.ResolveTarget(bindAs[0], bindTo);

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

        #region handle sub template tag
        private void HandleSubTemplateTag(object bindTo)
        {
            string token = string.Empty;

            Action<char> emitTo = (s) => 
            {
                if (s != ' ' && s != '>' && s != '{' && s != '}')
                { token += s; }
            };

            this.EmitCharToActionTill(emitTo, '}', true);

            object target = this.ResolveTarget(token, bindTo);
            //object target = ReflectionHelper.Expression.ReflectItem(bindTo, token);

            if (!(target is string))
            { throw new MergeException($"#sub template tag / token reflected value is not typeof string: {target}"); }

            TemplateEngine subEngine = new TemplateEngine(target as string)
                .WithScopeChain(_scopeChain)
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

        #region ensured sub pattern count emit char to action till
        private void EnsuredSubPatternCountEmitCharToActionTill(Action<char> emitTo, string till, string ensuring, bool greedy)
        {
            StringBuilder content = new StringBuilder();

            emitTo += (s) => 
            {
                content.Append(s);
            };

            int subOpenCount;
            int subCloseCount;
            //Need to account for the enclosed content having it's own #each tag... roll forward until #each count == #/each count...
            do
            {
                //roll through enclosed content and emit to local enclosed content appender action
                this.EmitCharToActionTill(emitTo, till, greedy);

                string template = content.ToString();

                subOpenCount = this.CountInstanceOfPattern(template, ensuring);
                subCloseCount = this.CountInstanceOfPattern(template, till);

                if (subOpenCount != subCloseCount)
                {
                    emitTo(this.Peek());
                    _index += 1;
                }

            } while (subOpenCount != subCloseCount);
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
        private void EnsureNewLineSuppression(string tag)
        {
            if (this.SuppressNewline || tag.EndsWith("-}"))
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