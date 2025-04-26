// Docs/Feature Designs/GitDiffAttachment.md

# Feature Design: Git Diff Attachment Option in Paperclip Menu

## Overview

Add a "Git Diff" option to the paperclip (attachment) menu in the InputBar. When selected, this triggers a callback to the server, which generates a complete git diff (including new/untracked files) for the current project (as defined by GeneralSettings.ProjectPath) and returns it as a downloadable text attachment to the client.

## UI/UX
- The paperclip menu (FileAttachment.tsx) will have a new dropdown item: "Git Diff".
- When clicked, it will:
  - Show a loading state (optional, for feedback)
  - Call a new API endpoint (e.g., `/api/gitDiff`)
  - On success, the client will receive a text file attachment containing the git diff and list of new files, which will be added to the attachments bar.

## Backend
- Add a new API endpoint `/api/gitDiff`.
- The handler will:
  - Use GeneralSettingsService.CurrentSettings.ProjectPath to locate the repo root.
  - Run `git diff HEAD` to get the diff for tracked files.
  - Run `git ls-files --others --exclude-standard` to get new/untracked files.
  - Optionally, for each new file, include its contents in the diff output (or just list names).
  - Combine results into a single text blob.
  - Return as a downloadable attachment (e.g., Content-Disposition: attachment; filename="git-diff.txt").

## Client Integration
- On receiving the file, add it to the attachments using the same mechanism as other file attachments.
- If error, show an alert or error message.

## Security/Validation
- Only allow this operation if ProjectPath is a valid git repo.
- Limit output size if necessary.

## Implementation Steps
1. Update FileAttachment.tsx to add the new menu item and trigger the API call.
2. Implement the backend API handler.
3. Add logic to run git commands and format the output.
4. Integrate with the attachment manager on the client.
5. Test end-to-end.

## Open Questions
- Should the diff include full contents of new/untracked files or just their names?
- Should there be a loading indicator?

## Future Enhancements
- Allow user to select diff range (e.g., against previous commit).
- Allow filtering by file type or path.