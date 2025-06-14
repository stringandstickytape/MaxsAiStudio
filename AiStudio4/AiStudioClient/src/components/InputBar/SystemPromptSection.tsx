// AiStudioClient\src\components\InputBar\SystemPromptSection.tsx
import React from 'react';
import { ArrowDownToLine } from 'lucide-react';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { SystemPromptComponent } from '@/components/SystemPrompt/SystemPromptComponent';
import { useJumpToEndStore } from '@/stores/useJumpToEndStore';
import { useModalStore } from '@/stores/useModalStore';

interface SystemPromptSectionProps {
    activeConvId: string | null;
}

// Custom comparison function for SystemPromptSection memoization
const areSystemPromptSectionPropsEqual = (prevProps: SystemPromptSectionProps, nextProps: SystemPromptSectionProps) => {
  return prevProps.activeConvId === nextProps.activeConvId;
};

export const SystemPromptSection = React.memo(({ activeConvId }: SystemPromptSectionProps) => {
    return (
        <div className="mb-2 rounded-lg flex-shrink-0 flex justify-between items-center">
            <SystemPromptComponent
                convId={activeConvId || undefined}
                onOpenLibrary={() => useModalStore.getState().openModal('systemPrompt', {})}
            />
        </div>
    );
}, areSystemPromptSectionPropsEqual);