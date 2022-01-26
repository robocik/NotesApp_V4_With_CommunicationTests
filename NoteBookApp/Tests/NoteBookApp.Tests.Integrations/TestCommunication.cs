using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using AutoFixture;
using Duende.IdentityServer.Models;
using FluentAssertions;
using IdentityModel.Client;
using NoteBookApp.Server.Controllers;
using NUnit.Framework;
using RestVerifier.AutoFixture;
using RestVerifier.Core;
using RestVerifier.Core.Interfaces;

namespace NoteBookApp.Tests.Integrations;

public abstract class TestRestCommunication<TClient> : RestVerifier.NUnit.TestCommunicationBase<TClient> where TClient:notnull
{

    protected TestWebApplicationFactory _service = null!;

    public override void CreateFixture()
    {
        base.CreateFixture();

        _service = new TestWebApplicationFactory();
    }

    protected override void ConfigureVerifier(IGlobalSetupStarter<TClient> builder)
    {
        builder
            .UseComparer(CreateComparer())
            .UseObjectCreator(CreateObjectCreator());

    }

    [Test, TestCaseSource(nameof(MethodTests))]
    public Task TestWithLogin(MethodInfo method)
    {
        _builder.CreateClient(v =>
        {
            return CreateClient(v, false);
        });

        return base.TestServices(method);

    }

    [Test, TestCaseSource(nameof(MethodTests))]
    public override Task TestServices(MethodInfo method)
    {
        _builder.CreateClient(v =>
        {
            var client = CreateClient(v, true);
            return client;
        });
        _builder.OnMethodExecuted(context =>
        {
            context.Abort = context.Exception is not UnauthorizedAccessException;
            return Task.CompletedTask;
        });
        return base.TestServices(method);
    }

    protected Task LoginMe(HttpClient client)
    {
        return LoginMe(client, "openid", "profile", "NoteBookApp.ServerAPI");
    }

    protected async Task LoginMe(HttpClient client, params string[] scopes)
    {
        var disco = await client.GetDiscoveryDocumentAsync();
        var passwordTokenRequest = new PasswordTokenRequest()
        {
            Address = disco.TokenEndpoint,
            ClientId = "test_client",
            GrantType = GrantTypes.ResourceOwnerPassword.ToString(),
            Scope = string.Join(' ', scopes),
            UserName = "alice",
            Password = "password"
        };
        var res = await client.RequestPasswordTokenAsync(passwordTokenRequest);

        client.SetBearerToken(res.AccessToken);
    }

    

    protected async Task<TClient> CreateClient(CompareRequestValidator v, bool skipLogin)
    {
        _service.SetCompareRequestValidator(v);
        _service.SkipAuthentication = false;
        var httpClient = _service.CreateClient();
        var test = CreateClientFactory(httpClient);
        if (!skipLogin)
        {
            await LoginMe(httpClient);
        }

        return test;
    }

    protected virtual TClient CreateClientFactory(HttpClient httpClient)
    {
        var test = (TClient)Activator.CreateInstance(typeof(TClient), httpClient);
        return test;
    }

    protected ITestObjectCreator CreateObjectCreator()
    {
        var creator = new AutoFixtureObjectCreator();
        creator.Fixture.Register<byte[], Stream>((byte[] data) => new MemoryStream(data));
        creator.Fixture.Register<byte[], MemoryStream>((byte[] data) => new MemoryStream(data));
        return creator;
    }

    protected IObjectsComparer CreateComparer()
    {
        var creator = new NoteBookAppAssertionComparer();

        return creator;
    }
}

public class NoteBookAppAssertionComparer : IObjectsComparer
{
    public void Compare(object obj1, object obj2)
    {
        if (obj1 is UploadAvatarParameter fc1)
        {
            obj1 = fc1.Meta;
        }
        obj1.Should().BeEquivalentTo(obj2, h => h.IgnoringCyclicReferences());
    }
}