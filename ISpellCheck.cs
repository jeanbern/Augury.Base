using System.Collections.Generic;

namespace Augury.Base
{
    public interface ISpellCheck
    {
        IEnumerable<IWordSimilarityNode> Lookup(string input, int maxResults);
    }
}
