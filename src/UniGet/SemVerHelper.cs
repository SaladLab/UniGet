using System.Collections.Generic;

namespace UniGet
{
    internal static class SemVerHelper
    {
        public static int GetSatisfiedVersionIndex(this SemVer.Range versionRange, IList<SemVer.Version> versions)
        {
            if (versions.Count == 0)
                return -1;

            var highest = (versionRange.ToString().Contains("*") ||
                           versionRange.ToString().Contains("x"));

            var selected = -1;
            for (int i = 0; i < versions.Count; i++)
            {
                if (versionRange.IsSatisfied(versions[i]))
                {
                    if (selected == -1)
                    {
                        selected = i;
                    }
                    else if (highest)
                    {
                        if (versions[selected] < versions[i])
                            selected = i;
                    }
                    else
                    {
                        if (versions[selected] > versions[i])
                            selected = i;
                    }
                }
            }

            return selected;
        }
    }
}
