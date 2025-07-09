# Tip of the Day Feature Design

## Overview
This feature displays a daily tip with a sample prompt when the app starts. The tip appears as a fixed-size popup overlay in the center of the chat space area.

## Architecture

### 1. State Management (Zustand Store)

```typescript
// stores/useTipOfTheDayStore.ts
interface TipOfTheDay {
  id: string;
  tip: string;
  samplePrompt: string;
  category?: string;
  createdAt?: string;
}

interface TipOfTheDayStore {
  // State
  currentTip: TipOfTheDay | null;
  isVisible: boolean;
  isLoading: boolean;
  error: string | null;
  lastShownDate: string | null; // ISO date string
  
  // Actions
  fetchNextTip: () => Promise<void>;
  showTip: (tip: TipOfTheDay) => void;
  hideTip: () => void;
  setLastShownDate: (date: string) => void;
  checkShouldShowTip: () => boolean;
}
```

### 2. API Integration

```typescript
// services/api/apiClient.ts
export interface TipOfTheDayResponse {
  id: string;
  tip: string;
  samplePrompt: string;
  category?: string;
  createdAt?: string;
}

// Add to existing apiClient
async getNextTipOfTheDay(): Promise<TipOfTheDayResponse> {
  const response = await fetch('/api/getNextTipOfTheDay', {
    method: 'GET',
    headers: this.getHeaders(),
  });
  
  if (!response.ok) {
    throw new Error('Failed to fetch tip of the day');
  }
  
  return response.json();
}
```

### 3. Component Structure

```typescript
// components/TipOfTheDay/TipOfTheDayPopup.tsx
interface TipOfTheDayPopupProps {
  tip: TipOfTheDay;
  onShowMe: () => void;
  onDismiss: () => void;
}

// Main popup component with:
// - Fixed dimensions (e.g., 500x300px)
// - Centered overlay positioning
// - Semi-transparent backdrop
// - Smooth fade-in animation
// - "Show Me" and "OK" buttons
```

### 4. Integration Points

The popup should be dismissed when:
- User clicks "OK" button
- User clicks "Show Me" button (also inserts prompt)
- User sends any message
- User starts a new conversation
- User clicks a historical conversation

### 5. Component Hierarchy

```
App.tsx
├── ChatContainer.tsx
│   ├── ChatSpace.tsx (existing)
│   └── TipOfTheDayOverlay.tsx (new)
│       └── TipOfTheDayPopup.tsx (new)
```

## Implementation Details

### Store Implementation
```typescript
// stores/useTipOfTheDayStore.ts
import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { apiClient } from '@/services/api/apiClient';

export const useTipOfTheDayStore = create<TipOfTheDayStore>()(
  persist(
    (set, get) => ({
      currentTip: null,
      isVisible: false,
      isLoading: false,
      error: null,
      lastShownDate: null,
      
      fetchNextTip: async () => {
        set({ isLoading: true, error: null });
        try {
          const tip = await apiClient.getNextTipOfTheDay();
          set({ 
            currentTip: tip, 
            isLoading: false,
            isVisible: true 
          });
        } catch (error) {
          set({ 
            error: error.message || 'Failed to fetch tip', 
            isLoading: false 
          });
        }
      },
      
      showTip: (tip) => set({ currentTip: tip, isVisible: true }),
      
      hideTip: () => set({ isVisible: false }),
      
      setLastShownDate: (date) => set({ lastShownDate: date }),
      
      checkShouldShowTip: () => {
        const { lastShownDate } = get();
        if (!lastShownDate) return true;
        
        const today = new Date().toISOString().split('T')[0];
        const lastShown = new Date(lastShownDate).toISOString().split('T')[0];
        
        return today !== lastShown;
      },
    }),
    {
      name: 'tip-of-the-day-storage',
      partialize: (state) => ({ 
        lastShownDate: state.lastShownDate 
      }),
    }
  )
);
```

### Overlay Component
```typescript
// components/TipOfTheDay/TipOfTheDayOverlay.tsx
import { useEffect } from 'react';
import { useTipOfTheDayStore } from '@/stores/useTipOfTheDayStore';
import { TipOfTheDayPopup } from './TipOfTheDayPopup';

export const TipOfTheDayOverlay: React.FC = () => {
  const { 
    currentTip, 
    isVisible, 
    fetchNextTip, 
    checkShouldShowTip,
    setLastShownDate 
  } = useTipOfTheDayStore();

  useEffect(() => {
    if (checkShouldShowTip()) {
      fetchNextTip();
      setLastShownDate(new Date().toISOString());
    }
  }, []);

  if (!isVisible || !currentTip) return null;

  return (
    <div className="tip-overlay">
      <TipOfTheDayPopup tip={currentTip} />
    </div>
  );
};
```

