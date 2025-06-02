# YouTubeSearchTool

*Searches YouTube for videos, channels, or playlists based on a query.*

## Usage

This tool allows the AI to search YouTube for relevant content. It can be used to find tutorials, reviews, official channels, or playlists related to a topic.

**Parameters:**
-   `query` (string, required): The search term(s) to use for the YouTube search.
-   `maxResults` (integer, optional, default: 10, min: 1, max: 50): The maximum number of results to return.
-   `type` (string, optional, default: "video,channel,playlist"): A comma-separated list of resource types to search for. Valid types are `video`, `channel`, and `playlist`.

## Examples

To search for "React state management tutorials" and get up to 5 video results:

```json
{
  "query": "React state management tutorials",
  "maxResults": 5,
  "type": "video"
}
```

To search for official channels related to "Blender 3D":

```json
{
  "query": "Official Blender 3D",
  "type": "channel",
  "maxResults": 3
}
```

## Notes

-   This tool requires a YouTube Data API v3 key to be configured in AiStudio4 (`File > Settings > Set YouTube API Key...`). Ensure the API key has the YouTube Data API v3 service enabled in your Google Cloud Console.
-   The output is a formatted list of search results. For videos, it includes the title and a direct URL. For channels and playlists, it provides their titles and links.
-   The search results are based on YouTube's relevance algorithms.

## Usage

*Details about how to use the tool, its parameters, and expected output.*

## Examples

*Provide examples of how to use this tool.*

## Notes

*Any specific considerations or tips for using this tool.*