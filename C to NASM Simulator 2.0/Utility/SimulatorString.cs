using System;
using System.Collections.Generic;
using System.Linq;

namespace C_to_NASM_Simulator_2._0.Utility
{
    public static class SimulatorString
    {
        public static bool HasEquatChar(this String str)
        {
            if ((str.Contains("+") || str.Contains("-") || str.Contains("*") || str.Contains("/"))  && !str.Contains("'"))
                    return true;
            return false;
        }
        public static bool IsVarDeclaration(this String str)
        {
            if(str.Substring(0, 3).Equals("int") || str.Substring(0, 5).Equals("float")
                || str.Substring(0, 6).Equals("double") || str.Substring(0, 4).Equals("bool")
                || str.Substring(0, 4).Equals("char"))
                return true;
            return false;
        }
        public static string InnerString(this String str, int startIndex, int endIndex)
        {
            string innerString = "";
            for (int i = startIndex; i < endIndex; i++)
                innerString += str.ElementAt(i);
            return innerString;
        }
        public static Array NumbsInArr(this String str, char separator, char delimiter)
        {
            List<int> intNumbs = new List<int>();
            List<double> realNumbs = new List<double>();
            int _delimiterStart = str.IndexOf(delimiter);
            char reversedDelimiter = ReverseDelimiter(delimiter);
            int _delimiterEnd = str.LastIndexOf(reversedDelimiter);
            string unparsedNumb = "";
            for (int i = _delimiterStart; i <= _delimiterEnd; i++)
            {
                if (Char.IsDigit(str.ElementAt(i)) || str.ElementAt(i).Equals('.'))
                    unparsedNumb += str.ElementAt(i);
                if (str.ElementAt(i).Equals(separator) || str.ElementAt(i).Equals(reversedDelimiter))
                {
                    if (String.IsNullOrEmpty(unparsedNumb)) return null;
                    if (unparsedNumb.Contains('.'))
                    {
                        realNumbs.Add(double.Parse(unparsedNumb, System.Globalization.CultureInfo.InvariantCulture));
                        unparsedNumb = "";
                        continue;
                    }
                    realNumbs.Add(double.Parse(unparsedNumb));
                    intNumbs.Add(int.Parse(unparsedNumb));
                    unparsedNumb = "";
                }
            }
            if (realNumbs.Count > intNumbs.Count)
                return realNumbs.ToArray();
            return intNumbs.ToArray();
            //return realNumbs.Count == 0 ? intNumbs.ToArray() : realNumbs.ToArray(); 
        }
        public static Array CharsInArr(this String str, char separator, char delimiter)
        {
            List<char> chars = new List<char>();
            int _delimiterStart = str.IndexOf(delimiter);
            char reversedDelimiter = ReverseDelimiter(delimiter);
            int _delimiterEnd = str.LastIndexOf(reversedDelimiter);
            if (_delimiterEnd - _delimiterStart < 2) return null;
            string buffer = "";
            for (int i = _delimiterStart; i <= _delimiterEnd; i++)
            {
                buffer += str.ElementAt(i);
                if (str.ElementAt(i).Equals(separator) || str.ElementAt(i).Equals(reversedDelimiter))
                {
                    string oneChar = "";
                    if (buffer.Contains("'"))
                    {
                        if (!buffer.Contains(reversedDelimiter))
                            oneChar = buffer.InnerString(buffer.IndexOf("'") + 1, buffer.LastIndexOf(separator) - 1);
                        else
                            oneChar = buffer.InnerString(buffer.IndexOf("'") + 1, buffer.LastIndexOf(reversedDelimiter) - 1);
                    }
                    else if (buffer.Contains(delimiter))
                    {
                        oneChar = buffer.InnerString(buffer.IndexOf("{") + 1, buffer.LastIndexOf(separator) - 1);
                        char asciiChar = (char)int.Parse(oneChar);
                        oneChar = "" + asciiChar;
                    }
                    else if (buffer.Contains(reversedDelimiter))
                    {
                        oneChar = buffer.InnerString(buffer.IndexOf(" ") + 1, buffer.LastIndexOf(reversedDelimiter));
                        char asciiChar = (char)int.Parse(oneChar);
                        oneChar = "" + asciiChar;
                    }
                    chars.Add(Char.Parse(oneChar));
                    buffer = "";
                }
            }

            return chars.ToArray();
        }
        public static int CharCounter(this String str, char ch)
        {
            int count = 0;
            foreach (char _char in str)
                if (_char.Equals(ch))
                    count++;
            return count;
        }
        private static char ReverseDelimiter(char _dlmt)
        {
            switch (_dlmt)
            {
                case '{':
                    return '}';
                case '(':
                    return ')';
                case '[':
                    return ']';
                case '<':
                    return '>';
                default:
                    return Char.MinValue;
            }
        }
    }
}
