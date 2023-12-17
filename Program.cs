using System;
using System.Net;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;

class SimpleHTTPserver
{
    static void Main()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:3000/");
        Console.WriteLine("Server running at http://127.0.0.1:3000/");
        listener.Start();

        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest req = context.Request;
            HttpListenerResponse resp = context.Response;
            string relativePath = Truncate_Url(req.RawUrl);
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            string rootDir = Directory.GetCurrentDirectory();

            if (Directory.Exists(fullPath))
            {
                ServeDirectoryListing(fullPath, resp);
            }
            else if (File.Exists(fullPath))
            {
                ServeFile(fullPath, resp);
            }
            else
            {
                resp.StatusCode = (int)HttpStatusCode.NotFound;
                resp.Close();
            }
            Console.WriteLine($"127.0.0.1 - - [{DateTime.Now:dd/MMM/yyyy HH:mm:ss}] {req.HttpMethod} '{relativePath}' ");
        }
    }

    public static (List<string> fileNames, List<string> directoryNames) Truncate_Dir(string dir)
    {
        List<string> fileNames = new List<string>();
        List<string> directoryNames = new List<string>();


        string[] fileEntries = Directory.GetFiles(dir);
        foreach (string fileName in fileEntries)
        {
            string truncatedName = Path.GetFileName(fileName);
            fileNames.Add(truncatedName);
        }

        string[] directoryEntries = Directory.GetDirectories(dir);
        foreach (string directoryName in directoryEntries)
        {
            string truncatedName = Path.GetFileName(directoryName);
            directoryNames.Add(truncatedName);
        }
        return (fileNames, directoryNames);
    }

    static void ServeDirectoryListing(string dir, HttpListenerResponse resp)
    {
        var files = new DirectoryInfo(dir).GetFiles().Select(f => f.Name).ToList();
        var directories = new DirectoryInfo(dir).GetDirectories().Select(d => d.Name).ToList();

        if (files.Contains("index.html"))
        {
            string filePath = Path.Combine(dir, "index.html");
            ServeFile(filePath, resp);
        }
        else
        {
            string content = GenerateDirectoryListingHtml(dir, files, directories);
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            resp.ContentLength64 = buffer.Length;
            resp.ContentType = "text/html";
            resp.StatusCode = (int)HttpStatusCode.OK;
            resp.OutputStream.Write(buffer, 0, buffer.Length);
            resp.Close();
        }
    }

    static void ServeFile(string filePath, HttpListenerResponse resp)
    {
        try
        {
            byte[] buffer = File.ReadAllBytes(filePath);
            resp.ContentLength64 = buffer.Length;
            resp.ContentType = GetMimeType(filePath);
            resp.OutputStream.Write(buffer, 0, buffer.Length);
            resp.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error serving file: {ex.Message}");
            resp.StatusCode = (int)HttpStatusCode.InternalServerError;
            resp.Close();
        }
    }


    public static string Truncate_Url(string url)
    {
        string decodedUrl = Uri.UnescapeDataString(url);

        string normalizedPath = decodedUrl.Replace('/', Path.DirectorySeparatorChar);

        if (normalizedPath.StartsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            normalizedPath = normalizedPath.TrimStart(Path.DirectorySeparatorChar);
        }

        return normalizedPath;
    }

    public static string GetMimeType(string fileName)
    {
        string mimeType = "application/octet-stream";

        var mimeTypes = new Dictionary<string, string> {
            { ".htm", "text/html" },
            { ".html", "text/html" },
            { ".css", "text/css" },
            { ".js", "application/javascript" },
            { ".json", "application/json" },
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".gif", "image/gif" },
            { ".svg", "image/svg+xml" },
            { ".txt", "text/plain" },
        };

        string extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (mimeTypes.ContainsKey(extension))
        {
            mimeType = mimeTypes[extension];
        }

        return mimeType;
    }

    public static string GenerateDirectoryListingHtml(string dir, List<string> files, List<string> directories)
    {
        StringBuilder contentBuilder = new StringBuilder();

        Uri baseUri = new Uri(new DirectoryInfo(dir).FullName + Path.DirectorySeparatorChar);

        foreach (var file in files)
        {
            Uri fileUri = new Uri(baseUri, file);
            string relativeFileUrl = Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString());
            contentBuilder.AppendLine($"<article class='card'><a href='{relativeFileUrl}' class='item'>{file}</a></article>");
        }

        foreach (var directory in directories)
        {
            Uri directoryUri = new Uri(baseUri, directory + Path.DirectorySeparatorChar);
            string relativeDirectoryUrl = Uri.UnescapeDataString(baseUri.MakeRelativeUri(directoryUri).ToString());
            contentBuilder.AppendLine($"<article class='card'><a href='{relativeDirectoryUrl}' class='item'>{directory}/</a></article>");
        }

        string htmlTemplate = @"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>C# Server</title>
<style>
  body {
    font-family: 'Segoe UI', sans-serif;
    color: #ffffff;
    background-color: #1e1e1e;
    margin: 0;
    padding: 0;
  }
  .header {
    background-color: #0078d4;
    color: #ffffff;
    padding: 10px 20px;
  }
  .main-content {
    display: flex;
    flex-direction: column; 
    padding: 20px;
  }
  .sidebar {
    background-color: #2d2d2d;
    padding: 10px;
    margin-bottom: 10px;
    max-width: 100%;
    box-sizing: border-box; 
  }
  .content {
    background-color: #333333;
    padding: 10px;
  }
  .card {
    background-color: #252526;
    margin: 10px 0; 
    padding: 20px;
    border-left: 5px solid #0078d4;
  }
  .item {
    display: block; 
    font-style: None;
    text-decoration: None;
    color: #0078d4;
    overflow-wrap: break-word; 
  }
  @media (max-width: 768px) {
    .main-content {
      flex-direction: column;
    }
    .sidebar, .content {
      width: 100%; 
    }
    #depends{
      display:None;
    }
  }
</style>
</head>
<body>
<div class='header'>
  <h1>Simple C# server</h1>
</div>
<div class='main-content'>
  <aside class='sidebar'>
    <h1 id='depends'>Directory Listing: " + dir + @" </h1>
  </aside>
  <section class='content'>
  " + contentBuilder.ToString() + @"
  </section>
</div>
</body>
</html>";
        return htmlTemplate;
    }
}
