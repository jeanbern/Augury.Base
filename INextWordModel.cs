using System.Collections.Generic;

namespace Augury.Base
{
    public interface INextWordModel
    {
        IReadOnlyList<string> NextWord(IReadOnlyList<string> nGram);
    }
}
