using NoteBookApp.Server.Controllers;
using RestVerifier.FluentAssertions;

namespace NoteBookApp.Tests.Integrations;


public class NoteBookAppAssertionComparer : FluentAssertionComparer
{
    public override void Compare(object obj1, object obj2)
    {
        if (obj1 is UploadAvatarParameter fc1)
        {
            obj1 = fc1.Meta;
        }
        base.Compare(obj1, obj2);
    }
}