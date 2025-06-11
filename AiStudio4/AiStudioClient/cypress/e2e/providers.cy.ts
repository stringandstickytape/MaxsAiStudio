// cypress/e2e/providers.cy.ts

describe('Service Provider Management', () => {
  // Use a unique name for the provider in each test run to avoid conflicts
  const providerName = `Test Provider ${Date.now()}`;

  beforeEach(() => {
    // Visit the application root and ensure it's loaded before each test
    cy.visit('/');
    cy.get('.InputBar').should('be.visible');
  });

  it('should create, verify, and then delete a new service provider', () => {
    // 1. Open the "Service Providers" modal via the command bar
    cy.get('body').type('{ctrl}k');
    cy.get('#command-input').should('be.visible').and('be.focused');
    cy.get('#command-input').type('Service Providers');
    cy.get('.command-dropdown-menu').contains('div.font-bold', 'Settings').should('be.visible');
    cy.get('.command-dropdown-menu').contains('.font-medium', 'Service Providers').click();

    // 2. Verify the providers management modal is open
    cy.get('[role="dialog"]').contains('h2', 'Service Providers').should('be.visible');

    // 3. Click the "Add Provider" button to open the form dialog
    cy.contains('button', 'Add Provider').click();
    cy.get('[role="dialog"]').contains('h2', 'Add New Provider').should('be.visible');

    // 4. Fill out the form
    cy.contains('label', 'Friendly Name').parent().find('input').type(providerName);

    // For shadcn selects, we click to open the dropdown, then click the desired item
    cy.contains('label', 'Service Name').parent().find('[role="combobox"]').click();
    cy.contains('[role="option"]', 'Claude').click();

    cy.contains('label', 'API URL').parent().find('input').type('https://api.test-provider.com/v1');
    cy.contains('label', 'API Key').parent().find('input[type="password"]').type('test-api-key-12345');
    
    // Select the charging strategy
    cy.contains('label', 'Charging Strategy').parent().find('[role="combobox"]').click();
    cy.contains('[role="option"]', 'Claude Caching Model').click();

  });
});