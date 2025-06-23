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
    cy.searchCommand('primary');
    cy.pinCommand(`${modelName} [Primary]`);
    cy.closeCommandDropdown();

    // --- TEST: SELECTING PINNED SHORTCUT ---
    cy.clickPinnedShortcut(`${modelName} [Primary]`);
    cy.verifyPrimaryModelSelected(modelName);

    // --- TEST: UNPINNING ---
    cy.searchCommand('primary');
    cy.unpinCommand(`${modelName} [Primary]`);
    cy.closeCommandDropdown();
  });
});