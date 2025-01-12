using System;
using System.IO;
using TinyRadius.NET.Dictionary;

namespace TinyRadius.Dictionary
{
    /// <summary>
    /// The default dictionary is a singleton object containing
    /// a dictionary in the memory that is filled on application
    /// startup using the default dictionary file from the
    /// root directory.
    /// </summary>
    public class DefaultDictionary : MemoryDictionary
    {
        private const string DICTIONARY_FOLDER_NAME = "Resource";
        private const string DICTIONARY_FILE_NAME = "DefaultDictionary";
        private static DefaultDictionary instance;

        /// <summary>
        /// Returns the singleton instance of this object.
        /// </summary>
        /// <returns>DefaultDictionary instance</returns>
        public static IDictionary GetDefaultDictionary()
        {
            return instance;
        }

        /// <summary>
        /// Make constructor private so that a DefaultDictionary
        /// cannot be constructed by other classes.
        /// </summary>
        private DefaultDictionary()
        {
        }

        /// <summary>
        /// Static constructor that creates the singleton instance of this object
        /// and parses the dictionary file.
        /// </summary>
        static DefaultDictionary()
        {
            try
            {
                instance = new DefaultDictionary();
                
                string mainPath = Path.Combine(AppContext.BaseDirectory, DICTIONARY_FOLDER_NAME, DICTIONARY_FILE_NAME);

                if (File.Exists(mainPath))
                {
                    using (FileStream stream = File.OpenRead(mainPath))
                    {
                        DictionaryParser.ParseDictionary(stream, instance);
                    }
                }
                else
                {
                    var assembly = typeof(DefaultDictionary).Assembly;
                    using (Stream? stream = assembly.GetManifestResourceStream(DICTIONARY_FILE_NAME))
                    {
                        if (stream == null)
                        {
                            throw new FileNotFoundException("Dictionary file could not be found in any location");
                        }
                        
                        DictionaryParser.ParseDictionary(stream, instance);
                    }
                }
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("Default dictionary unavailable", ex);
            }
        }
    }
}
