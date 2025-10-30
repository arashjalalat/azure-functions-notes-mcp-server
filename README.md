# Azure Functions Notes MCP Server

An Azure Functions-based MCP (Model Context Protocol) server implementation for managing notes and related functionality. This project demonstrates how to build a MCP server using Azure Functions, following best practices in a cloud native way.

---

## Features

- **MCP Protocol Support**: Implements the Model Context Protocol for seamless integration with GitHub Copilot Chat and other MCP-aware clients
- **Azure Functions**: Built on .NET 8.0 using the latest Azure Functions runtime
- **Infrastructure as Code**: Complete infrastructure definition using Bicep templates
- **Azure Integration**: Integration with Azure services including Application Insights for monitoring
- **Role-Based Access Control (RBAC)**: Access control for various Azure resources

### MCP Tools

The server provides the following MCP tools for note management:

| Tool Name | Description |
|-----------|-------------|
| `save_note` | Saves a note with a title, category, tags, and content. Use for any type of note: meetings, tasks, ideas, code snippets, reminders, etc. |
| `get_note` | Retrieves a note by its title |
| `list_notes` | Lists all saved notes with their titles and categories |
| `search_notes` | Search notes by tags or category |
| `delete_note` | Deletes a note by its title |

#### Tool Properties

- **save_note**:
  - `title` (required): The title or identifier for the note
  - `category`: The category of the note (e.g., meeting, task, idea, code-snippet, reminder)
  - `tags`: Comma-separated tags for organizing the note
  - `content` (required): The main content of the note

- **get_note**:
  - `title` (required): The title of the note to retrieve

- **search_notes**:
  - `query`: Search query for tags or category

- **delete_note**:
  - `title` (required): The title of the note to delete

---

## Prerequisites

