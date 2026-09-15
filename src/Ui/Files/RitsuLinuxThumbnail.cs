using System.Runtime.InteropServices;
using Godot;

namespace STS2RitsuLib.Ui.Files
{
    internal static partial class RitsuLinuxThumbnail
    {
        private const string Service = "org.freedesktop.thumbnails.Thumbnailer1";
        private static bool _serviceUnavailable;

        internal static async Task<Image?> LoadAsync(string path, CancellationToken cancellation)
        {
            var file = FileNewForPath(path);
            if (file == 0)
                return null;
            nint connection = 0;
            try
            {
                var cached = Lookup(file, out var mime);
                if (cached != null || _serviceUnavailable || string.IsNullOrEmpty(mime))
                    return cached;
                connection = BusGetSync(2, 0, out var error);
                FreeError(error);
                if (connection == 0)
                    return null;
                var uri = new Uri(path).AbsoluteUri;
                var parameters = VariantNewTuple([
                    VariantNewArray(0, [VariantNewString(uri)], 1), VariantNewArray(0, [VariantNewString(mime)], 1),
                    VariantNewString("large"), VariantNewString("foreground"), VariantNewUInt32(0),
                ], 5);
                var response = ConnectionCallSync(connection, Service, "/org/freedesktop/thumbnails/Thumbnailer1",
                    Service, "Queue", parameters, 0, 0, 1500, 0, out error);
                if (response == 0)
                {
                    if (error != 0)
                    {
                        var remote = ErrorGetRemoteError(error);
                        var name = Marshal.PtrToStringUTF8(remote);
                        _serviceUnavailable = name is "org.freedesktop.DBus.Error.ServiceUnknown" or
                            "org.freedesktop.DBus.Error.NameHasNoOwner";
                        Free(remote);
                    }

                    FreeError(error);
                    return null;
                }

                VariantUnref(response);
                FreeError(error);
                for (var attempt = 0; attempt < 20; attempt++)
                {
                    await Task.Delay(100, cancellation).ConfigureAwait(false);
                    cached = Lookup(file, out _);
                    if (cached != null)
                        return cached;
                }

                return null;
            }
            finally
            {
                if (connection != 0) ObjectUnref(connection);
                ObjectUnref(file);
            }
        }

        private static Image? Lookup(nint file, out string? mime)
        {
            mime = null;
            var info = FileQueryInfo(file, "thumbnail::*,standard::content-type", 0, 0, out var error);
            FreeError(error);
            if (info == 0)
                return null;
            try
            {
                var content = FileInfoGetString(info, "standard::content-type");
                if (content != 0)
                {
                    var type = ContentTypeGetMimeType(content);
                    mime = Marshal.PtrToStringUTF8(type);
                    Free(type);
                }

                foreach (var suffix in new[] { "-large", "", "-xlarge", "-xxlarge" })
                {
                    var valid = "thumbnail::is-valid" + suffix;
                    var location = "thumbnail::path" + suffix;
                    if (FileInfoHasAttribute(info, valid) == 0 || FileInfoGetBoolean(info, valid) == 0 ||
                        FileInfoHasAttribute(info, location) == 0)
                        continue;
                    var path = Marshal.PtrToStringUTF8(FileInfoGetByteString(info, location));
                    if (path != null && File.Exists(path))
                        return RitsuDesktopThumbnail.LoadImage(path);
                }

                return null;
            }
            finally
            {
                ObjectUnref(info);
            }
        }

        private static void FreeError(nint error)
        {
            if (error != 0)
                ErrorFree(error);
        }

        [LibraryImport("libgio-2.0.so.0", EntryPoint = "g_file_new_for_path",
            StringMarshalling = StringMarshalling.Utf8)]
        private static partial nint FileNewForPath(string path);

        [LibraryImport("libgio-2.0.so.0", EntryPoint = "g_file_query_info", StringMarshalling = StringMarshalling.Utf8)]
        private static partial nint FileQueryInfo(nint file, string attributes,
            int flags, nint cancellable, out nint error);

        [LibraryImport("libgio-2.0.so.0", EntryPoint = "g_file_info_has_attribute",
            StringMarshalling = StringMarshalling.Utf8)]
        private static partial int
            FileInfoHasAttribute(nint info, string attribute);

        [LibraryImport("libgio-2.0.so.0", EntryPoint = "g_file_info_get_attribute_boolean",
            StringMarshalling = StringMarshalling.Utf8)]
        private static partial int FileInfoGetBoolean(nint info, string attribute);

        [LibraryImport("libgio-2.0.so.0", EntryPoint = "g_file_info_get_attribute_string",
            StringMarshalling = StringMarshalling.Utf8)]
        private static partial nint FileInfoGetString(nint info, string attribute);

        [LibraryImport("libgio-2.0.so.0", EntryPoint = "g_file_info_get_attribute_byte_string",
            StringMarshalling = StringMarshalling.Utf8)]
        private static partial nint FileInfoGetByteString(nint info,
            string attribute);

        [LibraryImport("libgio-2.0.so.0", EntryPoint = "g_content_type_get_mime_type")]
        private static partial nint ContentTypeGetMimeType(nint contentType);

        [LibraryImport("libgio-2.0.so.0", EntryPoint = "g_bus_get_sync")]
        private static partial nint BusGetSync(int busType, nint cancellable, out nint error);

        [LibraryImport("libgio-2.0.so.0", EntryPoint = "g_dbus_connection_call_sync",
            StringMarshalling = StringMarshalling.Utf8)]
        private static partial nint ConnectionCallSync(nint connection,
            string destination, string path,
            string interfaceName,
            string method,
            nint parameters, nint replyType, int flags, int timeout, nint cancellable, out nint error);

        [LibraryImport("libgio-2.0.so.0", EntryPoint = "g_dbus_error_get_remote_error")]
        private static partial nint ErrorGetRemoteError(nint error);

        [LibraryImport("libglib-2.0.so.0", EntryPoint = "g_variant_new_array")]
        private static partial nint VariantNewArray(nint type, [In] nint[] children, nuint count);

        [LibraryImport("libglib-2.0.so.0", EntryPoint = "g_variant_new_string",
            StringMarshalling = StringMarshalling.Utf8)]
        private static partial nint VariantNewString(string value);

        [LibraryImport("libglib-2.0.so.0", EntryPoint = "g_variant_new_uint32")]
        private static partial nint VariantNewUInt32(uint value);

        [LibraryImport("libglib-2.0.so.0", EntryPoint = "g_variant_new_tuple")]
        private static partial nint VariantNewTuple([In] nint[] children, nuint count);

        [LibraryImport("libglib-2.0.so.0", EntryPoint = "g_variant_unref")]
        private static partial void VariantUnref(nint value);

        [LibraryImport("libglib-2.0.so.0", EntryPoint = "g_error_free")]
        private static partial void ErrorFree(nint error);

        [LibraryImport("libglib-2.0.so.0", EntryPoint = "g_free")]
        private static partial void Free(nint value);

        [LibraryImport("libgobject-2.0.so.0", EntryPoint = "g_object_unref")]
        private static partial void ObjectUnref(nint value);
    }
}
