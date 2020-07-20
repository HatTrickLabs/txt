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
        #endregion

        #region constructors
        public ScopeChain()
        {
        }
        #endregion

        #region push
        public void Push(object item)
        {
            ScopeLink link;
            link = (_links == null) ? new ScopeLink(item) : new ScopeLink(item, _links);
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
            if (back < 0)
                throw new ArgumentException("value must be a positive number", nameof(back));

            if (_links == null)
                throw new MergeException("cannot 'Peek' scope link, the stack is empty.");

            return _links.Peek(--back);
        }
        #endregion

        #region set variable
        public void SetVariable(string name, object value)
        {
            _links.SetVariable(name, value);
        }
        #endregion

        #region get variable
        public object AccessVariable(string name)
        {
            return _links.AccessVariable(name);
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

        #region get variable
        public object AccessVariable(string name)
        {
            if (_variables.TryGet(name, out object value))
                return value;

            if (_children != null)
            {
                return _children.AccessVariable(name);
            }

            throw new MergeException($"Attemted access of unknown variable: {name}");
        }
        #endregion

        #region peek
        public object Peek(int back)
        {
            if (back < 0)
                throw new ArgumentException("value must be a positive number", nameof(back));

            if (back == 0)
                return _item;

            return _children.Peek(--back);
        }
        #endregion
    }

    public class VariableBag
    {
        #region internals
        bool _isSet;
        private string _name;
        private object _value;
		private VariableBag _next;
		#endregion

		#region interface
		public string Name => _name;
        public object Value => _value;
		#endregion

		#region constructors
		public VariableBag()
        {
            _isSet = false;
        }
		#endregion

		#region add
		public void Add(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name == string.Empty)
                throw new ArgumentException("arg must contain a value", nameof(name));

            if (_isSet)
            {
                if (_next == null)
                {
                    _next = new VariableBag();
                }
                _next.Add(name, value);
            }
            else
            {
                _name = name;
                _value = value;
                _isSet = true;
            }
        }
		#endregion

		#region get
		public bool TryGet(string name, out object value)
        {
            value = null;
            if (name == null)
                return false;
            if (name == string.Empty)
                return false;

            if (name == _name)
            {
                value = _value;
                return true;
            }

            return (_next == null) ? false : _next.TryGet(name, out value);
        }
		#endregion
	}
}