For local development:
- [Azure Developer CLI (azd)](https://aka.ms/install-azd)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- An Azure subscription
- Visual Studio Code (recommended) with:
  - C# Dev Kit extension
  - Azure Functions extension
  - Bicep extension

---

## Getting Started

### Local Development

1. Clone the repository:
```bash
git clone <repository-url>
cd azure-functions-mcp-server
```

2. Install dependencies:
```bash
dotnet restore
```

3. Configure local settings:
- Copy `local.settings.sample.json` to `local.settings.json`

4. Run the project locally:
```bash
cd NotesMcp
func start
```

### Using with VS Code and GitHub Copilot

To use the MCP tools in VS Code, you'll need to configure the MCP endpoint in your workspace. Create a `.vscode/mcp.json` file with the  configuration based on your environment:

#### Local Development
When running the Function App locally:

```json
{
    "servers": {
        "notes-mcp": {
            "type": "http",
            "url": "http://localhost:7071/runtime/webhooks/mcp"
        }
    }
}
```

### Deployment

The project uses Azure Developer CLI (azd)

#### Deploy to Azure

1. Initialize the environment:
```bash
azd init
```

2. Login to Azure:
```bash
azd auth login
```

3. Provision and deploy:
```bash
azd up
```

This command will:
- Create required Azure resources
- Deploy the application code
- Set up configurations
- Output the Function App URL

To remove all resources:
```bash
azd down --purge
```

#### Usage after deployment
When using the deployed Function App in Azure the `mcp.json` from the client looks like this:

```json
{
    "inputs": [
        {
            "type": "promptString",
            "id": "function-key",
            "description": "Azure Function App Key",
            "password": true
        }
    ],
    "servers": {
        "notes-mcp": {
            "type": "http",
            "url": "https://<replace with correct value>/runtime/webhooks/mcp", //See Function App URL
            "headers": {
              "x-functions-key": "${input:function-key}"
            }
        }
    }
}
```

#### Examples

1. **Saving a note**
   - Select the text you want to save in VS Code
   - Open GitHub Copilot Chat
   - Switch to Agent mode (click the robot icon)
   - Type a command like:
     ```
     Save this selected text as a note titled "git-commands" with category "code-snippet" and tags "git,commands,reference"
     ```
   This will invoke the `save_note` tool with your selected text as content.

<p align="center">
  <img src="https://raw.githubusercontent.com/arashjalalat/azure-functions-notes-mcp-server/main/images/save_note_mcp.jpg" alt="Saving a note"><br>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/arashjalalat/azure-functions-notes-mcp-server/main/images/save_note_output_mcp.jpg" alt="Saving a note output"><br>
</p>

2. **Retrieving a note**
   - In GitHub Copilot Chat (Agent mode), type:
     ```
     Get the note with title "git-commands"
     ```
   This will use the `get_note` tool to fetch and display the note.

<p align="center">
  <img src="https://raw.githubusercontent.com/arashjalalat/azure-functions-notes-mcp-server/main/images/get_note_output_mcp.jpg" alt="Get note"><br>
</p>

3. **Listing all notes**
   - In GitHub Copilot Chat (Agent mode), type:
     ```
     List all my saved notes
     ```
   This will use the `list_notes` tool to show all your saved notes.

4. **Searching notes**
   - In GitHub Copilot Chat (Agent mode), type:
     ```
     Search for notes with tag "git"
     ```
   This will use the `search_notes` tool to find relevant notes.

<p align="center">
  <img src="https://raw.githubusercontent.com/arashjalalat/azure-functions-notes-mcp-server/main/images/search_notes_output_mcp.jpg" alt="Search notes"><br>
</p>

5. **Delete a note**
   - In GitHub Copilot Chat (Agent mode), type:
     ```
     Delete my note called git-commands
     ```
   This will use the `delete_note` tool to delete the note.

<p align="center">
  <img src="https://raw.githubusercontent.com/arashjalalat/azure-functions-notes-mcp-server/main/images/delete_note_mcp.jpg" alt="Delete note"><br>
</p>

6. **Listing all notes (verify)**
   - In GitHub Copilot Chat (Agent mode), type:
     ```
     List all my saved notes
     ```
   This will use the `list_notes` tool to show all your saved notes. Verify that after delete nothing is saved anymore.

<p align="center">
  <img src="https://raw.githubusercontent.com/arashjalalat/azure-functions-notes-mcp-server/main/images/list_notes_mcp.jpg" alt="List notes"><br>
</p>

---

## Infrastructure

The project uses Bicep for Infrastructure as Code with the following components:

- `main.bicep`: Main infrastructure template
- `app/api.bicep`: API-related resources
- `app/monitoring.bicep`: Monitoring setup with Application Insights
- `rbac/`: Role-based access control definitions
  - `appinsights-access.bicep`
  - `storage-access.bicep`

---

## Guidelines & best practices

### Region availability

Azure Function App availability (including Flex Consumption) varies by region, verify availability [here](
https://learn.microsoft.com/en-us/azure/azure-functions/flex-consumption-how-to?tabs=azure-cli%2Cazure-cli-publish&pivots=programming-language-csharp#view-currently-supported-regions).

Choose the same region for related resources (Function App, Storage account) to reduce latency and egress costs. **westeurope** is a good default if you're in or near Europe.

### Cost considerations

Estimate costs using the [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/). Primary cost drivers:

- Azure Functions – Consumption or Flex tiers (invocations, memory/time)
- Storage Account – capacity and transactions

Tip: enable cost alerts in your subscription and test workloads in a sandbox subscription before production.

### Security

This project uses a **User-Assigned Managed Identity** for authentication between the Azure resources.

The Function App is assigned the following RBAC roles:
  * Storage Blob Data Owner
  * Application Insights Monitoring Metrics Publisher

When using in Production environment, the following is strongly recommended:

* Restrict inbound traffic with Private endpoints + VNet integration
* Use service endpoints + firewall rules where applicable

---

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

---

## License

[MIT License](LICENSE)