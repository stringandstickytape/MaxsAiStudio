# Code Block State Management Refactor Design

## Context and Problem Statement

### Current Issue
When a user toggles the visibility (expand/collapse) of a single code block in a message, ALL code blocks within that message re-render. This causes:
- Performance degradation with multiple code blocks
- Unnecessary re-execution of expensive renderers (Mermaid diagrams, syntax highlighting, etc.)
- Poor user experience with visible flashing/re-rendering

### Root Cause Analysis
The issue stems from how state is currently managed in `MarkdownPane.tsx`:

1. **Shared State Object**: The collapse state for all code blocks is stored in a single state object:
   ```typescript
   const [isCodeCollapsed, setIsCodeCollapsed] = useState<Record<string, boolean>>({});
   ```

2. **Component Recreation**: The `components` object (which defines how to render markdown elements) includes `isCodeCollapsed` in its dependency array:
   ```typescript
   const components = useMemo(() => ({...}), [showRawContent, isCodeCollapsed, mermaidKey, isVisualStudio, variant]);
   ```

3. **Cascade Effect**: When any code block's state changes:
   - The entire `isCodeCollapsed` object reference changes
   - The `components` object is recreated
   - All code blocks receive new component function references
   - React.memo can't prevent re-renders because the functions are new

### Why Current Approaches Don't Work
- **Removing dependencies**: Makes the UI non-responsive (closures capture stale state)
- **React.memo**: Ineffective when parent components recreate child functions
- **Local state in CodeBlock**: Would lose state on unmount/remount

## Proposed Solution: Zustand State Management

### Why Zustand?
1. **Already in the project**: No new dependencies
2. **Granular subscriptions**: Components only re-render when their specific data changes
3. **Outside React**: State changes don't trigger parent re-renders
4. **Simple API**: Easy to implement and maintain

## Implementation Design

### 1. Store Structure

Create a new file: `stores/useCodeBlockStore.ts`

```typescript
import { create } from 'zustand';
import { shallow } from 'zustand/shallow';

interface CodeBlockState {
  // State
  collapsedBlocks: Record<string, boolean>;
  rawViewBlocks: Record<string, boolean>;
  
  // Actions
  toggleCollapse: (blockId: string) => void;
  toggleRawView: (blockId: string) => void;
  setCollapsed: (blockId: string, collapsed: boolean) => void;
  setRawView: (blockId: string, isRaw: boolean) => void;
  clearBlockStates: () => void;
  
  // Bulk actions
  expandAll: () => void;
  collapseAll: (blockIds: string[]) => void;
  
  // Getters
  isCollapsed: (blockId: string) => boolean;
  isRawView: (blockId: string) => boolean;
}

export const useCodeBlockStore = create<CodeBlockState>((set, get) => ({
  // Initial state
  collapsedBlocks: {},
  rawViewBlocks: {},
  
  // Actions
  toggleCollapse: (blockId: string) => {
    set((state) => ({
      collapsedBlocks: {
        ...state.collapsedBlocks,
        [blockId]: !(state.collapsedBlocks[blockId] ?? true) // default to collapsed
      }
    }));
  },
  
  toggleRawView: (blockId: string) => {
    set((state) => ({
      rawViewBlocks: {
        ...state.rawViewBlocks,
        [blockId]: !state.rawViewBlocks[blockId]
      }
    }));
  },
  
  setCollapsed: (blockId: string, collapsed: boolean) => {
    set((state) => ({
      collapsedBlocks: {
        ...state.collapsedBlocks,
        [blockId]: collapsed
      }
    }));
  },
  
  setRawView: (blockId: string, isRaw: boolean) => {
    set((state) => ({
      rawViewBlocks: {
        ...state.rawViewBlocks,
        [blockId]: isRaw
      }
    }));
  },
  
  clearBlockStates: () => {
    set({ collapsedBlocks: {}, rawViewBlocks: {} });
  },
  
  expandAll: () => {
    set({ collapsedBlocks: {} });
  },
  
  collapseAll: (blockIds: string[]) => {
    const collapsed: Record<string, boolean> = {};
    blockIds.forEach(id => { collapsed[id] = true; });
    set({ collapsedBlocks: collapsed });
  },
  
  // Getters
  isCollapsed: (blockId: string) => {
    return get().collapsedBlocks[blockId] ?? true; // default to collapsed
  },
  
  isRawView: (blockId: string) => {
    return get().rawViewBlocks[blockId] ?? false;
  }
}));
```

