// cypress/e2e/lorem_ipsum_messaging.cy.ts

describe('LoremIpsum Service Provider Messaging', () => {
  const providerName = `Lorem Test Provider ${Date.now()}`;
  const modelName = `Lorem Test Model ${Date.now()}`;

  beforeEach(() => {
    cy.visit('/');
    cy.get('.InputBar').should('be.visible');
  });

  afterEach(() => {
    cy.visit('/');
    cy.get('.InputBar').should('be.visible');

    // Clean up: delete model first due to foreign key constraints
    cy.deleteItemInModal(modelName, 'model');
    cy.deleteItemInModal(providerName, 'provider');
  });

  it('should create a LoremIpsum provider, model, and successfully send/receive messages', () => {
    // --- SETUP: Create LoremIpsum Provider ---
    cy.createLoremIpsumProvider(providerName);
    cy.closeModal();

    // --- SETUP: Create a Model using the LoremIpsum Provider ---
    cy.createModel({ name: modelName, providerName });
    cy.closeModal();

    // --- SETUP: Set the new model as primary ---
    cy.searchCommand('primary');
    cy.get('.command-dropdown-menu').contains('.font-medium', `${modelName} [Primary]`).click();
    cy.closeCommandDropdown();

    // Verify the model is selected
    cy.verifyPrimaryModelSelected(modelName);

    // --- TEST: Send a message and verify response ---
    const testMessage = 'Hello, please generate some lorem ipsum text for testing.';
    cy.sendMessage(testMessage);

    // LoremIpsum service should respond with lorem ipsum text
    // The exact response may vary, but it should contain typical lorem ipsum words
    cy.verifyResponse('lorem');
  });


});