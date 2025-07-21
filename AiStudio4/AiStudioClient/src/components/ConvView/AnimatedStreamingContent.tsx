import React, { useEffect, useRef } from 'react';
import { MarkdownPane } from '@/components/MarkdownPane';

interface AnimatedStreamingContentProps {
  content: string;
  messageId: string;
  newContentInfo: {
    previousLength: number;
    animationKey: number;
  };
}

export function AnimatedStreamingContent({ 
  content, 
  messageId, 
  newContentInfo
}: AnimatedStreamingContentProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const { previousLength, animationKey } = newContentInfo;
  
  console.log(`🎬 AnimatedStreamingContent render: messageId=${messageId}, contentLength=${content.length}, previousLength=${previousLength}, animationKey=${animationKey}`);
  
  useEffect(() => {
    if (!containerRef.current || previousLength === 0) return;
    
    console.log(`🎬 Animation effect running: previousLength=${previousLength}, animationKey=${animationKey}`);
    
    // Find all text nodes in the rendered markdown
    const walkTextNodes = (node: Node): Text[] => {
      const textNodes: Text[] = [];
      
      if (node.nodeType === Node.TEXT_NODE) {
        textNodes.push(node as Text);
      } else {
        for (let i = 0; i < node.childNodes.length; i++) {
          textNodes.push(...walkTextNodes(node.childNodes[i]));
        }
      }
      
      return textNodes;
    };
    
    // Get all text nodes and find the ones containing new content
    const textNodes = walkTextNodes(containerRef.current);
    let totalLength = 0;
    
    console.log(`🎬 Found ${textNodes.length} text nodes, content length: ${content.length}`);
    
    for (const textNode of textNodes) {
      const nodeLength = textNode.textContent?.length || 0;
      
      // If this text node contains new content
      if (totalLength < previousLength && totalLength + nodeLength > previousLength) {
        const newTextStart = previousLength - totalLength;
        const newText = textNode.textContent?.slice(newTextStart) || '';
        
        if (newText) {
          // Split the text node
          const existingText = textNode.textContent?.slice(0, newTextStart) || '';
          
          // Create new text node for existing content
          const existingTextNode = document.createTextNode(existingText);
          
          // Create span for new content with animation
          const newSpan = document.createElement('span');
          newSpan.textContent = newText;
          newSpan.style.opacity = '0';
          newSpan.style.transition = 'opacity 200ms ease-in-out';
          
          // Replace the original text node
          if (textNode.parentNode) {
            textNode.parentNode.insertBefore(existingTextNode, textNode);
            textNode.parentNode.insertBefore(newSpan, textNode);
            textNode.parentNode.removeChild(textNode);
          }
          
          // Animate in
          requestAnimationFrame(() => {
            newSpan.style.opacity = '1';
          });
        }
        
        break;
      } else if (totalLength >= previousLength) {
        // This entire text node is new content
        const span = document.createElement('span');
        span.textContent = textNode.textContent;
        span.style.opacity = '0';
        span.style.transition = 'opacity 200ms ease-in-out';
        
        if (textNode.parentNode) {
          textNode.parentNode.replaceChild(span, textNode);
        }
        
        // Animate in
        requestAnimationFrame(() => {
          span.style.opacity = '1';
        });
      }
      
      totalLength += nodeLength;
    }
  }, [animationKey, previousLength]);
  
  return (
    <div ref={containerRef}>
      <MarkdownPane message={content} messageId={messageId} variant="default" />
    </div>
  );
}