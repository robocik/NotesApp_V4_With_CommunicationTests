using System;
using System.Collections.Generic;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NoteBookApp.Server;
using NoteBookApp.Server.Infrastructure;
using NUnit.Framework;

namespace NoteBookApp.Tests.Integrations;
public class WebApiTestStartup : Startup
{

    public WebApiTestStartup(IConfiguration configuration, IWebHostEnvironment env) : base(configuration)
    {
        //env.EnvironmentName = EnvironmentName;
        env.ApplicationName = "NoteBookApp.Server";
    }

    public List<TestUser> GetUsers()
    {
        return new List<TestUser>
        {
            new TestUser
            {
                SubjectId =Guid.NewGuid().ToString(),
                Username = "alice",
                Password = "password",
            }
        };
    }
    public static IEnumerable<Duende.IdentityServer.Models.Client> GetClients()
    {
        return new List<Duende.IdentityServer.Models.Client>
        {
            new Duende.IdentityServer.Models.Client
            {
                ClientId = "test_client",
                ClientName = "test_client",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                AllowAccessTokensViaBrowser = false,
                RequirePkce = true,
                RequireConsent = false,

                RequireClientSecret = false,
                AlwaysIncludeUserClaimsInIdToken = true,

                AllowedScopes = new List<string>
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    "NoteBookApp.ServerAPI"
                },
            }
        };
    }

    protected override void ConfigureNHibernate(IServiceCollection services, AppSettings settings)
    {
    }

    protected override void ConfigureIdentityServer(IServiceCollection services)
    {
        services.AddIdentityServer(options =>
            {
                options.KeyManagement.Enabled = false;
            })
            .AddTestUsers(GetUsers())
            .AddInMemoryClients(GetClients())
            .AddIdentityResources()
            .AddApiResources()
            .AddDeveloperSigningCredential();

        services.AddAuthentication().AddIdentityServerJwt();

    }
    
}