# Feature Implementation: Interjection Button During Tool Loop

## Overview

This implementation adds a dedicated button for sending interjections during tool loops, complementing the existing Ctrl+Enter keyboard shortcut. The button provides a more discoverable and accessible way for users to send additional information while the AI is processing tools.

## Implementation Details

### 1. ActionButtons Component Modifications

The `ActionButtons` component was modified to:

- Accept `inputText` and `setInputText` props from the parent `InputBar` component
- Add a new interjection button with the `MessageSquarePlus` icon from Lucide React
- Make the button only visible during tool loops when a message has been sent (`isLoading && messageSent`)
- Disable the button when there's no input text to send
- Implement a `handleInterjection` function that calls `webSocketService.sendInterjection` and clears the input field

```tsx
// New interjection button added to ActionButtons component
{isLoading && messageSent && (
    <Button
        variant="outline"
        size="icon"
        onClick={handleInterjection}
        aria-label="Send interjection"
        title="Send interjection during processing"
        disabled={!inputText.trim() || disabled}
        style={{
            backgroundColor: 'var(--inputbar-button-bg, #2d3748)',
            borderColor: 'var(--inputbar-border-color, #4a5568)',
            color: 'var(--inputbar-text-color, #e2e8f0)',
            opacity: (!inputText.trim() || disabled) ? 0.5 : 1,
            ...(window?.theme?.InputBar?.style || {})
        }}
    >
        <MessageSquarePlus className="h-5 w-5" />
    </Button>
)}
```

### 2. InputBar Component Modifications

The `InputBar` component was updated to pass the necessary props to `ActionButtons`:

```tsx
<ActionButtons
    onSend={handleSend}
    onVoiceInputClick={onVoiceInputClick}
    addAttachments={addAttachments}
    isLoading={isLoading}
    isCancelling={isCancelling}
    disabled={disabled}
    inputText={inputText}
    setInputText={setInputText}
/>
```

## User Experience

1. User sends a message to the AI
2. AI begins responding with tool use (enters a tool loop)
3. Input area is cleared but remains enabled
4. User can type a new message
5. User can either:
   - Press Ctrl+Enter to send an interjection (existing behavior)
   - Click the new interjection button (new behavior)
6. The interjection is sent to the server and stored
7. When the current tool loop phase completes, the interjection is incorporated

## Benefits

- Improved discoverability: The button makes the interjection feature more visible
- Enhanced accessibility: Provides an alternative to keyboard shortcuts
- Consistent UI: The button follows the same styling patterns as other action buttons
- Clear feedback: The button is only enabled when there's text to send

## Future Enhancements

- Add a tooltip or help text explaining what an interjection is
- Consider adding visual feedback when an interjection is successfully sent
- Explore allowing multiple interjections to be sent and tracked separately