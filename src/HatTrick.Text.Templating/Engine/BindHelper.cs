using System;
using HatTrick.Reflection;

namespace HatTrick.Text.Templating
{
    public static class BindHelper
    {
        #region resolve bind target
        public static object ResolveBindTarget(ReadOnlySpan<char> bindAs, LambdaRepository lambdaRepo, ScopeChain scopeChain, int scopeLinkDepth = 0)
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

            else if (BindHelper.IsNumericLiteral(bindAs))//numeric literal
                target = bindAs.ToString();

            else//simple bind
                target = ReflectionHelper.Expression.ReflectItem(localScope, bindAs);

            return target;
        }
        #endregion

        #region resolve rooted bind target
        private static object ResolveRootedBindTarget(ReadOnlySpan<char> bindAs, object localScope)
        {
            var expression = bindAs.Slice(2, bindAs.Length - 2);//remove the $.
            object target = ReflectionHelper.Expression.ReflectItem(localScope, expression);
            return target;
        }
        #endregion

        #region resolve variable reference bind target
        private static object ResolveVariableReferenceBindTarget(ReadOnlySpan<char> bindAs, LambdaRepository lambdaRepo, ScopeChain scopeChain)
        {
            object target = null;
            int dot = bindAs.IndexOf('.');
            if (dot > -1)
            {
                target = scopeChain.AccessVariable(bindAs.Slice(0, dot));
                scopeChain.Push(target);
                target = BindHelper.ResolveBindTarget(bindAs.Slice(++dot, bindAs.Length - dot), lambdaRepo, scopeChain);
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
        private static object ResolveScopeWalkBindTarget(ReadOnlySpan<char> bindAs, LambdaRepository lambdaRepo, ScopeChain scopeChain)
        {
            BindHelper.WalkScope(bindAs, out int depth, out int endsAt);
            object target = BindHelper.ResolveBindTarget(bindAs.Slice(endsAt, bindAs.Length - endsAt), lambdaRepo, scopeChain, depth);
            return target;
        }
        #endregion

        #region resolve lamba expression bind target
        private static object ResolveLambdaExpressionBindTarget(ReadOnlySpan<char> bindAs, LambdaRepository lambdaRepo, ScopeChain scopeChain)
        {
            Func<object> lambda = lambdaRepo?.Resolve(bindAs, scopeChain)
                ?? throw new InvalidOperationException($"Encountered function that does not exist in lambda repository: {bindAs}");

            object target = lambda();

            return target;
        }
        #endregion

        #region is lambda expression
        public static bool IsLambdaExpression(ReadOnlySpan<char> bindAs)
        {
            return !bindAs.IsEmpty && bindAs[0] == '(';
        }
        #endregion

        #region walk scope
        private static void WalkScope(ReadOnlySpan<char> content, out int depth, out int endsAt)
        {
            int i = 0;
            int pos = 0;
            depth = 0;
            endsAt = 0;

            const string token = @"..\";

            do
            {
                if (content[i++] != token[pos++])
                    break;

                if (pos == 3)
                {
                    depth += 1;
                    pos = 0;
                }

            } while (i < content.Length);

            endsAt = depth * token.Length;
        }
        #endregion

        #region is single quoted
        public static bool IsSingleQuoted(ReadOnlySpan<char> value)
        {
            const char singleQuote = '\'';
            return value[0] == singleQuote && value[value.Length - 1] == singleQuote;
        }
        #endregion

        #region is double quoted
        public static bool IsDoubleQuoted(ReadOnlySpan<char> value)
        {
            const char doubleQuote = '"';
            return value[0] == doubleQuote && value[value.Length - 1] == doubleQuote;
        }
        #endregion

        #region un quote
        public static ReadOnlySpan<char> UnQuote(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
                return value;

            return value.Slice(1, value.Length - 2);
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

        public static string Strip(char character, ReadOnlySpan<char> from)
        {
            if (from.IsEmpty)
                return string.Empty;

            char[] result = new char[from.Length];
            int at = 0;
            for (int i = 0; i < from.Length; i++)
            {
                if (from[i] != character)
                    result[at++] = from[i];
            }

            return new string(result, 0, at);
        }
        #endregion

        #region is numeric literal
        public static bool IsNumericLiteral(ReadOnlySpan<char> value)
        {
            return !value.IsEmpty
                && (
                    char.IsDigit(value[0])
                    || value[0] == '+'
                    || value[0] == '-'
                    || (value[0] == '.' && value.Length > 1 && value[1] != '.')
                );
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
