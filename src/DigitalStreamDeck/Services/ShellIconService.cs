using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DigitalStreamDeck.Services;

public sealed class ShellIconService
{
    private const uint ShgfiIcon = 0x100;
    private const uint ShgfiLargeIcon = 0x0;
    private static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object CacheLock = new();

    public ImageSource? GetIcon(string? path)
    {
        var fullPath = NormalizePath(path);
        if (fullPath is null)
        {
            return null;
        }

        lock (CacheLock)
        {
            if (!Cache.TryGetValue(fullPath, out var icon))
            {
                icon = ExtractIcon(fullPath);
                Cache[fullPath] = icon;
            }

            return icon;
        }
    }

    public void Clear(string path)
    {
        var fullPath = NormalizePath(path);
        if (fullPath is not null)
        {
            lock (CacheLock)
            {
                Cache.Remove(fullPath);
            }
        }
    }

    private static string? NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or SecurityException)
        {
            return null;
        }
    }

    private static ImageSource? ExtractIcon(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var info = new ShellFileInfo();
        try
        {
            if (SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf<ShellFileInfo>(), ShgfiIcon | ShgfiLargeIcon) == IntPtr.Zero || info.Icon == IntPtr.Zero)
            {
                return null;
            }

            var image = Imaging.CreateBitmapSourceFromHIcon(info.Icon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            image.Freeze();
            return image;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or Win32Exception or ExternalException or InvalidOperationException or ArgumentException)
        {
            return null;
        }
        finally
        {
            if (info.Icon != IntPtr.Zero)
            {
                DestroyIcon(info.Icon);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShellFileInfo
    {
        public IntPtr Icon;
        public int IconIndex;
        public uint Attributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string TypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string path, uint fileAttributes, ref ShellFileInfo fileInfo, uint fileInfoSize, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);
}
