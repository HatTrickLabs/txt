using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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
            if (!_lambdas.TryAdd(name, function))
                throw new ArgumentException($"A function with the provided name: {name} has already been added");
        }
        #endregion

        #region deregister
        public void Deregister(string name)
        {
            if (!_lambdas.Remove(name))
                throw new ArgumentException($"No lambda exists for the provided name: {name}");
        }
        #endregion

        #region resolve
        public Func<object> Resolve(ReadOnlySpan<char> lambdaExpression, ScopeChain scopeChain)
        {
            this.Split(lambdaExpression, out ReadOnlySpan<char> nameSpan, out ReadOnlySpan<char> argumentsSpan);

            string name = nameSpan.ToString();

            if (!_lambdas.TryGetValue(name, out Delegate expr))
                throw new KeyNotFoundException($"Encountered function that does not exist in lambda repository: {name}");

            MethodInfo mi = expr.Method;
            ParameterInfo[] pInfos = mi.GetParameters();

            string[] argVals = new string[pInfos.Length];
            int count = this.ParseLambdaArgs(argumentsSpan, ref argVals);

            if (pInfos.Length != count)
            {
                string msg = $"Attempted function invocation with invalid number of parameters...Func name: {name} expected aruments: {pInfos.Length} provided argument: {count}";
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
        private void Split(ReadOnlySpan<char> expression, out ReadOnlySpan<char> name, out ReadOnlySpan<char> paramList)
        {
            int opIndex = expression.IndexOf("=>");

            if (opIndex < 0)
                throw new ArgumentException("Expression is not a properly formatted lambda function", nameof(expression));

            name = expression.Slice(opIndex + 2);
            paramList = expression.Slice(0, opIndex);
        }
        #endregion

        #region parse lambda args
        private int ParseLambdaArgs(ReadOnlySpan<char> argsExpr, ref string[] args)
        {
            char c;
            int at = 0;
            char singleQuote = '\'';
            char doubleQuote = '"';
            char escape      = '\\';
            char comma       = ',';
            char openParen   = '(';
            char closeParen  = ')';

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
                        if (at < args.Length)
                            args[at++] = sb.ToString();
                        sb.Clear();
                        continue;
                    }
                }
                sb.Append(c);
            }

            if (sb.Length > 0 && at < args.Length)
                args[at++] = sb.ToString();

            return at;
        }
        #endregion

        #region capture lambda arguments
        private object CaptureLambdaArgument(ReadOnlySpan<char> arg, ScopeChain scopeChain, ParameterInfo paramInfo, string lambda, int index)
        {
            TypeCode tCode = Type.GetTypeCode(paramInfo.ParameterType);
            return tCode switch
            {
                TypeCode.Object   => BindHelper.ResolveBindTarget(arg, this, scopeChain),
                TypeCode.Boolean  => this.EnsureBooleanArgument(arg, scopeChain, lambda, index),
                TypeCode.Byte     => this.EnsureNumericArgument<byte>(arg, scopeChain, lambda, index),
                TypeCode.Char     => this.EnsureCharArgument(arg, scopeChain, lambda, index),
                TypeCode.String   => this.EnsureStringArgument(arg, scopeChain, lambda, index),
                TypeCode.DateTime => this.EnsureDateTimeArgument(arg, scopeChain, lambda, index),
                TypeCode.Decimal  => this.EnsureNumericArgument<decimal>(arg, scopeChain, lambda, index),
                TypeCode.Double   => this.EnsureNumericArgument<double>(arg, scopeChain, lambda, index),
                TypeCode.Int16    => this.EnsureNumericArgument<short>(arg, scopeChain, lambda, index),
                TypeCode.Int32    => this.EnsureNumericArgument<int>(arg, scopeChain, lambda, index),
                TypeCode.Int64    => this.EnsureNumericArgument<long>(arg, scopeChain, lambda, index),
                TypeCode.UInt16   => this.EnsureNumericArgument<ushort>(arg, scopeChain, lambda, index),
                TypeCode.UInt32   => this.EnsureNumericArgument<uint>(arg, scopeChain, lambda, index),
                TypeCode.UInt64   => this.EnsureNumericArgument<ulong>(arg, scopeChain, lambda, index),
                TypeCode.SByte    => this.EnsureNumericArgument<sbyte>(arg, scopeChain, lambda, index),
                TypeCode.Single   => this.EnsureNumericArgument<float>(arg, scopeChain, lambda, index),
                _                 => throw new InvalidOperationException($"Encountered un-expected Type.TypeCode: {tCode}")
            };
        }
        #endregion

        #region ensure argument
        private object EnsureStringArgument(ReadOnlySpan<char> arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            if (BindHelper.IsDoubleQuoted(arg) || BindHelper.IsSingleQuoted(arg))
                return arg.Slice(1, arg.Length - 2).ToString();

            object target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
            this.EnsureArgumentType(arg, target, TypeCode.String, lambdaName, index);
            return target;
        }

        private object EnsureDateTimeArgument(ReadOnlySpan<char> arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            if (BindHelper.IsDoubleQuoted(arg) || BindHelper.IsSingleQuoted(arg))
            {
                arg = arg.Slice(1, arg.Length - 2);
                if (!DateTime.TryParse(arg, out DateTime dt))
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.DateTime));
                return dt;
            }

            object target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
            this.EnsureArgumentType(arg, target, TypeCode.DateTime, lambdaName, index);
            return target;
        }

        public object EnsureBooleanArgument(ReadOnlySpan<char> arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            if (this.IsTrue(arg))
                return true;

            if (this.IsFalse(arg))
                return false;

            object target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
            this.EnsureArgumentType(arg, target, TypeCode.Boolean, lambdaName, index);
            return target;
        }

        private object EnsureCharArgument(ReadOnlySpan<char> arg, ScopeChain scopeChain, string lambdaName, int index)
        {
            if (BindHelper.IsDoubleQuoted(arg) || BindHelper.IsSingleQuoted(arg))
            {
                if (arg.Length != 3)
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, TypeCode.Char));
                return arg[1];
            }

            object target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
            this.EnsureArgumentType(arg, target, TypeCode.Char, lambdaName, index);
            return target;
        }

        private object EnsureNumericArgument<T>(ReadOnlySpan<char> arg, ScopeChain scopeChain, string lambdaName, int index)
            where T : struct, ISpanParsable<T>
        {
            if (arg.Length > 0 && (char.IsDigit(arg[0]) || arg[0] == '.' || arg[0] == '-' || arg[0] == '+'))
            {
                if (!T.TryParse(arg, null, out T val))
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, Type.GetTypeCode(typeof(T))));
                return val;
            }

            object target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
            this.EnsureArgumentType(arg, target, Type.GetTypeCode(typeof(T)), lambdaName, index);
            return target;
        }
        #endregion

        #region ensure argument type
        private void EnsureArgumentType(ReadOnlySpan<char> arg, object value, TypeCode typeCode, string lambdaName, int index)
        {
            if (value is null || Type.GetTypeCode(value.GetType()) != typeCode)
            {
                string msg = "Attempted function invocation with invalid argument type..."
                           + $"Func name: {lambdaName}...expected argument of type: '{typeCode}'...."
                           + $"argument value provided: {arg}...at parameter position: {index}";
                throw new ArgumentException(msg);
            }
        }
        #endregion

        #region is true / is false
        private bool IsTrue(ReadOnlySpan<char> arg) =>
            MemoryExtensions.Equals(arg, "true", StringComparison.OrdinalIgnoreCase);

        private bool IsFalse(ReadOnlySpan<char> arg) =>
            MemoryExtensions.Equals(arg, "false", StringComparison.OrdinalIgnoreCase);
        #endregion

        #region format exception message builder
        private string FormatExceptionMessageBuilder(string lambdaName, ReadOnlySpan<char> arg, int index, TypeCode expectedType)
        {
            string msg = "Attempted function invocation with invalid parameter..."
                           + $"Func name: {lambdaName}  expected: a properly formated {expectedType} literal. "
                           + $"value provided: {arg} at parameter position: {index}";

            return msg;
        }
        #endregion
    }
}
