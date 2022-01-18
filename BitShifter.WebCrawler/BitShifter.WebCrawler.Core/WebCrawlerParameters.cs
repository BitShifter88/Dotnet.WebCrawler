using System;
using System.Collections.Generic;
using System.Text;

namespace BitShifter.WebCrawler.Core
{
    public class WebCrawlerParameters
    {
        public int WorkerThreads { get; set; } = 100;
        public int HttpServicePointConnectionLimit { get; set; } = 200;
        public string UserAgentString { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36";
        public int HttpRequestTimeoutInSeconds { get; set; } = 15;
        public int HttpRequestMaxAutoRedirects { get; set; } = 7;
        public bool IsHttpRequestAutomaticDecompressionEnabled { get; set; } = false;
        public bool IsHttpRequestAutoRedirectsEnabled { get; set; } = false;
        public bool IsSendingCookiesEnabled { get; set; } = false;
        public bool IsSslCertificateValidationEnabled { get; set; } = false;
    }
}
