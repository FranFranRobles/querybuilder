using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlKata
{
    public static class Helper
    {
        public static bool IsArray(object value)
        {
            if(value is string || value is byte[])
            {
                return false;
            }
            
            return value is IEnumerable;
        }

        /// <summary>
        /// Flat IEnumerable one level down
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static IEnumerable<object> Flatten(IEnumerable<object> array)
        {
            foreach (object item in array)
            {
                if (IsArray(item))
                {
                    foreach (object sub in (item as IEnumerable))
                    {
                        yield return sub;
                    }
                }
                else
                {
                    yield return item;
                }

            }
        }

        public static IEnumerable<object> FlattenDeep(IEnumerable<object> array)
        {
            return array.SelectMany(o => IsArray(o) ? FlattenDeep(o as IEnumerable<object>) : new[] { o });
        }

        public static IEnumerable<int> AllIndexesOf(string str, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                yield break;
            }

            int index = 0;

            do
            {
                index = str.IndexOf(value, index, StringComparison.Ordinal);

                if (index == -1)
                {
                    yield break;
                }

                yield return index;

            } while ((index += value.Length) < str.Length);
        }

        public static string ReplaceAll(string subject, string match, Func<int, string> callback)
        {
            if (string.IsNullOrWhiteSpace(subject) || !subject.Contains(match))
            {
                return subject;
            }

            string[] splitted = subject.Split(
                new[] { match },
                StringSplitOptions.None
            );

            return splitted.Skip(1)
                .Select((item, index) => callback(index) + item)
                .Aggregate(splitted.First(), (left, right) => left + right);
        }

        public static string JoinArray(string glue, IEnumerable array)
        {
            List<string> result = new List<string>();

            foreach (object item in array)
            {
                result.Add(item.ToString());
            }

            return string.Join(glue, result);
        }

        public static string ExpandParameters(string sql, string placeholder, object[] bindings)
        {
            return ReplaceAll(sql, placeholder, i =>
            {
                object parameter = bindings[i];

                if (IsArray(parameter))
                {
                    int count = EnumerableCount(parameter as IEnumerable);
                    return string.Join(",", placeholder.Repeat(count));
                }

                return placeholder.ToString();
            });
        }

        public static int EnumerableCount(IEnumerable obj)
        {
            int count = 0;

            foreach (object item in obj)
            {
                count++;
            }

            return count;
        }

        public static List<string> ExpandExpression(string expression)
        {
            string regex = @"^(?:\w+\.){1,2}{(.*)}";
            Match match = Regex.Match(expression, regex);

            if (!match.Success)
            {
                // we did not found a match return the string as is.
                return new List<string> { expression };
            }

            string table = expression.Substring(0, expression.IndexOf(".{"));

            string captures = match.Groups[1].Value;

            List<string> cols = Regex.Split(captures, @"\s*,\s*")
                .Select(x => $"{table}.{x.Trim()}")
                .ToList();

            return cols;
        }

        public static IEnumerable<string> Repeat(this string str, int count)
        {
            return Enumerable.Repeat(str, count);
        }
        
        public static string ReplaceIdentifierUnlessEscaped(this string input, string escapeCharacter, string identifier, string newIdentifier)
        {
            //Replace standard, non-escaped identifiers first
            Regex nonEscapedRegex = new Regex($@"(?<!{Regex.Escape(escapeCharacter)}){Regex.Escape(identifier)}");
            string nonEscapedReplace = nonEscapedRegex.Replace(input, newIdentifier);
            
            //Then replace escaped identifiers, by just removing the escape character
            Regex escapedRegex = new Regex($@"{Regex.Escape(escapeCharacter)}{Regex.Escape(identifier)}");
            return escapedRegex.Replace(nonEscapedReplace, identifier);
        }
    }
}