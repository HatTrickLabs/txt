using System;
using HatTrick.Reflection;

namespace HatTrick.Text.Templating
{
    public static class BindHelper
    {
        #region resolve bind target
        public static object ResolveBindTarget(string bindAs, LambdaRepository lambdaRepo, ScopeChain scopeChain, int scopeLinkDepth = 0)
        {
            object target = null;
            object localScope = scopeChain.Peek(scopeLinkDepth);

            if (bindAs.Length == 1 && bindAs[0] == '$')//bindto is localscope (this)
                target = localScope;

            else if (bindAs[0] == '$' && bindAs[1] == '.')//reflect from bindto object
                target = BindHelper.ResolveRootedBindTarget(bindAs, localScope);

            else if (bindAs[0] == ':')//variable reference
                target = BindHelper.ResolveVariableReferenceBindTarget(bindAs, lambdaRepo, scopeChain);

            else if (bindAs[0] == '.' && bindAs[1] == '.' && bindAs[2] == '\\')//scope chain walk ..\
                target = BindHelper.ResolveScopeWalkBindTarget(bindAs, lambdaRepo, scopeChain);

            else if (BindHelper.IsLambdaExpression(bindAs))//lambda expression
                target = BindHelper.ResolveLambdaExpressionBindTarget(bindAs, lambdaRepo, scopeChain);

            else//simple bind
                target = ReflectionHelper.Expression.ReflectItem(localScope, bindAs);

            return target;
        }
        #endregion

        #region resolve rooted bind target
        private static object ResolveRootedBindTarget(string bindAs, object localScope)
        {
            string expression = bindAs.Substring(2, bindAs.Length - 2);//remove the $.
            object target = ReflectionHelper.Expression.ReflectItem(localScope, expression);
            return target;
        }
        #endregion

        #region resolve variable reference bind target
        private static object ResolveVariableReferenceBindTarget(string bindAs, LambdaRepository lambdaRepo, ScopeChain scopeChain)
        {
            object target = null;
            int dot = bindAs.IndexOf('.');
            if (dot > -1)
            {
                target = scopeChain.AccessVariable(bindAs.Substring(0, dot));
                scopeChain.Push(target);
                target = BindHelper.ResolveBindTarget(bindAs.Substring(++dot, bindAs.Length - dot), lambdaRepo, scopeChain);
                scopeChain.Pop();
            }
            else
            {
                target = scopeChain.AccessVariable(bindAs);
            }
            return target;
        }
        #endregion

        #region resolve scope walk bind target
        private static object ResolveScopeWalkBindTarget(string bindAs, LambdaRepository lambdaRepo, ScopeChain scopeChain)
        {
            int lastIdxOf;
            int depth = BindHelper.CountInstancesOfPattern(bindAs, @"..\", out lastIdxOf);
            object target = BindHelper.ResolveBindTarget(bindAs.Substring(lastIdxOf + 3, bindAs.Length - (depth * 3)), lambdaRepo, scopeChain, depth);
            return target;
        }
        #endregion

        #region resolve lamba expression bind target
        private static object ResolveLambdaExpressionBindTarget(string bindAs, LambdaRepository lambdaRepo, ScopeChain scopeChain)
        {
            Func<object> lambda = lambdaRepo?.Resolve(bindAs, scopeChain) 
                ?? throw new InvalidOperationException($"Encountered function that does not exist in lambda repository: {bindAs}");

            object target = lambda();

            return target;
        }
        #endregion

        #region is lambda expression
        public static bool IsLambdaExpression(string bindAs)
        {
            return bindAs.IndexOf("=>") > -1;
        }
        #endregion

        #region count instances of pattern
        public static int CountInstancesOfPattern(string content, string pattern, out int lastIndexOf)
        {
            lastIndexOf = -1;
            int cnt = 0;
            int idx = 0;

            while ((idx = content.IndexOf(pattern, idx)) != -1)
            {
                lastIndexOf = idx;
                idx += pattern.Length;
                cnt += 1;
            }
            return cnt;
        }
        #endregion

        #region is single quoted
        public static bool IsSingleQuoted(string value)
        {
            char singleQuote = '\'';
            return value[0] == singleQuote && value[value.Length - 1] == singleQuote;
        }
        #endregion

        #region is double quoted
        public static bool IsDoubleQuoted(string value)
        {
            char doubleQuote = '"';
            return value[0] == doubleQuote && value[value.Length - 1] == doubleQuote;
        }
        #endregion

        #region un quote
        public static string UnQuote(string value)
        {
            if (value == null)
                return null;

            if (value == string.Empty)
                return value;

            return value.Substring(1, value.Length - 2);
        }
        #endregion

