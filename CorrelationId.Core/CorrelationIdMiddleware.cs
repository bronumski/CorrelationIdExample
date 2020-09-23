using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace CorrelationId.Core
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext, CorrelationContextFactory correlationContextFactory)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }
            
            var hasCorrelationIdHeader = httpContext.Request.Headers.TryGetValue("CorrelationId", out var cid) &&
                                         !StringValues.IsNullOrEmpty(cid);
            
            var correlationId = hasCorrelationIdHeader ? cid.FirstOrDefault() : Guid.NewGuid().ToString();
            
            correlationContextFactory.Create(correlationId);

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            });
            await _next(httpContext);
        }

    }

    public class CorrelationContextFactory : IDisposable
    {
        private readonly CorrelationContextAccessor _correlationContextAccessor;

        public CorrelationContextFactory(CorrelationContextAccessor correlationContextAccessor)
            => _correlationContextAccessor = correlationContextAccessor;
        
        public CorrelationContext Create(string correlationId)
        {
            var correlationContext = new CorrelationContext(correlationId);

            if (_correlationContextAccessor != null)
            {
                _correlationContextAccessor.CorrelationContext = correlationContext;
            }

            return correlationContext;
        }
        
        public void Dispose()
        {
            if (_correlationContextAccessor != null)
            {
                _correlationContextAccessor.CorrelationContext = null;
            }
        }
    }

    public class CorrelationContext
    {
        public CorrelationContext(string correlationId)
            => CorrelationId = correlationId;

        public string CorrelationId { get; }
    }

    public class CorrelationContextAccessor
    {
        private static readonly AsyncLocal<CorrelationContext> _correlationContext = new AsyncLocal<CorrelationContext>();
        public CorrelationContext CorrelationContext
        {
            get => _correlationContext.Value;
            set => _correlationContext.Value = value;
        }
    }
}