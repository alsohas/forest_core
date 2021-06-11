﻿using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace forest_core.Utils
{
    internal class BinaryIO
    {
        private BinaryIO()
        {
        }

        /// <summary>
        ///     Writes the given object instance to a binary file.
        ///     <para>Object type (and all child types) must be decorated with the [Serializable] attribute.</para>
        ///     <para>
        ///         To prevent a variable from being serialized, decorate it with the [NonSerialized] attribute; cannot be
        ///         applied to properties.
        ///     </para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the binary file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the binary file.</param>
        /// <param name="append">
        ///     If false the file will be overwritten if it already exists. If true the contents will be appended
        ///     to the file.
        /// </param>
        public static void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, objectToWrite);
            stream.Close();
        }

        /// <summary>
        ///     Reads an object instance from a binary file.
        /// </summary>
        /// <typeparam name="T">The type of object to read from the binary file.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the binary file.</returns>
        public static T ReadFromBinaryFile<T>(string filePath)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var obj = (T) formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }
    }
}