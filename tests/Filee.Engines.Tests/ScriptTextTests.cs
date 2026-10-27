// Scripts and style sheets become a PDF of their text, never run; INI settings retain their contents as UTF-8 TXT.

using Filee.Core.Conversion;
using Filee.Core.Presets;
using Filee.Engines.Pdf;

namespace Filee.Engines.Tests;

public class ScriptTextTests(EngineFixture fx) : IClassFixture<EngineFixture>
{
    [Theory]
    [InlineData("settings.ini", "utf-8")]
    [InlineData("DEVICE.INI", "utf-16")]
    public async Task Ini_settings_become_utf8_text_without_interpreting_the_contents(string name, string encoding)
    {
        var path = Path.Combine(fx.NewFolder(), name);
        const string Content = "; Device settings\r\n[device]\r\nname=中文 한글 café\r\npath=C:\\data\\input\r\nvalue=<tag> & **literal**\r\nempty=\r\n";
        await File.WriteAllTextAsync(path, Content, System.Text.Encoding.GetEncoding(encoding), TestContext.Current.CancellationToken);
        var original = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);

        var job = await fx.ConvertAsync([path], new Preset { TargetFormat = "txt" });

        Assert.True(job.State == JobState.Completed, string.Join("; ", job.Files.Select(f => $"{f.ErrorKey} {f.ErrorDetail}")));
        var output = Assert.Single(job.Outputs);
        Assert.Equal(".txt", Path.GetExtension(output));
        Assert.Equal(Content, await File.ReadAllTextAsync(output, System.Text.Encoding.UTF8, TestContext.Current.CancellationToken));
        Assert.Equal(original, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("site.css", "body { color: #1d9e75; }")]
    [InlineData("init.lua", "local greeting = \"hello\" print(greeting)")]
    public async Task The_code_becomes_a_pdf_of_its_text(string name, string code)
    {
        var available = fx.Converters.Where(c => fx.Catalog.StatusOf(c).IsAvailable).Select(c => c.Id).ToHashSet();
        Assert.SkipUnless(available.Contains("rhwp") || available.Contains("libreoffice"), "neither rhwp nor LibreOffice is available");
        var path = Path.Combine(fx.NewFolder(), name);
        await File.WriteAllTextAsync(path, code, TestContext.Current.CancellationToken);

        var job = await fx.ConvertAsync([path], new Preset { TargetFormat = "pdf" });

        Assert.True(job.State == JobState.Completed, string.Join("; ", job.Files.Select(f => $"{f.ErrorKey} {f.ErrorDetail}")));
        var text = PdfDocumentReader.PlainText(PdfDocumentReader.ReadText(Assert.Single(job.Outputs)));
        Assert.Contains(code.Split(' ')[1], text);
    }
}
