using System;
using System.Diagnostics;
using System.IO;

namespace Rdr.Extensions
{
    public static class FileInfoExt
    {
        public static void Launch(this FileInfo file)
            => Launch(file, new ProcessStartInfo(file.FullName));

        public static void Launch(this FileInfo file, ProcessStartInfo pInfo)
        {
            if (file == null) { throw new ArgumentNullException(nameof(file)); }

            if (file.Exists)
            {
                Process.Start(pInfo);
            }
        }
    }
}
