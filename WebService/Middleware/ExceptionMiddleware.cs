// Copyright (c) HOREICH GmbH, all rights reserved

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net;
using Newtonsoft.Json;
using Horeich.Services.Diagnostics;
using Horeich.Services.Exceptions;

namespace Horeich.IoTBridge.Middleware
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ExceptionMiddleware
    {
        // Function delegate to process HTTP requests
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Needed so that RequestDelegate can process requests
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext); //  _next.Invoke(httpContext)
            }
            catch (Exception e)
            {
                //_logger.Debug($"Error: {e}");
                await HandleExceptionAsync(httpContext, e);
            }
        }

        /// <summary>
        /// Handle exceptions and return HTTP error code
        /// </summary>
        /// <param name="context"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private Task HandleExceptionAsync(HttpContext context, Exception e)
        {
            var code = HttpStatusCode.InternalServerError; // 500 if unexpected

            // Bad request exceptions  
            if (e is ArgumentOutOfRangeException) // empty identity sent
            {
                _logger.Error(string.Format("Invalid device identity string: {0}", e.Message));
                code = HttpStatusCode.BadRequest;
            }
            else if (e is NullReferenceException) // variable is null
            {
                _logger.Error(string.Format("Invalid data format: {0}", e.Message));
                code = HttpStatusCode.BadRequest;
            }
            else if (e is FormatException) // wrong data format
            {
                _logger.Error(string.Format("Invalid data format: {0}", e.Message));
                code = HttpStatusCode.BadRequest;
            }

            // Internal server exceptions
            else if (e is OperationCanceledException) // send to IoT Hub timeout
            {
                _logger.Error(string.Format("Timeout error while sending message to IoT Hub: {0}", e.Message));   
                code = HttpStatusCode.InternalServerError;
            }

            // Cannot reach storage adapter
            else if (e is ExternalDependencyException)
            {
                _logger.Error(string.Format("Cannot reach storage adapter: {0}", e.Message)); //=> new { DateTime.Now });   
                code = HttpStatusCode.InternalServerError;
            }
            else if (e is Exception)
            {
                _logger.Error(string.Format("fatal error: {0}", e.Message));
                code = HttpStatusCode.InternalServerError;
            }
        
            var result = JsonConvert.SerializeObject(new { error = e.Message });
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;
            return context.Response.WriteAsync(result);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class MiddlewareExtensions
    {
        // public static IApplicationBuilder UseMiddleware(this IApplicationBuilder builder)
        // {
        //     return builder.UseMiddleware<Middleware>();
        // }
        public static void ConfigureCustomExceptionMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
