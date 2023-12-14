﻿using System.Security.Cryptography;
using System.Text;

namespace BackupCLI.FileSystem;

public static class FileSystemUtils
{
    public static readonly EnumerationOptions TopLevelOptions = new() { IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive };
    public static readonly EnumerationOptions RecursiveOptions = new() { IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true };
    private static readonly MD5 Hasher = MD5.Create();

    /// <summary>
    /// Checks whether two directories are direct ancestors of each other.
    /// <example>
    /// <code>AreDirectAncestors(new("C:\Windows"), new("C:\Windows\System32"))</code>
    /// => <c>true</c>
    /// </example>
    /// </summary>
    public static bool AreDirectAncestors(DirectoryInfo left, DirectoryInfo right)
    {
        left = FromPath(left.FullName.ToLower());
        right = FromPath(right.FullName.ToLower());

        if (left.FullName == right.FullName) return true;

        DirectoryInfo _left = left;
        while (_left.Parent is not null)
        {
            if (_left.Parent.FullName == right.FullName) return true;
            _left = _left.Parent;
        }

        DirectoryInfo _right = right;
        while (_right.Parent is not null)
        {
            if (_right.Parent.FullName == left.FullName) return true;
            _right = _right.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks whether two files are identical based on their properties and content.
    /// </summary>
    public static bool AreIdentical(FileInfo left, FileInfo right)
    {
        // early return based on file metadata
        if (!left.Exists || !right.Exists) return false;
        if (left.Length == right.Length && left.LastWriteTime == right.LastWriteTime) return true;

        // the word size on a 64bit processor
        const int size = sizeof(long);

        int iterations = (int)Math.Ceiling((double)left.Length / size);

        using var leftStream = left.OpenRead();
        using var rightStream = right.OpenRead();

        byte[] leftBuffer = new byte[size];
        byte[] rightBuffer = new byte[size];

        // reads the files chunk by chunk and compares them
        for (int i = 0; i < iterations; i++)
        {
            leftStream.Read(leftBuffer, 0, size);
            rightStream.Read(rightBuffer, 0, size);

            // early return if the two chunks are not equal - this is much faster than unconditionally hashing the entire file
            if (BitConverter.ToInt64(leftBuffer, 0) != BitConverter.ToInt64(rightBuffer, 0))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Adds or removes a trailing slash to make paths consistent.
    /// </summary>
    public static string NormalizePath(string path, bool isDir)
        => Path.TrimEndingDirectorySeparator(path) + (isDir ? Path.DirectorySeparatorChar : "");

    /// <returns><see cref="DirectoryInfo"/> with a normalized path in the .FullName property.</returns>
    public static DirectoryInfo FromPath(string path)
        => new (NormalizePath(path, true));

    /// <returns>MD5 hash of the specified path, normalizing it first</returns>
    public static string GetHashedPath(string path, bool isDir)
        => Convert.ToHexString(Hasher.ComputeHash(Encoding.ASCII.GetBytes(NormalizePath(path.ToLower(), isDir))));

    /// <returns>A dictionary of path:hash mapping for the specified <paramref name="paths"/></returns>
    public static Dictionary<string, string> GetHashedPaths(IEnumerable<string> paths)
        => paths.ToDictionary(p => p, p => GetHashedPath(p, Path.EndsInDirectorySeparator(p)));

    /// <returns>Names of direct subdirectories, ordered by directory creation time. Inaccessible subdirectories are ignored.</returns>
    public static List<string> GetOrderedSubdirectories(DirectoryInfo dir)
        => dir
            .EnumerateDirectories("*", TopLevelOptions)
            .OrderBy(d => d.CreationTime)
            .Select(d => d.Name)
            .ToList();
}
