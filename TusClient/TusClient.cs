using System;
using System.IO;
using System.Net;

namespace TusClient
{
    public class TusClient
    {
        // ***********************************************************************************************
        // Events
        public delegate void UploadingEvent(int bytesTransferred, int bytesTotal);
        public event UploadingEvent Uploading;

        public delegate void DownloadingEvent(int bytesTransferred, int bytesTotal);
        public event DownloadingEvent Downloading;
        // ***********************************************************************************************
        // Public
        //------------------------------------------------------------------------------------------------
        public string Create(string URL, System.IO.FileInfo file)
        {
            var client = new TusHTTPClient();
            var request = new TusHTTPRequest(URL);
            request.Method = "POST";
            request.AddHeader("Tus-Resumable", "1.0.0");
            request.AddHeader("Upload-Length", file.Length.ToString());
            request.AddHeader("Content-Length", "0");

            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                if (response.Headers.ContainsKey("Location"))
                {
                    return response.Headers["Location"];
                }
                else
                {
                    throw new Exception("Location Header Missing");
                }
                
            }
            else
            {
                throw new Exception("CreateFileInServer failed. " + response.ResponseString );
            }
        }
        //------------------------------------------------------------------------------------------------
        public void Upload(string URL, System.IO.FileInfo file)
        {

            var Offset = this.getFileOffset(URL);
            var client = new TusHTTPClient();
            System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider();
            int ChunkSize = (int) Math.Ceiling(0.5 * 1024.0 * 1024.0); //500kb

            using (var fs = new FileStream(file.FullName, FileMode.Open))
            {
                while (Offset < file.Length)
                {
                    fs.Seek(Offset, SeekOrigin.Begin);
                    byte[] buffer = new byte[ChunkSize];
                    var BytesRead = fs.Read(buffer, 0, ChunkSize);

                    Array.Resize(ref buffer, BytesRead);
                    var sha1hash = sha.ComputeHash(buffer);

                    var request = new TusHTTPRequest(URL);
                    request.Method = "PATCH";
                    request.AddHeader("Tus-Resumable", "1.0.0");
                    request.AddHeader("Upload-Offset", string.Format("{0}", Offset));
                    request.AddHeader("Upload-Checksum", "sha1 " + Convert.ToBase64String(sha1hash));
                    request.AddHeader("Content-Type", "application/offset+octet-stream");
                    request.BodyBytes = buffer;

                    request.Uploading += delegate(int bytesTransferred, int bytesTotal)
                    {
                        if (Uploading != null)
                            Uploading((int)Offset + bytesTransferred, (int)file.Length);
                    };

                    try
                    {
                        var response = client.PerformRequest(request);

                        if (response.StatusCode == HttpStatusCode.NoContent)
                        {
                            Offset += BytesRead;
                        }
                        else
                        {
                            throw new Exception("WriteFileInServer failed. " + response.ResponseString);
                        }
                    }
                    catch (IOException ex)
                    {
                        if (ex.InnerException.GetType() == typeof(System.Net.Sockets.SocketException))
                        {
                            var socketex = (System.Net.Sockets.SocketException) ex.InnerException;
                            if (socketex.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset)
                            {
                                // retry by continuing the while loop but get new offset from server to prevent Conflict error
                                Offset = this.getFileOffset(URL);
                            }
                            else
                            {
                                throw socketex;
                            }                            
                        }
                        else
                        {
                            throw;
                        }                        
                    }



                }
            }
        }
        //------------------------------------------------------------------------------------------------
        public TusHTTPResponse Download(string URL)
        {
            var client = new TusHTTPClient();

            var request = new TusHTTPRequest(URL);
            request.Method = "GET";

            request.Downloading += delegate(int bytesTransferred, int bytesTotal)
            {
                if (Downloading != null)
                    Downloading((int)bytesTransferred, (int)bytesTotal);
            };

            var response = client.PerformRequest(request);

            return response;
        }
        //------------------------------------------------------------------------------------------------
        public class TusServerInfo
        {
            public string Version = "";
            public string SupportedVersions = "";
            public string Extensions = "";
            public int MaxSize = 0;
            
            public bool SupportsDelete
            {
                get { return this.Extensions.Contains("termination"); }
            }
            
        }

        public TusServerInfo getServerInfo(string URL)
        {
            var client = new TusHTTPClient();
            var request = new TusHTTPRequest(URL);
            request.Method = "OPTIONS";

            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK)
            {
                // Spec says NoContent but tusd gives OK because of browser bugs
                var info = new TusServerInfo();
                response.Headers.TryGetValue("Tus-Resumable", out info.Version);
                response.Headers.TryGetValue("Tus-Version", out info.SupportedVersions);
                response.Headers.TryGetValue("Tus-Extension", out info.Extensions);

                string MaxSize;
                if (response.Headers.TryGetValue("Tus-Max-Size", out MaxSize))
                {
                    info.MaxSize = int.Parse(MaxSize);
                }
                else
                {
                    info.MaxSize = 0;
                }

                return info;
            }
            else
            {
                throw new Exception("getServerInfo failed. " + response.ResponseString);
            }
        }
        //------------------------------------------------------------------------------------------------
        public bool Delete(string URL)
        {
            var client = new TusHTTPClient();
            var request = new TusHTTPRequest(URL);
            request.Method = "DELETE";
            request.AddHeader("Tus-Resumable", "1.0.0");

            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Gone)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        // ***********************************************************************************************
        // Internal
        //------------------------------------------------------------------------------------------------
        private long getFileOffset(string URL)
        {
            var client = new TusHTTPClient();
            var request = new TusHTTPRequest(URL);
            request.Method = "HEAD";
            request.AddHeader("Tus-Resumable", "1.0.0");

            var response = client.PerformRequest(request);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                if (response.Headers.ContainsKey("Upload-Offset"))
                {
                    return long.Parse(response.Headers["Upload-Offset"]);
                }
                else
                {
                    throw new Exception("Offset Header Missing");
                }
            }
            else
            {
                throw new Exception("getFileOffset failed. " + response.ResponseString);
            }
        }
        // ***********************************************************************************************
    } // End of Class
} // End of Namespace
