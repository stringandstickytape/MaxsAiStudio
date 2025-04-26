# ClaudeCode.md

This file contains key insights and documentation for the MaxsAiTool project.

## Project Architecture

### Core Components

- **AiStudio4**: Main application with WPF UI and web components
- **AiServices**: AI service integrations (Claude, OpenAI, etc.)
- **SharedClasses**: Common utilities and models
- **VSIXTest**: Visual Studio extension integration
- **ChromeExtension**: Chrome browser extension

### Key Patterns

- **Service-based architecture**: Dependency injection for services
- **WebView2 integration**: Web UI components in WPF application
- **Message-based communication**: Between components

## Implementation Decisions

### AI Integration

- Multiple AI service providers supported through common interface
- Streaming responses for real-time feedback
- Tool-based extensions for AI capabilities

### UI Architecture

- React-based web components for rich UI
- WPF shell for desktop integration
- Theme customization support

## Project Structure Evolution

### Web Components

- Modern React with TypeScript
- Zustand for state management
- Tailwind CSS for styling

### Desktop Integration

- WebView2 for embedding web UI
- WPF for native Windows features
- File system access for persistence

## Feature Documentation

### Conversation Management

- Linear and branched conversation models
- Message history with metadata
- Attachments support

### Tool Integration

- Built-in tools for common operations
- Extensible tool framework
- Tool execution in AI context
- Debug file generation for CodeDiff merge failures

### Jump to End Functionality

#### Overview
The Jump to End feature allows users to control whether the conversation view automatically scrolls to the bottom when new messages arrive. This is implemented via a toggle button in the InputBar component and managed through a Zustand store.

#### Implementation Details

- **Store**: `useJumpToEndStore` maintains a boolean state `jumpToEndEnabled`
- **UI Control**: Toggle button in InputBar with ArrowDownToLine icon
- **Scroll Management**: 
  - Programmatic scrolling is tracked with `isScrollingProgrammatically` ref
  - User-initiated scrolling is detected through multiple event listeners:
    - `scroll` events for general scrolling
    - `wheel` events for mouse wheel scrolling
    - `touchmove` events for touch devices
    - `mousedown` events for scrollbar interaction
  - When user scrolls away from bottom, `jumpToEndEnabled` is automatically set to false
  - When user scrolls to bottom, `jumpToEndEnabled` is automatically set to true

#### Recent Fixes

- Enhanced event handling to properly detect all user scroll interactions
- Improved flag management for programmatic vs. user scrolling
- Added multiple event listeners to catch all scroll-related user interactions
- Implemented more robust timing mechanisms to handle browser inconsistencies
- Added explicit user intent detection to prevent false positives