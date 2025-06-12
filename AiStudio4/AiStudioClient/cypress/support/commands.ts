// cypress/support/commands.ts

/**
 * Opens the command bar with Ctrl+K and verifies it's ready.
 */
Cypress.Commands.add('openCommandBar', () => {
  cy.get('body').type('{ctrl}k');
  cy.get('#command-input').should('be.visible').and('be.focused');
});

/**
 * Opens the command bar and executes a given command by name.
 * @param {string} commandName The exact text of the command to execute.
 */
Cypress.Commands.add('executeCommand', (commandName: string) => {
  cy.openCommandBar();
  cy.get('#command-input').type(commandName);
  // Wait for any section header to ensure the list has filtered and rendered
  cy.get('.command-dropdown-menu').find('div.font-bold').first().should('be.visible');
  cy.get('.command-dropdown-menu').contains('.font-medium', commandName).click();
});

/**
 * Closes any active modal dialog using the Escape key and verifies it's gone.
 */
Cypress.Commands.add('closeModal', () => {
  cy.get('body').type('{esc}');
  cy.get('[role="dialog"]').should('not.exist');
});

/**
 * Creates a new Service Provider via the UI.
 * Assumes the command bar is available.
 */
Cypress.Commands.add('createProvider', (provider: { name: string }) => {
  cy.executeCommand('Service Providers');
  cy.get('[role="dialog"]').contains('h2', 'Service Providers').should('be.visible');
  cy.contains('button', 'Add Provider').click();

  // Fill Form
  cy.get('[role="dialog"]').contains('h2', 'Add New Provider').should('be.visible');
  cy.contains('label', 'Friendly Name').parent().find('input').type(provider.name);
  cy.contains('label', 'Service Name').parent().find('[role="combobox"]').click();
  cy.contains('[role="option"]', 'Claude').click(); // Defaulting for this example
  cy.contains('label', 'API URL').parent().find('input').type('https://api.test-provider.com/v1');
  cy.contains('label', 'API Key').parent().find('input[type="password"]').type('test-api-key-12345');
  cy.contains('label', 'Charging Strategy').parent().find('[role="combobox"]').click();
  cy.contains('[role="option"]', 'Claude Caching Model').click();

  // Submit
  cy.get('#service-provider-management-add-provider-button').click();
  cy.get('[role="dialog"]').contains('h2', 'Service Providers').should('be.visible');
});

/**
 * Creates a new Model via the UI.
 * Assumes the command bar is available and the specified provider exists.
 */
Cypress.Commands.add('createModel', (model: { name: string; providerName: string }) => {
  cy.executeCommand('Models');
  cy.get('[role="dialog"]').contains('h2', 'Models').should('be.visible');
  cy.contains('button', 'Add Model').click();

  // Fill Form
  cy.get('[role="dialog"]').contains('h2', 'Add New Model').should('be.visible');
  cy.contains('label', 'Friendly Name').parent().find('input').type(model.name);
  cy.contains('label', 'Model Name').parent().find('input').type('gpt-4-test'); // Defaulting
  cy.get('#trigger-providerGuid').click();
  cy.contains('[role="option"]', model.providerName).click();

  // Submit
  cy.get('#model-management-add-model-button').click();
  cy.get('[role="dialog"]').contains('h2', 'Models').should('be.visible');
  cy.get('.model-modal-grid').should('contain', model.name);
});

/**
 * Finds and deletes an item (Model or Provider) from its management modal.
 * @param {string} itemName The name of the item to delete.
 * @param {'model' | 'provider'} itemType The type of item.
 */
Cypress.Commands.add('deleteItemInModal', (itemName: string, itemType: 'model' | 'provider') => {
  const command = itemType === 'model' ? 'Models' : 'Service Providers';
  const gridSelector = itemType === 'model' ? '.model-modal-grid' : '.server-modal-grid';
  const deleteButtonSelector = itemType === 'model' ? 'button:last' : '.service-provider-delete-button';

  cy.executeCommand(command);

  // Use .then() to handle cases where the item might not exist (e.g., failed creation)
  cy.get('body').then($body => {
    if ($body.find(`${gridSelector}:contains("${itemName}")`).length) {
      cy.get(gridSelector)
        .contains('.card-base', itemName)
        .scrollIntoView()
        .within(() => {
          cy.get(deleteButtonSelector).click();
        });

      cy.get('[role="dialog"]').contains('h2', 'Confirm Deletion').should('be.visible');
      cy.contains('button', 'Delete').click();
      cy.get(gridSelector, { timeout: 10000 }).should('not.contain', itemName);
    }
  });

  cy.closeModal();
});