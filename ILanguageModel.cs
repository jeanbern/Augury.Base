using System.Collections.Generic;

namespace Augury.Base
{
    public interface ILanguageModel
    {
        double Evaluate(IReadOnlyList<string> nGram);
    }
}
