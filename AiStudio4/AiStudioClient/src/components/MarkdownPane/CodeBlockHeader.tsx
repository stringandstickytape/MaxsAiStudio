// MarkdownPane/CodeBlockHeader.tsx
import React from 'react';
import { ExternalLink, Clipboard, ChevronDown, ChevronUp, Save } from 'lucide-react';
import { createApiRequest } from '@/utils/apiUtils';
import { cn } from '@/lib/utils';
import { MarkdownVariant } from '../MarkdownPane';

export interface CodeBlockHeaderProps {
    language: string;
    content: string;
    isCollapsed: boolean;
    isRawView: boolean;
    isVisualStudio: boolean;
    onToggleRaw: () => void;
    onToggleCollapse: () => void;
    launchHtml?: () => void;
    variant: MarkdownVariant; // <-- ADD PROP
}

export const CodeBlockHeader: React.FC<CodeBlockHeaderProps> = ({
    language,
    content,
    isCollapsed,
    isRawView,
    isVisualStudio,
    onToggleRaw,
    onToggleCollapse,
    launchHtml,
    variant,
}) => {
    const handleSaveToFile = async () => {
        let suggestedFilename = `codeblock.${language || 'txt'}`; // Use language for extension, fallback to .txt
        try {
            const saveFileRequest = createApiRequest('/api/saveCodeBlockAsFile', 'POST');
            await saveFileRequest({ content, suggestedFilename });
        } catch (e) {
            console.error('Save As failed:', e);
            alert('Failed to save file. Check console for details.'); // Simple user feedback
        }
    };

    // --- ADD CONDITIONAL STYLING ---
    const headerClasses = cn(
        "MarkdownPane flex items-center justify-between px-2 py-1 rounded-t-xl border-b text-sm",
        variant === 'system' && "bg-destructive/20 border-destructive/50 text-destructive-foreground"
    );

    const buttonClasses = cn(
        "px-2 py-1 rounded transition-colors",
        variant === 'system' 
            ? "bg-destructive hover:bg-destructive/80 text-destructive-foreground" 
            : ""
    );

    const iconButtonClasses = cn(
        "p-1 transition-colors",
        variant === 'system' 
            ? "text-destructive-foreground/80 hover:text-destructive-foreground"
            : ""
    );

    return (
        // --- APPLY CONDITIONAL CLASSES ---
        <div
            className={headerClasses}
            style={variant === 'system' ? {} : {
                background: 'var(--global-background-color, var(--markdownpane-codeheader-bg, #181c20))',
                color: 'var(--global-text-color, var(--markdownpane-codeheader-text, #bfc7d5))',
                borderColor: 'var(--global-border-color, var(--markdownpane-codeheader-border, #283040))',
                border: '0px',
                borderTopWidth: 0,
            }}
        >
            <div className="flex items-center space-x-2">
                <button
                    onClick={onToggleCollapse}
                    className={cn("transition-colors", iconButtonClasses)}
                    style={variant === 'system' ? {} : { color: 'var(--global-primary-color, var(--markdownpane-codeheader-accent, #4f8cff))' }}
                    title={isCollapsed ? "Expand code block" : "Collapse code block"}
                >
                    {isCollapsed ? <ChevronDown size={16} /> : <ChevronUp size={16} />}
                </button>
                <div className="font-medium">{language}</div>
            </div>
            <div className="flex space-x-2">
                {isVisualStudio && (
                    <button
                        onClick={() => window.chrome.webview.postMessage({ type: 'applyNewDiff', content: content.trim() })}
                        className={buttonClasses}
                        style={variant === 'system' ? {} : {
                            background: 'var(--global-primary-color, var(--markdownpane-codeheader-accent, #4f8cff))',
                            color: 'var(--global-background-color, var(--markdownpane-codeheader-bg, #181c20))',
                        }}
                        title="Apply as Diff"
                    >
                        Apply Diff
                    </button>
                )}
                {launchHtml && (
                    <button
                        onClick={launchHtml}
                        className={cn(buttonClasses, "flex items-center gap-1")}
                        style={variant === 'system' ? {} : {
                            background: 'var(--global-primary-color, var(--markdownpane-codeheader-accent, #4f8cff))',
                            color: 'var(--global-background-color, var(--markdownpane-codeheader-bg, #181c20))',
                        }}
                        title="Launch HTML in new tab"
                    >
                        <ExternalLink className="h-3 w-3" />
                        Launch
                    </button>
                )}
                {language === 'theme' && (
                    <>
                        <button
                            onClick={async () => {
                                try {
                                    // Parse the theme content
                                    const parsedContent = JSON.parse(content);
                                    
                                    // Apply theme visually
                                    window.applyLLMTheme?.(parsedContent);
                                    
                                    // Get theme name from the theme-name property if available, otherwise use timestamp
                                    const themeName = parsedContent['theme-name'] || `Theme from code block ${new Date().toLocaleTimeString()}`;
                                    
                                    // Add theme to zustand store
                                    if (window.addThemeToStore) {
                                        window.addThemeToStore({
                                            name: themeName,
                                            description: 'Theme applied from markdown code block',
                                            themeJson: parsedContent
                                        });
                                    }
                                } catch (e) {
                                    console.error('[Theme Debug] Error processing theme:', e);
                                }
                            }}
                            className={cn(buttonClasses, "mr-2")}
                            style={{
                                background: 'var(--global-primary-color, var(--markdownpane-codeheader-accent, #4f8cff))',
                                color: 'var(--global-background-color, var(--markdownpane-codeheader-bg, #181c20))',
                            }}
                            title="Apply this theme"
                        >
                            Use Theme
                        </button>
                        <button
                            onClick={async () => {
                                try {
                                    // Parse the theme content
                                    const parsedContent = JSON.parse(content);
                                    
                                    // Get theme name from the theme-name property if available, otherwise use timestamp
                                    const themeName = parsedContent['theme-name'] || `Theme from code block ${new Date().toLocaleTimeString()}`;
                                    
                                    // Install the theme to the library
                                    if (typeof window.createTheme === 'function') {
                                        await window.createTheme({
                                            name: themeName,
                                            description: 'Theme installed from markdown code block',
                                            themeJson: parsedContent,
                                            previewColors: Object.values(parsedContent)
                                                .filter(value => typeof value === 'string' && value.startsWith('#'))
                                                .slice(0, 5)
                                        });
                                    } else {
                                        console.error('createTheme function not available on window');
                                        // Try to import the function directly as a fallback
                                        try {
                                            const { useThemeManagement } = await import('@/hooks/useThemeManagement');
                                            const { createTheme } = useThemeManagement();
                                            
                                            if (typeof createTheme === 'function') {
                                                await createTheme({
                                                    name: themeName,
                                                    description: 'Theme installed from markdown code block',
                                                    themeJson: parsedContent,
                                                    previewColors: Object.values(parsedContent)
                                                        .filter(value => typeof value === 'string' && value.startsWith('#'))
                                                        .slice(0, 5)
                                                });
                                            } else {
                                                throw new Error('createTheme function not available');
                                            }
                                        } catch (importError) {
                                            console.error('Failed to import useThemeManagement:', importError);
                                        }
                                    }
                                } catch (e) {
                                    console.error('[Theme Debug] Error installing theme:', e);
                                }
                            }}
                            className={buttonClasses}
                            style={{
                                background: 'var(--global-primary-color, var(--markdownpane-codeheader-accent, #4f8cff))',
                                color: 'var(--global-background-color, var(--markdownpane-codeheader-bg, #181c20))',
                            }}
                            title="Install this theme"
                        >
                            Install Theme
                        </button>
                    </>
                )}
                <button
                    onClick={() => navigator.clipboard.writeText(content)}
                    className={cn(buttonClasses, "flex items-center justify-center")}
                    style={{
                        background: 'var(--global-primary-color, var(--markdownpane-codeheader-accent, #4f8cff))',
                        color: 'var(--global-background-color, var(--markdownpane-codeheader-bg, #181c20))',
                    }}
                    title="Copy code block"
                >
                    <Clipboard size={14} />
                </button>
                <button
                    onClick={handleSaveToFile}
                    className={cn(buttonClasses, "flex items-center justify-center")}
                    style={{
                        background: 'var(--global-primary-color, var(--markdownpane-codeheader-accent, #4f8cff))',
                        color: 'var(--global-background-color, var(--markdownpane-codeheader-bg, #181c20))',
                    }}
                    title="Save code block to file"
                >
                    <Save size={14} />
                </button>
                <button
                    onClick={onToggleRaw}
                    className={buttonClasses}
                    style={{
                        background: 'var(--global-primary-color, var(--markdownpane-codeheader-accent, #4f8cff))',
                        color: 'var(--global-background-color, var(--markdownpane-codeheader-bg, #181c20))',
                    }}
                    title={isRawView ? "Show rendered content" : "Show raw content"}
                >
                    {isRawView ? 'Rendered' : 'Raw'}
                </button>
            </div>
        </div>
    );
};