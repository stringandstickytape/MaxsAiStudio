import React, { useState, KeyboardEvent, useCallback, useRef, useEffect, useMemo } from 'react';
import { Button } from '@/components/ui/button';
import { v4 as uuidv4 } from 'uuid';
import { Mic, Send, BookMarked, X, Wrench, ArrowDown } from 'lucide-react';
import { ModelStatusBar } from '@/components/ModelStatusBar';
import { FileAttachment } from './FileAttachment';
import { Attachment } from '@/types/attachment';
import { AttachmentPreviewBar } from './AttachmentPreview';
import { Textarea } from '@/components/ui/textarea';
import { useToolStore } from '@/stores/useToolStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useToolsManagement } from '@/hooks/useToolsManagement';
import { useConvStore } from '@/stores/useConvStore';
import { handlePromptShortcut } from '@/commands/shortcutPromptExecutor';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { useChatManagement } from '@/hooks/useChatManagement';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { useMediaQuery } from '@/hooks/use-media-query';

interface InputBarProps {
    selectedModel: string;
    onVoiceInputClick?: () => void;
    inputValue?: string;
    onInputChange?: (value: string) => void;
    activeTools?: string[];
    onManageTools?: () => void;
    onAttachmentChange?: (attachments: Attachment[]) => void;
    disabled?: boolean;
}

declare global {
    interface Window {
        scrollChatToBottom?: () => void;
        getScrollButtonState?: () => boolean;
    }
}

