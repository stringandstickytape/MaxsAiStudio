// AiStudioClient/src/utils/textAreaUtils.ts

/**
 * Get the position of the cursor in a textarea
 * @param textarea The textarea element
 * @returns The position of the cursor relative to the textarea
 */
export function getCursorPosition(textarea: HTMLTextAreaElement) {
  const { selectionStart, value } = textarea;
  const textBeforeCursor = value.substring(0, selectionStart);
  
  // Create a temporary element to measure text dimensions
  const temp = document.createElement('div');
  temp.style.position = 'absolute';
  temp.style.visibility = 'hidden';
  temp.style.whiteSpace = 'pre-wrap';
  temp.style.wordWrap = 'break-word';
  temp.style.width = getComputedStyle(textarea).width;
  temp.style.fontSize = getComputedStyle(textarea).fontSize;
  temp.style.lineHeight = getComputedStyle(textarea).lineHeight;
  temp.style.fontFamily = getComputedStyle(textarea).fontFamily;
  temp.style.padding = getComputedStyle(textarea).padding;
  
  // Add a span at the cursor position
  temp.innerHTML = textBeforeCursor.replace(/\n/g, '<br>') + '<span id="cursor"></span>';
  document.body.appendChild(temp);
  
  // Get position of the cursor span
  const cursorSpan = document.getElementById('cursor');
  const rect = cursorSpan!.getBoundingClientRect();
  const textareaRect = textarea.getBoundingClientRect();
  
  // Clean up
  document.body.removeChild(temp);
  
  // Return position relative to textarea
  return {
    top: rect.top - textareaRect.top + textarea.scrollTop,
    left: rect.left - textareaRect.left + textarea.scrollLeft
  };
}