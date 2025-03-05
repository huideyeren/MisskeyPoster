using System.ComponentModel.DataAnnotations;
using static Misharp.Controls.NotesApi;

namespace MisskeyPoster;

public class TextPost
{
     public required string I { get; set; }
     public required string Text { get; set; }
     public string? Cw { get; set; } = null;
}

public class PictPost
{
     public required string I { get; set; }
     public required string Text { get; set; }
     public string? Cw { get; set; } = null;
     public required Uri MediaUrl { get; set; }
}

public class Renote
{
     public required string I { get; set; }
     public required string NoteId { get; set; }
}

public class Fav
{
     public required string I { get; set; }
     public required string NoteId { get; set; }
     public string? Reaction { get; set; } = null;
}