export function InputBar({
    selectedModel,
    onVoiceInputClick,
    inputValue,
    onInputChange,
    activeTools: activeToolsFromProps,
    onManageTools,
    onAttachmentChange,
    disabled = false,
}: InputBarProps) {
    const textareaRef = useRef<HTMLTextAreaElement>(null);
    const toolsContainerRef = useRef<HTMLDivElement>(null);

    const [localInputText, setLocalInputText] = useState('');
    const [attachments, setAttachments] = useState<Attachment[]>([]);
    const [showScrollButton, setShowScrollButton] = useState(false);
    const [cursorPosition, setCursorPosition] = useState<number | null>(null);
    const [visibleToolCount, setVisibleToolCount] = useState(3);

    const inputText = inputValue ?? localInputText;
    const setInputText = onInputChange || setLocalInputText;
    const { activeTools: activeToolsFromStore, removeActiveTool } = useToolStore();
    const activeTools = activeToolsFromProps || activeToolsFromStore;

    const handleRemoveTool = (toolId: string) => {
        removeActiveTool(toolId);
    };

    const { convPrompts, defaultPromptId, prompts } = useSystemPromptStore();
    const { activeConvId, slctdMsgId, convs, createConv } = useConvStore();
    const { sendMessage, cancelMessage, isLoading } = useChatManagement();
    const { isCancelling, currentRequest, setIsCancelling, setCurrentRequest } = useWebSocketStore();
    const { tools } = useToolsManagement();

    const isXs = useMediaQuery('(max-width: 640px)');
    const isSm = useMediaQuery('(max-width: 768px)');
    const isMd = useMediaQuery('(max-width: 1024px)');

    // Function to format text attachments into message content
    const formatTextAttachments = (textAttachments: Attachment[]): string => {
        if (textAttachments.length === 0) return '';

        let formattedContent = '';

        textAttachments.forEach(attachment => {
            if (attachment.textContent) {
                // Determine language based on file extension
                const fileExt = attachment.name.split('.').pop()?.toLowerCase() || '';
                let language = '';

                // Map common extensions to languages
                switch (fileExt) {
                    case 'json': language = 'json'; break;
                    case 'md': language = 'markdown'; break;
                    case 'html': language = 'html'; break;
                    case 'css': language = 'css'; break;
                    case 'js': language = 'javascript'; break;
                    case 'ts': language = 'typescript'; break;
                    case 'py': language = 'python'; break;
                    case 'java': language = 'java'; break;
                    case 'c': language = 'c'; break;
                    case 'cpp': language = 'cpp'; break;
                    case 'cs': language = 'csharp'; break;
                    case 'php': language = 'php'; break;
                    case 'rb': language = 'ruby'; break;
                    case 'go': language = 'go'; break;
                    case 'rs': language = 'rust'; break;
                    case 'sh': language = 'bash'; break;
                    case 'sql': language = 'sql'; break;
                    case 'xml': language = 'xml'; break;
                    case 'csv': language = 'csv'; break;
                    case 'txt': default: language = '';
                }

                formattedContent += `\n\n**File: ${attachment.name}**\n\`\`\`${language}\n${attachment.textContent}\n\`\`\`\n`;
            }
        });

        return formattedContent;
    };

    // Handle attachment changes
    const handleAttachmentChange = useCallback((newAttachments: Attachment[]) => {
        setAttachments(newAttachments);
        if (onAttachmentChange) {
            onAttachmentChange(newAttachments);
        }
    }, [onAttachmentChange]);

    const handleTextAreaInput = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
        const value = e.target.value;
        setInputText(value);
        setCursorPosition(e.target.selectionStart);

        if (value.startsWith('/') && value.length > 1 && !value.includes(' ') &&
            e.nativeEvent instanceof InputEvent && e.nativeEvent.data === ' ' &&
            handlePromptShortcut(value)) {
            setTimeout(() => {
                const length = textareaRef.current?.value.length;
                textareaRef.current?.setSelectionRange(length, length);
                setCursorPosition(length);
            }, 0);
        }
    };

    const handleTextAreaClick = (e: React.MouseEvent<HTMLTextAreaElement>) => {
        setCursorPosition(e.currentTarget.selectionStart);
    };

    const handleTextAreaKeyUp = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
        setCursorPosition(e.currentTarget.selectionStart);
    };


    useEffect(() => {
        setVisibleToolCount(isXs ? 1 : isSm ? 2 : isMd ? 3 : 4);

        const observer = new ResizeObserver(() => {
            if (!toolsContainerRef.current) return;
            const containerWidth = toolsContainerRef.current.clientWidth;
            const estimatedToolCapacity = Math.floor(containerWidth / 120);

            let count = Math.max(1, estimatedToolCapacity);
            count = isXs ? Math.min(count, 1) :
                isSm ? Math.min(count, 2) :
                    isMd ? Math.min(count, 3) :
                        Math.min(count, 4);

            count !== visibleToolCount && setVisibleToolCount(count);
        });

        toolsContainerRef.current && observer.observe(toolsContainerRef.current);
        return () => observer.disconnect();
    }, [isXs, isSm, isMd, visibleToolCount]);

    const handleChatMessage = useCallback(async (message: string) => {
        console.log('Sending message with active tools:', activeTools);
        try {
            let convId = activeConvId;
            let parentMessageId = null;
            let systemPromptId = null;
            let systemPromptContent = null;

            if (!convId) {
                convId = `conv_${uuidv4()}`;
                const messageId = `msg_${uuidv4()}`;
                createConv({
                    id: convId,
                    rootMessage: {
                        id: messageId,
                        content: '',
                        source: 'system',
                        timestamp: Date.now(),
                    },
                });
                parentMessageId = messageId;
            } else {
                parentMessageId = slctdMsgId ||
                    (convs[convId]?.messages.length > 0 ?
                        convs[convId].messages[convs[convId].messages.length - 1].id :
                        null);
            }

            if (convId) {
                systemPromptId = convPrompts[convId] || defaultPromptId;
                const prompt = prompts.find(p => p.guid === systemPromptId);
                systemPromptContent = prompt?.content;
            }

            const messageId = `msg_${uuidv4()}`;
            setCurrentRequest({ convId, messageId });
            await sendMessage({
                convId,
                parentMessageId,
                message,
                model: selectedModel,
                toolIds: activeTools,
                systemPromptId,
                systemPromptContent,
                messageId,
                attachments: attachments.length > 0 ? attachments : undefined
            });

            setInputText('');
            setAttachments([]);
            setCursorPosition(0);
        } catch (error) {
            console.error('Error sending message:', error);
        }
    }, [
        selectedModel,
        setInputText,
        activeTools,
        convPrompts,
        defaultPromptId,
        prompts,
        sendMessage,
        activeConvId,
        slctdMsgId,
        convs,
        createConv,
        attachments
    ]);

    const handleSend = () => {
        if (isCancelling) return;

        isLoading && currentRequest
            ? (setIsCancelling(true), cancelMessage({
                convId: currentRequest.convId,
                messageId: currentRequest.messageId
            }))
            : inputText.trim() && !isLoading && (() => {
                // Filter text attachments
                const textAttachments = attachments.filter(att => att.textContent);

                // Format text from text files
                const textFileContent = formatTextAttachments(textAttachments);

                // Combine user input with text file content
                const fullMessage = inputText + textFileContent;

                handleChatMessage(fullMessage);
            })();
    };

    useEffect(() => {
        !isLoading && isCancelling && setIsCancelling(false);
    }, [isLoading, isCancelling, setIsCancelling]);

    const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && e.ctrlKey) {
            e.preventDefault();
            handleSend();
            return;
        }

        if ((e.key === ' ' || e.key === 'Tab') &&
            inputText.startsWith('/') &&
            !inputText.includes(' ') &&
            handlePromptShortcut(inputText)) {
            e.preventDefault();
        }
    };

    useEffect(() => {
        const focusTextarea = (length: number) => {
            if (!textareaRef.current) return;
            textareaRef.current.focus();
            setTimeout(() => {
                if (textareaRef.current) {
                    const len = length ?? textareaRef.current.value.length;
                    textareaRef.current.setSelectionRange(len, len);
                    setCursorPosition(len);
                }
            }, 0);
        };

        const handleAppendToPrompt = (event: CustomEvent<{ text: string }>) => {
            setInputText(text => text + event.detail.text);
            focusTextarea(null);
        };

        const handleSetPrompt = (event: CustomEvent<{ text: string }>) => {
            setInputText(event.detail.text);
            focusTextarea(null);
        };

        window.addEventListener('append-to-prompt', handleAppendToPrompt as EventListener);
        window.addEventListener('set-prompt', handleSetPrompt as EventListener);

        return () => {
            window.removeEventListener('append-to-prompt', handleAppendToPrompt as EventListener);
            window.removeEventListener('set-prompt', handleSetPrompt as EventListener);
        };
    }, []);

    const handlePrimaryModelClick = () =>
        window.dispatchEvent(new CustomEvent('select-primary-model'));

    useEffect(() => {
        const handleScrollButtonStateChange = (event: CustomEvent<{ visible: boolean }>) =>
            setShowScrollButton(event.detail.visible);

        window.addEventListener('scroll-button-state-change', handleScrollButtonStateChange as EventListener);
        window.getScrollButtonState && setShowScrollButton(window.getScrollButtonState());

        return () => window.removeEventListener('scroll-button-state-change',
            handleScrollButtonStateChange as EventListener);
    }, []);

    const handleScrollToBottom = () => window.scrollChatToBottom?.();
    const handleSecondaryModelClick = () =>
        window.dispatchEvent(new CustomEvent('select-secondary-model'));

    return (
        <div className="h-[280px] bg-gray-900 border-gray-700/50 shadow-2xl p-3 relative before:content-[''] before:absolute before:top-[-15px] before:left-0 before:right-0 before:h-[15px] before:bg-transparent backdrop-blur-sm">
            {showScrollButton && (
                <div className="fixed bottom-[280px] right-6 z-50 mb-2 animate-fade-in">
                    <Button variant="outline" size="sm" onClick={handleScrollToBottom}
                        className="bg-gray-700/80 hover:bg-gray-600 text-gray-300 px-4 py-1 rounded-full h-8 flex items-center justify-center transition-all duration-200 shadow-lg backdrop-blur-sm">
                        <ArrowDown className="h-4 w-4 mr-1" />
                        <span className="text-xs">Scroll to bottom</span>
                    </Button>
                </div>
            )}
            <div className="flex flex-col h-full">
                <div className="flex-1 flex gap-2">
                    <div className="relative flex-1">
                        {attachments.length > 0 && (
                            <div className="mb-2">
                                <AttachmentPreviewBar
                                    attachments={attachments}
                                    onRemove={(id) => setAttachments(prev => prev.filter(a => a.id !== id))}
                                    onClear={() => setAttachments([])}
                                />
                            </div>
                        )}
                        <Textarea
                            ref={textareaRef}
                            className="w-full h-[120px] p-4 border rounded-xl resize-none focus:outline-none shadow-inner transition-all duration-200 placeholder:text-gray-400 input-ghost"
                            value={inputText}
                            onChange={handleTextAreaInput}
                            onClick={handleTextAreaClick}
                            onKeyUp={handleTextAreaKeyUp}
                            onKeyDown={handleKeyDown}
                            placeholder="Type your message here... (Ctrl+Enter to send)"
                            disabled={isLoading}
                            showLineCount={true}
                            style={{ height: '100%' }}
                        />
                    </div>

                    <div className="flex flex-col gap-2 justify-end">
                        <FileAttachment onAttachmentChange={handleAttachmentChange} disabled={isLoading || disabled} />

                        <Button
                            variant="outline"
                            size="icon"
                            onClick={() => window.dispatchEvent(new CustomEvent('open-user-prompt-library'))}
                            className="btn-ghost icon-btn bg-gray-800 border-gray-700 hover:text-blue-400"
                            aria-label="User prompts"
                            disabled={isLoading}
                        >
                            <BookMarked className="h-5 w-5" />
                        </Button>

                        {onVoiceInputClick && (
                            <Button
                                variant="outline"
                                size="icon"
                                onClick={onVoiceInputClick}
                                className="btn-ghost icon-btn bg-gray-800 border-gray-700 hover:text-blue-400"
                                aria-label="Voice input"
                                disabled={isLoading}
                            >
                                <Mic className="h-5 w-5" />
                            </Button>
                        )}

                        <Button
                            variant="outline"
                            size="icon"
                            onClick={handleSend}
                            className={`${isLoading ? 'bg-red-600 hover:bg-red-700' : 'btn-primary'} icon-btn`}
                            aria-label={isLoading ? 'Cancel' : 'Send message'}
                            disabled={isCancelling}
                        >
                            {isLoading ? (
                                <X className="h-5 w-5" />
                            ) : (
                                <Send className="h-5 w-5" />
                            )}
                        </Button>
                    </div>
                </div>

                <div className="pt-2 border-t border-gray-700/30">
                    <div className="flex items-center">
                        <div className="flex items-center">
                            <ModelStatusBar
                                onPrimaryClick={handlePrimaryModelClick}
                                onSecondaryClick={handleSecondaryModelClick}
                            />
                        </div>

                        <div className="flex items-center gap-2 ml-3 pl-3 border-l border-gray-700/50">

                            <Button
                                variant="ghost"
                                size="sm"
                                onClick={onManageTools || (() => window.dispatchEvent(new CustomEvent('open-tool-library')))}
                                className="h-5 px-2 py-0 text-xs rounded-full bg-gray-600/10 border border-gray-700/20 text-gray-300 hover:bg-gray-600/30 hover:text-gray-100 transition-colors flex-shrink-0"
                            >
                                <Wrench className="h-3 w-3 mr-1" />
                                <span>Tools</span>
                            </Button>

                            {activeTools.length > 0 && (
                                <div ref={toolsContainerRef} className="flex items-center gap-1.5 overflow-hidden ml-2">
                                    {activeTools.slice(0, visibleToolCount).map(toolId => {
                                        const tool = tools.find(t => t.guid === toolId);
                                        return !tool ? null : (
                                            <TooltipProvider key={tool.guid}>
                                                <Tooltip>
                                                    <TooltipTrigger asChild>
                                                        <div className="flex items-center gap-0.5 h-5 px-2 py-0 text-xs rounded-full bg-green-600/10 border border-green-700/20 text-green-200 hover:bg-green-600/30 hover:text-green-100 transition-colors cursor-pointer group flex-shrink-0">
                                                            <span className="truncate max-w-[100px]">{tool.name}</span>
                                                            <button onClick={() => handleRemoveTool(tool.guid)}
                                                                className="ml-1 text-gray-400 hover:text-gray-100 p-0.5 rounded-full opacity-0 group-hover:opacity-100 transition-opacity">
                                                                <X className="h-3 w-3" />
                                                            </button>
                                                        </div>
                                                    </TooltipTrigger>
                                                    <TooltipContent>
                                                        <p>{tool.description}</p>
                                                    </TooltipContent>
                                                </Tooltip>
                                            </TooltipProvider>
                                        );
                                    })}
                                    {activeTools.length > visibleToolCount && (
                                        <TooltipProvider>
                                            <Tooltip>
                                                <TooltipTrigger asChild>
                                                    <div
                                                        className="flex items-center h-5 px-2 py-0 text-xs rounded-full bg-blue-600/20 border border-blue-700/20 text-blue-200 hover:bg-blue-600/30 hover:text-blue-100 transition-colors cursor-pointer flex-shrink-0"
                                                        onClick={onManageTools || (() => window.dispatchEvent(new CustomEvent('open-tool-library')))}
                                                    >
                                                        +{activeTools.length - visibleToolCount} more
                                                    </div>
                                                </TooltipTrigger>
                                                <TooltipContent>
                                                    <p>Click to see all active tools</p>
                                                </TooltipContent>
                                            </Tooltip>
                                        </TooltipProvider>
                                    )}
                                </div>
                            )}
                        </div>
                        <div className="flex-1"></div>
                    </div>
                </div>
            </div>
        </div>
    );
}

window.appendToPrompt = text => {
    window.dispatchEvent(new CustomEvent('append-to-prompt', { detail: { text } }));
    console.log(`Appended to prompt: "${text}"`);
    return true;
};

window.setPrompt = text => {
    window.dispatchEvent(new CustomEvent('set-prompt', { detail: { text } }));
    console.log(`Set prompt to: "${text}"`);
    return true;
};