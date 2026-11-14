# SwiftDrop AI Assistant

The AI Assistant provides a React chat page at `/assistant` and backend endpoints under
`/api/v1/ai-assistant`.

## Local setup

Create a Gemini API key in Google AI Studio. Set it only in the terminal that starts the
backend; never commit the real value.

Windows Command Prompt:

```cmd
set "AI__ApiKey=your-gemini-api-key"
dotnet run --project backend\src\Host\SwiftDrop.Host
```

The default provider uses Gemini's OpenAI-compatible endpoint with
`gemini-3.5-flash-lite`. These optional environment variables override the defaults:

- `AI__Endpoint`
- `AI__Model`
- `AI__InternalApiBaseUrl`

## Endpoints

- `POST /api/v1/ai-assistant/chat` - chat, fee estimation, and parcel tracking.
- `GET /api/v1/ai-assistant/mcp/tools` - lists the assistant tools.
- `POST /api/v1/ai-assistant/mcp` - MCP JSON-RPC endpoint supporting `tools/list` and `tools/call`.

Example chat request:

```json
{
  "message": "How much to send 2 kg to Colombo?"
}
```

The API key is used only by the backend. The frontend and browser never receive it.
