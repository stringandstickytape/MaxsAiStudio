// cypress/e2e/app.cy.ts

describe('App', () => {
  it('should load the application', () => {
    // Visit the root URL
    cy.visit('/');
    
    // Verify the app loads by checking for some core element
    // This selector may need to be adjusted based on your actual app structure
    cy.get('#root').should('exist');
    
    // You can add more specific assertions based on your UI
    // For example, check if the header is visible
    // cy.contains('h1', 'AiStudio').should('be.visible');
  });
});