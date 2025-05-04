// AiStudioClient/src/utils/textAreaUtils.ts

/**
 * Get the position of the cursor in a textarea
 * @param textarea The textarea element
 * @returns The position of the cursor relative to the textarea
 */
export function getCursorPosition(textarea: HTMLTextAreaElement) {
  try {
    // Get the textarea's position and dimensions
    const textareaRect = textarea.getBoundingClientRect();
    
    // Get the current line number based on cursor position
    const { selectionStart, value } = textarea;
    const textBeforeCursor = value.substring(0, selectionStart);
    const lines = textBeforeCursor.split('\n');
    const currentLineNumber = lines.length;
    const currentLineText = lines[lines.length - 1];
    
    // Calculate approximate position
    // This is a simplified approach that works better than the previous method
    const lineHeight = parseInt(getComputedStyle(textarea).lineHeight) || 20;
    const paddingTop = parseInt(getComputedStyle(textarea).paddingTop) || 0;
    const paddingLeft = parseInt(getComputedStyle(textarea).paddingLeft) || 0;
    
    // Calculate top position based on line number
    const top = paddingTop + (currentLineNumber - 1) * lineHeight;
    
    // Calculate left position based on character position in current line
    // Use canvas to measure the actual text width
    const canvas = document.createElement('canvas');
    const context = canvas.getContext('2d');
    if (context) {
      const computedStyle = window.getComputedStyle(textarea);
      context.font = `${computedStyle.fontWeight} ${computedStyle.fontSize} ${computedStyle.fontFamily}`;
      const textMetrics = context.measureText(currentLineText);
      const left = paddingLeft + textMetrics.width;
      return {
        top: top,
        left: left
      };
    }
    
    // Fallback to using the calculated top with a fallback left position
    return {
      top: top,
      left: paddingLeft
    };
  } catch (error) {
    // Return a default position if calculation fails
    return {
      top: 20,
      left: 20
    };
  }
}