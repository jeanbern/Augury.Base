using System.IO;

namespace Augury.Base
{
    public interface ISerializer<T>
    {
        T Deserialize(Stream stream);
        void Serialize(Stream stream, T data);
    }
}
