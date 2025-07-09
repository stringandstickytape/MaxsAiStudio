import React, { useEffect, useRef } from 'react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { useTipOfTheDayStore } from '@/stores/useTipOfTheDayStore';
import { useInputBarStore } from '@/stores/useInputBarStore';
import { useConvStore } from '@/stores/useConvStore';
import { Lightbulb, X } from 'lucide-react';

export const TipOfTheDayOverlay: React.FC = () => {
  const {
    currentTip,
    isVisible,
    isLoading,
    error,
    hideTip,
    fetchNextTip,
    getCurrentTip,
  } = useTipOfTheDayStore();
  
  const { setInputText } = useInputBarStore();
  const { sendMessage } = useConvStore();
  const overlayRef = useRef<HTMLDivElement>(null);
  
  // Fetch initial tip when component mounts
  useEffect(() => {
    if (!currentTip && !isLoading && isVisible) {
      fetchNextTip();
    }
  }, [currentTip, isLoading, isVisible, fetchNextTip]);

  // Handle click outside to close overlay
  useEffect(() => {
    if (!isVisible) return;

    const handleClickOutside = (event: MouseEvent) => {
      if (overlayRef.current && !overlayRef.current.contains(event.target as Node)) {
        hideTip();
      }
    };

    // Add event listener with a small delay to prevent immediate closure
    const timeoutId = setTimeout(() => {
      document.addEventListener('mousedown', handleClickOutside);
    }, 100);

    return () => {
      clearTimeout(timeoutId);
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isVisible, hideTip]);
  
  const handleShowMe = async () => {
    if (currentTip) {
      setInputText(currentTip.samplePrompt);
      hideTip();
      // Small delay to ensure overlay hides before sending
      setTimeout(() => {
        sendMessage();
      }, 100);
    }
  };
  
  const handleDismiss = () => {
    hideTip();
  };
  
  const handleNextTip = () => {
    fetchNextTip();
  };

  const handleManualReferenceClick = () => {
    if (currentTip?.manualReference) {
      const githubUrl = `https://github.com/stringandstickytape/MaxsAiStudio/blob/main/${currentTip.manualReference}`;
      window.open(githubUrl, '_blank');
    }
  };
  
  if (!isVisible) return null;
  
  return (
    <div 
      ref={overlayRef}
      className="tip-overlay-container"
      style={{
        position: 'absolute',
        top: '50%',
        left: '50%',
        transform: 'translate(-50%, -50%)',
        zIndex: 50,
        width: '90%',
        maxWidth: '500px',
        animation: 'tipFadeIn 0.3s ease-out',
      }}
    >
      <Card 
        className="tip-card"
        style={{
          backgroundColor: 'var(--global-background-color)',
          border: 'none',
          borderRadius: 'var(--global-border-radius)',
          boxShadow: '0 10px 30px rgba(0, 0, 0, 0.3)',
          padding: '24px',
          position: 'relative',
        }}
      >
        
        {/* Content */}
        <div className="tip-content">
          {isLoading && (
            <div 
              className="flex items-center justify-center py-8"
              style={{ color: 'var(--global-secondary-text-color)' }}
            >
              Loading tips...
            </div>
          )}
          
          {error && (
            <div 
              className="text-center py-8"
              style={{ color: 'var(--destructive)' }}
            >
              {error}
            </div>
          )}
          
          {!isLoading && !error && currentTip && (
            <div className="space-y-4">
              <div 
                className="tip-text"
                style={{
                  fontSize: 'var(--global-font-size)',
                  lineHeight: '1.6',
                  color: 'var(--global-text-color)',
                  marginBottom: '16px',
                }}
              >
                {currentTip.tip}
              </div>
              
              {currentTip.samplePrompt && currentTip.samplePrompt.trim() && (
                <div 
                  className="sample-prompt-container"
                  style={{
                    backgroundColor: 'var(--global-ai-message-background, var(--muted))',
                    padding: '12px',
                    borderRadius: 'var(--global-border-radius)',
                    border: `1px solid var(--global-border-color)`,
                  }}
                >
                  <span 
                    className="prompt-label"
                    style={{
                      display: 'block',
                      fontSize: 'calc(var(--global-font-size) * 0.85)',
                      color: 'var(--global-secondary-text-color)',
                      marginBottom: '4px',
                    }}
                  >
                    Try this prompt:
                  </span>
                  <code 
                    className="prompt-text"
                    style={{
                      fontFamily: 'Monaco, Consolas, monospace',
                      fontSize: 'var(--global-font-size)',
                      display: 'block',
                      whiteSpace: 'pre-wrap',
                      color: 'var(--global-text-color)',
                    }}
                  >
                    {currentTip.samplePrompt}
                  </code>
                </div>
              )}
              
              {currentTip.manualReference && (
                <div 
                  className="manual-reference-container"
                  style={{
                    marginTop: '12px',
                    padding: '8px 12px',
                    backgroundColor: 'var(--global-user-message-background, var(--muted))',
                    borderRadius: 'var(--global-border-radius)',
                    border: `1px solid var(--global-border-color)`,
                  }}
                >
                  <span 
                    className="manual-reference-label"
                    style={{
                      fontSize: 'calc(var(--global-font-size) * 0.85)',
                      color: 'var(--global-secondary-text-color)',
                      marginRight: '8px',
                    }}
                  >
                    📖 Manual:
                  </span>
                  <button
                    onClick={handleManualReferenceClick}
                    className="manual-reference-link"
                    style={{
                      background: 'none',
                      border: 'none',
                      color: 'var(--global-primary-color)',
                      cursor: 'pointer',
                      fontSize: 'var(--global-font-size)',
                      textDecoration: 'underline',
                      padding: '0',
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.opacity = '0.8';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.opacity = '1';
                    }}
                  >
                    {currentTip.manualReference}
                  </button>
                </div>
              )}
              
            </div>
          )}
          
          {!isLoading && !error && !currentTip && (
            <div 
              className="text-center py-8"
              style={{ color: 'var(--global-secondary-text-color)' }}
            >
              No tip available
            </div>
          )}
        </div>
        
        {/* Footer */}
        <div 
          className="tip-footer"
          style={{
            marginTop: '4px',
            paddingTop: '4px',
            borderTop: `1px solid var(--global-border-color)`,
          }}
              >
          <div className="flex items-center justify-between gap-2">
            <span 
              style={{
                fontSize: 'calc(var(--global-font-size) * 0.8)',
                color: 'var(--global-secondary-text-color)',
                opacity: '0.7',
              }}
            >
              You can use the app right away - no need to close this tip
            </span>
            
            <div className="flex items-center gap-2">
              <Button 
                            onClick={handleNextTip}
                            variant="outline"
                            disabled={isLoading}
                            style={{
                                backgroundColor: 'var(--global-background-color)',
                                color: 'var(--global-primary-color)',
                                border: 'none',
                            } }
              >
                Next Tip
              </Button>

              {currentTip?.samplePrompt && currentTip.samplePrompt.trim() && (
                <Button 
                  onClick={handleShowMe} 
                  variant="default"
                  disabled={isLoading || !currentTip}
                  style={{
                    backgroundColor: 'var(--global-primary-color)',
                    color: 'white',
                  }}
                >
                  Show Me
                </Button>
              )}
            </div>
          </div>
        </div>
      </Card>
      
      <style jsx>{`
        @keyframes tipFadeIn {
          from { 
            opacity: 0;
            transform: translate(-50%, -50%) scale(0.95);
          }
          to { 
            opacity: 1;
            transform: translate(-50%, -50%) scale(1);
          }
        }
        
        .tip-text {
        }
        
        .sample-prompt-container {
        }
        
        .manual-reference-container {
        }
        
        @keyframes fadeIn {
          from { opacity: 0; }
          to { opacity: 1; }
        }
        
        @keyframes slideUp {
          from { 
            transform: translateY(10px);
            opacity: 0;
          }
          to { 
            transform: translateY(0);
            opacity: 1;
          }
        }
      `}</style>
    </div>
  );
};