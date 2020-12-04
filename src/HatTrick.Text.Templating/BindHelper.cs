using System;
using System.Collections.Generic;
using System.Text;
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
            if (bindAs.Length == 1 && bindAs[0] == '$') //append bindto obj
            {
                target = localScope;
            }
            else if (bindAs[0] == '$' && bindAs[1] == '.') //reflect from bindto object
            {
                string expression = bindAs.Substring(2, bindAs.Length - 2);//remove the $.
                target = ReflectionHelper.Expression.ReflectItem(localScope, expression);
            }
            else if (bindAs[0] == ':') //variable reference
            {
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
            }
            else if (bindAs[0] == '.' && bindAs[1] == '.' && bindAs[2] == '\\')
            {
                int lastIdxOf;
                int depth = BindHelper.CountInstancesOfPattern(bindAs, @"..\", out lastIdxOf);
                target = BindHelper.ResolveBindTarget(bindAs.Substring(lastIdxOf + 3, bindAs.Length - (depth * 3)), lambdaRepo, scopeChain, depth);
            }
            else if (BindHelper.IsLambdaExpression(bindAs)) //lambda expression
            {
                //{($.abc, $.xyz) => ConcatToValues}
                //{("keyVal") => GetSomething}
                //{(true) => GetSomething}
                Func<object> lambda = lambdaRepo.Resolve(bindAs, scopeChain);
                target = lambda();
            }
            else
            {
                target = ReflectionHelper.Expression.ReflectItem(localScope, bindAs);
            }
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

        #region is numeric literal
        public static bool IsNumericLiteral(string value)
        {
            return value != null 
                && value != string.Empty 
                && (char.IsDigit(value[0]) || (value[0] == '.' && value[1] != '.'));
        }
        #endregion

        #region get numeric literal suffix
        public static char GetNumericLiteralSuffix(string literal)
        {
            if (!BindHelper.IsNumericLiteral(literal))
                return '\0';

            int lastIndex = literal.Length - 1;
            if (char.IsDigit(literal[lastIndex]) || literal[lastIndex] == '.')
                return '\0';

            return literal[lastIndex];
        }
		#endregion

		#region get numeric literal type
		public static TypeCode GetNumericLiteralType(string literal)
        {
            char suffix = BindHelper.GetNumericLiteralSuffix(literal);
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
                return $"attempted to parse numeric literal: {lit} as: {tc} failed";
            };

            TypeCode typeCode = BindHelper.GetNumericLiteralType(literal);
            object value = null;
            switch (typeCode)
            {
                case TypeCode.Decimal:
                    if (!decimal.TryParse(literal.TrimEnd('m', 'M'), out decimal dec))
                        throw new MergeException(exceptionMsg(TypeCode.Decimal, literal));
                    value = dec;
                    break;
                case TypeCode.Double:
                    if (!double.TryParse(literal.TrimEnd('d', 'D'), out double dbl))
                        throw new MergeException(exceptionMsg(TypeCode.Double, literal));
                    value = dbl;
                    break;
                case TypeCode.Int32:
                    if (!int.TryParse(literal.TrimEnd('i', 'I'), out int i))
                        throw new MergeException(exceptionMsg(TypeCode.Int32, literal));
                    value = i;
                    break;
                case TypeCode.Int64:
                    if (!long.TryParse(literal.TrimEnd('l', 'L'), out long l))
                        throw new MergeException(exceptionMsg(TypeCode.Int64, literal));
                    value = l;
                    break;
                case TypeCode.Single:
                    if (!Single.TryParse(literal.TrimEnd('f', 'F'), out Single s))
                        throw new MergeException(exceptionMsg(TypeCode.Single, literal));
                    value = s;
                    break;
                case TypeCode.Empty:
                default:
                    throw new MergeException($"attempted to parse numeric literal: {literal} type could not be determined");
            }

            return value;
        }
		#endregion
	}
}
