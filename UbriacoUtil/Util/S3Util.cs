/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/



using Amazon.S3;
using Amazon.S3.Model;

namespace Ubriaco.Util
{
    public static class S3Util
    {
        public static string? BucketName;

        public static void Init(string profileName, string bucketName, string region = "us-east-1")
        {
            Environment.SetEnvironmentVariable("AWS_PROFILE", profileName);
            Environment.SetEnvironmentVariable("AWS_REGION", region);
            BucketName = bucketName;
        }

        public static void ConfigureAPIKey(string key, string secret, string bucketName, string region = "us-east-1")
        {
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", key);
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", secret);
            Environment.SetEnvironmentVariable("AWS_REGION", region);
            BucketName = bucketName;
        }

        public static void SetCredentialsFile(string path)
        {
            Environment.SetEnvironmentVariable("AWS_SHARED_CREDENTIALS_FILE", path);
            var contents = File.ReadAllText(path);
            var lines = contents.Split('\n');
            foreach (var l in lines)
            {
                var line = l.Replace("\r", "");
                if (line.Contains("="))
                {
                    var p = line.Split('=');
                    var vr = p[0].Trim().ToLower();
                    var vl = p[1].Trim();
                    if (vr == "aws_access_key_id")
                        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", vl);
                    else if (vr == "aws_secret_access_key")
                        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", vl);
                }
            }
        }

        public static bool Put(string key, Stream stream)
        {
            var task = Task.Run(async () =>
            {
                return await PutAsync(key, stream);
            });
            task.Wait();
            return task.Result;
        }

        public static async Task<bool> PutAsync(string key, Stream stream)
        {
            var s3Client = new AmazonS3Client();

            PutObjectResponse resp = await s3Client.PutObjectAsync(new PutObjectRequest()
            {
                BucketName = BucketName,
                Key = key,
                InputStream = stream
            });

            return (resp.HttpStatusCode == System.Net.HttpStatusCode.OK || resp.HttpStatusCode == System.Net.HttpStatusCode.Accepted || resp.HttpStatusCode == System.Net.HttpStatusCode.Created);
        }

        public static Stream Get(string key)
        {
            var task = Task.Run(async () =>
            {
                return await GetAsync(key);
            });
            task.Wait();
            return task.Result;
        }

        public static async Task<Stream> GetAsync(string key)
        {
            if (key == null || key.Length == 0)
                return null;
            var s3Client = new AmazonS3Client();

            try
            {
                GetObjectResponse resp = await s3Client.GetObjectAsync(BucketName, key);
                if (resp.HttpStatusCode == System.Net.HttpStatusCode.OK || resp.HttpStatusCode == System.Net.HttpStatusCode.Accepted || resp.HttpStatusCode == System.Net.HttpStatusCode.Created)
                {
                    return resp.ResponseStream;
                }
                else
                    return null;
            }
            catch
            {
                return null;
            }

        }

        public static async Task<bool> Delete(string key)
        {
            var s3Client = new AmazonS3Client();

            DeleteObjectResponse resp = await s3Client.DeleteObjectAsync(new DeleteObjectRequest()
            {
                BucketName = BucketName,
                Key = key
            });

            return (resp.HttpStatusCode == System.Net.HttpStatusCode.OK || resp.HttpStatusCode == System.Net.HttpStatusCode.Accepted || resp.HttpStatusCode == System.Net.HttpStatusCode.Created);
        }

        public static Task<bool> DeleteAsync(string key)
        {
            return Delete(key);
        }

        public static async Task<List<string>> List(string path)
        {
            if (path == null)
                return null;
            var s3Client = new AmazonS3Client();

            try
            {
                ListObjectsRequest req = new ListObjectsRequest()
                {
                    BucketName = BucketName,
                    Prefix = path
                };

                ListObjectsResponse resp;
                List<string> list = new List<string>();
                do
                {
                    resp = await s3Client.ListObjectsAsync(req);
                    foreach (var i in resp.S3Objects)
                    {
                        list.Add(i.Key);
                    }
                    req.Marker = resp.NextMarker;
                }
                while (resp.IsTruncated != null && resp.IsTruncated.Value);
                return list;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
