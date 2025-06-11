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
    // 1. Open the "Service Providers" modal via the command bar (CommandBar component)
    cy.get('body').type('{ctrl}k');
    cy.get('#command-input').should('be.visible').and('be.focused');
    cy.get('#command-input').type('Service Providers');
    cy.get('.command-dropdown-menu').contains('div.font-bold', 'Settings').should('be.visible');
    cy.get('.command-dropdown-menu').contains('.font-medium', 'Service Providers').click();

    // 2. Verify the ProvidersModal component is open and showing ServiceProviderManagement
    cy.get('[role="dialog"]').contains('h2', 'Service Providers').should('be.visible');

    // 3. Click the "Add Provider" button (ServiceProviderManagement component) to open the form dialog
    cy.contains('button', 'Add Provider').click();
    cy.get('[role="dialog"]').contains('h2', 'Add New Provider').should('be.visible');

    // 4. Fill out the ServiceProviderForm (uses GenericForm component)
    // Fill the friendly name field (text input)
    cy.contains('label', 'Friendly Name').parent().find('input').type(providerName);

    // Fill the service name field (select dropdown from ServiceProviderForm)
    cy.contains('label', 'Service Name').parent().find('[role="combobox"]').click();
    cy.contains('[role="option"]', 'Claude').click();

    // Fill the API URL field (text input)
    cy.contains('label', 'API URL').parent().find('input').type('https://api.test-provider.com/v1');
    
    // Fill the API Key field (password input)
    cy.contains('label', 'API Key').parent().find('input[type="password"]').type('test-api-key-12345');
    
    // Select the charging strategy (select dropdown from ServiceProviderForm)
    cy.contains('label', 'Charging Strategy').parent().find('[role="combobox"]').click();
    cy.contains('[role="option"]', 'Claude Caching Model').click();

    // 5. Submit the form by clicking the Add Provider button (GenericForm component's save button, as id-marked by its caller)
    cy.get('#service-provider-management-add-provider-button').click();

    // 6. Wait a moment for the form to process and close (handleAddProvider in ServiceProviderManagement)
    cy.wait(1000);

    // 7. Verify we're back to the main providers modal (ProvidersModal showing ServiceProviderManagement)
    cy.get('[role="dialog"]').contains('h2', 'Service Providers').should('be.visible');

    // 8. Verify the new provider appears in the provider cards grid (ServiceProviderManagement's provider grid)
    cy.get('.grid').should('contain', providerName);
    
    // 9. Verify the provider card shows correct information (Card component in ServiceProviderManagement)
    cy.get('.grid').contains('.card-base', providerName).scrollIntoView().within(() => {
      // Check friendly name is displayed (CardTitle component shows provider.friendlyName)
      cy.contains(providerName).should('be.visible');
      
      // Check protocol/service name is displayed (provider.serviceName)
      cy.contains('Protocol: Claude').should('be.visible');
      
      // Check API URL is displayed (provider.url)
      cy.contains('https://api.test-provider.com/v1').should('be.visible');
      
      // Check charging strategy badge is displayed (provider.chargingStrategy)
      cy.contains('Cache Pricing: Claude').should('be.visible');
    });

    // 10. Delete the provider by clicking the delete button (Trash2 icon button in ServiceProviderManagement)
    cy.get('.grid').contains('.card-base', providerName).scrollIntoView().within(() => {
      // Find the delete button using the specific class (positioned absolute bottom-right in the card)
      cy.get('.service-provider-delete-button').should('be.visible').click();
    });

    // 11. Confirm deletion in the delete confirmation dialog (UnifiedModalDialog for deletion)
    cy.get('[role="dialog"]').contains('h2', 'Confirm Deletion').should('be.visible');
    cy.get('[role="dialog"]').contains(`delete the provider ${providerName}`).should('be.visible');
    cy.contains('button', 'Delete').click(); // Click the destructive delete button

    // 12. Wait for deletion to complete (handleDeleteProviderConfirm in ServiceProviderManagement)
    cy.wait(1000);

    // 13. Verify the provider has been removed from the provider grid (ServiceProviderManagement)
    cy.get('.grid').should('not.contain', providerName);

    // 14. Close the providers modal (UnifiedModalDialog close button)
    cy.get('button[aria-label="Close modal"]').click();
  });
});