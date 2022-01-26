using Microsoft.AspNetCore.Identity;
using NHibernate;
using NoteBookApp.Logic.Domain;

namespace NoteBookApp.Server.Infrastructure;

public class DemoDataCreator
{
    private ISession _nhSession;
    private StandardDateTimeProvider _dateTime;

    public DemoDataCreator(ISession nhSession)
    {
        _nhSession = nhSession;
        _dateTime = new StandardDateTimeProvider();
    }

    public void Create()
    {
        var applicationUser = new ApplicationUser();
        applicationUser.NormalizedUserName = "TEST@NOTEAPP.PL";
        applicationUser.Email= applicationUser.UserName= "test@noteapp.pl";
        applicationUser.NormalizedEmail = "TEST@NOTEAPP.PL";
        applicationUser.EmailConfirmed = true;
        applicationUser.SecurityStamp = "1234";

        PasswordHasher<ApplicationUser> hasher = new PasswordHasher<ApplicationUser>();
        var hash = hasher.HashPassword(applicationUser, "test");
        applicationUser.PasswordHash = hash;
        _nhSession.Save(applicationUser);

        var note = new Note();
        note.Content = "Test note 1";
        note.CreatedDateTime = _dateTime.UtcNow.AddDays(-30);
        note.User = applicationUser;
        _nhSession.Save(note);

        var file = new File();
        file.CreatedDateTime = _dateTime.UtcNow.AddDays(-30);
        file.FileName = "opis.txt";
        file.Length = 1000;
        file.CreatedBy = applicationUser;
        file.ObjectId = note.Id;
        _nhSession.Save(file);

        note.Files.Add(file);
        _nhSession.Update(note);

        var note2 = new Note();
        note2.Content = "Test note 2";
        note2.CreatedDateTime = _dateTime.UtcNow.AddDays(-14);
        note2.User = applicationUser;
        _nhSession.Save(note2);

        var file2 = new File();
        file2.CreatedDateTime = _dateTime.UtcNow.AddDays(-10);
        file2.FileName = "opis.txt";
        file2.Length = 1000;
        file2.CreatedBy = applicationUser;
        file2.ObjectId = note.Id;
        _nhSession.Save(file2);

        note2.Files.Add(file2);
        _nhSession.Update(note2);
    }
}