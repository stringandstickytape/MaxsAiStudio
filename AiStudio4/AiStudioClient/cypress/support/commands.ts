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
  
  // Wait for the modal to be visible first
  cy.get('[role="dialog"]').should('be.visible');
  
  // Wait for the grid to load
  cy.get(gridSelector, { timeout: 10000 }).should('be.visible');

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
      cy.contains('button', 'Confirm').click();
      // Wait for the confirmation dialog to close first
      cy.get('[role="dialog"]').contains('h2', 'Confirm Deletion').should('not.exist');
      // Then verify the item is removed from the main modal (only if modal still exists)
      cy.get('body').then($body => {
        if ($body.find(gridSelector).length) {
          cy.get(gridSelector).should('not.contain', itemName);
        }
      });
    }
  });

  cy.closeModal();
});

/**
 * Closes the command bar dropdown by clicking outside and verifies it's gone.
 */
Cypress.Commands.add('closeCommandDropdown', () => {
  cy.get('body').click();
  cy.get('.command-dropdown-menu', { timeout: 3000 }).should('not.exist');
});

/**
 * Pins a command from the command dropdown.
 * @param {string} commandName The exact text of the command to pin.
 */
Cypress.Commands.add('pinCommand', (commandName: string) => {
  cy.get('.command-dropdown-menu').contains('.font-medium', commandName)
    .parents('.px-2.py-1').within(() => {
      cy.get('.command-pin-button').click();
    });
  // Verify it's pinned (button turns blue)
  cy.get('.command-dropdown-menu').contains('.font-medium', commandName)
    .parents('.px-2.py-1').within(() => {
      cy.get('.command-pin-button.text-blue-400').should('exist');
    });
});

/**
 * Unpins a command from the command dropdown.
 * @param {string} commandName The exact text of the command to unpin.
 */
Cypress.Commands.add('unpinCommand', (commandName: string) => {
  cy.get('.command-dropdown-menu').contains('.font-medium', commandName)
    .parents('.px-2.py-1').within(() => {
      cy.get('.command-pin-button').click();
    });
  // Verify it's unpinned (button turns gray)
  cy.get('.command-dropdown-menu').contains('.font-medium', commandName)
    .parents('.px-2.py-1').within(() => {
      cy.get('.command-pin-button.text-gray-500').should('exist');
    });
});

/**
 * Clicks a pinned shortcut by name.
 * @param {string} shortcutName The exact text of the pinned shortcut to click.
 */
Cypress.Commands.add('clickPinnedShortcut', (shortcutName: string) => {
  cy.get('[class*="PinnedShortcuts"]', { timeout: 5000 })
    .should('be.visible')
    .contains(shortcutName)
    .click();
});

/**
 * Verifies that a model is selected as the primary model.
 * @param {string} modelName The name of the model to verify is selected.
 */
Cypress.Commands.add('verifyPrimaryModelSelected', (modelName: string) => {
  cy.get('button').contains('span', modelName, { timeout: 10000 })
    .should('be.visible');
});

/**
 * Searches for a command in the command bar.
 * @param {string} searchTerm The term to search for.
 */
Cypress.Commands.add('searchCommand', (searchTerm: string) => {
  cy.openCommandBar();
  cy.get('#command-input').type(searchTerm);
  cy.get('.command-dropdown-menu').find('div.font-bold').first().should('be.visible');
});

/**
 * Sends a message and waits for response.
 * @param {string} message The message to send.
 */
Cypress.Commands.add('sendMessage', (message: string) => {
  cy.get('.InputBar textarea').clear().type(message);
  cy.get('.InputBar textarea').type('{ctrl+enter}');
  
  // Wait for the message to appear using data-message-id
  cy.get('[data-message-id]', { timeout: 10000 }).should('contain', message);
  
  // Wait for AI response (loading state should appear then disappear)
  cy.get('button[aria-label="Cancel"]', { timeout: 3000 }).should('be.visible');
  cy.get('button[aria-label="Cancel"]', { timeout: 30000 }).should('not.exist');
});

/**
 * Verifies that a message response contains specific text.
 * @param {string} expectedText The text that should appear in the AI response.
 */
Cypress.Commands.add('verifyResponse', (expectedText: string) => {
  cy.get('.message-container').should('have.length.greaterThan', 1);
  cy.get('.ConvView.ConvViewMain').should('contain', expectedText);
});

/**
 * Creates a LoremIpsum service provider for testing.
 * @param {string} name The name for the provider.
 */
Cypress.Commands.add('createLoremIpsumProvider', (name: string) => {
  cy.executeCommand('Service Providers');
  cy.get('[role="dialog"]').contains('h2', 'Service Providers').should('be.visible');
  cy.contains('button', 'Add Provider').click();

  // Fill Form for LoremIpsum service
  cy.get('[role="dialog"]').contains('h2', 'Add New Provider').should('be.visible');
  cy.contains('label', 'Friendly Name').parent().find('input').type(name);
  cy.contains('label', 'Service Name').parent().find('[role="combobox"]').click();
  cy.contains('[role="option"]', 'LoremIpsumService').click();
  cy.contains('label', 'API URL').parent().find('input').type('http://localhost:8080/test');
  cy.contains('label', 'API Key').parent().find('input[type="password"]').type('test-key');
  cy.contains('label', 'Charging Strategy').parent().find('[role="combobox"]').click();
  cy.contains('[role="option"]', 'No Caching (Standard)').click();

  // Submit
  cy.get('#service-provider-management-add-provider-button').click();
  cy.get('[role="dialog"]').contains('h2', 'Service Providers').should('be.visible');
});