using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace TusClient
{
    public class TusHTTPRequest
    {
        public delegate void UploadingEvent(int bytesTransferred, int bytesTotal);
        public event UploadingEvent Uploading;

        public delegate void DownloadingEvent(int bytesTransferred, int bytesTotal);
        public event DownloadingEvent Downloading;

        public string URL { get; set; }
        public string Method { get; set; }
        public Dictionary<string,string> Headers { get; set; }
        public byte[] BodyBytes { get; set; }

        public string BodyText
        {
            get { return System.Text.Encoding.UTF8.GetString(this.BodyBytes); }
            set { BodyBytes = System.Text.Encoding.UTF8.GetBytes(value); }
        }
        

        public TusHTTPRequest(string u)
        {
            this.URL = u;
            this.Method = "GET";
            this.Headers = new Dictionary<string, string>();
            this.BodyBytes = new byte[0];
        }

        public void AddHeader(string k,string v)
        {
            this.Headers[k] = v;
        }
        
        public void FireUploading(int bytesTransferred, int bytesTotal)
        {
            if (Uploading != null)
                Uploading(bytesTransferred, bytesTotal);
        }

        public void FireDownloading(int bytesTransferred, int bytesTotal)
        {
            if (Downloading != null)
                Downloading(bytesTransferred, bytesTotal);
        }

    }
    public class TusHTTPResponse
    {
        public byte[] ResponseBytes { get; set; }
        public string ResponseString { get { return System.Text.Encoding.UTF8.GetString(this.ResponseBytes); } }
        public HttpStatusCode StatusCode { get; set; }
        public Dictionary<string, string> Headers { get; set; }

        public TusHTTPResponse()
        {
            this.Headers = new Dictionary<string, string>();
        }

    }

    public class TusHTTPClient
    {


        public TusHTTPResponse PerformRequest(TusHTTPRequest req)
        {

            try
            {
                var instream = new MemoryStream(req.BodyBytes);                

                HttpWebRequest request = (HttpWebRequest) HttpWebRequest.Create(req.URL);
                request.AutomaticDecompression = DecompressionMethods.GZip;

                request.Timeout = System.Threading.Timeout.Infinite;
                request.ReadWriteTimeout = System.Threading.Timeout.Infinite;
                request.Method = req.Method;
                request.KeepAlive = false;

                ServicePoint currentServicePoint = request.ServicePoint;
                currentServicePoint.Expect100Continue = false;

                //SEND
                req.FireUploading(0, 0);
                byte[] buffer = new byte[4096];

                int contentlength = 0;

                int byteswritten = 0;
                int totalbyteswritten = 0;

                contentlength = (int) instream.Length;
                request.AllowWriteStreamBuffering = false;
                request.ContentLength = instream.Length;

                foreach (var header in req.Headers)
                {
                    switch (header.Key)
                    {
                        case "Content-Length":
                            request.ContentLength = long.Parse(header.Value);
                            break;
                        case "Content-Type":
                            request.ContentType = header.Value;
                            break;
                        default:
                            request.Headers.Add(header.Key, header.Value);
                            break;
                    }                 
                }

                if (req.BodyBytes.Length > 0)
                {
                    using (System.IO.Stream requestStream = request.GetRequestStream())
                    {
                        instream.Seek(0, SeekOrigin.Begin);
                        byteswritten = instream.Read(buffer, 0, buffer.Length);

                        while (byteswritten > 0)
                        {
                            totalbyteswritten += byteswritten;

                            req.FireUploading(totalbyteswritten, contentlength);

                            requestStream.Write(buffer, 0, byteswritten);

                            byteswritten = instream.Read(buffer, 0, buffer.Length);
                        }


                    }
                }

                req.FireDownloading(0, 0);

                HttpWebResponse response = (HttpWebResponse) request.GetResponse();


                contentlength = 0;
                contentlength = (int) response.ContentLength;
                //contentlength=0 for gzipped responses due to .net bug

                buffer = new byte[4096];
                var outstream = new MemoryStream();

                using (Stream responseStream = response.GetResponseStream())
                {
                    int bytesread = 0;
                    int totalbytesread = 0;

                    bytesread = responseStream.Read(buffer, 0, buffer.Length);

                    while (bytesread > 0)
                    {
                        totalbytesread += bytesread;

                        req.FireDownloading(totalbytesread, contentlength);

                        outstream.Write(buffer, 0, bytesread);

                        bytesread = responseStream.Read(buffer, 0, buffer.Length);
                    }
                }

                TusHTTPResponse resp = new TusHTTPResponse();
                resp.ResponseBytes = outstream.ToArray();
                resp.StatusCode = response.StatusCode;
                foreach (string headerName in response.Headers.Keys)
                {
                    resp.Headers[headerName] = response.Headers[headerName];
                }

                return resp;

            }
            catch (WebException ex)
            {
                RestWebException rex = new RestWebException(ex);
                throw rex;
            }
        }
    }


    public class RestWebException : WebException
    {

        public string ResponseContent { get; set; }
        public HttpStatusCode statuscode { get; set; }
        public string statusdescription { get; set; }


        public WebException OriginalException;
        public RestWebException(RestWebException ex, string msg)
            : base(string.Format("{0}{1}", msg, ex.Message), ex, ex.Status, ex.Response)
        {
            this.OriginalException = ex;


            this.statuscode = ex.statuscode;
            this.statusdescription = ex.statusdescription;
            this.ResponseContent = ex.ResponseContent;


        }

        public RestWebException(WebException ex, string msg = "")
            : base(string.Format("{0}{1}", msg, ex.Message), ex, ex.Status, ex.Response)
        {

            this.OriginalException = ex;

            HttpWebResponse webresp = (HttpWebResponse)ex.Response;


            if (webresp != null)
            {
                this.statuscode = webresp.StatusCode;
                this.statusdescription = webresp.StatusDescription;

                StreamReader readerS = new StreamReader(webresp.GetResponseStream());

                dynamic resp = readerS.ReadToEnd();

                readerS.Close();

                this.ResponseContent = resp;
            }

        }

        public string FullMessage
        {
            get
            {
                var bits = new List<string>();
                if (this.Response != null)
                {
                    bits.Add(string.Format("URL:{0}", this.Response.ResponseUri));
                }
                bits.Add(this.Message);
                if (this.statuscode != HttpStatusCode.OK)
                {
                    bits.Add(string.Format("{0}:{1}", this.statuscode, this.statusdescription));
                }
                if (!string.IsNullOrEmpty(this.ResponseContent))
                {
                    bits.Add(this.ResponseContent);
                }

                return string.Join(Environment.NewLine, bits.ToArray());
            }
        }

    }
}


