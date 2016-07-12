using System.Collections.Generic;

namespace Augury.Base
{
    public interface IPrefixLookup
    {
        IEnumerable<IWordSimilarityNode> PrefixLookup(string input, int maxResults);
    }
}
