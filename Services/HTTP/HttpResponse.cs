// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Services.Test")]
namespace Horeich.Services.Http
{
    public interface IHttpResponse
    {
        HttpStatusCode StatusCode { get; }
        HttpResponseHeaders Headers { get; }
        string Content { get; }
        bool IsRetriableError { get; }
        bool IsSuccessStatusCode { get; }
        bool IsError { get; }
    }

    public class HttpResponse : IHttpResponse
    {
        private const int TOO_MANY_REQUESTS = 429;

        public bool IsSuccessStatusCode { get; internal set; }

        public HttpResponse()
        {
        }

        public HttpResponse(
            HttpStatusCode statusCode,
            string content,
            HttpResponseHeaders headers)
        {
            this.StatusCode = statusCode;
            this.Headers = headers;
            this.Content = content;
        }

        public HttpStatusCode StatusCode { get; internal set; }
        public HttpResponseHeaders Headers { get; internal set; }
        public string Content { get; internal set; }

        public bool IsRetriableError => this.StatusCode == HttpStatusCode.NotFound ||
                                        this.StatusCode == HttpStatusCode.RequestTimeout ||
                                        (int)this.StatusCode == TOO_MANY_REQUESTS;

        public bool IsError => (int) this.StatusCode >= 400 || (int) this.StatusCode == 0;
    }
}
