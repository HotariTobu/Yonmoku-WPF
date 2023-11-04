using System.Linq;
using System.Text.RegularExpressions;

namespace Yonmoku-WPF
{
    static class StringExtension
    {
        public static Regex BinaryPattern = new Regex(@"(.+?)\s(.+)", RegexOptions.Compiled | RegexOptions.Singleline);

        public static double[] SplitToDouble(this string value, string separator) => value.Split(separator).Select(x =>
        {
            if (double.TryParse(x, out double y))
            {
                return y;
            }
            else
            {
                return 0;
            }
        }).ToArray();
    }
}
