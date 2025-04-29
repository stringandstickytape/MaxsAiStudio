# Cypress Testing for AiStudio4 Web

This directory contains Cypress tests for the AiStudio4 Web frontend application.

## Getting Started

Cypress is already installed as a dev dependency in the project, but you need to install the Cypress binary first:

```bash
# Install the Cypress binary
pnpm run cypress:install

# Open Cypress in interactive mode
pnpm run cypress:open

# Run Cypress tests headlessly
pnpm run cypress:run
```

## Configuration

We're using JavaScript for Cypress configuration instead of TypeScript to avoid potential configuration issues:

- `cypress.config.js` - Main configuration file
- `cypress/support/e2e.js` - Support file loaded before tests
- `cypress/support/commands.js` - Custom Cypress commands
- `cypress/e2e/app.cy.js` - Basic test file

## Troubleshooting

If you encounter issues with the TypeScript configuration, we've provided JavaScript alternatives for all configuration files.

## Test Structure

- `cypress/e2e/` - Contains end-to-end test files
- `cypress/support/` - Contains support files and custom commands

## Writing Tests

Tests are written using Cypress's testing API. Here's a simple example:

```typescript
describe('Feature', () => {
  it('should do something', () => {
    cy.visit('/');
    cy.get('.some-element').click();
    cy.contains('Expected Text').should('be.visible');
  });
});
```

## Running Tests

1. Start the development server in one terminal:
   ```bash
   npm run dev
   ```

2. Run Cypress in another terminal:
   ```bash
   npm run cypress:open
   ```

3. Select E2E Testing in the Cypress UI

4. Choose a browser and run the tests

## Additional Resources

- [Cypress Documentation](https://docs.cypress.io/)
- [Cypress Best Practices](https://docs.cypress.io/guides/references/best-practices)