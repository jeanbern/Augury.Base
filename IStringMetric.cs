namespace Augury.Base
{
    public interface IStringMetric
    {
        /// <summary>
        /// Computes the similarity between two strings. A higher value represents closer strings.
        /// </summary>
        /// <param name="x">The first of two strings to compare.</param>
        /// <param name="y">The second string to compare.</param>
        /// <returns>A value between 0 and 1 representing how similar the strings are.</returns>
        double Similarity(string x, string y);
    }
}
