using System;
using System.Linq;
using System.Text;

namespace HatTrick.Text.Templating
{
    public static class StringInterpolation
    {
        #region interpolate via callback
        public static string Run(string baseText, Func<string, string> valueProvider)
        {
            int idx = -1;
            int length = baseText.Length;
            char ch = '\0';
            bool isSeeking = true;
            string token = string.Empty;
            string injectionValue = null;
            StringBuilder sb = new StringBuilder((int)(length * 1.1));

            while (idx < (length - 1))
            {
                ch = baseText[++idx];

                if (ch == '{') //token start...
                {
                    if (((idx + 1) < length) && (baseText[idx + 1] == '{')) // brace escape --> {{...
                    {
                        continue;
                    }
                    else if (((idx - 1) > 0) && (baseText[idx - 1] == '{'))
                    {
                        sb.Append(ch); // first was escape... append the second...
                        continue;
                    }
                    else
                    {
                        isSeeking = false; // token opened.. begin parse token (no longer seeking a token)...
                        continue;
                    }
                }
                else if (ch == '}') //token end...
                {
                    if (((idx + 1) < length) && (baseText[idx + 1] == '}')) // brace escape --> }}...
                    {
                        continue;
                    }
                    else if (baseText[idx - 1] == '}')
                    {
                        sb.Append(ch); //first was escaped... append the second...
                        continue;
                    }
                    else
                    {
                        injectionValue = valueProvider(token); //pass token to value provider to resolve key value...
                        if (injectionValue != null)
                        {
                            if (injectionValue.Contains('{'))
                            {
                                string subInjectionValue = StringInterpolation.Run(injectionValue, valueProvider);
                                sb.Append(subInjectionValue);
                            }
                            else
                            {
                                sb.Append(injectionValue);
                            }
                        }
                        else //TODO: JRod, ignore and assume static text, or throw exception ???
                        {
                            //assume token is static text .. rewrap with braces and inject...
                            sb.Append('{');
                            sb.Append(token);
                            sb.Append('}');
                        }
                        token = string.Empty;
                        isSeeking = true;
                    }
                }
                else if (!isSeeking)
                {
                    token += ch; // if inside un-escaped brace.. append char to injection token...
                    continue;
                }
                else
                {
                    sb.Append(ch); // still seeking token, append char as static output...
                    continue;
                }
            }
            return sb.ToString();
        }
        #endregion

        #region interpolate via reflection
        public static string Run(string baseText, object values)
        {
            Func<string, string> func = (token) =>
            {
                string val = Reflection.ReflectionHelper.Expression.ReflectItem(values, token).ToString();
                return val;
            };

            string output = Run(baseText, func);

            return output;
        }
        #endregion
    }
}
