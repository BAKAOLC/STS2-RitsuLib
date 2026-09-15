using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Godot;

namespace STS2RitsuLib.Ui.Files
{
    [SupportedOSPlatform("windows")]
    internal static partial class RitsuWindowsThumbnail
    {
        internal static unsafe Image? Load(string path)
        {
            var initialized = CoInitializeEx(0, 0);
            if (initialized < 0)
                return null;
            nint factory = 0;
            nint bitmap = 0;
            nint dc = 0;
            try
            {
                var id = new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b");
                if (ShCreateItemFromParsingName(path, 0, ref id, out factory) < 0 || factory == 0)
                    return null;
                var getImage = (delegate* unmanaged[Stdcall]<nint, Size, uint, nint*, int>)(*(nint**)factory)[3];
                if (getImage(factory, new() { Width = 160, Height = 160 }, 8, &bitmap) < 0 || bitmap == 0)
                    return null;
                if (GetObject(bitmap, Marshal.SizeOf<Bitmap>(), out var description) == 0 ||
                    description.Width is <= 0 or > 1024 || description.Height is <= 0 or > 1024)
                    return null;
                var width = description.Width;
                var height = description.Height;
                var bytes = new byte[checked(width * height * 4)];
                var info = new BitmapInfo
                {
                    HeaderSize = (uint)Marshal.SizeOf<BitmapInfo>(), Width = width, Height = -height,
                    Planes = 1, BitCount = 32,
                };
                dc = CreateCompatibleDC(0);
                if (dc == 0 || GetDiBits(dc, bitmap, 0, (uint)height, bytes, ref info, 0) != height)
                    return null;
                var hasAlpha = false;
                for (var i = 3; i < bytes.Length; i += 4)
                    hasAlpha |= bytes[i] != 0;
                for (var i = 0; i < bytes.Length; i += 4)
                {
                    (bytes[i], bytes[i + 2]) = (bytes[i + 2], bytes[i]);
                    if (!hasAlpha)
                        bytes[i + 3] = 255;
                    else if (bytes[i + 3] is > 0 and < 255)
                        for (var channel = 0; channel < 3; channel++)
                            bytes[i + channel] = (byte)Math.Min(255, bytes[i + channel] * 255 / bytes[i + 3]);
                }

                return Image.CreateFromData(width, height, false, Image.Format.Rgba8, bytes);
            }
            finally
            {
                if (dc != 0) _ = DeleteDc(dc);
                if (bitmap != 0) _ = DeleteObject(bitmap);
                if (factory != 0) Marshal.Release(factory);
                CoUninitialize();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Size
        {
            internal int Width;
            internal int Height;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Bitmap
        {
            internal int Type;
            internal int Width;
            internal int Height;
            internal int WidthBytes;
            internal ushort Planes;
            internal ushort BitsPixel;
            internal nint Bits;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfo
        {
            internal uint HeaderSize;
            internal int Width;
            internal int Height;
            internal ushort Planes;
            internal ushort BitCount;
            internal uint Compression;
            internal uint SizeImage;
            internal int XPelsPerMeter;
            internal int YPelsPerMeter;
            internal uint ColorsUsed;
            internal uint ColorsImportant;
        }

        [LibraryImport("ole32.dll")]
        private static partial int CoInitializeEx(nint reserved, uint concurrency);

        [LibraryImport("ole32.dll")]
        private static partial void CoUninitialize();

        [LibraryImport("shell32.dll", EntryPoint = "SHCreateItemFromParsingName",
            StringMarshalling = StringMarshalling.Utf16)]
        private static partial int ShCreateItemFromParsingName(string path, nint bindContext, ref Guid id,
            out nint factory);

        [LibraryImport("gdi32.dll", EntryPoint = "GetObjectW")]
        private static partial int GetObject(nint bitmap, int size, out Bitmap description);

        [LibraryImport("gdi32.dll")]
        private static partial nint CreateCompatibleDC(nint dc);

        [LibraryImport("gdi32.dll", EntryPoint = "GetDIBits")]
        private static partial int GetDiBits(nint dc, nint bitmap, uint start, uint count, [Out] byte[] bits,
            ref BitmapInfo info, uint usage);

        [LibraryImport("gdi32.dll", EntryPoint = "DeleteDC")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DeleteDc(nint dc);

        [LibraryImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DeleteObject(nint handle);
    }
}
