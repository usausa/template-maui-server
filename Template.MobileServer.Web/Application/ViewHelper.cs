namespace Template.MobileServer.Web.Application;

using MudBlazor;

public static class ViewHelper
{
    //--------------------------------------------------------------------------------
    // Format
    //--------------------------------------------------------------------------------

    public static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        < 1024L * 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
        _ => $"{bytes / (1024.0 * 1024 * 1024 * 1024):F2} TB"
    };

    public static string FormatSize(StorageEntry entry) =>
        entry.IsDirectory ? string.Empty : FormatBytes(entry.Size);

    public static string FormatTimestamp(DateTime value) =>
        value.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

    //--------------------------------------------------------------------------------
    // Icon
    //--------------------------------------------------------------------------------

    private static readonly HashSet<string> ImageExtensions =
        new([".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".svg", ".ico"], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> VideoExtensions =
        new([".mp4", ".avi", ".mov", ".wmv", ".mkv", ".webm"], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> AudioExtensions =
        new([".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a"], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ArchiveExtensions =
        new([".zip", ".rar", ".7z", ".tar", ".gz"], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> TextExtensions =
        new([".txt", ".log", ".md", ".csv"], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> CodeExtensions =
        new([".cs", ".js", ".ts", ".py", ".java", ".cpp", ".h", ".html", ".css", ".json", ".xml"], StringComparer.OrdinalIgnoreCase);

    // 拡張子によるアイコン出し分け
    public static string GetIcon(StorageEntry entry)
    {
        if (entry.IsDirectory)
        {
            return Icons.Material.Filled.Folder;
        }

        var ext = Path.GetExtension(entry.Name);
        if (ImageExtensions.Contains(ext))
        {
            return Icons.Material.Filled.Image;
        }

        if (VideoExtensions.Contains(ext))
        {
            return Icons.Material.Filled.Movie;
        }

        if (AudioExtensions.Contains(ext))
        {
            return Icons.Material.Filled.MusicNote;
        }

        if (ext == ".pdf")
        {
            return Icons.Material.Filled.PictureAsPdf;
        }

        if (ArchiveExtensions.Contains(ext))
        {
            return Icons.Material.Filled.Archive;
        }

        if (TextExtensions.Contains(ext))
        {
            return Icons.Material.Filled.Description;
        }

        if (CodeExtensions.Contains(ext))
        {
            return Icons.Material.Filled.Code;
        }

        return Icons.Material.Filled.InsertDriveFile;
    }
}
