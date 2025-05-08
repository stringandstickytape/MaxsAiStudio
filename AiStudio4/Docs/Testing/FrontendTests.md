# Front-End User Tests for State Management

This document outlines manual and automated test cases to verify the state management improvements in the AiStudio4 web application.

## Manual Test Cases

### Command Execution Tests

1. **Command Palette Execution**
   - Open the command palette (Ctrl+K or Cmd+K)
   - Search for "New Conv"
   - Execute the command
   - **Expected**: A new conversation should be created

2. **Keyboard Shortcut Execution**
   - Press Ctrl+N (or Cmd+N on Mac)
   - **Expected**: A new conversation should be created

3. **Button Command Execution**
   - Click the "Tools" button in the input bar
   - **Expected**: The tool library should open

4. **Theme Command Execution**
   - Open the command palette
   - Search for a theme name
   - Select the theme
   - **Expected**: The theme should be applied to the application

### Window Events Tests

5. **System Prompt Selection**
   - Open the system prompt library
   - Select a system prompt with associated tools
   - **Expected**: The prompt should be applied and associated tools should be activated

6. **User Prompt Application**
   - Open the user prompt library
   - Click on a user prompt
   - **Expected**: The prompt text should be inserted into the input field

7. **Append to Prompt**
   - Type some text in the input field
   - Click a user prompt that uses the append functionality
   - **Expected**: The prompt text should be appended to the existing text

8. **Set Prompt**
   - Type some text in the input field
   - Click a user prompt that uses the set functionality
   - **Expected**: The input field should be replaced with the prompt text

### State Persistence Tests

9. **Command State Persistence**
   - Register a custom command
   - Refresh the page
   - Open the command palette
   - **Expected**: The custom command should still be available

10. **Theme Persistence**
    - Select a theme
    - Refresh the page
    - **Expected**: The selected theme should still be applied

11. **Active Tools Persistence**
    - Activate several tools
    - Refresh the page
    - **Expected**: The same tools should still be active

### Component Interaction Tests

12. **InputBar and Command Interaction**
    - Type a command shortcut (e.g., "/help") in the input field
    - Press space
    - **Expected**: The shortcut should be executed

13. **System Prompt and Tools Interaction**
    - Select a system prompt with associated tools
    - **Expected**: The tools panel should show the associated tools as active

14. **Model Selection Interaction**
    - Click on the model status bar
    - Select a different model
    - **Expected**: The model should change and be reflected in the UI

## Automated Test Cases

These tests could be implemented using Cypress, Playwright, or another front-end testing framework.

```javascript
// Example Cypress test for command execution
describe('Command Execution', () => {
  beforeEach(() => {
    cy.visit('/');
  });

  it('should execute a command from the palette', () => {
    // Open command palette
    cy.get('body').type('{ctrl+k}');
    
    // Search for New Conv
    cy.get('.command-input').type('New Conv');
    
    // Select the first result
    cy.get('.command-item').first().click();
    
    // Verify a new conversation is created
    cy.get('.conversation-item').should('exist');
  });

  it('should execute a command via keyboard shortcut', () => {
    // Use the shortcut
    cy.get('body').type('{ctrl+n}');
    
    // Verify a new conversation is created
    cy.get('.conversation-item').should('exist');
  });
});

// Example Cypress test for window events
describe('Window Events', () => {
  beforeEach(() => {
    cy.visit('/');
  });

  it('should apply a user prompt when selected', () => {
    // Open user prompt library
    cy.get('.user-prompt-button').click();
    
    // Select a prompt
    cy.get('.user-prompt-item').first().click();
    
    // Verify the prompt is inserted into the input field
    cy.get('.input-textarea').should('not.be.empty');
  });
});
```

## Regression Test Suite

To ensure the state management improvements don't introduce regressions, the following areas should be thoroughly tested:

1. **All Command Types**
   - Core commands (new conversation, clear conversation)
   - Settings commands
   - System prompt commands
   - User prompt commands
   - Theme commands
   - Tool commands

2. **All Event Types**
   - System prompt events
   - User prompt events
   - Command events
   - Input events
   - Tool events
   - Model events
   - Server events
   - Stream events

3. **All Component Interactions**
   - InputBar interactions
   - CommandInitializationPlugin functionality
   - System prompt selection and application
   - User prompt selection and application
   - Tool activation and deactivation

## Performance Tests

Since the state management improvements aim to optimize rendering, performance tests should be conducted:

1. **Rendering Performance**
   - Measure time to render the application with many conversations
   - Measure time to render the application with many commands
   - Measure time to render the application with many tools active

2. **Command Execution Performance**
   - Measure time to execute commands via different methods
   - Compare performance before and after the improvements

## Compatibility Tests

Test the application across different browsers and platforms to ensure compatibility:

1. **Browser Compatibility**
   - Chrome
   - Firefox
   - Safari
   - Edge

2. **Platform Compatibility**
   - Windows
   - macOS
   - Linux