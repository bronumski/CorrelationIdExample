using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using CorrelationId.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace WebAppA
{
    public class WebAppAStartup
    {
        public WebAppAStartup(IConfiguration configuration)
            => Configuration = configuration;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services
                .AddAuthentication("Basic")
                .AddScheme<BasicAuthSchemeOptions, BasicAuthHandler>("Basic", null);
            
            services.AddSingleton<CorrelationContextAccessor>();
            services.AddTransient<CorrelationContextFactory>();
            
            services.AddTransient<CorrelationIdHandler>();
            services.AddHttpClient<WeatherAppClient>(x =>
            {
                x.BaseAddress = new Uri("http://localhost:5005");
                
            }).AddHttpMessageHandler<CorrelationIdHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<CorrelationIdMiddleware>();
            
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<SerilogUsernameEnricherMiddleware>();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            
        }
    }
    
    class BasicAuthSchemeOptions : AuthenticationSchemeOptions {}
    
    class BasicAuthHandler : AuthenticationHandler<BasicAuthSchemeOptions>
    {
        public BasicAuthHandler(IOptionsMonitor<BasicAuthSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var userHeader = Request.Headers["User"].FirstOrDefault();
            
            if (userHeader == null) return AuthenticateResult.NoResult();
            
            var claims = new[] {new Claim(ClaimTypes.Name, userHeader)};
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
    
    class SerilogUsernameEnricherMiddleware
    {
        readonly RequestDelegate _next;

        public SerilogUsernameEnricherMiddleware(RequestDelegate next)
            => _next = next ?? throw new ArgumentNullException(nameof(next));
        
        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var userName = httpContext.User.Identity.Name;

            if (userName != null)
            {
                using (LogContext.PushProperty("UserName", userName))
                {
                    await _next(httpContext);
                }
            }
            else
            {
                await _next(httpContext);
            }
        }
    }
    
    class CorrelationIdHandler : DelegatingHandler
    {
        private readonly CorrelationContextAccessor _correlationContextAccessor;
        
        public CorrelationIdHandler(CorrelationContextAccessor correlationContextAccessor)
            => _correlationContextAccessor = correlationContextAccessor;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var correlationContext = _correlationContextAccessor.CorrelationContext;
            if (correlationContext != null)
            {
                request.Headers.Add("CorrelationId", _correlationContextAccessor.CorrelationContext.CorrelationId);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}