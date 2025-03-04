using System.Text.Json.Serialization;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Misharp.Models;
using MisskeyPoster;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Urls.Add("http://*:8080");

var miApi = app.MapGroup("/");

var misskeyHost = Environment.GetEnvironmentVariable("MISSKEY_HOST") ?? "misskey.io";

var sensitiveKeyword = Environment.GetEnvironmentVariable("SENSITIVE_KEYWORD") ?? "お清楚ふぉと";

miApi.MapGet("/", () => "Hello, World!");
miApi.MapPost("/postText", (TextPost post) => {
    var mi = new Misharp.App(host: misskeyHost, token: post.I);
    return mi.NotesApi.Create(text: post.Text, cw: post.Cw).Result.Result;
});
miApi.MapPost("/renote", (Renote post) => {
    var mi = new Misharp.App(host: misskeyHost, token: post.I);
    return mi.NotesApi.Renotes(noteId: post.NoteId).Result.Result;
});
miApi.MapPost("/fav", (Fav post) => {
    var mi = new Misharp.App(host: misskeyHost, token: post.I);
    return mi.NotesApi.ReactionsApi.Create(noteId: post.NoteId, reaction: post.Reaction).Result.Result;
});
miApi.MapPost("/postPict", (PictPost post) =>
{
    var httpClient = new HttpClient();
    var mi = new Misharp.App(host: misskeyHost, token: post.I);
    if (post.Text.Contains(sensitiveKeyword))
    {
        post.Cw = sensitiveKeyword;
    }

    using (var request = new HttpRequestMessage(HttpMethod.Get, post.MediaUrl))
    using (var response = httpClient.Send(request, HttpCompletionOption.ResponseHeadersRead))
    {
        if (response.StatusCode == HttpStatusCode.OK)
        {
            using (var content = response.Content)
            using (var stream = content.ReadAsStream())
            {
                DriveFileModel file;
                if (post.Cw is not null)
                {
                    file = mi.DriveApi.FilesApi.Create(file: stream, isSensitive: true).Result.Result;
                }
                else
                {
                    file = mi.DriveApi.FilesApi.Create(file: stream).Result.Result;
                }
                
                var files = new List<string>{ file.Id };
                return mi.NotesApi.Create(text: post.Text, cw: post.Cw, fileIds: files).Result.Result;
            }
        }
        else throw new Exception("Post Failed.");
    }
});

app.Run();

[JsonSerializable(typeof(Misharp.Response<EmptyResponse>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
    
}