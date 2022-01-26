using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using NoteBookApp.Client.Services;
using NoteBookApp.Shared;
using NUnit.Framework;
using RestVerifier.Core;
using RestVerifier.Core.Configurator;
using RestVerifier.Core.Interfaces;

namespace NoteBookApp.Tests.Integrations;


[Category("Easy")]
public class AccountDataServiceTests_Easy : RestVerifier.NUnit.TestCommunicationBase<AccountDataService>
{
    protected override void ConfigureVerifier(IGlobalSetupStarter<AccountDataService> builder)
    {
        builder.CreateClient(v =>
        {
            var service = new TestWebApplicationFactory();
            service.SetCompareRequestValidator(v);
            service.SkipAuthentication = true;
            var httpClient = service.CreateClient();
            var client = new AccountDataService(httpClient);
            return Task.FromResult(client);
        });
    }
}



[Category("Easy")]
public class FileDataServiceTests_Easy : RestVerifier.NUnit.TestCommunicationBase<FileDataService>
{
    protected override void ConfigureVerifier(IGlobalSetupStarter<FileDataService> builder)
    {
        builder.UseObjectCreator<NoteBookAppObjectCreator>();
        builder.UseComparer<NoteBookAppAssertionComparer>();
        builder.CreateClient(v =>
        {
            var fileUploader = new Mock<IFileUploader>();
            var service = new TestWebApplicationFactory();
            service.SetCompareRequestValidator(v);
            service.SkipAuthentication = true;
            var httpClient = service.CreateClient();
            var client = new FileDataService(httpClient, fileUploader.Object);
            return Task.FromResult(client);
        });
        builder.ConfigureVerify(cons =>
        {
            cons.Verify(g => g.UploadAvatarFull(Behavior.Verify<UploadFileParam>(), Behavior.Ignore<Stream>()));
            cons.Verify(g => g.UploadFile(Behavior.Verify<FileMetaData>(), Behavior.Ignore<Stream>(), Behavior.Ignore<Action<long, long>>()))
                .Returns<Guid>(g =>
                {
                    var token = new FileAccessToken("blob url", "test token",g);
                    return token;
                });
            
        });
    }
}



[Category("Easy")]
public class NoteDataServiceTests_Easy : RestVerifier.NUnit.TestCommunicationBase<NoteDataService>
{
    protected override void ConfigureVerifier(IGlobalSetupStarter<NoteDataService> builder)
    {
        builder.CreateClient(v =>
        {
            var service = new TestWebApplicationFactory();
            service.SetCompareRequestValidator(v);
            service.SkipAuthentication = true;
            var httpClient = service.CreateClient();
            var client = new NoteDataService(httpClient);
            return Task.FromResult(client);
        });
    }
}