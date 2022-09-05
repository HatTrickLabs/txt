using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HatTrick.Text.Templating
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
                throw new ArgumentException($"A function with the provided name: {name} has already been added");

            _lambdas.Add(name, function);
        }
        #endregion

        #region deregister
        public void Deregister(string name)
        {
            if (!_lambdas.ContainsKey(name))
                throw new ArgumentException($"No lambda exists for the provided name: {name}");

            _lambdas.Remove(name);
        }
        #endregion

		#region resolve
		public Func<object> Resolve(string lambdaExpression, ScopeChain scopeChain)
        {
            this.Split(lambdaExpression, out string name, out string arguments);

            if (!_lambdas.ContainsKey(name))
                throw new KeyNotFoundException($"Encountered function that does not exist in lambda repository: {name}");

            Delegate expr = _lambdas[name];
            MethodInfo mi = expr.Method;
            ParameterInfo[] pInfos = mi.GetParameters();

            string[] argVals = new string[pInfos.Length];
            this.ParseLambdaArgs(arguments, ref argVals);

            if (pInfos.Length != argVals.Length)
            {
                string msg = $"Attempted function invocation with invalid number of parameters...lambda name: {name} expected: {pInfos.Length} provided: {argVals.Length}";
                throw new InvalidOperationException(msg);
            }

            object[] args = new object[pInfos.Length];
            for (int i = 0; i < pInfos.Length; i++)
            {
                args[i] = this.CaptureLambdaArgument(argVals[i], scopeChain, pInfos[i], name, i);
            }

            return () => expr.DynamicInvoke(args);
        }
        #endregion

        #region split
        private void Split(string expression, out string name, out string paramList)
        {
            //i.e. (arg1, arg2) => LambdaName
            name = null;
            paramList = null;

            string op = "=>";
            int opIndex = expression.IndexOf(op);

            if (opIndex < 0)
                throw new ArgumentException("Expression is not a properly formatted lambda function", nameof(expression));

            //right side of expression
            name = expression.Substring(opIndex + 2);

            //left side of expression 
            paramList = expression.Substring(0, opIndex);
        }
        #endregion

        #region parse lambda args
        private void ParseLambdaArgs(string argsExpr, ref string[] args)
        {
            if (args.Length == 0)
                return;

            char c;
            int at = -1;
            char singleQuote    = '\'';
            char doubleQuote    = '"';
            char escape         = '\\';
            char comma          = ',';
            char openParen      = '(';
            char closeParen     = ')';

            StringBuilder sb = new StringBuilder();
            bool singleQuoted = false;
            bool doubleQuoted = false;
            for (int i = 0; i < argsExpr.Length; i++)
            {
                c = argsExpr[i];
                if (c == openParen || c == closeParen)
                    continue;
                else if (c == doubleQuote)
                {
                    if (doubleQuoted && i > 0 && argsExpr[i - 1] == escape)
                        sb.Length -= 1;
                    else if (!singleQuoted)
                        doubleQuoted = !doubleQuoted;
                }
                else if (c == singleQuote)
                {
                    if (singleQuoted && i > 0 && argsExpr[i - 1] == escape)
                        sb.Length -= 1;
                    else if (!doubleQuoted)
                        singleQuoted = !singleQuoted;
                }
                else if (c == comma)
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

            args[++at] = sb.ToString(); //final...
        }
        #endregion

        #region capture lambda arguments
        private object CaptureLambdaArgument(string arg, ScopeChain scopeChain, ParameterInfo paramInfo, string lambda, int index)
        {
            object obj = null;
            TypeCode tCode = Type.GetTypeCode(paramInfo.ParameterType);
            switch (tCode)
            {
                case TypeCode.Object:
                    obj = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                    break;
                case TypeCode.Boolean:
                    obj = this.EnsureBooleanArgument(arg, scopeChain, lambda, index);
                    break;
                case TypeCode.Byte:
                    obj = this.EnsureByteArgument(arg, scopeChain, lambda, index);
                    break;
                case TypeCode.Char:
                    obj = this.EnsureCharArgument(arg, scopeChain, lambda, index);
                    break;
                case TypeCode.String:
                    obj = this.EnsureStringArgument(arg, scopeChain, lambda, index);
                    break;
                case TypeCode.DateTime:
                    obj = this.EnsureDateTimeArgument(arg, scopeChain, lambda, index);
                    break;
                case TypeCode.Decimal:
                    obj = this.EnsureDecimalArgument(arg, scopeChain, lambda, index);
                    break;
                case TypeCode.Double:
                    obj = this.EnsureDoubleArgument(arg, scopeChain, lambda, index);
                    break;
                case TypeCode.Int16:
                    obj = this.EnsureInt16Argument(arg, scopeChain, lambda, index);
                    break;
                case TypeCode.Int32:
                    obj = this.EnsureInt32Argument(arg, scopeChain, lambda, index);
                    break;
                case TypeCode.Int64:
                    obj = this.EnsureInt64Argument(arg, scopeChain, lambda, index);
                    break;
                case TypeCode.UInt16:
                    obj = this.EnsureUInt16Argument(arg, scopeChain, lambda, index);
                    break;
                case TypeCode.UInt32:
                    obj = this.EnsureUInt32Argument(arg, scopeChain, lambda, index);
                    break;
                case TypeCode.UInt64:
                    obj = this.EnsureUInt64Argument(arg, scopeChain, lambda, index);
                    break;
                case TypeCode.SByte:
                    obj = this.EnsureSByteArgument(arg, scopeChain, lambda, index);
                    break;
                case TypeCode.Single:
                    obj = this.EnsureSingleArgument(arg, scopeChain, lambda, index); ;
                    break;
                //case TypeCode.Empty:
                //    break;
                //case TypeCode.DBNull:
                //    break;
                default:
                    throw new InvalidOperationException($"Encountered un-expected Type.TypeCode: {tCode}");
            }

            return obj;
        }
        #endregion

        #region ensure argument
        private object EnsureStringArgument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (BindHelper.IsDoubleQuoted(arg) || BindHelper.IsSingleQuoted(arg))
            {
                target = arg.Substring(1, (arg.Length - 2));
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.String, lambdaName, index);
            }
            return target;
        }

        private object EnsureDateTimeArgument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (BindHelper.IsDoubleQuoted(arg) || BindHelper.IsSingleQuoted(arg))
            {
                arg = arg.Substring(1, (arg.Length - 2));
                if (!DateTime.TryParse(arg, out DateTime dt))
                {
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.DateTime));
                }
                return dt;
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.DateTime, lambdaName, index);
            }
            return target;
        }

        public object EnsureBooleanArgument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (string.Compare(arg, "true", true) == 0)
            {
                target = true;
            }
            else if (string.Compare(arg, "false", true) == 0)
            {
                target = false;
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.Boolean, lambdaName, index);
            }
            return target;
        }

        private object EnsureCharArgument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (BindHelper.IsDoubleQuoted(arg) || BindHelper.IsSingleQuoted(arg))
            {
                string val = arg.Substring(1, (arg.Length - 2));
                if (!char.TryParse(val, out char c))
                {
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.Char));
                }
                target = c;
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.Char, lambdaName, index);
            }
            return target;
        }

        private object EnsureByteArgument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (char.IsDigit(arg[0])) //must be numeric literal
            {
                if (!byte.TryParse(arg, out byte b))
                {
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.Byte));
                }
                target = b;
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.Byte, lambdaName, index);
            }
            return target;
        }

        private object EnsureDecimalArgument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (char.IsDigit(arg[0]) || arg[0] == '.' || arg[0] == '-' || arg[0] == '+') //must be numeric literal
            {
                if (!decimal.TryParse(arg, out decimal d))
                {
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.Decimal));
                }
                target = d;
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.Decimal, lambdaName, index);
            }
            return target;
        }

        private object EnsureDoubleArgument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (char.IsDigit(arg[0]) || arg[0] == '.' || arg[0] == '-' || arg[0] == '+') //must be numeric literal
            {
                if (!double.TryParse(arg, out double d))
                {
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.Double));
                }
                target = d;
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.Double, lambdaName, index);
            }
            return target;
        }

        private object EnsureInt16Argument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (char.IsDigit(arg[0]) || arg[0] == '-' || arg[0] == '+') //must be numeric literal
            {
                if (!Int16.TryParse(arg, out Int16 i))
                {
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.Int16));
                }
                target = i;
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.Int16, lambdaName, index);
            }
            return target;
        }

        private object EnsureInt32Argument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (char.IsDigit(arg[0]) || arg[0] == '-' || arg[0] == '+') //must be numeric literal
            {
                if (!int.TryParse(arg, out int i))
                {
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.Int32));
                }
                target = i;
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.Int32, lambdaName, index);
            }
            return target;
        }

        private object EnsureInt64Argument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (char.IsDigit(arg[0]) || arg[0] == '-' || arg[0] == '+') //must be numeric literal
            {
                if (!long.TryParse(arg, out long l))
                {
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.Int64));
                }
                target = l;
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.Int64, lambdaName, index);
            }
            return target;
        }

        private object EnsureSByteArgument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (char.IsDigit(arg[0]) || arg[0] == '-' || arg[0] == '+') //must be numeric literal
            {
                if (!sbyte.TryParse(arg, out sbyte s))
                {
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.SByte));
                }
                target = s;
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.SByte, lambdaName, index);
            }
            return target;
        }

        private object EnsureSingleArgument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (char.IsDigit(arg[0]) || arg[0] == '.' || arg[0] == '-' || arg[0] == '+') //must be numeric literal
            {
                if (!Single.TryParse(arg, out Single s))
                {
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.Single));
                }
                target = s;
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.Single, lambdaName, index);
            }
            return target;
        }

        private object EnsureUInt16Argument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (char.IsDigit(arg[0])) //must be numeric literal
            {
                if (!UInt16.TryParse(arg, out UInt16 u))
                {
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.UInt16));
                }
                target = u;
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.UInt16, lambdaName, index);
            }
            return target;
        }

        private object EnsureUInt32Argument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (char.IsDigit(arg[0])) //must be numeric literal
            {
                if (!UInt32.TryParse(arg, out UInt32 u))
                {
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.UInt32));
                }
                target = u;
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.UInt32, lambdaName, index);
            }
            return target;
        }

        private object EnsureUInt64Argument(string arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            object target = null;
            if (char.IsDigit(arg[0])) //must be numeric literal
            {
                if (!UInt64.TryParse(arg, out UInt64 u))
                {
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.UInt64));
                }
                target = u;
            }
            else
            {
                target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
                this.EnsureArgumentType(arg, target, TypeCode.UInt64, lambdaName, index);
            }
            return target;
        }
        #endregion

        #region ensure argument type
        private void EnsureArgumentType(string arg, object value, TypeCode typeCode, string lambdaName, int index)
        {
            if (Type.GetTypeCode(value.GetType()) != typeCode)
            {
                string msg = "Attempted function invocation with invalid argument type..."
                           + $"lambda name: {lambdaName}...expected argument of type: '{typeCode}'...."
                           + $"argument value provided: {arg}...at parameter position: {index}";

                throw new ArgumentException(msg);
            }
        }
        #endregion

        #region format exception message builder
        private string FormatExceptionMessageBuilder(string lambdaName, string arg, int index, TypeCode expectedType)
        {
            string msg = "Attempted function invocation with invalid parameter..."
                           + $"lambda name: {lambdaName}  expected: a properly formated {expectedType} literal. "
                           + $"value provided: {arg} at parameter position: {index}";

            return msg;
        }
        #endregion
    }
}
