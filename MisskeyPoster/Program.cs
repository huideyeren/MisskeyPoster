using System.Text.Json.Serialization;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Misharp.Models;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

var miApi = app.MapGroup("/");

var postText = async (string host, string token, string text) =>
{
    var mi = new Misharp.App(host: host, token: token);
    return (await mi.NotesApi.Create(text: text)).Result;
};

var renote = async (string host, string token, string noteId) =>
{
    var mi = new Misharp.App(host: host, token: token);
    return (await mi.NotesApi.Renotes(noteId: noteId)).Result;
};

var fav = async (string host, string token, string favId, string emoji) =>
{
    var mi = new Misharp.App(host: host, token: token);
    return (await mi.NotesApi.ReactionsApi.Create(noteId: favId, reaction: emoji)).Result;
};

var postTextWithPict = async (string host, string token, string text, string mediaUrl) =>
{
    var httpClient = new HttpClient();
    var mi = new Misharp.App(host: host, token: token);

    using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(mediaUrl)))
    using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
    {
        if (response.StatusCode == HttpStatusCode.OK)
        {
            using (var content = response.Content)
            using (var stream = await content.ReadAsStreamAsync())
            {
                var file = (await mi.DriveApi.FilesApi.Create(stream)).Result;
                return (await mi.NotesApi.Create(text: text, fileIds: new List<string> { file.Id })).Result;
            }
        }
        else throw new Exception("Post Failed.");
    }
};

miApi.MapGet("/", () => "Hello, World!");
miApi.MapPost("/postText", (string host, string token, string text) => Results.Ok(postText(host, token, text)));
miApi.MapPost("/renote", (string host, string token, string noteId) => Results.Ok(renote(host, token, noteId)));
miApi.MapPost("/fav", (string host, string token, string favId, string emoji = "❤️" ) => Results.Ok(fav(host, token, favId, emoji)));
miApi.MapPost("/postPict", (string host, string token, string text, string mediaUrl) =>
{
    try
    {
        Results.Ok(postTextWithPict(host, token, text, mediaUrl));
    }
    catch (Exception e)
    {
        Results.BadRequest(e.Message);
    }
});

[JsonSerializable(typeof(Misharp.Response<EmptyResponse>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
    
}