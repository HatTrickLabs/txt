using System;
using System.Collections;
using System.Reflection;

namespace HatTrick.Text.Reflection
{
    //reflects nested class members; i.e. itemExpression="User.Address.City"....JRod
    public static partial class ReflectionHelper
    {
        public static class Expression
        {
            #region reflect item
            public static object ReflectItem(object sourceObject, string itemExpression, bool throwOnNoPropExists = true)
            {
                if (sourceObject == null) { throw new ArgumentNullException(nameof(sourceObject)); }
                if (itemExpression == null) { throw new ArgumentNullException(nameof(itemExpression)); }

                //re-usable internal object o
                object o = sourceObject;

                var itemExists = false;

                int memberAccessorIdx = itemExpression.IndexOf('.');
                string thisExpression = (memberAccessorIdx > -1) ? itemExpression.Substring(0, memberAccessorIdx) : itemExpression;

                //if the caller is reflecting data from a dictionary, attempt dictionary lookup
                IDictionary idict;
                if ((idict = sourceObject as IDictionary) != null)
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
                    o = ReflectItem(o, itemExpression.Substring(++memberAccessorIdx, itemExpression.Length - memberAccessorIdx));
                }

                if (!itemExists && throwOnNoPropExists)
                {
                    throw new NoPropertyExistsException($"Property does not exist on source object. Property: {itemExpression}, Bound Type: {sourceObject.GetType()}");
                }

                return itemExists ? o : null;
            }
            #endregion
        }
    }
}
