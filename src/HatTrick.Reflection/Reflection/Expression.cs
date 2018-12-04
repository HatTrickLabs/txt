using System;
using System.Collections;
using System.Reflection;

namespace HatTrick.Reflection
{
    //reflects nested class members; i.e. itemExpression="User.Address.City"....JRod
    public static partial class ReflectionHelper
    {
        public static class Expression
        {
            #region reflect item
            public static object ReflectItem(object source, string expression, bool throwOnNoPropExists = true)
            {
                if (source == null) { throw new ArgumentNullException(nameof(source)); }
                if (expression == null) { throw new ArgumentNullException(nameof(expression)); }

                //re-usable internal object o
                object o = source;

                var itemExists = false;

                int memberAccessorIdx = expression.IndexOf('.');
                string thisExpression = (memberAccessorIdx > -1) ? expression.Substring(0, memberAccessorIdx) : expression;

                //if the caller is reflecting data from a dictionary, attempt dictionary lookup
                IDictionary idict;
                if ((idict = source as IDictionary) != null)
                {
                    if (idict.Contains(thisExpression))
                    {
                        itemExists = true;
                        o = idict[thisExpression];
                    }
                }
                else
                {
                    Type t = o.GetType();

                    PropertyInfo p = t.GetProperty(thisExpression);

                    if (p != null)
                    {
                        itemExists = true;
                        o = p.GetValue(o, null);
                    }
                    else
                    {
                        FieldInfo f = t.GetField(thisExpression);

                        if (f != null)
                        {
                            itemExists = true;
                            o = f.GetValue(o);
                        }
                    }
                }

                if (itemExists && o != null && memberAccessorIdx > -1)
                {
                    //recursive call...
                    o = ReflectItem(o, expression.Substring(++memberAccessorIdx, expression.Length - memberAccessorIdx));
                }

                if (!itemExists && throwOnNoPropExists)
                {
                    throw new NoPropertyExistsException($"Property does not exist on source object. Property: {expression}, Bound Type: {source.GetType()}");
                }

                return itemExists ? o : null;
            }
            #endregion
        }
    }
}
