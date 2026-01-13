/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 

using Google.Cloud.Storage.V1;

namespace Ubriaco.Util
{
    public class StorageUtil
    {
        private static string? BucketName;
        public static void Init(string fileName = "google-cloud.json", string bucketName = "default")
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", fileName);
            BucketName = bucketName;
        }

        public static string? Put(Stream st, string? ident, string mimeType = "application/unknown", string? originalName = null)
        {
            if (ident == null)
                ident = Guid.NewGuid().ToString();
            try
            {
                using (var client = StorageClient.Create())
                {
                    var obj = client.UploadObject(BucketName, ident, null, st);
                    return ident;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("StorageUtil: Put: " + e.Message);
                return null;
            }
        }

        public static async Task<string?> PutAsync(Stream st, string? ident, string mimeType = "application/unknown", string? originalName = null)
        {
            if (ident == null)
                ident = Guid.NewGuid().ToString();
            try
            {
                using (var client = StorageClient.Create())
                {
                    var obj = await client.UploadObjectAsync(BucketName, ident, null, st);
                    return ident;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("StorageUtil: PutAsync: " + e.Message);
                return null;
            }
        }

        public static byte[]? GetItem(string ident)
        {
            try
            {
                using (var client = StorageClient.Create())
                {
                    using (var ms = new MemoryStream())
                    {
                        var obj = client.DownloadObject(BucketName, ident, ms);
                        return ms.ToArray();
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<byte[]?> GetItemAsync(string ident)
        {
            try
            {
                using (var client = StorageClient.Create())
                {
                    using (var ms = new MemoryStream())
                    {
                        var obj = await client.DownloadObjectAsync(BucketName, ident, ms);
                        return ms.ToArray();
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static bool Get(string ident, Stream st)
        {
            try
            {
                using (var client = StorageClient.Create())
                {
                    var obj = client.DownloadObject(BucketName, ident, st);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> GetAsync(string ident, Stream st)
        {
            try
            {
                using (var client = StorageClient.Create())
                {
                    var obj = await client.DownloadObjectAsync(BucketName, ident, st);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool Delete(string ident)
        {
            try
            {
                using (var client = StorageClient.Create())
                {
                    client.DeleteObject(BucketName, ident);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> DeleteAsync(string ident)
        {
            try
            {
                using (var client = StorageClient.Create())
                {
                    await client.DeleteObjectAsync(BucketName, ident);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string GetMimeTypeFromExt(string ext)
        {
            ext = ext.ToLower();
            if (ext == "jpg")
                return "image/jpeg";
            else if (ext == "png" || ext == "bmp" || ext == "tiff")
                return "image/" + ext;
            else if (ext == "pdf")
                return "application/" + ext;
            else if (ext == "csv")
                return "text/csv";
            else
                return "application/unknown";
        }
    }
}
