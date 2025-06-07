# Changelog

All notable changes to AiStudio4 will be documented in this file.

## [Unreleased]

### Added
- **Repack Project Source Code**: New menu item `Project > Repack Project Source Code` that allows quick re-packaging of project source code using the previously saved output file path
- **DRY Refactored Packaging Logic**: Extracted common packaging logic into a reusable `ExecutePackingOperationAsync` helper method for better maintainability
- **Last Packer Output File Persistence**: The application now remembers the last used output file path for packing operations, stored in `GeneralSettings.LastPackerOutputFile`

### Changed
- **Improved Pack Project Source Code**: Refactored the existing pack functionality to use the new DRY architecture and automatically save the output path for future repack operations
- **Enhanced User Experience**: Streamlined workflow for iterative development where users frequently need to update packaged project snapshots

### Technical Details
- Added `LastPackerOutputFile` property to `GeneralSettings.cs`
- Created `ExecutePackingOperationAsync` helper method in `MainWindow.xaml.cs`
- Refactored `PackProjectSourceCode_Click` to use the new helper method and save output path
- Added new `RepackProjectSourceCode_Click` event handler for the repack functionality
- Updated documentation in user guide sections 4.8 and 5.4.2 to cover the new repack feature

## Changes Since Version 0.93

### New Features

#### Git Branch Management and GitHub PR Tools
- **New GitBranch Tool**: Complete branch management including create, switch, list, delete operations
- **New GitStatus Tool**: Repository state information including working directory status, current branch, and remote tracking
- **New GitHubCreatePullRequest Tool**: Create pull requests directly from the AI with full GitHub API integration
- **Enhanced GitCommitTool**: Added `push_new_branch` parameter to automatically set upstream for new branches
- Complete workflow support: create issue → create branch → fix code → push branch → submit PR
- See [GitBranch documentation](tools/git-branch-tool.md), [GitStatus documentation](tools/git-status-tool.md), and [GitHubCreatePullRequest documentation](tools/github-create-pull-request-tool.md) for details

#### GitLogTool
- **New Tool Added**: `GitLogTool` for retrieving git commit history between references
- Supports various output formats (oneline, short, full)
- Configurable commit limits and chronological ordering
- Useful for understanding changes between versions and generating release notes
- See [GitLogTool documentation](tools/git-log-tool.md) for details

#### Enhanced GitCommitTool
- **Push by Default**: GitCommitTool now pushes changes by default after committing
- **Optional Push Parameter**: Added `push` parameter to control whether changes are pushed
- **New Branch Support**: Added `push_new_branch` parameter for automatic upstream setting
- **Backward Compatibility**: Existing usage continues to work, but now pushes automatically
- See updated [GitCommitTool documentation](tools/git-commit-tool.md) for details

### UI/UX Improvements

#### InputBar Layout Redesign
- **Consolidated Action Bar**: Model status, tools, and temperature controls moved to a horizontal top action bar
- **Improved Status Messages**: Status messages now display prominently above the input area for better visibility
- **Better Organization**: All interactive elements are logically grouped for improved usability
- **Reduced Layout Disruption**: Fixed issues where status message wrapping could disrupt the button bar layout

#### Panel Management
- **Panel Size Persistence**: Panel sizes are now automatically saved and restored between sessions
- **Debounced Saving**: Implemented debounced saving to prevent performance issues during resizing
- **Better User Experience**: Panels remember their size preferences across application restarts

### Bug Fixes

#### Google Drive Integration
- **Fixed Conversation Loading**: Resolved issues where imported conversations from Google AI Studio would fail to load properly
- **Improved Import Process**: Enhanced the Google Drive file download process to return both filename and content
- **Better Error Handling**: Improved error handling during the import/export process

#### Git Integration
- **GitLogTool Error Handling**: Fixed GitLogTool to properly handle errors and only continue processing on success
- **Improved Reliability**: Better error cascading prevention in git operations

### Technical Improvements

#### Code Quality
- **Refactored Components**: Consolidated InputBar layout components for better maintainability
- **Improved Architecture**: Better separation of concerns in UI components
- **Enhanced Error Handling**: More robust error handling across various tools and features

#### Performance
- **Debounced Panel Saving**: Prevents excessive file I/O during panel resizing
- **Optimized Layout**: Reduced layout recalculations in the InputBar area

### Documentation Updates
- Updated user manual to reflect all new features and improvements
- Added comprehensive GitLogTool documentation
- Updated GitCommitTool documentation with new push functionality
- Enhanced troubleshooting guide with solutions for new features
- Updated core concepts and key features documentation

### Developer Notes
- All changes maintain backward compatibility
- New features follow established patterns and conventions
- UI improvements enhance the overall user experience without breaking existing workflows