using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rdr.Common;

namespace Rdr
{
    public interface IRepo
    {
        FileInfo File { get; }

        Task<IReadOnlyList<Uri>> LoadAsync();
    }

    public class TxtRepo : IRepo
    {
        private readonly FileInfo _file = null;
        public FileInfo File => _file;

        public TxtRepo(FileInfo file)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }
        
        public async Task<IReadOnlyList<Uri>> LoadAsync()
        {
            var feeds = new List<Uri>();

            FileStream fsAsync = null;

            try
            {
                fsAsync = new FileStream(
                    File.FullName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None,
                    4096,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                
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