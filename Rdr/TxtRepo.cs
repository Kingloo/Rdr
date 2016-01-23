using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rdr
{
    public interface IRepo
    {
        string FilePath { get; set; }

        Task<IEnumerable<string>> LoadAsync();
    }

    public class TxtRepo : IRepo
    {
        private string _filePath = string.Empty;
        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                _filePath = value;
            }
        }

        public TxtRepo(string filePath)
        {
            if (String.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath was null or whitespace");

            FilePath = filePath;
        }
        
        public async Task<IEnumerable<string>> LoadAsync()
        {
            List<string> feeds = new List<string>();

            try
            {
                using (FileStream fsAsync = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None, 1024, true))
                using (StreamReader sr = new StreamReader(fsAsync))
                {
                    string line = string.Empty;

                    while ((line = await sr.ReadLineAsync()) != null)
                    {
                        if (line.StartsWith("#") == false)
                        {
                            Uri tmp = null;
                            if (Uri.TryCreate(line, UriKind.Absolute, out tmp))
                            {
                                feeds.Add(line);
                            }
                        }
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                Utils.LogException(e);
            }

            return feeds;
        }
    }
}