import React, { useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { useTipOfTheDayStore } from '@/stores/useTipOfTheDayStore';
import { useInputBarStore } from '@/stores/useInputBarStore';
import { useConvStore } from '@/stores/useConvStore';
import { ChevronLeft, ChevronRight, Lightbulb, X } from 'lucide-react';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';

export const TipOfTheDayOverlay: React.FC = () => {
  const {
    tips,
    currentTipIndex,
    showOnStartup,
    isVisible,
    isLoading,
    error,
    setShowOnStartup,
    hideTip,
    nextTip,
    previousTip,
    fetchSettings,
    saveSettings,
    getCurrentTip,
  } = useTipOfTheDayStore();
  
  const { setInputValue } = useInputBarStore();
  const { sendMessage } = useConvStore();
  
  // Fetch settings when component mounts if not already loaded
  useEffect(() => {
    if (tips.length === 0 && !isLoading) {
      fetchSettings();
    }
  }, [tips.length, isLoading, fetchSettings]);
  
  const currentTip = getCurrentTip();
  
  const handleShowMe = async () => {
    if (currentTip) {
      setInputValue(currentTip.samplePrompt);
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
  
  const handleShowOnStartupChange = (checked: boolean) => {
    setShowOnStartup(checked);
    saveSettings();
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
          border: `2px solid var(--global-primary-color)`,
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
            💡 Tip of the Day
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
              
              {currentTip.category && (
                <div 
                  className="tip-category"
                  style={{
                    fontSize: 'calc(var(--global-font-size) * 0.85)',
                    color: 'var(--global-secondary-text-color)',
                  }}
                >
                  Category: {currentTip.category}
                </div>
              )}
            </div>
          )}
          
          {!isLoading && !error && tips.length === 0 && (
            <div 
              className="text-center py-8"
              style={{ color: 'var(--global-secondary-text-color)' }}
            >
              No tips available
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
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-2">
              <Switch
                id="show-on-startup"
                checked={showOnStartup}
                onCheckedChange={handleShowOnStartupChange}
              />
              <Label 
                htmlFor="show-on-startup"
                style={{
                  fontSize: 'calc(var(--global-font-size) * 0.9)',
                  color: 'var(--global-secondary-text-color)',
                }}
              >
                Show on startup
              </Label>
            </div>
            
            <div className="flex items-center gap-2">
              {tips.length > 1 && (
                <>
                  <Button
                    onClick={previousTip}
                    variant="ghost"
                    size="sm"
                    disabled={isLoading}
                  >
                    <ChevronLeft className="w-4 h-4" />
                  </Button>
                  <span 
                    style={{ 
                      fontSize: 'calc(var(--global-font-size) * 0.85)',
                      color: 'var(--global-secondary-text-color)',
                    }}
                  >
                    {currentTipIndex + 1} / {tips.length}
                  </span>
                  <Button
                    onClick={nextTip}
                    variant="ghost"
                    size="sm"
                    disabled={isLoading}
                  >
                    <ChevronRight className="w-4 h-4" />
                  </Button>
                </>
              )}
              
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
              <Button 
                onClick={handleDismiss} 
                variant="outline"
              >
                OK
              </Button>
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