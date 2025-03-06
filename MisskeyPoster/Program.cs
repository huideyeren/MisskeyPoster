using System.Text.Json.Serialization;
using System.Net;
using Misharp.Controls;
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

var miApi = app.MapGroup("/");

var misskeyHost = Environment.GetEnvironmentVariable("MISSKEY_HOST") ?? "misskey.io";

var sensitiveKeyword = Environment.GetEnvironmentVariable("SENSITIVE_KEYWORD") ?? "お清楚ふぉと";

var postPictAsync = async (PictPost post) =>
{
    var fileName = Path.GetFileName(post.MediaUrl.ToString());
    var mi = new Misharp.App(host: misskeyHost, token: post.I);
    if (post.Text.Contains(sensitiveKeyword))
    {
        post.Cw = sensitiveKeyword;
    }

    var httpClient = new HttpClient();
    var isSensitive = post.Cw != null;
    var visibility = post.Cw is not null
        ? NotesApi.NotesCreatePropertiesVisibilityEnum.Home
        : NotesApi.NotesCreatePropertiesVisibilityEnum.Public;
    using (var request = new HttpRequestMessage(HttpMethod.Get, post.MediaUrl))
    using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
    {
        if (response.StatusCode == HttpStatusCode.OK)
        {
            using (var content = response.Content)
            using (var stream = await content.ReadAsStreamAsync())
            using (var fileStream = new FileStream($"./{fileName}", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.CopyTo(fileStream);
            }

            await using var fs = new FileStream($"./{fileName}", FileMode.Open);
            
            {
                try
                {
                    var file = (await mi.DriveApi.FilesApi.Create(file: fs, isSensitive: isSensitive)).Result;
                    Task.Delay(5000).Wait();
                    var fileIds = new List<string> { file.Id };
                    var result = (await mi.NotesApi.Create(text: post.Text, cw: post.Cw, fileIds: fileIds, visibility: visibility)).Result;
                    File.Delete($"./{fileName}");
                    return "OK: " + result.CreatedNote.Url;
                }
                catch (Exception ex)
                {
                    File.Delete($"./{fileName}");
                    return $"Error: {ex.Message}";
                }
            }
        }
        return "Can not download picture.";
    }
};

miApi.MapGet("/", () => "Hello, World!");
miApi.MapPost("/postText", async (TextPost post) => {
    var visibility = post.Cw is not null
        ? NotesApi.NotesCreatePropertiesVisibilityEnum.Home
        : NotesApi.NotesCreatePropertiesVisibilityEnum.Public;
    var mi = new Misharp.App(host: misskeyHost, token: post.I);
    return (await mi.NotesApi.Create(text: post.Text, cw: post.Cw, visibility: visibility)).Result;
});
miApi.MapPost("/renote", async (Renote post) => {
    var mi = new Misharp.App(host: misskeyHost, token: post.I);
    return (await mi.NotesApi.Renotes(noteId: post.NoteId)).Result;
});
miApi.MapPost("/fav", async (Fav post) => {
    var mi = new Misharp.App(host: misskeyHost, token: post.I);
    await mi.NotesApi.ReactionsApi.Create(noteId: post.NoteId, reaction: post.Reaction = "❤️");
});
miApi.MapPost("/postPict", async (PictPost post) => await postPictAsync(post));


app.Run();

[JsonSerializable(typeof(Misharp.Response<EmptyResponse>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
    
}