### Popup Component
```typescript
// components/TipOfTheDay/TipOfTheDayPopup.tsx
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { useTipOfTheDayStore } from '@/stores/useTipOfTheDayStore';
import { useInputBarStore } from '@/stores/useInputBarStore';

export const TipOfTheDayPopup: React.FC<{ tip: TipOfTheDay }> = ({ tip }) => {
  const { hideTip } = useTipOfTheDayStore();
  const { setInputValue, sendMessage } = useInputBarStore();

  const handleShowMe = () => {
    setInputValue(tip.samplePrompt);
    hideTip();
    sendMessage();
  };

  const handleDismiss = () => {
    hideTip();
  };

  return (
    <Card className="tip-popup">
      <div className="tip-header">
        <h3>💡 Tip of the Day</h3>
      </div>
      <div className="tip-content">
        <p className="tip-text">{tip.tip}</p>
        <div className="sample-prompt">
          <span className="prompt-label">Try this:</span>
          <code className="prompt-text">{tip.samplePrompt}</code>
        </div>
      </div>
      <div className="tip-actions">
        <Button onClick={handleShowMe} variant="default">
          Show Me
        </Button>
        <Button onClick={handleDismiss} variant="outline">
          OK
        </Button>
      </div>
    </Card>
  );
};
```

### CSS Styling
```css
/* Overlay container */
.tip-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: rgba(0, 0, 0, 0.3);
  z-index: 100;
  animation: fadeIn 0.2s ease-out;
}

/* Popup card */
.tip-popup {
  width: 500px;
  max-height: 350px;
  padding: 24px;
  background: var(--background);
  border-radius: 12px;
  box-shadow: 0 10px 50px rgba(0, 0, 0, 0.3);
  animation: slideUp 0.3s ease-out;
}

.tip-header {
  margin-bottom: 16px;
  border-bottom: 1px solid var(--border);
  padding-bottom: 12px;
}

.tip-content {
  margin-bottom: 24px;
}

.tip-text {
  font-size: 16px;
  line-height: 1.6;
  margin-bottom: 16px;
}

.sample-prompt {
  background: var(--muted);
  padding: 12px;
  border-radius: 6px;
}

.prompt-label {
  display: block;
  font-size: 12px;
  color: var(--muted-foreground);
  margin-bottom: 4px;
}

.prompt-text {
  font-family: 'Monaco', 'Consolas', monospace;
  font-size: 14px;
  display: block;
  white-space: pre-wrap;
}

.tip-actions {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes slideUp {
  from { 
    transform: translateY(20px);
    opacity: 0;
  }
  to { 
    transform: translateY(0);
    opacity: 1;
  }
}
```

### Integration with Existing Components

1. **ChatContainer Integration**
```typescript
// components/ChatContainer.tsx
// Add TipOfTheDayOverlay as a child
<div className="chat-container">
  <ChatSpace />
  <TipOfTheDayOverlay />
</div>
```

2. **Message Sending Integration**
```typescript
// In useInputBarStore or relevant message sending logic
const sendMessage = () => {
  // Existing logic...
  
  // Hide tip when sending message
  useTipOfTheDayStore.getState().hideTip();
};
```

3. **Conversation Selection Integration**
```typescript
// In conversation selection handlers
const handleConversationSelect = (convId: string) => {
  // Existing logic...
  
  // Hide tip when selecting conversation
  useTipOfTheDayStore.getState().hideTip();
};
```

## Server-Side API Endpoint

```csharp
// Controllers/TipOfTheDayController.cs
[HttpGet("api/getNextTipOfTheDay")]
public async Task<IActionResult> GetNextTipOfTheDay()
{
    // Logic to return next tip based on:
    // - User's previous tips
    // - Rotation strategy
    // - Category preferences
    
    return Ok(new TipOfTheDayResponse
    {
        Id = Guid.NewGuid().ToString(),
        Tip = "Use system prompts to give your AI assistant specific expertise...",
        SamplePrompt = "/system You are an expert Python developer...",
        Category = "system-prompts",
        CreatedAt = DateTime.UtcNow.ToString("O")
    });
}
```
