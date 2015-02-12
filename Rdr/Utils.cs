using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Rdr
{
    public static class Utils
    {
        public static void SetWindowToMiddleOfScreen(Window window)
        {
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowHeight = window.Height;
            window.Top = (screenHeight / 2) - (windowHeight / 2);

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double windowWidth = window.Width;
            window.Left = (screenWidth / 2) - (windowWidth / 2);
        }


        public static void OpenUriInBrowser(string uri)
        {
            Uri tmp = null;

            if (Uri.TryCreate(uri, UriKind.Absolute, out tmp))
            {
                System.Diagnostics.Process.Start(tmp.AbsoluteUri);
            }
            else
            {
                Utils.LogMessage(string.Format("Uri.TryCreate returned false on {0}", uri));
            }
        }

        public static void OpenUriInBrowser(Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                System.Diagnostics.Process.Start(uri.AbsoluteUri);
            }
            else
            {
                Utils.LogMessage(string.Format("Uri ({0}) was not absolute", uri.OriginalString));
            }
        }


        public static void LogMessage(string message)
        {
            string logFilePath = string.Format(@"C:\Users\{0}\Documents\logfile.txt", Environment.UserName);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("{0} logged the following message at {1}", Application.Current.ToString(), DateTime.Now));
            sb.AppendLine(message);
            sb.AppendLine(Environment.NewLine);

            using (FileStream fs = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.None, 4096, false))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(sb.ToString());
                }
            }
        }

        public static async Task LogMessageAsync(string message)
        {
            string logFilePath = string.Format(@"C:\Users\{0}\Documents\logfile.txt", Environment.UserName);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("{0} logged the following message at {1}", Application.Current.ToString(), DateTime.Now));
            sb.AppendLine(message);
            sb.AppendLine(Environment.NewLine);

            using (FileStream fsAsync = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.None, 4096, true))
            {
                using (StreamWriter sw = new StreamWriter(fsAsync))
                {
                    await sw.WriteAsync(sb.ToString()).ConfigureAwait(false);
                }
            }
        }


        public static void LogException(Exception e)
        {
            string logFilePath = string.Format(@"C:\Users\{0}\Documents\logfile.txt", Environment.UserName);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("{0} occurred in {1} at {2}", e.GetType().ToString(), Application.Current.ToString(), DateTime.Now));

            sb.AppendLine(e.Message);
            sb.AppendLine(e.StackTrace);
            sb.AppendLine(Environment.NewLine);

            using (FileStream fs = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.None, 4096, false))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(sb.ToString());
                }
            }
        }

        public static void LogException(Exception e, string message)
        {
            string logFilePath = string.Format(@"C:\Users\{0}\Documents\logfile.txt", Environment.UserName);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("{0} occurred in {1} at {2}", e.GetType().ToString(), Application.Current.ToString(), DateTime.Now));
            sb.AppendLine(message);
            sb.AppendLine(e.Message);
            sb.AppendLine(e.StackTrace);
            sb.AppendLine(Environment.NewLine);

            using (FileStream fs = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.None, 4096, false))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(sb.ToString());
                }
            }
        }

        public static async Task LogExceptionAsync(Exception e)
        {
            string logFilePath = string.Format(@"C:\Users\{0}\Documents\logfile.txt", Environment.UserName);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("{0} occurred in {1} at {2}", e.GetType().ToString(), Application.Current.ToString(), DateTime.Now));
            sb.AppendLine(e.Message);
            sb.AppendLine(e.StackTrace);
            sb.AppendLine(Environment.NewLine);

            using (FileStream fsAsync = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.None, 4096, true))
            {
                using (StreamWriter sw = new StreamWriter(fsAsync))
                {
                    await sw.WriteAsync(sb.ToString()).ConfigureAwait(false);
                }
            }
        }

        public static async Task LogExceptionAsync(Exception e, string message)
        {
            string logFilePath = string.Format(@"C:\Users\{0}\Documents\logfile.txt", Environment.UserName);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("{0} occurred in {1} at {2}", e.GetType().ToString(), Application.Current.ToString(), DateTime.Now));
            sb.AppendLine(message);
            sb.AppendLine(e.Message);
            sb.AppendLine(e.StackTrace);
            sb.AppendLine(Environment.NewLine);

            using (FileStream fsAsync = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.None, 4096, true))
            {
                using (StreamWriter sw = new StreamWriter(fsAsync))
                {
                    await sw.WriteAsync(sb.ToString()).ConfigureAwait(false);
                }
            }
        }


        private static bool _useLogging = false;

        public static string DownloadWebsiteAsString(HttpWebRequest req, int rounds = 1)
        {
            string response = string.Empty;

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponseExt(_useLogging, rounds))
            {
                if (resp != null)
                {
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            try
                            {
                                response = sr.ReadToEnd();
                            }
                            catch (IOException)
                            {
                                response = string.Empty;
                            }
                        }
                    }
                    else
                    {
                        string errorMessage = string.Format("Getting website {0} failed with code {1}", req.RequestUri.AbsoluteUri, resp.StatusCode.ToString());

                        Utils.LogMessage(errorMessage);
                    }
                }
                else
                {
                    if (req != null)
                    {
                        req.Abort();
                    }
                }
            }

            return response;
        }

        public static async Task<string> DownloadWebsiteAsStringAsync(HttpWebRequest req, int rounds = 1)
        {
            string response = string.Empty;

            using (HttpWebResponse resp = (HttpWebResponse)(await req.GetResponseAsyncExt(_useLogging, rounds)))
            {
                if (resp != null)
                {
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            try
                            {
                                response = await sr.ReadToEndAsync().ConfigureAwait(false);
                            }
                            catch (IOException)
                            {
                                response = string.Empty;
                            }
                        }
                    }
                    else
                    {
                        string errorMessage = string.Format("Getting website {0} failed with code {1}", req.RequestUri.AbsoluteUri, resp.StatusCode.ToString());

                        await Utils.LogMessageAsync(errorMessage).ConfigureAwait(false);
                    }
                }
                else
                {
                    Debug.WriteLine("BEGIN");

                    if (req != null)
                    {
                        Debug.WriteLine(string.Format("Host: {0}", req.Host));

                        req.Abort();
                    }
                    else
                    {
                        Debug.WriteLine("req was null");
                    }

                    Debug.WriteLine("END");
                }
            }

            return response;
        }

        public static async Task<string> DownloadWebsiteAsStringAsyncComplicated(HttpWebRequest req)
        {
            string response = string.Empty;

            using (HttpWebResponse resp = await GetResponseAsyncComplicated(req))
            {
                if (resp != null)
                {
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            response = await sr.ReadToEndAsync().ConfigureAwait(false);
                        }
                    }
                }
            }

            return response;
        }





        private static async Task<HttpWebResponse> GetResponseAsyncComplicated(HttpWebRequest req)
        {
            return await Task.Factory.StartNew<HttpWebResponse>(() =>
                {
                    HttpWebResponse resp = null;
                    ManualResetEvent mre = new ManualResetEvent(false);

                    try
                    {
                        IAsyncResult ar = req.BeginGetResponse(new AsyncCallback((innerAr) =>
                            {
                                HttpWebRequest innerReq = (HttpWebRequest)innerAr.AsyncState;

                                try
                                {
                                    resp = (HttpWebResponse)innerReq.EndGetResponse(innerAr);
                                }
                                catch (WebException)
                                {
                                    Debug.WriteLine("inner: " + req.RequestUri.AbsoluteUri);
                                }

                                mre.Set();
                            }), req);

                        ThreadPool.RegisterWaitForSingleObject(ar.AsyncWaitHandle, timeoutCallback, req, 4500, true);

                        mre.WaitOne();
                    }
                    catch (WebException)
                    {
                        Debug.WriteLine("outer: " + req.RequestUri.AbsoluteUri);
                    }

                    return resp;
                });
        }

        private static void timeoutCallback(object state, bool timedOut)
        {
            if (timedOut)
            {
                HttpWebRequest req = (HttpWebRequest)state;

                if (req != null)
                {
                    Debug.WriteLine("timedOut: " + req.RequestUri.AbsoluteUri);

                    req.Abort();
                }
            }
        }
    }
}