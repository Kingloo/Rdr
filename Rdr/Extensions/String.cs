using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Rdr.Extensions
{
    public enum Result { None, Success, BeginningNotFound, BeginningNotUnique, EndingNotFound, EndingBeforeBeginning };

    public class FromBetweenResult
    {
        private Result _result = Result.None;
        public Result Result
        {
            get
            {
                return _result;
            }
        }

        private string _resultValue = string.Empty;
        public string ResultValue
        {
            get
            {
                return _resultValue;
            }
        }

        public FromBetweenResult(Result result, string resultValue)
        {
            _result = result;
            _resultValue = resultValue;
        }
    }

    public static class StringExt
    {
        public static bool ContainsExt(this string target, string toFind, StringComparison comparison)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            return (target.IndexOf(toFind, comparison) > -1);
        }

        public static string RemoveNewLines(this string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            string toReturn = value;

            if (toReturn.Contains("\r\n"))
            {
                toReturn = toReturn.Replace("\r\n", " ");
            }

            if (toReturn.Contains("\r"))
            {
                toReturn = toReturn.Replace("\r", " ");
            }

            if (toReturn.Contains("\n"))
            {
                toReturn = toReturn.Replace("\n", " ");
            }

            if (toReturn.Contains(Environment.NewLine))
            {
                toReturn = toReturn.Replace(Environment.NewLine, " ");
            }

            return toReturn;
        }

        public static FromBetweenResult FromBetween(this string whole, string beginning, string ending)
        {
            if (whole == null) throw new ArgumentNullException(nameof(whole));
            if (beginning == null) throw new ArgumentNullException(nameof(beginning));
            if (ending == null) throw new ArgumentNullException(nameof(ending));

            if (whole.Contains(beginning) == false)
            {
                return new FromBetweenResult(Result.BeginningNotFound, string.Empty);
            }

            int firstAppearanceOfBeginning = whole.IndexOf(beginning, StringComparison.OrdinalIgnoreCase);
            int lastAppearanceOfBeginning = whole.LastIndexOf(beginning, StringComparison.OrdinalIgnoreCase);

            if (firstAppearanceOfBeginning != lastAppearanceOfBeginning)
            {
                return new FromBetweenResult(Result.BeginningNotUnique, string.Empty);
            }

            int indexOfBeginning = firstAppearanceOfBeginning;

            if (whole.Contains(ending) == false)
            {
                return new FromBetweenResult(Result.EndingNotFound, string.Empty);
            }

            int indexOfEnding = 0;

            try
            {
                indexOfEnding = whole.IndexOf(ending, indexOfBeginning, StringComparison.OrdinalIgnoreCase);
            }
            catch (ArgumentOutOfRangeException)
            {
                return new FromBetweenResult(Result.EndingBeforeBeginning, string.Empty);
            }

            int indexOfResult = indexOfBeginning + beginning.Length;
            int lengthOfResult = indexOfEnding - indexOfResult;

            string result = whole.Substring(indexOfResult, lengthOfResult);

            return new FromBetweenResult(Result.Success, result);
        }

        public static string RemoveUnicodeCategories(this string self, IEnumerable<UnicodeCategory> categories)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (categories == null) throw new ArgumentNullException(nameof(categories));

            StringBuilder sb = new StringBuilder();

            foreach (char c in self)
            {
                if (IsCharInCatergories(c, categories) == false)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private static bool IsCharInCatergories(char c, IEnumerable<UnicodeCategory> categories)
        {
            if (categories == null) throw new ArgumentNullException(nameof(categories));

            foreach (UnicodeCategory category in categories)
            {
                if (Char.GetUnicodeCategory(c) == category)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
