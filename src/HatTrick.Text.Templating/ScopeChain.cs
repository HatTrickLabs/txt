using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Text.Templating
{
    public class ScopeChain
    {
        #region internals
        private ScopeLink _links;
        private int _depth;
		#endregion

		#region interface
		public int Depth => _depth;
		#endregion

		#region constructors
		public ScopeChain()
        {
            _depth = 0;
        }
        #endregion

        #region push
        public void Push(object item)
        {
            ScopeLink link;
            link = (_links == null) ? new ScopeLink(item) : new ScopeLink(item, _links);
            _depth += 1;
            _links = link;
        }
        #endregion

        #region pop
        public object Pop()
        {
            if (_links == null)
                throw new MergeException("cannot 'Pop' scope link, the chain is empty.");

            object item = _links.Item;
            _links = _links.Parent;
            _depth -= 1;
            return item;
        }
        #endregion

        #region peek
        public object Peek()
        {
            if (_links == null)
                throw new MergeException("cannot 'Peek' scope link, the chain is empty.");

            return _links.Item;
        }

        public object Peek(int back)
        {
            if (back >= _depth)
                throw new ArgumentException("value must be < ScopeChain.Depth", nameof(back));

            if (back < 0)
                throw new ArgumentException("value cannot be a negative number", nameof(back));

            if (_links == null)
                throw new MergeException("cannot 'Peek' scope link, the chain is empty.");

            return _links.Peek(back);
        }
        #endregion

        #region set variable
        public void SetVariable(string name, object value)
        {
            _links.SetVariable(name, value);
        }
        #endregion

        #region access variable
        public object AccessVariable(string name)
        {
            if (_links == null)
                throw new MergeException($"Attempted access of unknown variable: {name}");

            return _links.AccessVariable(name);
        }
        #endregion

        #region apply variable scope marker
        public void ApplyVariableScopeMarker()
        {
            if (_depth == 0)
                return;

            _links.ApplyVariableScopeMarker();
        }
        #endregion

        #region dereference variable scope
        public void DereferenceVariableScope()
        {
            _links.DereferenceVariableScope();
        }
		#endregion

		#region clear
		public void Clear()
        {
            _links = null;
        }
		#endregion
	}

	public class ScopeLink
    {
        #region internals
        private ScopeLink _children;
        private object _item;
        private VariableBag _variables;
        #endregion

        #region interface
        public object Item => _item;

        public ScopeLink Parent => _children;

        public int VariableCount => _variables.Count;
        #endregion

        #region constructors
        public ScopeLink(object item) : this(item, null)
        {
        }

        public ScopeLink(object item, ScopeLink parent)
        {
            _item = item;
            _children = parent;
            _variables = new VariableBag();
        }
        #endregion

        #region set variable
        public void SetVariable(string name, object value)
        {
            _variables.Add(name, value);
        }
        #endregion

        #region access variable
        public object AccessVariable(string name)
        {
            if (_variables.TryGet(name, out object value))
                return value;

            if (_children != null)
            {
                return _children.AccessVariable(name);
            }

            throw new MergeException($"Attempted access of unknown variable: {name}");
        }
        #endregion

        #region apply variable scope marker
        public void ApplyVariableScopeMarker()
        {
            _variables.ApplyScopeMarker();
        }
        #endregion

        #region dereference variable scope
        public void DereferenceVariableScope()
        {
            _variables.DereferenceScope();
        }
        #endregion

        #region peek
        public object Peek(int back)
        {
            if (back < 0)
                throw new ArgumentException("value cannot be a negative number", nameof(back));

            if (back == 0)
                return _item;

            return _children.Peek(--back);
        }
        #endregion
    }

    public class VariableBag
    {
        #region internals
		private VariableStack _stack;
        private int _depth;
        #endregion

        #region interface
        public int Count => _depth;
		#endregion

		#region constructors
		public VariableBag()
        {
            _depth = 0;
        }
        #endregion

        #region apply scope marker
        public void ApplyScopeMarker()
        {
            if (_stack == null)
                return;

            _stack.ApplyScopeMarker();
        }
        #endregion

        #region dereference scope
        public void DereferenceScope()
        {
            while (_stack != null && !_stack.IsMarked)
            {
                _stack = _stack.SubStack;
            }
            if (_stack != null)
                _stack.RemoveScopeMarker();
        }
        #endregion

        #region add
        public void Add(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name == string.Empty)
                throw new ArgumentException("arg must contain a value", nameof(name));

            if (_stack == null)
            {
                _stack = new VariableStack(name, value);
            }
            else
            {
                var vs = new VariableStack(name, value, _stack);
                _stack = vs;
            }
            _depth += 1;
        }
        #endregion

		#region try get
		public bool TryGet(string name, out object value)
        {
            value = null;
            if (name == null)
                return false;
            if (name == string.Empty)
                return false;

            return (_stack == null) ? false : _stack.TryGet(name, out value);
        }
        #endregion

        //#region peek
        //public void Dereference(int count)
        //{
        //    if (count >= _depth)
        //        throw new ArgumentException("value must be < VariableBag.Depth", nameof(count));

        //    if (count < 0)
        //        throw new ArgumentException("value cannot be a negative number", nameof(count));

        //    if (_stack == null)
        //        throw new MergeException("cannot 'Dereference' variable stack, the stack is empty.");

        //    _stack = _stack.Peek(count);
        //}
        //#endregion

        #region clear
        public void Clear()
        {
            _stack = null;
        }
		#endregion
	}

    public class VariableStack
    {
        #region internals
        private string _name;
        private object _value;
        private bool _isMarked;
        private VariableStack _subStack;
        #endregion

        #region interface
        public string Name => _name;
        public object Value => _value;
        public bool IsMarked => _isMarked;
        public VariableStack SubStack => _subStack;
        #endregion

        #region constructors
        public VariableStack(string name, object value) : this(name, value, null)
        {
        }

        public VariableStack(string name, object value, VariableStack subStack)
        {
            _name = name;
            _value = value;
            _subStack = subStack;
        }
        #endregion

        #region apply scope marker
        public void ApplyScopeMarker()
        {
            _isMarked = true;
        }
        #endregion

        #region apply scope marker
        public void RemoveScopeMarker()
        {
            _isMarked = false;
        }
        #endregion

        //#region peek
        //public VariableStack Peek(int back)
        //{
        //    if (back < 0)
        //        throw new ArgumentException("value cannot be a negative number", nameof(back));

        //    if (back == 0)
        //        return this;

        //    return _subStack.Peek(--back);
        //}
        //#endregion

        #region try get
        public bool TryGet(string name, out object value)
        {
            value = null;
            bool found = false;
            if (!string.IsNullOrEmpty(name))
            {
                if (name == _name)
                {
                    value = _value;
                    found = true;
                }
                else if (_subStack != null)
                {
                    found = _subStack.TryGet(name, out value);
                }
            }
            return found;
        }
		#endregion
	}
}
