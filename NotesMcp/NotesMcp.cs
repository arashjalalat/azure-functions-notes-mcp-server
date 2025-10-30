using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using System.Text.Json.Nodes;
using static NotesMcp.Models.NoteConstants;
using Azure.Identity;

namespace NotesMcp;

public class NotesMcp(ILogger<NotesMcp> logger)
{
    private readonly ILogger<NotesMcp> _logger = logger;
    private readonly string containerName = BlobPath?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "notes";

    [Function(nameof(SaveNote))]
    [BlobOutput(BlobPath)]
    public string SaveNote(
        [McpToolTrigger(SaveNoteToolName, SaveNoteToolDescription)] ToolInvocationContext context,
        [McpToolProperty("title", "The title or identifier for the note", true)] string title,
        [McpToolProperty("category", "The category of the note (e.g., meeting, task, idea, code-snippet, reminder)")] string category,
        [McpToolProperty("tags", "Comma-separated tags for organizing the note")] string tags,
        [McpToolProperty("content", "The main content of the note", true)] string content)
    {
        _logger.LogInformation("Saving note: {title}", title);
        
        var note = new
        {
            Title = title,
            Category = category ?? "general",
            Tags = tags?.Split(',').Select(t => t.Trim()).ToList() ?? [],
            Content = content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(note, new JsonSerializerOptions { WriteIndented = true });
    }

    [Function(nameof(GetNote))]
    public string GetNote(
        [McpToolTrigger(GetNoteToolName, GetNoteToolDescription)] ToolInvocationContext context,
        [McpToolProperty("title", "The title of the note to retrieve", true)] string title,
        [BlobInput(BlobPath)] string noteContent)
    {
        _logger.LogInformation("Retrieving note: {title}", title);
        if (string.IsNullOrEmpty(noteContent))
        {
            return JsonSerializer.Serialize(new { error = "Note not found", title });
        }

        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(noteContent);
        }
        catch
        {
            parsed = null;
        }

        var previewLength = 500;
        var preview = noteContent.Length > previewLength ? string.Concat(noteContent.AsSpan(0, previewLength), "...") : noteContent;

        var result = new
        {
            title,
            found = true,
            rawContent = noteContent,
            parsedContent = parsed,
            preview
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [Function(nameof(ListNotes))]
    public async Task<string> ListNotes(
        [McpToolTrigger(ListNotesToolName, ListNotesToolDescription)] ToolInvocationContext context)
    {
        _logger.LogInformation("Listing all notes");
        
        var containerClient = GetBlobContainerClient();
        var results = new List<object>();

        try
        {
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                results.Add(new
                {
                    name = blobItem.Name,
                    contentLength = blobItem.Properties.ContentLength ?? 0,
                    contentType = blobItem.Properties.ContentType ?? string.Empty,
                    lastModified = blobItem.Properties.LastModified?.UtcDateTime,
                    metadata = blobItem.Metadata
                });
            }
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Error listing blobs in container {container}", containerName);
            return JsonSerializer.Serialize(new { error = "Failed to access notes storage", details = ex.Message });
        }

        return JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
    }

    [Function(nameof(SearchNotes))]
    public async Task<string> SearchNotes(
        [McpToolTrigger(SearchNotesToolName, SearchNotesToolDescription)] ToolInvocationContext context,
        [McpToolProperty("query", "Search query for tags or category")] string query)
    {
        _logger.LogInformation("Searching notes with query: {query}", query);

        var containerClient = GetBlobContainerClient();

        var results = new List<JsonObject>();

        try
        {
            var q = (query ?? string.Empty).Trim();
            bool matchAll = string.IsNullOrEmpty(q);

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                try
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var download = await blobClient.DownloadContentAsync();
                    var content = download.Value.Content.ToString();

                    if (string.IsNullOrWhiteSpace(content))
                        continue;

                    var node = JsonNode.Parse(content)?.AsObject();
                    if (node == null)
                        continue;

                    var category = node["Category"]?.GetValue<string>() ?? string.Empty;
                    var tagsNode = node["Tags"];
                    var tags = new List<string>();
                    if (tagsNode is JsonArray ja)
                    {
                        tags = [.. ja.Select(t => t?.ToString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s))];
                    }

                    bool isMatch = matchAll;
                    if (!isMatch && !string.IsNullOrEmpty(category) && category.Contains(q, StringComparison.OrdinalIgnoreCase))
                        isMatch = true;

                    if (!isMatch && tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)))
                        isMatch = true;

                    if (!isMatch)
                    {
                        var title = node["Title"]?.GetValue<string>() ?? string.Empty;
                        var contentField = node["Content"]?.GetValue<string>() ?? string.Empty;
                        if (!string.IsNullOrEmpty(title) && title.Contains(q, StringComparison.OrdinalIgnoreCase))
                            isMatch = true;
                        if (!isMatch && !string.IsNullOrEmpty(contentField) && contentField.Contains(q, StringComparison.OrdinalIgnoreCase))
                            isMatch = true;
                    }

                    if (isMatch)
                    {
                        node["_blobName"] = blobItem.Name;
                        results.Add(node);
                    }
                }
                catch (RequestFailedException rfe)
                {
                    _logger.LogWarning(rfe, "Failed to read blob {blob}", blobItem.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unexpected error reading blob {blob}", blobItem.Name);
                }
            }
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Error listing blobs in container {container}", containerName);
            return JsonSerializer.Serialize(new { error = "Failed to access notes storage", details = ex.Message });
        }

        return JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
    }

    [Function(nameof(DeleteNote))]
    public async Task<string> DeleteNote(
        [McpToolTrigger(DeleteNoteToolName, DeleteNoteToolDescription)] ToolInvocationContext context,
        [McpToolProperty("title", "The title of the note to delete", true)] string title)
    {
        _logger.LogInformation("Deleting note: {title}", title);

        var containerClient = GetBlobContainerClient();
        BlobClient blobClient = containerClient.GetBlobClient(title + ".json");

        bool deleted = await blobClient.DeleteIfExistsAsync();

        var result = new
        {
            success = deleted,
            message = deleted
                ? $"Note '{title}' deleted successfully"
                : $"Note '{title}' not found"
        };

        return JsonSerializer.Serialize(result);
    }
    
    private BlobContainerClient GetBlobContainerClient()
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var azureBlobUri = Environment.GetEnvironmentVariable("AzureWebJobsStorage__blobServiceUri");

        BlobServiceClient? blobServiceClient;

        if (!string.IsNullOrEmpty(connectionString))
        {
            blobServiceClient = new BlobServiceClient(connectionString);
        }
        else if (!string.IsNullOrEmpty(azureBlobUri))
        {
            blobServiceClient = new BlobServiceClient(new Uri(azureBlobUri), new DefaultAzureCredential());
        }
        else
        {
            _logger.LogError("AzureWebJobsStorage or AzureWebJobsStorage__blobServiceUri not configured");
            throw new InvalidOperationException("Storage configuration is missing");
        }
        
        return blobServiceClient.GetBlobContainerClient(containerName);
    }
}
