using System.Net;
using System.Net.Sockets;
using PuppeteerSharp;
using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.Sandbox
{
    public class SandboxScreenshotService : ISandboxScreenshotService, IAsyncDisposable
    {
        private static readonly TimeSpan NavigationTimeout = TimeSpan.FromSeconds(20);

        private readonly SemaphoreSlim _launchLock = new(1, 1);
        private IBrowser? _browser;

        public async Task<Result<byte[]>> CapturePreviewAsync(string url, CancellationToken ct)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return Result<byte[]>.Failure(ResultEnum.Failed);
            }

            if (!await IsSafeToFetchAsync(uri, ct))
            {
                return Result<byte[]>.Failure(ResultEnum.Failed);
            }

            IBrowser browser;
            try
            {
                browser = await GetBrowserAsync(ct);
            }
            catch
            {
                return Result<byte[]>.Failure(ResultEnum.Failed);
            }

            IBrowserContext? context = null;
            try
            {
                context = await browser.CreateBrowserContextAsync();
                var page = await context.NewPageAsync();
                await page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 800 });

                await page.GoToAsync(uri.ToString(), new NavigationOptions
                {
                    Timeout = (int)NavigationTimeout.TotalMilliseconds,
                    WaitUntil = new[] { WaitUntilNavigation.Load },
                });

                var bytes = await page.ScreenshotDataAsync(new ScreenshotOptions { Type = ScreenshotType.Png });
                return Result<byte[]>.Succeeded(bytes, ResultEnum.Successful);
            }
            catch
            {
                return Result<byte[]>.Failure(ResultEnum.Failed);
            }
            finally
            {
                if (context is not null)
                {
                    await context.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Minimal SSRF guard: only http/https, and refuse hostnames that resolve
        /// to loopback/private/link-local ranges so this endpoint can't be used to
        /// probe the server's own internal network.
        /// </summary>
        private static async Task<bool> IsSafeToFetchAsync(Uri uri, CancellationToken ct)
        {
            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(uri.Host, ct);
            }
            catch
            {
                return false;
            }

            if (addresses.Length == 0)
            {
                return false;
            }

            return addresses.All(IsPublicAddress);
        }

        private static bool IsPublicAddress(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            if (IPAddress.IsLoopback(address))
            {
                return false;
            }

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                var b = address.GetAddressBytes();
                if (b[0] == 10) return false;                              // 10.0.0.0/8
                if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return false;  // 172.16.0.0/12
                if (b[0] == 192 && b[1] == 168) return false;               // 192.168.0.0/16
                if (b[0] == 169 && b[1] == 254) return false;               // link-local incl. cloud metadata
                if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return false; // 100.64.0.0/10 shared address space
                return true;
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal) return false;
                var b = address.GetAddressBytes();
                if ((b[0] & 0xfe) == 0xfc) return false; // fc00::/7 unique local
                return true;
            }

            return false;
        }

        private async Task<IBrowser> GetBrowserAsync(CancellationToken ct)
        {
            if (_browser is { IsClosed: false })
            {
                return _browser;
            }

            await _launchLock.WaitAsync(ct);
            try
            {
                if (_browser is { IsClosed: false })
                {
                    return _browser;
                }

                await new BrowserFetcher().DownloadAsync();
                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-gpu" },
                });
                return _browser;
            }
            finally
            {
                _launchLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser is not null)
            {
                await _browser.CloseAsync();
            }
        }
    }
}
