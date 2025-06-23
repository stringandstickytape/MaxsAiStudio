// cypress/e2e/command_bar.cy.ts

describe('Command Bar Functionality', () => {
  const providerName = `Test Provider ${Date.now()}`;
  const modelName = `Test Model ${Date.now()}`;

  beforeEach(() => {
    cy.visit('/');
    cy.get('.InputBar').should('be.visible');
  });

  // HARDENED: This hook now reloads the app to ensure a stable state for cleanup.
  afterEach(() => {
    cy.visit('/');
    cy.get('.InputBar').should('be.visible');

    // We must delete the model first due to foreign key constraints.
    cy.deleteItemInModal(modelName, 'model');
    cy.deleteItemInModal(providerName, 'provider');
  });

  it('should open the Models modal via the command bar and close it', () => {
    cy.executeCommand('Models');
    cy.get('[role="dialog"]').should('be.visible');
    cy.get('[role="dialog"] h2').should('contain', 'Models');
    cy.contains('button', 'Add Model').should('be.visible');
    cy.closeModal();
  });

  it('should create, pin, select, and unpin a model using the command bar', () => {
    // --- SETUP ---
    cy.createProvider({ name: providerName });
    cy.closeModal();
    cy.createModel({ name: modelName, providerName });
    cy.closeModal();

    // --- TEST: PINNING ---
    cy.openCommandBar();
    cy.get('#command-input').type('primary');
    cy.get('.command-dropdown-menu').contains('.font-medium', `${modelName} [Primary]`)
      .parents('.px-2.py-1').within(() => {
        cy.get('.command-pin-button').click();
      });
    // Re-query the element after the pin action to avoid stale reference
    cy.get('.command-dropdown-menu').contains('.font-medium', `${modelName} [Primary]`)
      .parents('.px-2.py-1').within(() => {
        cy.get('.command-pin-button.text-blue-400').should('exist');
      });
    // Close the command bar dropdown by clicking outside
    cy.get('body').click();
    cy.get('.command-dropdown-menu', { timeout: 3000 }).should('not.exist');

    // --- TEST: SELECTING PINNED SHORTCUT ---
    cy.get('[class*="PinnedShortcuts"]', { timeout: 5000 })
      .should('be.visible')
      .contains(`${modelName} [Primary]`)
      .click();

    // ROBUST: Assert on the outcome of the click, don't just wait.
    // Replace '[data-testid="active-model-display"]' with your app's actual selector.
    cy.get('[data-testid="active-model-display"]', { timeout: 10000 })
      .should('contain.text', modelName);

    // --- TEST: UNPINNING ---
    cy.openCommandBar();
    cy.get('#command-input').type('primary');
    cy.get('.command-dropdown-menu')
      .contains('.font-medium', `${modelName} [Primary]`)
      .parents('.px-2.py-1')
      .within(() => {
        cy.get('.command-pin-button').click();
      });
    cy.get('.command-dropdown-menu')
      .contains('.font-medium', `${modelName} [Primary]`)
      .parents('.px-2.py-1')
      .within(() => {
        cy.get('.command-pin-button.text-gray-500').should('exist');
      });
    // Close the command bar dropdown by clicking outside
    cy.get('body').click();
    cy.get('.command-dropdown-menu', { timeout: 3000 }).should('not.exist');
  });
});