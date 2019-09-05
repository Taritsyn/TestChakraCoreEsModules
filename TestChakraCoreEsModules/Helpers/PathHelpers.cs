using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace TestChakraCoreEsModules.Helpers
{
	/// <summary>
	/// Path helpers
	/// </summary>
	internal static class PathHelpers
	{
		/// <summary>
		/// Characters used to separate directory levels in a path string
		/// </summary>
		private static char[] _directorySeparatorChars = new[] { '/', '\\' };

		/// <summary>
		/// Regular expression for working with the URI schemes or drive letters
		/// </summary>
		private static Regex _schemeOrDriveLetterRegex = new Regex(@"^[a-zA-Z]+:");


		/// <summary>
		/// Normalizes a path
		/// </summary>
		/// <param name="path">Path</param>
		/// <returns>Normalized path</returns>
		public static string NormalizePath(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			if (string.IsNullOrWhiteSpace(path))
			{
				return path;
			}

			string[] pathSegments = path.Split(_directorySeparatorChars);
			int pathSegmentCount = pathSegments.Length;
			if (pathSegmentCount == 0)
			{
				return path;
			}

			var resultPathSegments = new List<string>(pathSegmentCount);

			for (int pathSegmentIndex = 0; pathSegmentIndex < pathSegmentCount; pathSegmentIndex++)
			{
				string pathSegment = pathSegments[pathSegmentIndex];

				switch (pathSegment)
				{
					case "..":
						int resultPathSegmentCount = resultPathSegments.Count;
						int resultPathSegmentLastIndex = resultPathSegmentCount - 1;

						if (resultPathSegmentCount == 0 || resultPathSegments[resultPathSegmentLastIndex] == "..")
						{
							resultPathSegments.Add(pathSegment);
						}
						else
						{
							resultPathSegments.RemoveAt(resultPathSegmentLastIndex);
						}
						break;

					case ".":
						break;

					default:
						resultPathSegments.Add(pathSegment);
						break;
				}
			}

			string resultPath = string.Join("/", resultPathSegments);
			resultPathSegments.Clear();

			return resultPath;
		}

		/// <summary>
		/// Gets a boolean value indicating whether the specified path is absolute
		/// </summary>
		/// <param name="path">The path to check</param>
		/// <returns>true if path is an absolute path; otherwise, false</returns>
		public static bool IsAbsolutePath(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			bool isAbsolutePath = path.StartsWith("/")
				|| path.StartsWith(@"\")
				|| _schemeOrDriveLetterRegex.IsMatch(path)
				;

			return isAbsolutePath;
		}

		/// <summary>
		/// Transforms a relative path to absolute
		/// </summary>
		/// <param name="basePath">The base path</param>
		/// <param name="path">The path</param>
		/// <returns>The absolute path</returns>
		public static string MakeAbsolutePath(string basePath, string path)
		{
			if (basePath == null)
			{
				throw new ArgumentNullException(nameof(basePath));
			}

			if (path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			string resultPath;

			if (string.IsNullOrWhiteSpace(basePath) || IsAbsolutePath(path))
			{
				resultPath = path;
			}
			else
			{
				string baseDirPath = Path.GetDirectoryName(basePath) ?? string.Empty;
				resultPath = baseDirPath + "/" + path;
			}

			resultPath = NormalizePath(resultPath);

			return resultPath;
		}
	}
}