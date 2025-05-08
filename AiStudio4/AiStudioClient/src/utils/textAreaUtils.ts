// AiStudioClient/src/utils/textAreaUtils.ts

/**
 * Get the position of the cursor in a textarea
 * @param textarea The textarea element
 * @returns The position of the cursor relative to the textarea
 */
export function getCursorPosition(textarea: HTMLTextAreaElement) {
  try {
    // Get the current line number based on cursor position
    const { selectionStart, value } = textarea;
    const textBeforeCursor = value.substring(0, selectionStart);
    const lines = textBeforeCursor.split('\n');
    const currentLineNumber = lines.length;
    
    // Calculate approximate position
    const lineHeight = parseInt(getComputedStyle(textarea).lineHeight) || 20;
    const paddingTop = parseInt(getComputedStyle(textarea).paddingTop) || 0;
    const paddingLeft = parseInt(getComputedStyle(textarea).paddingLeft) || 0;
    
    // Calculate top position based on line number
    const top = paddingTop + (currentLineNumber - 1) * lineHeight;
    
    // Return a simple position - this function is no longer used for dropdown positioning
    return {
      top: top,
      left: paddingLeft
    };
  } catch (error) {
    console.error('Error calculating cursor position:', error);
    // Return a default position if calculation fails
    return {
      top: 20,
      left: 20
    };
  }
}