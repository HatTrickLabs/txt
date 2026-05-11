using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;

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
            _lambdas = new Dictionary<string, Delegate>(StringComparer.Ordinal);
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

            var lookup = _lambdas.GetAlternateLookup<ReadOnlySpan<char>>();
            if (!lookup.TryGetValue(nameSpan, out Delegate expr))
                throw new KeyNotFoundException($"Encountered function that does not exist in lambda repository: {nameSpan}");

            MethodInfo mi = expr.Method;
            ParameterInfo[] pInfos = mi.GetParameters();

            object[] args = new object[pInfos.Length];
            int count = this.ParseAndCaptureArgs(argumentsSpan, args, pInfos, scopeChain, nameSpan);

            if (pInfos.Length != count)
            {
                string msg = $"Attempted function invocation with invalid number of parameters...Func name: {nameSpan} expected arguments: {pInfos.Length} provided argument: {count}";
                throw new InvalidOperationException(msg);
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

        #region parse and capture args
        private int ParseAndCaptureArgs(ReadOnlySpan<char> argsExpr, object[] args, ParameterInfo[] pInfos, ScopeChain scopeChain, ReadOnlySpan<char> lambdaName)
        {
            Span<char> stackBuffer = stackalloc char[256];
            char[] rentedBuffer = null;
            Span<char> argBuffer = stackBuffer;
            int bufLen = 0;
            int at = 0;
            bool singleQuoted = false;
            bool doubleQuoted = false;

            try
            {
                for (int i = 0; i < argsExpr.Length; i++)
                {
                    char c = argsExpr[i];

                    if (c == '(' || c == ')')
                        continue;

                    if (c == '"')
                    {
                        if (doubleQuoted && i > 0 && argsExpr[i - 1] == '\\')
                            bufLen -= 1;

                        else if (!singleQuoted)
                            doubleQuoted = !doubleQuoted;
                    }
                    else if (c == '\'')
                    {
                        if (singleQuoted && i > 0 && argsExpr[i - 1] == '\\')
                            bufLen -= 1;

                        else if (!doubleQuoted)
                            singleQuoted = !singleQuoted;
                    }
                    else if (c == ',' && !(singleQuoted || doubleQuoted))
                    {
                        if (at < args.Length)
                        {
                            int idx = at;
                            args[idx] = this.CaptureLambdaArgument(argBuffer[..bufLen], scopeChain, pInfos[idx], lambdaName, idx);
                            at++;
                        }
                        bufLen = 0;
                        continue;
                    }

                    if (bufLen == argBuffer.Length)
                        argBuffer = this.GrowArgBuffer(ref rentedBuffer, argBuffer, bufLen);

                    argBuffer[bufLen++] = c;
                }

                if (bufLen > 0 && at < args.Length)
                {
                    int idx = at;
                    args[idx] = this.CaptureLambdaArgument(argBuffer[..bufLen], scopeChain, pInfos[idx], lambdaName, idx);
                    at++;
                }

                return at;
            }
            finally
            {
                if (rentedBuffer != null)
                    ArrayPool<char>.Shared.Return(rentedBuffer);
            }
        }

        private Span<char> GrowArgBuffer(ref char[] rentedBuffer, Span<char> current, int len)
        {
            int newSize = current.Length * 2;
            char[] newRented = ArrayPool<char>.Shared.Rent(newSize);
            current[..len].CopyTo(newRented);

            if (rentedBuffer != null)
                ArrayPool<char>.Shared.Return(rentedBuffer);

            rentedBuffer = newRented;
            return newRented;
        }
        #endregion

        #region capture lambda arguments
        private object CaptureLambdaArgument(ReadOnlySpan<char> arg, ScopeChain scopeChain, ParameterInfo paramInfo, ReadOnlySpan<char> lambdaName, int index)
        {
            TypeCode tCode = Type.GetTypeCode(paramInfo.ParameterType);
            return tCode switch
            {
                TypeCode.Object   => BindHelper.ResolveBindTarget(arg, this, scopeChain),
                TypeCode.Boolean  => this.EnsureBooleanArgumentCore(arg, scopeChain, lambdaName, index),
                TypeCode.Byte     => this.EnsureNumericArgument<byte>(arg, scopeChain, lambdaName, index),
                TypeCode.Char     => this.EnsureCharArgument(arg, scopeChain, lambdaName, index),
                TypeCode.String   => this.EnsureStringArgument(arg, scopeChain, lambdaName, index),
                TypeCode.DateTime => this.EnsureDateTimeArgument(arg, scopeChain, lambdaName, index),
                TypeCode.Decimal  => this.EnsureNumericArgument<decimal>(arg, scopeChain, lambdaName, index),
                TypeCode.Double   => this.EnsureNumericArgument<double>(arg, scopeChain, lambdaName, index),
                TypeCode.Int16    => this.EnsureNumericArgument<short>(arg, scopeChain, lambdaName, index),
                TypeCode.Int32    => this.EnsureNumericArgument<int>(arg, scopeChain, lambdaName, index),
                TypeCode.Int64    => this.EnsureNumericArgument<long>(arg, scopeChain, lambdaName, index),
                TypeCode.UInt16   => this.EnsureNumericArgument<ushort>(arg, scopeChain, lambdaName, index),
                TypeCode.UInt32   => this.EnsureNumericArgument<uint>(arg, scopeChain, lambdaName, index),
                TypeCode.UInt64   => this.EnsureNumericArgument<ulong>(arg, scopeChain, lambdaName, index),
                TypeCode.SByte    => this.EnsureNumericArgument<sbyte>(arg, scopeChain, lambdaName, index),
                TypeCode.Single   => this.EnsureNumericArgument<float>(arg, scopeChain, lambdaName, index),
                _                 => throw new InvalidOperationException($"Encountered un-expected Type.TypeCode: {tCode}")
            };
        }
        #endregion

        #region ensure argument
        private object EnsureStringArgument(ReadOnlySpan<char> arg, ScopeChain scopeChain, ReadOnlySpan<char> lambdaName, int index)
        {
            if (BindHelper.IsDoubleQuoted(arg) || BindHelper.IsSingleQuoted(arg))
                return arg.Slice(1, arg.Length - 2).ToString();

            object target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
            this.EnsureArgumentType(arg, target, TypeCode.String, lambdaName, index);
            return target;
        }

        private object EnsureDateTimeArgument(ReadOnlySpan<char> arg, ScopeChain scopeChain, ReadOnlySpan<char> lambdaName, int index)
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
            => this.EnsureBooleanArgumentCore(arg, scopeChain, lambdaName, index);

        private object EnsureBooleanArgumentCore(ReadOnlySpan<char> arg, ScopeChain scopeChain, ReadOnlySpan<char> lambdaName, int index)
        {
            if (this.IsTrue(arg))
                return true;

            if (this.IsFalse(arg))
                return false;

            object target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
            this.EnsureArgumentType(arg, target, TypeCode.Boolean, lambdaName, index);
            return target;
        }

        private object EnsureCharArgument(ReadOnlySpan<char> arg, ScopeChain scopeChain, ReadOnlySpan<char> lambdaName, int index)
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

        private object EnsureNumericArgument<T>(ReadOnlySpan<char> arg, ScopeChain scopeChain, ReadOnlySpan<char> lambdaName, int index)
            where T : struct, ISpanParsable<T>
        {
            if (arg.Length > 0 && (char.IsDigit(arg[0]) || arg[0] == '.' || arg[0] == '-' || arg[0] == '+'))
            {
                if (!T.TryParse(arg, null, out T val))
                    throw new FormatException(this.FormatExceptionMessageBuilder(lambdaName, arg, index, Type.GetTypeCode(typeof(T))));

                return val;
            }

            object target = BindHelper.ResolveBindTarget(arg, this, scopeChain);
            if (target is string s && T.TryParse(s, null, out T parsed))
                return parsed;

            this.EnsureArgumentType(arg, target, Type.GetTypeCode(typeof(T)), lambdaName, index);
            return target;
        }
        #endregion

        #region ensure argument type
        private void EnsureArgumentType(ReadOnlySpan<char> arg, object value, TypeCode typeCode, ReadOnlySpan<char> lambdaName, int index)
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
        private bool IsTrue(ReadOnlySpan<char> arg)
        {
            return MemoryExtensions.Equals(arg, "true", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsFalse(ReadOnlySpan<char> arg)
        {
            return MemoryExtensions.Equals(arg, "false", StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        #region format exception message builder
        private string FormatExceptionMessageBuilder(ReadOnlySpan<char> lambdaName, ReadOnlySpan<char> arg, int index, TypeCode expectedType)
        {
            return "Attempted function invocation with invalid parameter..."
                 + $"Func name: {lambdaName}  expected: a properly formated {expectedType} literal. "
                 + $"value provided: {arg} at parameter position: {index}";
        }
        #endregion
    }
}
