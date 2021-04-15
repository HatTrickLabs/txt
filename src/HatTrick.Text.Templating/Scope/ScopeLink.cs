using System;

namespace HatTrick.Text.Templating
{
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

		#region upate variable
		public void UpdateVariable(string name, object value)
		{
			if (_variables.TryUpdate(name, value))
				return;

			if (_children != null)
				_children.UpdateVariable(name, value);
			else
				throw new MergeException($"attempted variable re-assignment for undeclared variable: {name}");
		}
		#endregion

		#region access variable
		public object AccessVariable(string name)
		{
			if (_variables.TryGet(name, out object value))
				return value;

			if (_children != null)
				return _children.AccessVariable(name);

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
}
