using System.IO;
using AutoFixture;
using AutoFixture.Kernel;
using RestVerifier.AutoFixture;

namespace NoteBookApp.Tests.Integrations;

public class NoteBookAppObjectCreator:AutoFixtureObjectCreator
{
    protected override void Configure(Fixture fixture)
    {
        base.Configure(fixture);
        fixture.Register<byte[], Stream>((byte[] data) => new MemoryStream(data));
        fixture.Register<byte[], MemoryStream>((byte[] data) => new MemoryStream(data));
        fixture.Customizations.Add(
            new TypeRelay(
                typeof(System.IO.Stream),
                typeof(MemoryStream)));
    }
}