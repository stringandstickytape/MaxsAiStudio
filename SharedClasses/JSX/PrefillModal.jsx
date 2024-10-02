// PrefillModal.jsx
const { useState, useEffect, useRef } = React;
const { useColorScheme } = window;

const PrefillModal = ({ isOpen, onClose, onSubmit }) => {
    const { colorScheme } = useColorScheme();
    const [prefillText, setPrefillText] = useState('');
    const modalRef = useRef(null);

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (modalRef.current && !modalRef.current.contains(event.target)) {
                onClose();
            }
        };

        if (isOpen) {
            document.addEventListener('mousedown', handleClickOutside);
        }

        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [isOpen, onClose]);

    const handleSubmit = () => {
        onSubmit(prefillText);
        setPrefillText('');
    };

    if (!isOpen) return null;

    return (
        <div style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.5)',
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            zIndex: 1000,
        }}>
            <div ref={modalRef} style={{
                backgroundColor: colorScheme.backgroundColor,
                padding: '20px',
                borderRadius: '5px',
                width: '80%',
                maxWidth: '500px',
            }}>
                <h2 style={{ color: colorScheme.textColor }}>Enter Prefill Text</h2>
                <textarea
                    value={prefillText}
                    onChange={(e) => setPrefillText(e.target.value)}
                    style={{
                        width: '100%',
                        height: '150px',
                        marginBottom: '10px',
                        padding: '5px',
                        backgroundColor: colorScheme.inputBackgroundColor,
                        color: colorScheme.inputTextColor,
                        border: `1px solid ${colorScheme.borderColor}`,
                        borderRadius: '3px',
                    }}
                />
                <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
                    <button
                        onClick={onClose}
                        style={{
                            marginRight: '10px',
                            padding: '5px 10px',
                            backgroundColor: colorScheme.buttonBackgroundColor,
                            color: colorScheme.buttonTextColor,
                            border: 'none',
                            borderRadius: '3px',
                            cursor: 'pointer',
                        }}
                    >
                        Cancel
                    </button>
                    <button
                        onClick={handleSubmit}
                        style={{
                            padding: '5px 10px',
                            backgroundColor: colorScheme.buttonBackgroundColor,
                            color: colorScheme.buttonTextColor,
                            border: 'none',
                            borderRadius: '3px',
                            cursor: 'pointer',
                        }}
                    >
                        OK
                    </button>
                </div>
            </div>
        </div>
    );
};

window.PrefillModal = PrefillModal;