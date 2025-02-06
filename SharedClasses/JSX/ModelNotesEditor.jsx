// ModelNotesEditor.jsx
const { useState, useEffect, useRef } = React;
const { useColorScheme } = window; // Assuming useColorScheme is globally available

const ModelNotesEditor = ({ modelGuid, initialNotes, onSave, onClose }) => {
    const { colorScheme } = useColorScheme();
    const [notes, setNotes] = useState(initialNotes || '');  // Start with initial notes, default to empty string
    const textareaRef = useRef(null);
    const modalRef = useRef(null);

    // Focus the textarea when the component mounts (and notes change)
    useEffect(() => {
        if (textareaRef.current) {
            textareaRef.current.focus();
        }
    }, [notes]);

    // Handle clicks outside the modal to close it.
    useEffect(() => {
        const handleClickOutside = (event) => {
            if (modalRef.current && !modalRef.current.contains(event.target)) {
                onClose(); // Call the onClose prop (important for state management in parent)
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [onClose]);


    const handleSave = () => {
        onSave(modelGuid, notes); // Pass both the guid and the updated notes
    };

    const handleTextareaChange = (event) => {
        setNotes(event.target.value);
    };

    const handleKeyDown = (event) => {
        if (event.key === 'Escape') {
            onClose();
        }
    }

    // Prevent rendering if modelGuid is missing (critical for data integrity)
    if (!modelGuid) {
        return null;
    }

    return (
        <div style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.5)', // Semi-transparent background
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            zIndex: 1001, // Ensure it's above other modals (if any)
        }}>
            <div ref={modalRef} style={{
                backgroundColor: colorScheme.backgroundColor,
                padding: '20px',
                borderRadius: colorScheme.borderRadius || '5px',
                width: '80%',
                maxWidth: '600px', // Larger width than PrefillModal
                boxShadow: '0 4px 8px rgba(0,0,0,0.2)', // More pronounced shadow
            }}>
                <h2 style={{ color: colorScheme.textColor, marginBottom: '10px' }}>Edit Notes for {modelGuid}</h2>
                <textarea
                    ref={textareaRef}
                    value={notes}
                    onChange={handleTextareaChange}
                    onKeyDown={handleKeyDown}
                    style={{
                        width: '100%',
                        height: '200px', // Increased height
                        marginBottom: '15px', // More spacing
                        padding: '10px',
                        backgroundColor: colorScheme.inputBackgroundColor,
                        color: colorScheme.inputTextColor,
                        border: `1px solid ${colorScheme.borderColor}`,
                        borderRadius: colorScheme.borderRadius || '3px',
                        fontFamily: colorScheme.fontFamily, // Consistent font
                        fontSize: '14px', // Slightly larger font
                        resize: 'vertical', // Allow vertical resizing
                    }}
                />
                <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
                    <button
                        onClick={onClose}
                        style={{
                            marginRight: '10px',
                            padding: '8px 16px', // Larger buttons
                            backgroundColor: colorScheme.buttonBackgroundColor,
                            color: colorScheme.buttonTextColor,
                            border: colorScheme.buttonBorder || 'none',
                            borderRadius: colorScheme.borderRadius || '3px',
                            cursor: 'pointer',
                            fontSize: '14px',
                        }}
                    >
                        Cancel
                    </button>
                    <button
                        onClick={handleSave}
                        style={{
                            padding: '8px 16px',
                            backgroundColor: colorScheme.buttonBackgroundColor,
                            color: colorScheme.buttonTextColor,
                            border: colorScheme.buttonBorder || 'none',
                            borderRadius: colorScheme.borderRadius || '3px',
                            cursor: 'pointer',
                            fontSize: '14px',
                        }}
                    >
                        Save
                    </button>
                </div>
            </div>
        </div>
    );
};

// Make it globally accessible, consistent with your other components.
window.ModelNotesEditor = ModelNotesEditor;