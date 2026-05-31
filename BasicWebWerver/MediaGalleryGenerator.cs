using System;
using System.IO;
using System.Linq;
using System.Text;

public static class MediaGalleryGenerator
{
    public static void Generate(string mediaFolder)
    {
        var mediaFiles = Directory.GetFiles(mediaFolder)
            .Where(f =>
                f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileName)
            .ToList();

        StringBuilder html = new();

        html.AppendLine("""
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<title>Media Library</title>

<style>
body
{
    margin:0;
    padding:20px;
    font-family:Segoe UI, Arial, sans-serif;
    background:#1e1e1e;
    color:white;
}

h1
{
    text-align:center;
    margin-bottom:30px;
}

.gallery
{
    display:grid;
    grid-template-columns:repeat(auto-fill,minmax(300px,1fr));
    gap:20px;
}

.card
{
    background:#2d2d2d;
    border-radius:12px;
    overflow:hidden;
    box-shadow:0 4px 12px rgba(0,0,0,.4);
    transition:transform .2s;
}

.card:hover
{
    transform:translateY(-4px);
}

.title
{
    padding:12px;
    font-size:14px;
    word-break:break-word;
}

video,
audio
{
    width:100%;
    display:block;
}
</style>

</head>
<body>

<h1>Media Library</h1>

<div class="gallery">
""");

        foreach (var file in mediaFiles)
        {
            string fileName = Path.GetFileName(file);
            string encoded = Uri.EscapeDataString(fileName);

            if (file.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                html.AppendLine($"""
<div class="card">
    <video controls preload="metadata">
        <source src="{encoded}" type="video/mp4">
    </video>
    <div class="title">{fileName}</div>
</div>
""");
            }
            else
            {
                html.AppendLine($"""
<div class="card">
    <div class="title">{fileName}</div>
    <audio controls preload="metadata">
        <source src="{encoded}" type="audio/mpeg">
    </audio>
</div>
""");
            }
        }

        html.AppendLine("""
</div>

</body>
</html>
""");

        File.WriteAllText(
            Path.Combine(mediaFolder, "index.html"),
            html.ToString(),
            Encoding.UTF8);
    }
}