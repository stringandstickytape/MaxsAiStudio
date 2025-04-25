// ClaudeCode.md

# ClaudeCode: AiStudio4 Project Architectural Notes

## Tool Implementation Patterns

### GitCommitTool (2024-06)
- Implements a safe, schema-driven git commit tool for the AI.
- Only allows commit of explicitly listed files, with full project root enforcement and validation.
- Follows the conventions of CodeDiffTool and other tools (schema, status, logging).
- See Docs/Feature Designs/GitCommitTool.md for full design.