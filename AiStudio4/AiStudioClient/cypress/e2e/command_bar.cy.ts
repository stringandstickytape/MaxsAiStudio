// cypress/e2e/command_bar.cy.ts

describe('Command Bar Functionality', () => {
  beforeEach(() => {
    // Visit the application's root URL before each test
    cy.visit('/');
    // Wait for the InputBar to ensure the app is fully loaded
    cy.get('.InputBar').should('be.visible');
  });

  it('should open the Models modal via the command bar', () => {
    // 1. Open the command bar using the keyboard shortcut (Ctrl+K)
    cy.get('body').type('{ctrl}k');

    // 2. Verify the command input is now visible and focused
    cy.get('#command-input').should('be.visible').and('be.focused');

    // 3. Type "models" into the command bar to filter the list
    cy.get('#command-input').type('models');

    // 4. *** FIX: Wait for the "Settings" section header to appear within the dropdown ***
    // This is the most reliable way to know the filtered list has rendered.
    cy.get('.command-dropdown-menu').contains('div.font-bold', 'Settings').should('be.visible');

    // 5. Now that we know the list is ready, find and click the "Models" item.
    // The previous selector was good, it just ran too early.
    cy.get('.command-dropdown-menu').contains('.font-medium', 'Models').click();

    // 6. Verify the Models modal has appeared
    cy.get('[role="dialog"]').should('be.visible');
    cy.get('[role="dialog"] h2').contains('Models').should('be.visible');

    // 7. Verify some content inside the modal to be sure
    cy.contains('button', 'Add Model').should('be.visible');

    // 8. Verify the modal can be closed with the Escape key
    cy.get('body').type('{esc}');
    cy.get('[role="dialog"]').should('not.exist');
  });
});