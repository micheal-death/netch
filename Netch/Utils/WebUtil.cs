using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.VisualStudio.Threading;

namespace Netch.Utils;

public sealed class WebRequestOptions
{
    public string Url { get; init; } = string.Empty;

    public int Timeout { get; init; }

    public string UserAgent { get; set; } = WebUtil.DefaultUserAgent;

    public IWebProxy? Proxy { get; set; }
}

public static class WebUtil
{
    public const string DefaultUserAgent =
        @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.61 Safari/537.36 Edg/94.0.992.31";

    static WebUtil()
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
    }

    private static int DefaultGetTimeout => Global.Settings.RequestTimeout;

    public static WebRequestOptions CreateRequest(string url, int? timeout = null, string? userAgent = null)
    {
        return new WebRequestOptions
        {
            Url = url,
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? DefaultUserAgent : userAgent,
            Timeout = timeout ?? DefaultGetTimeout
        };
    }

    public static async Task<byte[]> DownloadBytesAsync(WebRequestOptions req)
    {
        using var httpClient = CreateHttpClient(req);
        using var requestMessage = CreateRequestMessage(req);
        using var webResponse = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
        var memoryStream = new MemoryStream();
        await using (memoryStream)
        {
            var input = await webResponse.Content.ReadAsStreamAsync();
            await using (input)
            {
                await input.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }

    public static async Task<(HttpStatusCode, string)> DownloadStringAsync(WebRequestOptions req, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        using var httpClient = CreateHttpClient(req);
        using var requestMessage = CreateRequestMessage(req);
        using var webResponse = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

        var responseStream = await webResponse.Content.ReadAsStreamAsync();
        await using (responseStream)
        {
            using var streamReader = new StreamReader(responseStream, encoding);

            return (webResponse.StatusCode, await streamReader.ReadToEndAsync());
        }
    }

    public static Task DownloadFileAsync(string address, string fileFullPath, IProgress<int>? progress = null)
    {
        return DownloadFileAsync(CreateRequest(address), fileFullPath, progress);
    }

    public static async Task DownloadFileAsync(WebRequestOptions req, string fileFullPath, IProgress<int>? progress)
    {
        var fileStream = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await using (fileStream)
        {
            using var httpClient = CreateHttpClient(req);
            using var requestMessage = CreateRequestMessage(req);
            using var webResponse = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            var input = await webResponse.Content.ReadAsStreamAsync();
            await using (input)
            {
                using var downloadTask = input.CopyToAsync(fileStream);
                if (progress != null)
                    ReportProgressAsync(webResponse.Content.Headers.ContentLength ?? -1, downloadTask, fileStream, progress, 200).Forget();

                await downloadTask;
            }
        }

        progress?.Report(100);
    }

    private static HttpClient CreateHttpClient(WebRequestOptions req)
    {
        var handler = new HttpClientHandler();
        if (req.Proxy != null)
        {
            handler.Proxy = req.Proxy;
            handler.UseProxy = true;
        }

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(req.Timeout)
        };
    }

    private static HttpRequestMessage CreateRequestMessage(WebRequestOptions req)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, req.Url);
        requestMessage.Headers.TryAddWithoutValidation("User-Agent", req.UserAgent);
        requestMessage.Headers.TryAddWithoutValidation("Accept", "*/*");
        requestMessage.Headers.TryAddWithoutValidation("Accept-Charset", "utf-8");
        return requestMessage;
    }

    private static async Task ReportProgressAsync(long total, Task downloadTask, Stream stream, IProgress<int> progress, int interval)
    {
        if (total <= 0)
        {
            return;
        }

        var n = 0;
        while (!downloadTask.IsCompleted)
        {
            var n1 = (int)((double)stream.Length / total * 100);
            if (n != n1)
            {
                n = n1;
                progress.Report(n);
            }

            await Task.Delay(interval);
        }
    }
}
