using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rdr.Common;

namespace Rdr
{
    public interface IRepo
    {
        string FilePath { get; }

        Task<IReadOnlyList<Uri>> LoadAsync();
    }

    public class TxtRepo : IRepo
    {
        private readonly string _filePath = string.Empty;
        public string FilePath => _filePath;

        public TxtRepo(string filePath)
        {
            if (String.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _filePath = filePath;
        }
        
        public async Task<IReadOnlyList<Uri>> LoadAsync()
        {
            var feeds = new List<Uri>();

            FileStream fsAsync = null;

            try
            {
                fsAsync = new FileStream(FilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None,
                    4096,
                    useAsync: true);
                
                using (StreamReader sr = new StreamReader(fsAsync))
                {
                    fsAsync = null;

                    string line = string.Empty;

                    while ((line = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        if (line.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        
                        if (Uri.TryCreate(line, UriKind.Absolute, out Uri uri))
                        {
                            feeds.Add(uri);
                        }
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Log.LogException(ex, includeStackTrace: false);
            }
            finally
            {
                fsAsync?.Dispose();
            }

            return feeds.AsReadOnly();
        }
    }
}