        #region strip
        public static string Strip(char character, string from)
        {
            if (from == null)
                return null;

            if (from == string.Empty)
                return from;

            char c;
            char[] result = new char[from.Length];
            int at = 0;
            for (int i = 0; i < from.Length; i++)
            {
                c = from[i];
                if (c == character)
                    continue;

                result[at++] = c;
            }

            return new string(result, 0, at);
        }
        #endregion

        #region is numeric literal
        public static bool IsNumericLiteral(string value)
        {
            return value != null 
                && value != string.Empty 
                && (
                    char.IsDigit(value[0]) 
                    || value[0] == '+' 
                    || value[0] == '-' 
                    || (value[0] == '.' && value[1] != '.')// ../ is a scope walk literal...
                );
        }
        #endregion

        #region get numeric literal suffix
        public static char GetNumericLiteralSuffix(string literal, out string number)
        {
            number = literal;
            if (!BindHelper.IsNumericLiteral(literal))
                return '\0';

            int lastIndex = literal.Length - 1;
            if (char.IsDigit(literal[lastIndex]) || literal[lastIndex] == '.')
                return '\0';

            number = literal.Remove(lastIndex);
            return literal[lastIndex];
        }
        #endregion

        #region get numeric literal type
        public static TypeCode GetNumericLiteralType(string literal, out string number)
        {
            char suffix = BindHelper.GetNumericLiteralSuffix(literal, out number);
            switch (suffix)
            {
                case 'F':
                case 'f':
                    return TypeCode.Single;
                case 'M':
                case 'm':
                    return TypeCode.Decimal;
                case 'D':
                case 'd':
                    return TypeCode.Double;
                case 'L':
                case 'l':
                    return TypeCode.Int64;
                case 'I':
                case 'i':
                    return TypeCode.Int32;
                case '\0':
                default:
                    return TypeCode.Empty;
            }
        }
        #endregion

        #region parse numeric literal
        public static object ParseNumericLiteral(string literal)
        {
            Func<TypeCode, string, string> exceptionMsg = (tc, lit) =>
            {
                return $"Cannot parse numeric literal: {lit} as: {tc}";
            };

            TypeCode typeCode = BindHelper.GetNumericLiteralType(literal, out string number);
            object value = null;
            switch (typeCode)
            {
                case TypeCode.Decimal:
                    if (!decimal.TryParse(number, out decimal dec))
                        throw new FormatException(exceptionMsg(TypeCode.Decimal, number));

                    value = dec;
                    break;
                case TypeCode.Double:
                    if (!double.TryParse(number, out double dbl))
                        throw new FormatException(exceptionMsg(TypeCode.Double, number));

                    value = dbl;
                    break;
                case TypeCode.Int32:
                    if (!int.TryParse(number, out int i))
                        throw new FormatException(exceptionMsg(TypeCode.Int32, number));

                    value = i;
                    break;
                case TypeCode.Int64:
                    if (!long.TryParse(number, out long l))
                        throw new FormatException(exceptionMsg(TypeCode.Int64, number));

                    value = l;
                    break;
                case TypeCode.Single:
                    if (!Single.TryParse(number, out Single s))
                        throw new FormatException(exceptionMsg(TypeCode.Single, number));

                    value = s;
                    break;
                case TypeCode.Empty:
                default:
                    throw new InvalidOperationException($"Cannot parse numeric literal: {literal} ... type could not be determined. valid type suffix values: m,d,i,l,f");
            }

            return value;
        }
        #endregion

        #region is true
        public static bool IsTrue(object val)
        {
            bool? bit;
            int? i;
            uint? ui;
            long? l;
            ulong? ul;
            double? dbl;
            float? flt;
            decimal? dec;
            short? sht;
            ushort? usht;
            char? c;
            string s;
            System.Collections.IEnumerable col;

            bool isFalse = (val == null)
                       || (bit = val as bool?) != null && bit == false
                       || (i = val as int?) != null && i == 0
                       || (dbl = val as double?) != null && dbl == 0
                       || (l = val as long?) != null && l == 0
                       || (flt = val as float?) != null && flt == 0
                       || (dec = val as decimal?) != null && dec == 0
                       || (c = val as char?) != null && c == '\0'
                       || val == DBNull.Value
                       || (ui = val as uint?) != null && ui == 0
                       || (ul = val as ulong?) != null && ul == 0
                       || (sht = val as short?) != null && sht == 0
                       || (usht = val as ushort?) != null && usht == 0
                       || (col = val as System.Collections.IEnumerable) != null && !col.GetEnumerator().MoveNext() //NOTE: this will catch string.Empty
                       || (s = val as string) != null && (s.Length == 1 && s[0] == '\0');

            return !isFalse;
        }
        #endregion
    }
}
