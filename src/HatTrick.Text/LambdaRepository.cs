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

        //public void Register(string name, Func<string> function)
        //{
        //    if (_lambdas.ContainsKey(name))
        //    {
        //        throw new ArgumentException($"A function with the provided name: {name} has already been added");
        //    }
        //    _lambdas.Add(name, function);
        //}
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
        public void Parse(string expression, out string name, out string[] arguments)
        {
            //i.e. (arg1, arg2) => LambdaName
            name = null;
            arguments = null;

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

            arguments = new string[paramsCount];

            //left side of expression 
            string argsExpr = expression.Substring(0, opIndex);

            this.CaptureLambdaArgs(argsExpr, ref arguments);
        }
        #endregion

        #region extract lambda args
        private void CaptureLambdaArgs(string argsExpr, ref string[] args)
        {
            char c;
            int at = -1;
            StringBuilder sb = new StringBuilder();
            bool singleQuoted = false;
            bool doubleQuoted = false;
            for (int i = 0; i < argsExpr.Length; i++)
            {
                c = argsExpr[i];
                if (c == '(' || c == ')')
                {
                    continue;
                }
                else if (c == '"')
                {
                    if (doubleQuoted && i > 0 && argsExpr[i - 1] == '\\')
                    {
                        sb.Length -= 1;
                    }
                    else if (!singleQuoted)
                    {
                        doubleQuoted = !doubleQuoted;
                    }
                }
                else if (c == '\'')
                {
                    if (singleQuoted && i > 0 && argsExpr[i - 1] == '\\')
                    {
                        sb.Length -= 1;
                    }
                    else if (!doubleQuoted)
                    {
                        singleQuoted = !singleQuoted;
                    }
                }
                else if (c == ',')
                {
                    if (!(singleQuoted || doubleQuoted))
                    {
                        args[++at] = sb.ToString();
                        sb.Clear();
                        continue;
                    }
                }
                sb.Append(c);
            }

            if (args.Length > 0)
            {
                args[++at] = sb.ToString(); //final...
            }
        }
        #endregion

        #region parse numeric literal
        public object ParseNumericLiteral(string value, string suffix)
        {
            bool parsed = false;
            object output = null;
            switch (suffix)
            {
                case "int":
                    {
                        parsed = int.TryParse(value, out int val);
                        output = val;
                    }
                    break;
                case "long":
                    {
                        parsed = long.TryParse(value, out long val);
                        output = val;
                    }
                    break;
                case "decimal":
                case "dec":
                    {
                        parsed = decimal.TryParse(value, out decimal val);
                        output = val;
                    }
                    break;
                case "double":
                case "dbl":
                    {
                        parsed = double.TryParse(value, out double val);
                        output = val;
                    }
                    break;
                case "byte":
                    {
                        parsed = byte.TryParse(value, out byte val);
                        output = val;
                    }
                    break;
                default:
                    throw new MergeException($"encountered unknown numeric literal suffix: {suffix}");
            }

            if (!parsed)
            {
                throw new MergeException($"unable to parse provided numerical literal: {value}:{suffix}");
            }

            return output;
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
