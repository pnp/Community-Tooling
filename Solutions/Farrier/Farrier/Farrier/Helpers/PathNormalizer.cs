using System;

namespace Farrier.Helpers;

public class PathNormalizer
{
	public static string Normalize(string path)
	{
		if (string.IsNullOrEmpty(path)) return path;
		char sep = System.IO.Path.DirectorySeparatorChar;
		// Replace both types of slashes with the OS separator
		return path.Replace('/', sep).Replace('\\', sep);
	}
}
