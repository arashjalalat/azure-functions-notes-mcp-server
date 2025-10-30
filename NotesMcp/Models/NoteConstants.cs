namespace NotesMcp.Models;

internal sealed class NoteConstants
{
    // Tool Names
    public const string SaveNoteToolName = "save_note";
    public const string GetNoteToolName = "get_note";
    public const string ListNotesToolName = "list_notes";
    public const string SearchNotesToolName = "search_notes";
    public const string DeleteNoteToolName = "delete_note";

    // Tool Descriptions
    public const string SaveNoteToolDescription = "Saves a note with a title, category, tags, and content. Use for any type of note: meetings, tasks, ideas, code snippets, reminders, etc.";
    public const string GetNoteToolDescription = "Retrieves a note by its title";
    public const string ListNotesToolDescription = "Lists all saved notes with their titles and categories";
    public const string SearchNotesToolDescription = "Search notes by tags or category";
    public const string DeleteNoteToolDescription = "Deletes a note by its title";

    // Blob Configuration
    public const string BlobPath = "notes/{mcptoolargs.title}.json";
    
    // Property Names
    public const string TitleProperty = "title";
    public const string CategoryProperty = "category";
    public const string TagsProperty = "tags";
    public const string ContentProperty = "content";
    public const string QueryProperty = "query";
    
    // Property Descriptions
    public const string TitleDescription = "The title or identifier for the note";
    public const string CategoryDescription = "The category of the note (e.g., meeting, task, idea, code-snippet, reminder)";
    public const string TagsDescription = "Comma-separated tags for organizing the note";
    public const string ContentDescription = "The main content of the note";
    public const string QueryDescription = "Search query for tags or category";
    public const string TitleRetrieveDescription = "The title of the note to retrieve";
    public const string TitleDeleteDescription = "The title of the note to delete";
    
    // Default Values
    public const string DefaultCategory = "general";
}
