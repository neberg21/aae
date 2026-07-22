using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Service.Unit.WebUi;

public sealed class WebUiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _webRootPath;

    public WebUiWebApplicationFactory()
    {
        _webRootPath = Path.Combine(Path.GetTempPath(), "aae-webui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_webRootPath);
        var indexPath = Path.Combine(_webRootPath, "index.html");
        File.WriteAllText(indexPath, "<!doctype html><html><title>AAE UI</title><body>ok</body></html>");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseWebRoot(_webRootPath);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        try
        {
            Directory.Delete(_webRootPath, recursive: true);
        }
        catch (IOException)
        {
            // best-effort cleanup for temp web root
        }
        catch (UnauthorizedAccessException)
        {
            // best-effort cleanup for temp web root
        }
    }
}
