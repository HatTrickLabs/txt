using System;

namespace HatTrick.Text.Templating
{
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
                throw new ArgumentException("Arg must contain a value", nameof(name));

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

        #region try update
        public bool TryUpdate(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name == string.Empty)
                throw new ArgumentException("Arg must contain a value", nameof(name));

            return _stack == null ? false : _stack.TryUpdate(name, value);
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

        #region clear
        public void Clear()
        {
            _stack = null;
        }
        #endregion
    }
}
