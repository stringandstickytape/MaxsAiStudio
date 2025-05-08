

export function setupPromptUtils(setTextFn) {
  window.appendToPrompt = (text, options = {}) => {
    const { newLine = true, replaceCurrent = false } = options;

    try {
      let currentInputValue = '';

      if (window._currentPromptText !== undefined) {
        currentInputValue = window._currentPromptText;
      }

      const formattedText = newLine && currentInputValue.length > 0 ? '\n' + text : text;

      const newValue = replaceCurrent ? formattedText : currentInputValue + formattedText;

      setTextFn(newValue);

      window._currentPromptText = newValue;

      return true;
    } catch (error) {
      console.error('Failed to append to prompt:', error);
      return false;
    }
  };

  window.getPromptText = () => {
    return window._currentPromptText || '';
  };

}

export function cleanupPromptUtils() {
  delete window.appendToPrompt;
  delete window.getPromptText;
  delete window._currentPromptText;
}
