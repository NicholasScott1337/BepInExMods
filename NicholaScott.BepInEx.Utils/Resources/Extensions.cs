using System.IO;
using System.Reflection;

namespace NicholaScott.BepInEx.Utils.Resources
{
    /// <summary>
    /// Contains all extension methods for resource related features.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Reads all bytes of the provided stream.
        /// </summary>
        /// <param name="inStream"></param>
        /// <returns>All bytes of the stream.</returns>
        public static byte[] ReadAllBytes(this Stream inStream)
        {
            if (inStream is MemoryStream stream)
                return stream.ToArray();

            using (var memoryStream = new MemoryStream())
            {
                inStream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}