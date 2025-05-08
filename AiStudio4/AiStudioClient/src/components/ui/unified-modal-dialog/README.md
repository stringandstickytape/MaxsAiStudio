# Unified Modal Dialog Component

This directory contains the components for a unified, flexible modal dialog system for AiStudio4.

## Components

-   `UnifiedModalDialog`: The main wrapper component using Radix UI Dialog primitives.
-   `UnifiedModalHeader`: A standardized header section.
-   `UnifiedModalContent`: A standardized content section (handles scrolling).
-   `UnifiedModalFooter`: A standardized footer section.
-   `UnifiedModalContext`: React context to share modal properties (like variant) internally.
-   `types.ts`: TypeScript type definitions.
-   `variants.ts`: Predefined size, height, and style variant configurations.
-   `index.ts`: Barrel file for easy imports.

## Usage

```tsx
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
  UnifiedModalFooter
} from '@/components/ui/unified-modal-dialog';

function MyDialog() {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <UnifiedModalDialog open={isOpen} onOpenChange={setIsOpen} variant="form" size="lg">
      <UnifiedModalHeader>
        <h2 className="text-lg font-semibold">My Dialog Title</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent>
        <p>Dialog content goes here. It will scroll if it overflows.</p>
      </UnifiedModalContent>
      <UnifiedModalFooter>
        <Button variant="outline" onClick={() => setIsOpen(false)}>Cancel</Button>
        <Button onClick={() => { /* Action */ }}>Submit</Button>
      </UnifiedModalFooter>
    </UnifiedModalDialog>
  );
}
```

See `types.ts` for all available props and `variants.ts` for predefined configurations.