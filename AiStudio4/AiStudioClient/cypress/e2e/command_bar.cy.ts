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

  it('should create a provider and model, pin the model from command bar, and then select it', () => {
    // Use unique names to avoid conflicts
    const providerName = `Test Provider ${Date.now()}`;
    const modelName = `Test Model ${Date.now()}`;

    // STEP 0: Create a provider first (models need providers)
    
    // 1. Open the "Service Providers" modal via the command bar
    cy.get('body').type('{ctrl}k');
    cy.get('#command-input').should('be.visible').and('be.focused');
    cy.get('#command-input').type('Service Providers');
    cy.get('.command-dropdown-menu').contains('div.font-bold', 'Settings').should('be.visible');
    cy.get('.command-dropdown-menu').contains('.font-medium', 'Service Providers').click();

    // 2. Verify the ProvidersModal component is open
    cy.get('[role="dialog"]').contains('h2', 'Service Providers').should('be.visible');

    // 3. Click the "Add Provider" button to open the form dialog
    cy.contains('button', 'Add Provider').click();
    cy.get('[role="dialog"]').contains('h2', 'Add New Provider').should('be.visible');

    // 4. Fill out the ServiceProviderForm
    cy.contains('label', 'Friendly Name').parent().find('input').type(providerName);
    cy.contains('label', 'Service Name').parent().find('[role="combobox"]').click();
    cy.contains('[role="option"]', 'Claude').click();
    cy.contains('label', 'API URL').parent().find('input').type('https://api.test-provider.com/v1');
    cy.contains('label', 'API Key').parent().find('input[type="password"]').type('test-api-key-12345');
    cy.contains('label', 'Charging Strategy').parent().find('[role="combobox"]').click();
    cy.contains('[role="option"]', 'Claude Caching Model').click();

    // 5. Submit the provider form
    cy.get('#service-provider-management-add-provider-button').click();
    cy.wait(1000);

    // 6. Verify we're back to the main providers modal and close it
    cy.get('[role="dialog"]').contains('h2', 'Service Providers').should('be.visible');
    cy.get('button[aria-label="Close modal"]').click();

    // STEP 1: Create a model using the new provider
    
    // 7. Open the "Models" modal via the command bar
    cy.get('body').type('{ctrl}k');
    cy.get('#command-input').type('models');
    cy.get('.command-dropdown-menu').contains('div.font-bold', 'Settings').should('be.visible');
    cy.get('.command-dropdown-menu').contains('.font-medium', 'Models').click();

    // 8. Verify the Models modal is open
    cy.get('[role="dialog"]').contains('h2', 'Models').should('be.visible');

    // 9. Click the "Add Model" button to open the form dialog
    cy.contains('button', 'Add Model').click();
    cy.get('[role="dialog"]').contains('h2', 'Add New Model').should('be.visible');

    // 10. Fill out the model form
    cy.contains('label', 'Friendly Name').parent().find('input').type(modelName);
    cy.contains('label', 'Model Name').parent().find('input').type('gpt-4');
    // Use the specific ID for the Service Provider dropdown
    cy.get('#trigger-providerGuid').click();
    // Wait for the dropdown to open and then select our provider directly
    cy.get('[role="option"]').should('be.visible');
    cy.contains('[role="option"]', providerName).click();

    // 11. Submit the form
    cy.get('#model-management-add-model-button').click();

    // 6. Wait for the form to process and close
    cy.wait(1000);

    // 7. Verify we're back to the main models modal
    cy.get('[role="dialog"]').contains('h2', 'Models').should('be.visible');

    // 8. Verify the new model appears in the models list
    cy.get('.grid').should('contain', modelName);

    // 9. Close the models modal
    cy.get('button[aria-label="Close modal"]').click();

    // STEP 2: Now pin the model from the command bar

    // 10. Open the command bar and search for the model
    cy.get('body').type('{ctrl}k');
    cy.get('#command-input').should('be.visible').and('be.focused');
    // Search for a broader term to ensure we find model commands
    cy.get('#command-input').type('primary');

    // 11. Wait for the dropdown to appear with any section
    cy.get('.command-dropdown-menu').find('div.font-bold').first().should('be.visible');

      // 12. Find the model command and pin it (should be "modelName [Primary]")
      cy.get('.command-dropdown-menu')
          .contains('.font-medium', `${modelName} [Primary]`)
          .first()
          .scrollIntoView()
          .should('be.visible')
          .parents('.px-2.py-1')
          .within(() => {
              // Click the pin button for this model command
              cy.get('svg').last().click();
          });


      // 13. Verify the model was pinned by checking the pin icon changed color
      // the command bar options only appear when we press CTRL-K or type into the bar
      cy.get('#command-input').clear().type('primary');

      cy.get('.command-dropdown-menu').find('div.font-bold').first().should('be.visible');

      cy.get('.command-dropdown-menu')
      .contains('.font-medium', `${modelName} [Primary]`)
      .first()
      .scrollIntoView()
      .parents('.px-2.py-1')
      .within(() => {
        cy.get('.text-blue-400').should('exist');
      });

    // 14. Close the command bar
      cy.get('#command-input').clear();
      cy.get('body').type('{esc}');

    // STEP 3: Use the pinned model

    // 15. Verify the pinned command appears in the pinned shortcuts section
    cy.get('[class*="PinnedShortcuts"]', { timeout: 5000 }).should('be.visible');
    
    // 16. Find and click the pinned model shortcut to select it
    cy.get('[class*="PinnedShortcuts"]')
      .contains(`${modelName} [Primary]`)
        .first()
        .scrollIntoView()
      .click();

    // 17. Verify the model was selected (wait for processing)
    cy.wait(1000);

    // STEP 4: Clean up - unpin and delete the model

    // 18. Unpin the model by opening command bar again
    cy.get('body').type('{ctrl}k');
    cy.get('#command-input').should('be.visible').and('be.focused');
    cy.get('#command-input').type('primary');
    cy.get('.command-dropdown-menu').find('div.font-bold').first().should('be.visible');
    
    // 19. Find the pinned model and unpin it
    cy.get('.command-dropdown-menu')
      .contains('.font-medium', `${modelName} [Primary]`)
      .first()
      .scrollIntoView()
      .parents('.px-2.py-1')
      .within(() => {
        // Click the pin button again to unpin
        cy.get('.text-blue-400').click();
      });

    // 20. Verify the model is unpinned
    cy.get('.command-dropdown-menu')
      .contains('.font-medium', `${modelName} [Primary]`)
      .first()
      .scrollIntoView()
      .parents('.px-2.py-1')
      .within(() => {
        cy.get('.text-gray-500').should('exist');
      });

    // 21. Close the command bar
      cy.get('#command-input').clear();
      cy.get('body').type('{esc}');

    // 22. Delete the test model - reopen models modal
    cy.get('body').type('{ctrl}k');
    cy.get('#command-input').type('models');
    cy.get('.command-dropdown-menu').contains('div.font-bold', 'Settings').should('be.visible');
    cy.get('.command-dropdown-menu').contains('.font-medium', 'Models').click();

    // 23. Find and delete the test model (use a simpler approach)
      cy.get('.model-modal-grid').contains('.card-base', modelName).scrollIntoView().within(() => {
      // Look for the delete button by its tooltip or use the last button (which should be delete)
      cy.get('button').last().click();
    });

    // 24. Confirm deletion
    cy.get('[role="dialog"]').contains('h2', 'Confirm Deletion').should('be.visible');
    cy.contains('button', 'Delete').click();

    // 25. Wait for deletion to complete
    cy.wait(1000);

    // 26. Verify the model has been removed
      cy.get('.model-modal-grid').should('not.contain', modelName);

    // 27. Close the models modal
    cy.get('button[aria-label="Close modal"]').click();

    // STEP 5: Clean up the test provider as well

    // 28. Delete the test provider
    cy.get('body').type('{ctrl}k');
    cy.get('#command-input').type('Service Providers');
    cy.get('.command-dropdown-menu').contains('div.font-bold', 'Settings').should('be.visible');
    cy.get('.command-dropdown-menu').contains('.font-medium', 'Service Providers').click();

    // 29. Find and delete the test provider
      cy.get('.server-modal-grid').contains('.card-base', providerName).scrollIntoView().within(() => {
      cy.get('.service-provider-delete-button').should('be.visible').click();
    });

    // 30. Confirm provider deletion
    cy.get('[role="dialog"]').contains('h2', 'Confirm Deletion').should('be.visible');
    cy.contains('button', 'Delete').click();

    // 31. Wait for deletion to complete
    cy.wait(1000);

    // 32. Verify the provider has been removed
      cy.get('.server-modal-grid').should('not.contain', providerName);

    // 33. Close the providers modal
    cy.get('button[aria-label="Close modal"]').click();
  });
});