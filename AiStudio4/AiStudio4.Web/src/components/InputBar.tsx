
import React, { useState, KeyboardEvent, useCallback, useRef, useEffect, FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { v4 as uuidv4 } from 'uuid';
import { Mic, Send, BookMarked, X, Wrench } from 'lucide-react';
import { ModelStatusBar } from '@/components/ModelStatusBar';
import { FileAttachment } from './FileAttachment';
import { Textarea } from '@/components/ui/textarea';
import { useToolStore } from '@/stores/useToolStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useToolsManagement } from '@/hooks/useToolsManagement';
import { useConvStore } from '@/stores/useConvStore';
import { handlePromptShortcut } from '@/commands/shortcutPromptExecutor';
import { usePanelStore } from '@/stores/usePanelStore';
import { useChatManagement } from '@/hooks/useChatManagement';
import { cn } from '@/lib/utils';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { useMediaQuery } from '@/hooks/use-media-query';

interface InputBarProps {
    selectedModel: string;
    onVoiceInputClick?: () => void;
    inputValue?: string;
    onInputChange?: (value: string) => void;
    activeTools?: string[];
    onManageTools?: () => void;
}

export function InputBar({
    selectedModel,
    onVoiceInputClick,
    inputValue,
    onInputChange,
    activeTools: activeToolsFromProps,
    onManageTools,
}: InputBarProps) {
    const textareaRef = useRef<HTMLTextAreaElement>(null);

    const [localInputText, setLocalInputText] = useState('');

    const inputText = inputValue !== undefined ? inputValue : localInputText;
    const setInputText = onInputChange || setLocalInputText;

    const { activeTools: activeToolsFromStore, removeActiveTool } = useToolStore();

    const activeTools = activeToolsFromProps || activeToolsFromStore;

    const handleRemoveTool = (toolId: string) => {
        removeActiveTool(toolId);
    };

    const { convPrompts, defaultPromptId, prompts } = useSystemPromptStore();

    const { activeConvId, slctdMsgId, convs, createConv, getConv } = useConvStore();

    const { sendMessage, isLoading } = useChatManagement();
    const { tools } = useToolsManagement();

    const [cursorPosition, setCursorPosition] = useState<number | null>(null);
    const [visibleToolCount, setVisibleToolCount] = useState(3);
    const toolsContainerRef = useRef<HTMLDivElement>(null);

    
    const isXs = useMediaQuery('(max-width: 640px)');
    const isSm = useMediaQuery('(max-width: 768px)');
    const isMd = useMediaQuery('(max-width: 1024px)');

    const handleTextAreaInput = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
        const value = e.target.value;
        setInputText(value);
        setCursorPosition(e.target.selectionStart);

        if (value.startsWith('/') && value.length > 1 && !value.includes(' ')) {

            if (e.nativeEvent instanceof InputEvent && e.nativeEvent.data === ' ') {
                if (handlePromptShortcut(value)) {

                    setTimeout(() => {
                        if (textareaRef.current) {
                            const length = textareaRef.current.value.length;
                            textareaRef.current.setSelectionRange(length, length);
                            setCursorPosition(length);
                        }
                    }, 0);
                }
            }
        }
    };

    const handleTextAreaClick = (e: React.MouseEvent<HTMLTextAreaElement>) => {
        setCursorPosition(e.currentTarget.selectionStart);
    };

    const handleTextAreaKeyUp = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
        setCursorPosition(e.currentTarget.selectionStart);
    };

    
    useEffect(() => {
        
        if (isXs) {
            setVisibleToolCount(1);
        } else if (isSm) {
            setVisibleToolCount(2);
        } else if (isMd) {
            setVisibleToolCount(3);
        } else {
            setVisibleToolCount(4);
        }

        
        const observer = new ResizeObserver(() => {
            if (!toolsContainerRef.current) return;
            const containerWidth = toolsContainerRef.current.clientWidth;

            
            
            const estimatedToolCapacity = Math.floor(containerWidth / 120);

            
            let count = Math.max(1, estimatedToolCapacity);
            if (isXs) count = Math.min(count, 1);
            else if (isSm) count = Math.min(count, 2);
            else if (isMd) count = Math.min(count, 3);
            else count = Math.min(count, 4);

            if (count !== visibleToolCount) {
                setVisibleToolCount(count);
            }
        });

        if (toolsContainerRef.current) {
            observer.observe(toolsContainerRef.current);
        }

        return () => observer.disconnect();
    }, [isXs, isSm, isMd, visibleToolCount]);

    const handleAttachFile = (file: File, content: string) => {
        const fileName = file.name;

        const textToInsert = `\`\`\`${fileName}\n${content}\n\`\`\`\n`;

        const pos = cursorPosition !== null ? cursorPosition : inputText.length;

        const newText = inputText.substring(0, pos) + textToInsert + inputText.substring(pos);

        setInputText(newText);

        setTimeout(() => {
            if (textareaRef.current) {
                textareaRef.current.focus();
                const newPosition = pos + textToInsert.length;
                textareaRef.current.setSelectionRange(newPosition, newPosition);
                setCursorPosition(newPosition);
            }
        }, 0);
    };

    const handleChatMessage = useCallback(
        async (message: string) => {
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
                    parentMessageId = slctdMsgId;

                    if (!parentMessageId) {
                        const conv = convs[convId];
                        if (conv && conv.messages.length > 0) {
                            parentMessageId = conv.messages[conv.messages.length - 1].id;
                        }
                    }
                }

                if (convId) {
                    systemPromptId = convPrompts[convId] || defaultPromptId;

                    if (systemPromptId) {
                        const prompt = prompts.find((p) => p.guid === systemPromptId);
                        if (prompt) {
                            systemPromptContent = prompt.content;
                        }
                    }
                }

                await sendMessage({
                    convId,
                    parentMessageId,
                    message,
                    model: selectedModel,
                    toolIds: activeTools,
                    systemPromptId,
                    systemPromptContent,
                });

                setInputText('');
                setCursorPosition(0);
            } catch (error) {
                console.error('Error sending message:', error);
            }
        },
        [
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
            getConv,
        ],
    );

    const handleSend = () => {
        if (inputText.trim() && !isLoading) {
            handleChatMessage(inputText);
        }
    };

    const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && e.ctrlKey) {
            e.preventDefault();
            handleSend();
            return;
        }

        if (e.key === ' ' && inputText.startsWith('/') && !inputText.includes(' ')) {
            if (handlePromptShortcut(inputText)) {
                e.preventDefault();

            }
        }

        if (e.key === 'Tab' && inputText.startsWith('/') && !inputText.includes(' ')) {
            if (handlePromptShortcut(inputText)) {
                e.preventDefault();
            }
        }
    };

    useEffect(() => {
        const handleAppendToPrompt = (event: CustomEvent<{ text: string }>) => {
            const textToAppend = event.detail.text;

            setInputText((currentText) => currentText + textToAppend);

            if (textareaRef.current) {
                textareaRef.current.focus();

                setTimeout(() => {
                    if (textareaRef.current) {
                        const length = textareaRef.current.value.length;
                        textareaRef.current.setSelectionRange(length, length);
                        setCursorPosition(length);
                    }
                }, 0);
            }
        };

        const handleSetPrompt = (event: CustomEvent<{ text: string }>) => {
            const newText = event.detail.text;

            setInputText(newText);

            if (textareaRef.current) {
                textareaRef.current.focus();

                setTimeout(() => {
                    if (textareaRef.current) {
                        const length = textareaRef.current.value.length;
                        textareaRef.current.setSelectionRange(length, length);
                        setCursorPosition(length);
                    }
                }, 0);
            }
        };

        window.addEventListener('append-to-prompt', handleAppendToPrompt as EventListener);
        window.addEventListener('set-prompt', handleSetPrompt as EventListener);

        return () => {
            window.removeEventListener('append-to-prompt', handleAppendToPrompt as EventListener);
            window.removeEventListener('set-prompt', handleSetPrompt as EventListener);
        };
    }, []);

    
    const handlePrimaryModelClick = () => {
        const event = new CustomEvent('select-primary-model');
        window.dispatchEvent(event);
    };

    const handleSecondaryModelClick = () => {
        const event = new CustomEvent('select-secondary-model');
        window.dispatchEvent(event);
    };

    return (
        <div className="h-[280px] bg-gray-900 border-gray-700/50  shadow-2xl p-3 relative before:content-[''] before:absolute before:top-[-15px] before:left-0 before:right-0 before:h-[15px] before:bg-transparent backdrop-blur-sm">
            <div className="flex flex-col h-full">
                <div className="flex-1 flex gap-2">
                    <div className="relative flex-1">
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
                        <FileAttachment onAttach={handleAttachFile} disabled={isLoading} />

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
                            className="btn-primary icon-btn"
                            aria-label="Send message"
                            disabled={isLoading}
                        >
                            <Send className="h-5 w-5" />
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

                        <div className="flex items-center ml-3 pl-3 border-l border-gray-700/50">
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
                                    {activeTools.slice(0, visibleToolCount).map((toolId) => {
                                        const tool = tools.find((t) => t.guid === toolId);
                                        if (!tool) return null;

                                        return (
                                            <TooltipProvider key={tool.guid}>
                                                <Tooltip>
                                                    <TooltipTrigger asChild>
                                                        <div className="flex items-center gap-0.5 h-5 px-2 py-0 text-xs rounded-full bg-green-600/10 border border-green-700/20 text-green-200 hover:bg-green-600/30 hover:text-green-100 transition-colors cursor-pointer group flex-shrink-0">
                                                            <span className="truncate max-w-[100px]">{tool.name}</span>
                                                            <button
                                                                onClick={() => handleRemoveTool(tool.guid)}
                                                                className="ml-1 text-gray-400 hover:text-gray-100 p-0.5 rounded-full opacity-0 group-hover:opacity-100 transition-opacity"
                                                            >
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

window.appendToPrompt = function (text) {
    const appendEvent = new CustomEvent('append-to-prompt', {
        detail: { text: text },
    });

    window.dispatchEvent(appendEvent);

    console.log(`Appended to prompt: "${text}"`);

    return true;
};

window.setPrompt = function (text) {
    const setEvent = new CustomEvent('set-prompt', {
        detail: { text: text },
    });

    window.dispatchEvent(setEvent);

    console.log(`Set prompt to: "${text}"`);

    return true;
};