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
                    ProcessRequest(context);
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
        string? filename = context.Request.RawUrl?.TrimStart('/');

        if(!context.Request.IsLocal)
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


        if (File.Exists(filePath))
        {
            try
            {
                context.Response.ContentType = "text/html";
                Stream pageContent = new FileStream(filePath, FileMode.Open);
                pageContent.CopyTo(context.Response.OutputStream);
                context.Response.Close();
                pageContent.Close();
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