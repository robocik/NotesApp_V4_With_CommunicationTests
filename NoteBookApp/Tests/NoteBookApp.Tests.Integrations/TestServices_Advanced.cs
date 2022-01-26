using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using NoteBookApp.Client.Services;
using NoteBookApp.Shared;
using NUnit.Framework;
using RestVerifier.Core.Configurator;
using RestVerifier.Core.Interfaces;

namespace NoteBookApp.Tests.Integrations;

[Category("Advanced")]
public class NoteDataServiceTests_Advanced : TestRestCommunication<NoteDataService>
{
}

[Category("Advanced")]
public class FileDataServiceTests_Advanced : TestRestCommunication<FileDataService>
{
    protected override void ConfigureVerifier(IGlobalSetupStarter<FileDataService> builder)
    {
        base.ConfigureVerifier(builder);

        builder.ConfigureVerify(cons =>
        {
            cons.Verify(g => g.UploadAvatarFull(Behavior.Verify<UploadFileParam>(), Behavior.Ignore<Stream>()));
            cons.Verify(g => g.UploadFile(Behavior.Verify<FileMetaData>(), Behavior.Ignore<Stream>(), Behavior.Ignore<Action<long, long>>()))
                .Returns<Guid>(g =>
                {
                    var token = new FileAccessToken("blob url", "test token", g);
                    return token;
                });

        });
    }

    protected override FileDataService CreateClientFactory(HttpClient httpClient)
    {
        var fileUploader = new Mock<IFileUploader>();
        var client = new FileDataService(httpClient, fileUploader.Object);
        return client;
    }
}


[Category("Advanced")]
public class AccountDataServiceTests_Advanced : TestRestCommunication<AccountDataService>
{
}