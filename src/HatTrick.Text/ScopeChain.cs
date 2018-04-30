using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Text
{
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
        public object ReachBack(int back)
        {
            int count = _items.Count;
            object item = _items[count - back];
            return item;
        }
        #endregion
    }
}
