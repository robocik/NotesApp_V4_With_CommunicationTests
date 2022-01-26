using System;
using System.IO;
using System.Threading.Tasks;
using NoteBookApp.Logic.Domain;
using NoteBookApp.Logic.Interfaces;
using NoteBookApp.Shared;

namespace NoteBookApp.Server.Infrastructure;

public class LiveDemoFileSystemProvider: IFileSystemProvider
{
    public Task<FileMetaInfo> Upload(Stream stream, FileIdentifier data)
    {
        var info = new FileMetaInfo(data.File, stream.Length);
        return Task.FromResult(info);
    }

    public Task<FileAccessToken> GenerateUploadToken(string userId, string fileName)
    {
        var token = new FileAccessToken(Constants.Demo, Constants.Demo,Guid.Empty);
        
        return Task.FromResult(token);
    }

    public Task Delete(FileIdentifier data)
    {
        return Task.CompletedTask;
    }

    public string GenerateReadToken(string companyId, string fileId, string fileName)
    {
        return Constants.Demo;
    }

    public string GetFileUrl(string container, string file, string? param = null)
    {
        return Constants.Demo;
    }
}