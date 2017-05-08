using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DistributedSharedMemory_VirtualBank.Client
{
    class NaturalComparer : IComparer<string>
    {
        private Regex regex = new Regex("\\d+$", RegexOptions.IgnoreCase);

        private string MatchEvaluator(Match m)
        {
            return Convert.ToInt32(m.Value).ToString("D10");
        }

        public int Compare(string x, string y)
        {
            x = regex.Replace(x, MatchEvaluator);
            y = regex.Replace(y, MatchEvaluator);

            return x.CompareTo(y);
        }
    }
}
