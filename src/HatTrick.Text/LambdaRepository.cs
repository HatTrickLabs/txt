using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Text
{
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

        #region register
        public void Register(string name, Delegate function)
        {
            if (_lambdas.ContainsKey(name))
            {
                throw new ArgumentException($"A function with the provided name: {name} has already been added");
            }
            _lambdas.Add(name, function);
        }

        public void Register(string name, Func<string> function)
        {
            if (_lambdas.ContainsKey(name))
            {
                throw new ArgumentException($"A function with the provided name: {name} has already been added");
            }
            _lambdas.Add(name, function);
        }
        #endregion

        #region deregister
        public void Deregister(string name)
        {
            if (!_lambdas.ContainsKey(name))
            {
                throw new ArgumentException($"No lambda exists for the provided name: {name}");
            }
            _lambdas.Remove(name);
        }
        #endregion

        #region parse 
        public void ParseKnown(string expression, out string name, out string[] parameters)
        {
            name = null;
            parameters = null;

            string op = "=>";
            int opIndex = expression.IndexOf(op);

            if (opIndex < 0)
            {
                throw new ArgumentException("provided value is not a valid lambda", nameof(expression));
            }

            name = expression.Substring(opIndex + 2);

            if (!_lambdas.ContainsKey(name))
            { throw new MergeException($"Encountered function that does not exist in lambda repo: {name}"); }

            System.Reflection.MethodInfo mi = _lambdas[name].Method;

            int paramsCount = mi.GetParameters().Length;

            parameters = new string[paramsCount];
            int at = -1;

            string left = expression.Substring(0, opIndex);

            char c;
            StringBuilder sb = new StringBuilder();
            bool singleQuoted = false;
            bool doubleQuoted = false;
            for (int i = 0; i < left.Length; i++)
            {
                c = left[i];
                if (c == '(' || c == ')')
                {
                    continue;
                }
                else if (c == '"')
                {
                    if (doubleQuoted && i > 0 && left[i - 1] == '\\')
                    {
                        sb.Length -= 1;
                    }
                    else
                    {
                        doubleQuoted = !doubleQuoted;
                    }
                }
                else if (c == '\'')
                {
                    if (singleQuoted && i > 0 && left[i - 1] == '\\')
                    {
                        sb.Length -= 1;
                    }
                    else
                    {
                        singleQuoted = !singleQuoted;
                    }
                }
                else if (c == ',')
                {
                    if (!(singleQuoted || doubleQuoted))
                    {
                        parameters[++at] = sb.ToString();
                        sb.Clear();
                        continue;
                    }
                }

                sb.Append(c);
            }

            if (parameters.Length > 0)
            {
                parameters[++at] = sb.ToString(); //final...
            }
        }
        #endregion

        #region invoke
        public object Invoke(string name, params object[] parms)
        {
            if (!_lambdas.ContainsKey(name))
            { throw new MergeException($"Encountered function that does not exist in lambda repo: {name}"); }

            System.Reflection.MethodInfo mi = _lambdas[name].Method;

            int paramsCount = mi.GetParameters().Length;

            if (paramsCount != parms.Length)
            {
                string msg = $"Attempted function invocation with invalid number of parameters. Lambda name: {name}  Expected count: {paramsCount} Provided count: {parms.Length}";
                throw new MergeException(msg);
            }

            return _lambdas[name].DynamicInvoke(parms);
        }
        #endregion
    }
}
