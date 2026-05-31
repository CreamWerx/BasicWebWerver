using System.Net;
using System.Runtime.CompilerServices;

class BasicWebServer
{
    private HttpListener listener;
    private string directoryPath;
    private bool isRunning = false;

    public BasicWebServer(string directoryPath)
    {
        this.directoryPath = directoryPath;
        listener = new HttpListener();
    }

    public async Task StartServer(int port)
    {
        if (!HttpListener.IsSupported)
        {
            Msg("HttpListener not supported.", MsgType.Error);
            return;
        }

        if (!Directory.Exists(directoryPath))
        {
            Msg("Root directory does not exist.", MsgType.Error);
            return;
        }

        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        Msg($"Server started. Listening on port {port}...", MsgType.Info);
        isRunning = true;

        while (listener.IsListening)
        {
            try
            {
                HttpListenerContext context = await listener.GetContextAsync();
                if (context != null)
                {
                    //ProcessRequest(context);
                    _ = Task.Run(() => ProcessRequest(context));
                }
            }
            catch (Exception ex)
            {
                Msg(ex.Message, MsgType.Error);
            }
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        //Console.WriteLine("Range: " + context.Request.Headers["Range"]);

        string? filename = Uri.UnescapeDataString(context.Request.RawUrl?.TrimStart('/') ?? "");

        if (!context.Request.IsLocal)
        {
            Msg("Non local request - ignored", MsgType.Alert);
            context.Response.Close();
            return;
        }

        if (string.IsNullOrWhiteSpace(filename))
        {
            filename = "index.html";
        }

        Msg($"{context.Request.HttpMethod}: {context.Request.Url}", MsgType.Request);

        string filePath = Path.Combine(directoryPath, filename);


        //if (File.Exists(filePath))
        //{
        //    try
        //    {
        //        context.Response.ContentType = GetContentType(filePath);
        //        Stream pageContent = new FileStream(filePath, FileMode.Open);
        //        pageContent.CopyTo(context.Response.OutputStream);
        //        context.Response.Close();
        //        pageContent.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        var msg = ex.Message;
        //        if (msg.Contains("specified network"))
        //        {
        //            msg += " : " + context.Request.QueryString;
        //        }
        //        Msg(msg, MsgType.Error);
        //    }
        //}


        if (File.Exists(filePath))
        {
            try
            {
                using FileStream fs = File.OpenRead(filePath);

                long fileLength = fs.Length;

                context.Response.ContentType = GetContentType(filePath);

                // Tell browsers we support range requests
                context.Response.AddHeader("Accept-Ranges", "bytes");

                string? rangeHeader = context.Request.Headers["Range"];

                if (!string.IsNullOrEmpty(rangeHeader) &&
                    rangeHeader.StartsWith("bytes="))
                {
                    Msg($"Range request: {rangeHeader}", MsgType.Info);

                    string range = rangeHeader["bytes=".Length..];

                    long start = 0;

                    int dashIndex = range.IndexOf('-');

                    if (dashIndex >= 0)
                    {
                        string startText = range[..dashIndex];

                        if (!string.IsNullOrWhiteSpace(startText))
                        {
                            start = long.Parse(startText);
                        }
                    }

                    if (start >= fileLength)
                    {
                        context.Response.StatusCode = 416;
                        context.Response.Close();
                        return;
                    }

                    long bytesRemaining = fileLength - start;

                    context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
                    context.Response.ContentLength64 = bytesRemaining;

                    context.Response.AddHeader(
                        "Content-Range",
                        $"bytes {start}-{fileLength - 1}/{fileLength}");

                    fs.Position = start;

                    byte[] buffer = new byte[64 * 1024];

                    while (bytesRemaining > 0)
                    {
                        int bytesToRead = (int)Math.Min(buffer.Length, bytesRemaining);

                        int bytesRead = fs.Read(buffer, 0, bytesToRead);

                        if (bytesRead == 0)
                            break;

                        context.Response.OutputStream.Write(
                            buffer,
                            0,
                            bytesRead);

                        bytesRemaining -= bytesRead;
                    }
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentLength64 = fileLength;

                    byte[] buffer = new byte[64 * 1024];

                    int bytesRead;

                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        context.Response.OutputStream.Write(
                            buffer,
                            0,
                            bytesRead);
                    }
                }

                context.Response.Close();
            }
            catch (Exception ex)
            {
                var msg = ex.Message;

                if (msg.Contains("specified network"))
                {
                    msg += " : " + context.Request.QueryString;
                }

                Msg(msg, MsgType.Error);
            }
        }

        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.Close();
        }
    }

    private static string GetContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLower() switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }

    public bool IsRunning()
    {
        return isRunning;
    }

    public void StopServer()
    {
        try
        {
            if (listener != null && listener.IsListening)
            {
                listener.Stop();
                listener.Close();
                Msg("Server stopped.", MsgType.Info);
                isRunning = false;
            }
        }
        catch (Exception ex)
        {
            Msg(ex.Message, MsgType.Error);
            isRunning = true;
        }
    }

    private void Msg(string msg, MsgType type, [CallerMemberName] string membername = "")
    {
        if (type == MsgType.Error)
        {
            if (msg.Contains("Thread", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (isRunning)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Server error:");
            }
        }
        else if (type == MsgType.Request)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Client request:");
        }
        else if (type == MsgType.Alert)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Server alert:");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Server msg:");
        }
        Console.WriteLine("\t" + membername);
        Console.WriteLine("\t" + msg);
        Console.ForegroundColor = ConsoleColor.White;
    }
}

enum MsgType
{
    Error,
    Request,
    Info,
    Alert
}