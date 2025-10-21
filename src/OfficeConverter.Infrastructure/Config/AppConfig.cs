using System;

namespace OfficeConverter.Infrastructure.Config;

public class AppConfig
{
    public string OutputDir { get; set; } = DefaultOutputDir();
    public string NamingTemplate { get; set; } = "{name}.{ext}";

    public PdfOptions PdfOptions { get; set; } = new();
    public MdOptions MdOptions { get; set; } = new();
    public CsvOptions CsvOptions { get; set; } = new();

    public int Concurrency { get; set; } = Environment.ProcessorCount;
    public int MaxAttempts { get; set; } = 3;

    public static string DefaultOutputDir()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var target = System.IO.Path.Combine(baseDir, "OfficeConverter", "output");
        return target;
    }
}

public class PdfOptions
{
    public bool EmbedFonts { get; set; } = true;
}

public class MdOptions
{
    public bool KeepImages { get; set; } = true;
}

public class CsvOptions
{
    public string Encoding { get; set; } = "utf-8";
    public string Separator { get; set; } = ",";
}
