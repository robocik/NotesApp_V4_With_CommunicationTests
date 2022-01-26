using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using RestVerifier.Core;

namespace NoteBookApp.Tests.Integrations;

public class TestWebApplicationFactory
    : RestVerifier.AspNetCore.CustomWebApplicationFactory<WebApiTestStartup>
{

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddScoped<ISession>(s =>
            {
                return new Mock<ISession>().Object;
            });
        });
        return base.CreateHost(builder);
    }
    
}