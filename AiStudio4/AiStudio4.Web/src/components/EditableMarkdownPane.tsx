import { useState, useEffect, useRef } from "react"
import ReactMarkdown from 'react-markdown'
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter'
import { nightOwl } from 'react-syntax-highlighter/dist/cjs/styles/prism'
import { diagramRegistry } from '@/components/diagrams/registry'

interface MarkdownPaneProps {
    message: string;
    onUpdate?: (newContent: string) => void; // Optional callback for saving updates
    editable?: boolean; // Control whether editing is allowed
}

export function EditableMarkdownPane({ message, onUpdate, editable = false }: MarkdownPaneProps) {
    const [markdownContent, setMarkdownContent] = useState<string>('')
    const [mermaidKey, setMermaidKey] = useState(0)
    const [showRawContent, setShowRawContent] = useState<Record<string, boolean>>({})
    const [isEditing, setIsEditing] = useState(false)
    const [editableContent, setEditableContent] = useState('')
    const textareaRef = useRef<HTMLTextAreaElement>(null)

    useEffect(() => {
        setMarkdownContent(message);
        setEditableContent(message);
        setMermaidKey(prev => prev + 1);
    }, [message])

    useEffect(() => {
        diagramRegistry.renderAll();
    }, [markdownContent, mermaidKey]);

    // Auto-resize textarea when content changes
    useEffect(() => {
        if (isEditing && textareaRef.current) {
            textareaRef.current.style.height = 'auto';
            textareaRef.current.style.height = `${textareaRef.current.scrollHeight}px`;
        }
    }, [editableContent, isEditing]);

    const handleEdit = () => {
        setIsEditing(true);
        setEditableContent(markdownContent);
    };

    const handleSave = () => {
        setMarkdownContent(editableContent);
        setIsEditing(false);
        setMermaidKey(prev => prev + 1);
        if (onUpdate) onUpdate(editableContent);
    };

    const handleCancel = () => {
        setEditableContent(markdownContent);
        setIsEditing(false);
    };

    // Component rendering logic (existing components code)
    const components = { /* existing components code */ };

    // Edit mode UI
    if (isEditing) {
        return (
            <div className="flex flex-col h-full">
                <div className="flex justify-between items-center mb-4">
                    <div className="text-sm text-gray-400">Editing Markdown</div>
                    <div className="space-x-2">
                        <button
                            onClick={handleCancel}
                            className="px-3 py-1 text-sm bg-gray-700 hover:bg-gray-600 rounded-md transition-colors"
                        >
                            Cancel
                        </button>
                        <button
                            onClick={handleSave}
                            className="px-3 py-1 text-sm bg-blue-600 hover:bg-blue-500 rounded-md transition-colors"
                        >
                            Save
                        </button>
                    </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 h-full">
                    <div className="h-full">
                        <textarea
                            ref={textareaRef}
                            value={editableContent}
                            onChange={(e) => setEditableContent(e.target.value)}
                            className="w-full h-full min-h-[300px] p-4 bg-gray-800/40 rounded-xl backdrop-blur-sm 
                                       shadow-lg border border-gray-700/30 focus:border-blue-500 focus:ring-1 
                                       focus:ring-blue-500 focus:outline-none resize-none font-mono text-sm"
                        />
                    </div>
                    <div className="h-full overflow-auto p-4 bg-gray-800/20 rounded-xl backdrop-blur-sm shadow-lg border border-gray-700/30">
                        <ReactMarkdown components={components}>
                            {editableContent}
                        </ReactMarkdown>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="relative">
            {editable && (
                <button
                    onClick={handleEdit}
                    className="absolute right-2 top-2 text-xs text-gray-400 bg-gray-800 px-3 py-1 rounded 
                           hover:bg-gray-700 transition-colors z-10 flex items-center gap-1"
                >
                    <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24"
                        fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"
                        strokeLinejoin="round">
                        <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                        <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                    </svg>
                    Edit
                </button>
            )}
            <ReactMarkdown components={components}>
                {markdownContent}
            </ReactMarkdown>
        </div>
    );
}