### 2. Update MarkdownPane Component

Key changes to `MarkdownPane.tsx`:

```typescript
import { useCodeBlockStore } from '@/stores/useCodeBlockStore';

export const MarkdownPane: React.FC<MarkdownPaneProps> = ({ message, variant = 'default' }) => {
  // Remove these state declarations:
  // const [showRawContent, setShowRawContent] = useState<Record<string, boolean>>({});
  // const [isCodeCollapsed, setIsCodeCollapsed] = useState<Record<string, boolean>>({});
  
  const clearBlockStates = useCodeBlockStore(state => state.clearBlockStates);
  
  // Clear states when message changes
  useEffect(() => {
    clearBlockStates();
  }, [message.id, clearBlockStates]);
  
  // Keep track of code block index for unique IDs
  let codeBlockIndex = 0;
  
  // Simplified components with no state dependencies
  const components = useMemo(() => ({
    code({ className, children, ...props }: any) {
      const isCodeBlock = className !== undefined || String(children).includes('\n');
      
      if (!isCodeBlock) {
        return <code className={className}>{children}</code>;
      }
      
      const match = /language-(\w+)/.exec(className || '');
      const language = match ? match[1] : 'txt';
      const content = String(children).replace(/\n$/, '');
      const diagramRenderer = codeBlockRendererRegistry.get(language);
      
      // Create stable blockId that includes message ID
      const blockId = `${message.id}-code-block-${codeBlockIndex++}`;
      
      return (
        <CodeBlock
          key={blockId}
          blockId={blockId}
          language={language}
          content={content}
          diagramRenderer={diagramRenderer}
          isVisualStudio={isVisualStudio}
          mermaidKey={mermaidKey}
          launchHtml={launchHtml}
          variant={variant}
          fullMarkdown={markdownContent}
          extractNestedCodeBlocks={extractNestedCodeBlocks}
        />
      );
    },
    // ... other components remain the same ...
  }), [mermaidKey, isVisualStudio, variant]); // No more state dependencies!
  
  // ... rest of component remains the same ...
};
```

### 3. Update CodeBlock Component

Refactor `CodeBlock.tsx` to use Zustand:

```typescript
import { useCodeBlockStore } from '@/stores/useCodeBlockStore';
import { shallow } from 'zustand/shallow';

export interface CodeBlockProps {
  blockId: string; // Now required
  language: string;
  content: string;
  diagramRenderer?: any;
  isVisualStudio: boolean;
  mermaidKey: number;
  launchHtml: (content: string) => void;
  variant: MarkdownVariant;
  fullMarkdown?: string;
  extractNestedCodeBlocks?: (content: string) => NestedBlock[];
}

export const CodeBlock = React.memo<CodeBlockProps>(({
  blockId,
  language,
  content,
  diagramRenderer,
  isVisualStudio,
  mermaidKey,
  launchHtml,
  variant,
  fullMarkdown,
  extractNestedCodeBlocks,
}) => {
  // Optimized state subscription - only re-renders when this block's state changes
  const { isCollapsed, isRawView } = useCodeBlockStore(
    useCallback(
      (state) => ({
        isCollapsed: state.isCollapsed(blockId),
        isRawView: state.isRawView(blockId)
      }),
      [blockId]
    ),
    shallow
  );
  
  const toggleCollapse = useCodeBlockStore(state => state.toggleCollapse);
  const toggleRawView = useCodeBlockStore(state => state.toggleRawView);
  
  const handleToggleCollapse = useCallback(() => {
    // Optional: Handle scroll position preservation
    const scrollContainer = document.querySelector('.markdown-pane')?.parentElement?.parentElement;
    if (scrollContainer && scrollContainer !== document.documentElement) {
      const currentScrollPosition = scrollContainer.scrollTop;
      toggleCollapse(blockId);
      // You could restore scroll position here if needed
    } else {
      toggleCollapse(blockId);
    }
  }, [blockId, toggleCollapse]);
  
  const handleToggleRaw = useCallback(() => {
    toggleRawView(blockId);
  }, [blockId, toggleRawView]);
  
  const isHtmlBlock = language === 'html' || language === 'htm';
  const DiagramComponent = diagramRenderer ? diagramRenderer.Component : null;
  const nestedBlocks = extractNestedCodeBlocks ? extractNestedCodeBlocks(content) : [];
  
  // ... rest of the component remains the same ...
  // Just replace prop-based handlers with the local ones
});
```

