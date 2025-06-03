# Changelog

## Changes Since Version 0.93

### New Features

#### GitLogTool
- **New Tool Added**: `GitLogTool` for retrieving git commit history between references
- Supports various output formats (oneline, short, full)
- Configurable commit limits and chronological ordering
- Useful for understanding changes between versions and generating release notes
- See [GitLogTool documentation](tools/git-log-tool.md) for details

#### Enhanced GitCommitTool
- **Push by Default**: GitCommitTool now pushes changes by default after committing
- **Optional Push Parameter**: Added `push` parameter to control whether changes are pushed
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