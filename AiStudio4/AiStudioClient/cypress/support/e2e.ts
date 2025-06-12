// cypress/support/e2e.ts

// This is a great place to put global configuration and behavior that modifies Cypress.
// You can change the location of this file or turn off loading
// the support file with the 'supportFile' configuration option.

// You can read more here:
// https://on.cypress.io/configuration

// Import commands.js using ES2015 syntax:
import './commands'

// Add type definitions for the new custom commands
declare global {
  namespace Cypress {
    interface Chainable {
      openCommandBar(): Chainable<void>;
      executeCommand(commandName: string): Chainable<void>;
      closeModal(): Chainable<void>;
      createProvider(provider: { name: string }): Chainable<void>;
      createModel(model: { name: string, providerName: string }): Chainable<void>;
      deleteItemInModal(itemName: string, itemType: 'model' | 'provider'): Chainable<void>;
    }
  }
}