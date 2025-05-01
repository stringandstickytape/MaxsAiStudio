// Test script to verify the fontCdnUrl property is included in the LLM schema

// Run this in the browser console when on the Tool Editor page

const schema = window.generateThemeLLMSchema();
console.log('Full schema:', schema);

// Check if fontCdnUrl is in the properties
const hasFontCdnUrl = Object.keys(schema.parameters.properties).some(key => key === 'global-fontCdnUrl');
console.log('Has fontCdnUrl property:', hasFontCdnUrl);

// List all global properties
const globalProps = Object.keys(schema.parameters.properties)
  .filter(key => key.startsWith('global-'));
console.log('Global properties:', globalProps);

// If fontCdnUrl is missing, check the ThemeManager instance directly
if (!hasFontCdnUrl) {
  console.log('Checking ThemeManager schema directly...');
  const themeManager = window.themeManagerInstance;
  if (themeManager) {
    console.log('ThemeManager global schema:', themeManager.getSchema().global);
  } else {
    console.log('ThemeManager instance not accessible');
  }
}