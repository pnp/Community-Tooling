using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace Farrier.Helpers
{
    static class FileHelper
    {
        public static string PathFromDepth(string path, int depth)
        {
            var parts = Path.GetFullPath(path).Split(Path.DirectorySeparatorChar);
            return String.Join(Path.DirectorySeparatorChar, parts.Reverse().Take(depth + 1).Reverse().ToArray());
        }
    }
}
