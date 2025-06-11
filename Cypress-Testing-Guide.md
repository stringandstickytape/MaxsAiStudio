# Cypress Testing Guide for AiStudio4

This guide outlines best practices for writing reliable Cypress tests for the AiStudio4 application, based on lessons learned from implementing the service provider management test.

## Core Principles

### 1. Prefer ARIA Labels for Accessibility and Testing
- **Use ARIA labels as primary selectors** - they improve both accessibility and test reliability
- **Fall back to unique IDs or classes** only when ARIA labels aren't practical
- **Avoid generic text-based selectors** when multiple elements might have the same text

**Selector Preference Order:**
1. ARIA labels: `aria-label`, `aria-labelledby`
2. Semantic roles: `role="button"`, `role="dialog"`
3. Unique IDs: `#component-action-element`
4. Specific classes: `.component-action-element`
5. Text content (last resort)

**Examples:**
```typescript
// Best - ARIA label (accessible + testable)
cy.get('button[aria-label="Add new service provider"]').click();

// Good - unique ID when ARIA isn't suitable
cy.get('#service-provider-management-add-provider-button').click();

// Acceptable - specific class
cy.get('.service-provider-delete-button').click();

// Bad - generic, could match multiple elements
cy.contains('button', 'Add Provider').click();
```

### 2. Handle Visibility Issues Proactively
- **Use `.scrollIntoView()`** for elements that might be outside the viewport
- **Check visibility** before interactions with `.should('be.visible')`
- **Wait for elements** to be ready before proceeding

**Example:**
```typescript
cy.get('.grid').contains('.card-base', providerName)
  .scrollIntoView()
  .within(() => {
    cy.get('.service-provider-delete-button').should('be.visible').click();
  });
```

## Component-Specific Requirements

### Modal Dialogs (UnifiedModalDialog)
**Required Changes:**
- Close buttons need `aria-label="Close modal"` for testing
- Use `[role="dialog"]` to identify modal containers

**Test Pattern:**
```typescript
// Opening modal via command
cy.get('body').type('{ctrl}k');
cy.get('#command-input').type('Modal Name');
cy.get('.command-dropdown-menu').contains('.font-medium', 'Modal Name').click();

// Verifying modal is open
cy.get('[role="dialog"]').contains('h2', 'Modal Title').should('be.visible');

// Closing modal
cy.get('button[aria-label="Close modal"]').click();
```

### Interactive Buttons
**Required Changes (in order of preference):**
1. Add descriptive `aria-label` attributes
2. Use semantic `role` attributes where appropriate
3. Add unique IDs or class names as fallback

**Examples:**
```typescript
// Best - ARIA labels for all action buttons
<Button aria-label="Add new service provider" onClick={...}>
  <PlusCircle /> Add Provider
</Button>

<Button aria-label="Edit service provider" onClick={...}>
  <Pencil />
</Button>

<Button aria-label="Delete service provider" onClick={...}>
  <Trash2 />
</Button>

// Fallback - unique IDs/classes when ARIA isn't sufficient
<Button 
  id="service-provider-management-add-provider-button"
  className="service-provider-delete-button"
  onClick={...}
>
```

**Test Patterns:**
```typescript
// Preferred - ARIA labels
cy.get('button[aria-label="Add new service provider"]').click();
cy.get('button[aria-label="Delete service provider"]').click();

// Fallback - IDs/classes
cy.get('#service-provider-management-add-provider-button').click();
cy.get('.service-provider-delete-button').click();
```

### Form Components (GenericForm)
**Test Pattern:**
```typescript
// Text inputs
cy.contains('label', 'Field Name').parent().find('input').type('value');

// Password inputs
cy.contains('label', 'API Key').parent().find('input[type="password"]').type('secret');

// Select dropdowns (shadcn)
cy.contains('label', 'Field Name').parent().find('[role="combobox"]').click();
cy.contains('[role="option"]', 'Option Value').click();

// Submit
cy.contains('button', 'Save').click(); // or use specific submitButtonId
```

### Card/Grid Components
**Required Changes:**
- Ensure cards have consistent class names (`.card-base`)
- Use unique content for identification

**Test Pattern:**
```typescript
// Wait for grid to contain item
cy.get('.grid').should('contain', itemName);

// Interact with specific card
cy.get('.grid').contains('.card-base', itemName)
  .scrollIntoView()
  .within(() => {
    // Actions within the card
  });
```

## Writing a Complete CRUD Test

### 1. Test Structure
```typescript
describe('Feature Management', () => {
  const uniqueName = `Test Item ${Date.now()}`;

  beforeEach(() => {
    cy.visit('/');
    cy.get('.InputBar').should('be.visible');
  });

  it('should create, verify, and delete an item', () => {
    // 1. Navigate to feature
    // 2. Create item
    // 3. Verify creation
    // 4. Delete item  
    // 5. Verify deletion
    // 6. Cleanup/close
  });
});
```

