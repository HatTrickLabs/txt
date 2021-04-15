using System;

namespace HatTrick.Text.Templating
{
	public class VariableStack
	{
		#region internals
		private string _name;
		private object _value;
		private int _scopeDemarkation;
		private VariableStack _subStack;
		#endregion

		#region interface
		public string Name => _name;
		public object Value => _value;
		public bool IsMarked => _scopeDemarkation > 0;
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
			_scopeDemarkation += 1;
		}
		#endregion

		#region remove scope marker
		public void RemoveScopeMarker()
		{
			_scopeDemarkation -= 1;
		}
		#endregion

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

		#region try update
		public bool TryUpdate(string name, object value)
		{
			if (name == _name)
			{
				_value = value;
				return true;
			}
			else
			{
				return (_subStack != null) && _subStack.TryUpdate(name, value);
			}
		}
		#endregion
	}
}
