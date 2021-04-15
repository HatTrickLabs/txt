using System;

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
				throw new MergeException("cannot 'Peek' scope link, the chain is empty");

			return _links.Peek(back);
		}
		#endregion

		#region set variable
		public void SetVariable(string name, object value)
		{
			if (_depth == 0)
				throw new MergeException("Invalid request - Scope chain has 0 links.");

			_links.SetVariable(name, value);
		}
		#endregion

		#region update variable
		public void UpdateVariable(string name, object value)
		{
			if (_depth == 0)
				throw new MergeException("Invalid request - Scope chain has 0 links");

			_links.UpdateVariable(name, value);
		}
		#endregion

		#region access variable
		public object AccessVariable(string name)
		{
			if (_depth == 0)
				throw new MergeException($"Invalid request - Scope chain has 0 links");

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
}
