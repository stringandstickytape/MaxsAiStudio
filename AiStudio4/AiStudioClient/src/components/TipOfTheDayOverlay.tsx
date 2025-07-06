import React, { useEffect } from 'react';
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
  
  // Fetch initial tip when component mounts
  useEffect(() => {
    if (!currentTip && !isLoading && isVisible) {
      fetchNextTip();
    }
  }, [currentTip, isLoading, isVisible, fetchNextTip]);
  
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
  
  if (!isVisible) return null;
  
  return (
    <div 
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
        {/* Close button */}
        <Button
          onClick={handleDismiss}
          variant="ghost"
          size="sm"
          style={{
            position: 'absolute',
            top: '12px',
            right: '12px',
            padding: '4px',
            minWidth: 'auto',
            height: 'auto',
          }}
        >
          <X className="w-4 h-4" style={{ color: 'var(--global-secondary-text-color)' }} />
        </Button>
        
        {/* Header */}
        <div 
          className="tip-header"
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            marginBottom: '16px',
            paddingBottom: '12px',
            borderBottom: `1px solid var(--global-border-color)`,
          }}
        >
          <Lightbulb className="w-5 h-5" style={{ color: 'var(--global-primary-color)' }} />
          <span 
            style={{
              fontSize: 'calc(var(--global-font-size) * 1.2)',
              fontWeight: '600',
              color: 'var(--global-text-color)',
            }}
          >
            Tip of the Day
          </span>
        </div>
        
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
            marginTop: '24px',
            paddingTop: '16px',
            borderTop: `1px solid var(--global-border-color)`,
          }}
        >
          <div className="flex items-center justify-end gap-2">
            <Button 
              onClick={handleNextTip} 
              variant="outline"
              disabled={isLoading}
            >
              Next Tip
            </Button>
            <Button 
              onClick={handleDismiss} 
              variant="outline"
            >
              Clear
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
          animation: fadeIn 0.3s ease-out 0.1s both;
        }
        
        .sample-prompt-container {
          animation: slideUp 0.3s ease-out 0.2s both;
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