### 2. Comment Strategy
Include comments that reference:
- **Component names** being tested
- **Props/methods** being exercised
- **UI patterns** being followed

**Example:**
```typescript
// 3. Click the "Add Provider" button (ServiceProviderManagement component) to open the form dialog
cy.get('#service-provider-management-add-provider-button').click();

// 4. Fill out the ServiceProviderForm (uses GenericForm component)
cy.contains('label', 'Friendly Name').parent().find('input').type(providerName);
```

## Component Modification Checklist

When adding testability to components:

### Buttons
- [ ] Add descriptive `aria-label` for all action buttons (preferred)
- [ ] Use semantic `role` attributes where appropriate
- [ ] Include `aria-describedby` for additional context if needed
- [ ] Add unique `id` or `className` as fallback only
- [ ] Ensure ARIA labels are specific: "Delete service provider" not just "Delete"

### Modals
- [ ] Close button has `aria-label="Close modal"` or more specific like "Close service provider modal"
- [ ] Modal container has proper `role="dialog"`
- [ ] Modal has `aria-labelledby` pointing to header ID
- [ ] Modal has `aria-describedby` for additional context if needed
- [ ] Headers are properly structured for identification

### Forms
- [ ] Form fields have clear label associations (`aria-labelledby` or `<label>`)
- [ ] Submit buttons have descriptive `aria-label`: "Save service provider" not just "Save"
- [ ] Error messages use `aria-describedby` to link to field
- [ ] Select components use proper ARIA roles (`combobox`, `listbox`, `option`)

### Lists/Grids
- [ ] Container has `role="grid"` or `role="list"` where appropriate
- [ ] Items have `role="gridcell"` or `role="listitem"` 
- [ ] Items have `aria-label` with descriptive content: "Service provider: OpenAI"
- [ ] Action buttons within items have specific ARIA labels
- [ ] Use `aria-rowindex` or `aria-setsize` for large lists

## Common Patterns

### Command Bar Navigation
```typescript
cy.get('body').type('{ctrl}k');
cy.get('#command-input').type('Command Name');
cy.get('.command-dropdown-menu').contains('.font-medium', 'Command Name').click();
```

### Form Filling
```typescript
// Text input
cy.contains('label', 'Label Text').parent().find('input').type('value');

// Select dropdown
cy.contains('label', 'Label Text').parent().find('[role="combobox"]').click();
cy.contains('[role="option"]', 'Option Text').click();
```

### Confirmation Dialogs
```typescript
cy.get('[role="dialog"]').contains('h2', 'Confirm Action').should('be.visible');
cy.contains('button', 'Confirm').click();
```

## Debugging Failed Tests

### Common Issues and Solutions

1. **Element not found**: Add unique selectors to components
2. **Element not visible**: Use `.scrollIntoView()` and visibility checks
3. **Multiple elements match**: Make selectors more specific
4. **Timing issues**: Add appropriate waits or visibility checks
5. **Modal/overlay issues**: Ensure proper focus management and z-index

### Investigation Steps
1. Check the HTML structure in test runner
2. Verify CSS classes match expectations  
3. Confirm element visibility and positioning
4. Test selectors in browser console
5. Add debug screenshots: `cy.screenshot('debug-point')`

## Best Practices Summary

1. **ARIA First**: Prefer ARIA labels and roles over IDs/classes - they improve both accessibility and test reliability
2. **Semantic Selectors**: Use `role`, `aria-label`, `aria-labelledby` as primary selectors
3. **Descriptive Labels**: Make ARIA labels specific - "Delete service provider" not just "Delete" 
4. **Component Comments**: Reference React components and their responsibilities
5. **Visibility Handling**: Use `scrollIntoView()` for elements that might be off-screen
6. **Complete Flows**: Test entire user workflows, not just individual actions
7. **Unique Data**: Use timestamps or unique identifiers to avoid test conflicts
8. **Cleanup**: Ensure tests clean up after themselves (delete created items)
9. **Fallback Strategy**: Use the selector preference order (ARIA → roles → IDs → classes → text)

## Why ARIA Labels Matter

**For Accessibility:**
- Screen readers can understand button purposes
- Users with disabilities get better context
- Improves overall app usability

**For Testing:**
- More reliable than text content (which might change)
- Less brittle than CSS classes (which might be refactored)  
- Semantic meaning makes tests self-documenting
- Encourages developers to think about accessibility

**Example Impact:**
```typescript
// Fragile - text might change, could match multiple elements
cy.contains('button', 'Delete').click();

// Better - but still could match multiple delete buttons
cy.get('.delete-button').click();

// Best - semantic, specific, accessible
cy.get('button[aria-label="Delete service provider"]').click();
```

Following these patterns will result in more reliable, maintainable tests that accurately reflect user interactions while simultaneously improving the application's accessibility for all users.