### 4. Helper Function for Scroll Container

Add to `CodeBlock.tsx`:

```typescript
const findScrollContainer = (): Element | null => {
  const containers = [
    document.querySelector('.markdown-pane')?.parentElement?.parentElement,
    document.querySelector('[data-testid="chat-container"]'),
    document.querySelector('.chat-container'),
    document.querySelector('.overflow-auto'),
    document.querySelector('.scroll-container')
  ].filter(Boolean);
  
  return containers.find(container => 
    container && container !== document.documentElement && 
    container.scrollHeight > container.clientHeight
  ) || null;
};
```

## Migration Plan

### Phase 1: Setup (Low Risk)
1. Create the Zustand store file
2. Add necessary imports
3. Deploy store without using it

### Phase 2: Integration (Medium Risk)
1. Update CodeBlock component to use both props and store (backwards compatible)
2. Test thoroughly
3. Update MarkdownPane to pass blockIds
4. Remove old state management

### Phase 3: Optimization (Low Risk)
1. Add bulk operations (expand all/collapse all)
2. Add persistence if desired
3. Add performance monitoring

## Benefits

### Immediate Benefits
1. **Performance**: Only affected code blocks re-render
2. **User Experience**: No visual flashing or delays
3. **Scalability**: Can handle hundreds of code blocks efficiently

### Long-term Benefits
1. **Maintainability**: Centralized state management
2. **Extensibility**: Easy to add new features
3. **Testability**: State logic can be tested independently
4. **Reusability**: State can be accessed from any component

## Testing Strategy

### Unit Tests
```typescript
describe('useCodeBlockStore', () => {
  it('should toggle collapse state', () => {
    const { toggleCollapse, isCollapsed } = useCodeBlockStore.getState();
    
    expect(isCollapsed('block-1')).toBe(true); // default
    toggleCollapse('block-1');
    expect(isCollapsed('block-1')).toBe(false);
  });
});
```

### Integration Tests
1. Verify single block toggle doesn't trigger other renders
2. Test bulk operations
3. Verify state persistence across message changes
4. Test with different renderer types

### Performance Tests
1. Measure render count before/after
2. Profile with React DevTools
3. Test with 50+ code blocks

## Rollback Plan

If issues arise:
1. Keep old state management code commented
2. Add feature flag to toggle between implementations
3. Can revert by changing imports and prop passing

## Future Enhancements

1. **Persistence**: Save collapse state in localStorage
2. **Keyboard Shortcuts**: Ctrl+Shift+C to collapse all
3. **Smart Defaults**: Remember user preferences
4. **Analytics**: Track which languages users expand most
5. **Animations**: Smooth expand/collapse transitions

## Conclusion

This refactor solves the performance issue while improving code organization and enabling future features. The use of Zustand provides a clean, efficient solution that integrates well with the existing codebase.