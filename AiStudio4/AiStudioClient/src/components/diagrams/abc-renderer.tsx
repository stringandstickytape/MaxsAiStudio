// AiStudio4/AiStudioClient/src/components/diagrams/abc-renderer.tsx
import * as ABCJS from 'abcjs';
import { CodeBlockRenderer } from '@/components/diagrams/types';
import { useEffect, useRef, useState, useCallback } from 'react';

// Helper function to get theme variable from CSS custom properties
const getThemeVariable = (varName: string, fallback: string): string => {
    if (typeof window === 'undefined') return fallback;
    const value = getComputedStyle(document.documentElement).getPropertyValue(varName.trim()).trim();
    return value || fallback;
};

// Function to configure ABCJS rendering options based on current global CSS variables
const getAbcjsRenderingOptions = () => {
    return {
        add_classes: true,
        responsive: 'resize',
        staffwidth: 600,
        scale: 1.0,
        clickListener: (abcElem: any, tuneNumber: number, classes: string, analysis: any, drag: any) => {
            // Handle clicks on notation elements for seeking during playback
            if (analysis && analysis.startTime !== undefined) {
                // This will be used for seeking functionality
                console.log('Clicked at time:', analysis.startTime);
            }
        },
        format: {
            titlefont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 16`,
            subtitlefont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 14`,
            composerfont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 12`,
            partsfont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 12`,
            tempofont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 12`,
            gchordfont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 12`,
            annotationfont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 12`,
            vocalfont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 12`,
            wordsfont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 12`,
        },
        foregroundColor: getThemeVariable('--global-text-color', '#e0e0e0'),
        backgroundColor: 'transparent', // Let the container handle background
    };
};

// Helper function to create wrapper for SVG with zoom/pan and audio controls
const wrapSvgWithControls = (container: HTMLElement): void => {
    const existingContent = container.innerHTML;
    container.innerHTML = `
    <div class="abc-control-container">
      <div class="abc-audio-controls">
        <button class="abc-play-btn" title="Play">▶️</button>
        <button class="abc-pause-btn" title="Pause" style="display: none;">⏸️</button>
        <button class="abc-stop-btn" title="Stop">⏹️</button>
        <div class="abc-progress-container">
          <div class="abc-progress-bar">
            <div class="abc-progress-fill"></div>
          </div>
          <span class="abc-time-display">0:00 / 0:00</span>
        </div>
        <div class="abc-tempo-control">
          <label>Tempo: <span class="abc-tempo-value">120</span> BPM</label>
          <input type="range" class="abc-tempo-slider" min="60" max="200" value="120" step="5">
        </div>
        <div class="abc-volume-control">
          <label>Volume: <span class="abc-volume-value">70</span>%</label>
          <input type="range" class="abc-volume-slider" min="0" max="100" value="70" step="5">
        </div>
      </div>
      <div class="abc-zoom-controls">
        <button class="abc-zoom-in" title="Zoom In">+</button>
        <button class="abc-zoom-out" title="Zoom Out">-</button>
        <button class="abc-zoom-reset" title="Reset View">Reset</button>
      </div>
      <div class="abc-diagram-container">
        ${existingContent}
      </div>
      <div class="abc-hint">Use audio controls to play music, zoom controls for notation view, drag to pan</div>
    </div>
  `;
};

// Function to initialize zoom/pan functionality on an element
const initializeZoomPan = (container: HTMLElement) => {
    const diagramContainer = container.querySelector('.abc-diagram-container');
    const svg = diagramContainer?.querySelector('svg');

    if (!diagramContainer || !svg) return;

    svg.style.transformOrigin = '0 0';
    svg.style.transition = 'transform 0.1s';

    let scale = 1;
    let translateX = 0;
    let translateY = 0;
    let dragging = false;
    let lastX = 0;
    let lastY = 0;

    const applyTransform = () => {
        svg.style.transform = `translate(${translateX}px, ${translateY}px) scale(${scale})`;
    };

    const zoomInBtn = container.querySelector('.abc-zoom-in');
    if (zoomInBtn) {
        zoomInBtn.addEventListener('click', () => {
            scale = Math.min(scale + 0.1, 3);
            applyTransform();
        });
    }

    const zoomOutBtn = container.querySelector('.abc-zoom-out');
    if (zoomOutBtn) {
        zoomOutBtn.addEventListener('click', () => {
            scale = Math.max(scale - 0.1, 0.5);
            applyTransform();
        });
    }

    const resetBtn = container.querySelector('.abc-zoom-reset');
    if (resetBtn) {
        resetBtn.addEventListener('click', () => {
            scale = 1;
            translateX = 0;
            translateY = 0;
            applyTransform();
        });
    }

    diagramContainer.addEventListener('wheel', (e) => {
        e.preventDefault();
        const rect = diagramContainer.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;
        const delta = e.deltaY < 0 ? 0.1 : -0.1;
        const newScale = Math.max(0.5, Math.min(3, scale + delta));
        if (newScale !== scale) {
            const scaleRatio = newScale / scale;
            translateX -= (mouseX - translateX) * (scaleRatio - 1);
            translateY -= (mouseY - translateY) * (scaleRatio - 1);
            scale = newScale;
            applyTransform();
        }
    });

    diagramContainer.addEventListener('mousedown', (e) => {
        if (e.button === 0) {
            dragging = true;
            lastX = e.clientX;
            lastY = e.clientY;
            svg.style.transition = 'none';
        }
    });

    diagramContainer.addEventListener('mousemove', (e) => {
        if (dragging) {
            const deltaX = e.clientX - lastX;
            const deltaY = e.clientY - lastY;
            translateX += deltaX;
            translateY += deltaY;
            lastX = e.clientX;
            lastY = e.clientY;
            applyTransform();
        }
    });

    const endDrag = () => {
        if (dragging) {
            dragging = false;
            svg.style.transition = 'transform 0.1s';
        }
    };

    diagramContainer.addEventListener('mouseup', endDrag);
    diagramContainer.addEventListener('mouseleave', endDrag);
};

// Function to initialize audio playback functionality using correct ABCJS API
const initializeAudioPlayback = (container: HTMLElement, visualObj: any, content: string) => {
    let synth: any = null;
    let isPlaying = false;
    let isPaused = false;
    let currentTempo = 120;
    let currentVolume = 0.7;
    let totalDuration = 0;
    let startTime = 0;
    let pausedTime = 0;

    const playBtn = container.querySelector('.abc-play-btn') as HTMLButtonElement;
    const pauseBtn = container.querySelector('.abc-pause-btn') as HTMLButtonElement;
    const stopBtn = container.querySelector('.abc-stop-btn') as HTMLButtonElement;
    const tempoSlider = container.querySelector('.abc-tempo-slider') as HTMLInputElement;
    const tempoValue = container.querySelector('.abc-tempo-value') as HTMLSpanElement;
    const volumeSlider = container.querySelector('.abc-volume-slider') as HTMLInputElement;
    const volumeValue = container.querySelector('.abc-volume-value') as HTMLSpanElement;
    const progressFill = container.querySelector('.abc-progress-fill') as HTMLElement;
    const timeDisplay = container.querySelector('.abc-time-display') as HTMLSpanElement;

    // Calculate milliseconds per measure from tempo
    const getMillisecondsPerMeasure = (bpm: number) => {
        // Assuming 4/4 time signature for simplicity
        // 4 beats per measure, so milliseconds per measure = (60000 / bpm) * 4
        return (60000 / bpm) * 4;
    };

    // Initialize audio synthesis using correct ABCJS API
    const initSynth = async () => {
        try {
            if (ABCJS.synth.supportsAudio()) {
                synth = new ABCJS.synth.CreateSynth();
                
                // Initialize with correct options
                await synth.init({
                    visualObj: visualObj[0],
                    millisecondsPerMeasure: getMillisecondsPerMeasure(currentTempo),
                    options: {
                        soundFontUrl: "https://paulrosen.github.io/midi-js-soundfonts/FluidR3_GM/",
                        program: 0, // Piano
                    }
                });

                // Prime the synthesizer
                const primeResult = await synth.prime();
                totalDuration = primeResult.duration || 30; // fallback to 30 seconds

                updateTimeDisplay(0);
                
                return true;
            } else {
                console.warn('Audio synthesis not supported');
                return false;
            }
        } catch (error) {
            console.error('Failed to initialize audio synthesis:', error);
            return false;
        }
    };

    const updateTimeDisplay = (currentTime: number) => {
        const formatTime = (seconds: number) => {
            const mins = Math.floor(seconds / 60);
            const secs = Math.floor(seconds % 60);
            return `${mins}:${secs.toString().padStart(2, '0')}`;
        };

        if (timeDisplay) {
            timeDisplay.textContent = `${formatTime(currentTime)} / ${formatTime(totalDuration)}`;
        }

        if (progressFill && totalDuration > 0) {
            const progress = (currentTime / totalDuration) * 100;
            progressFill.style.width = `${Math.min(progress, 100)}%`;
        }
    };

    const startPlayback = async () => {
        if (!synth) {
            const initialized = await initSynth();
            if (!initialized) return;
        }

        try {
            isPlaying = true;
            isPaused = false;
            startTime = Date.now();
            
            if (playBtn) playBtn.style.display = 'none';
            if (pauseBtn) pauseBtn.style.display = 'inline-block';

            // Start playback using correct API
            synth.start();

            // Update progress during playback
            const updateProgress = () => {
                if (isPlaying && !isPaused) {
                    const elapsed = (Date.now() - startTime + pausedTime) / 1000;
                    updateTimeDisplay(elapsed);
                    
                    if (elapsed < totalDuration) {
                        requestAnimationFrame(updateProgress);
                    } else {
                        stopPlayback();
                    }
                }
            };
            updateProgress();

        } catch (error) {
            console.error('Playback failed:', error);
            stopPlayback();
        }
    };

    const pausePlayback = () => {
        if (synth && isPlaying && !isPaused) {
            synth.pause();
            isPaused = true;
            pausedTime += Date.now() - startTime;
            
            if (playBtn) playBtn.style.display = 'inline-block';
            if (pauseBtn) pauseBtn.style.display = 'none';
        }
    };

    const resumePlayback = () => {
        if (synth && isPlaying && isPaused) {
            synth.resume();
            isPaused = false;
            startTime = Date.now();
            
            if (playBtn) playBtn.style.display = 'none';
            if (pauseBtn) pauseBtn.style.display = 'inline-block';
        }
    };

    const stopPlayback = () => {
        if (synth) {
            synth.stop();
        }
        isPlaying = false;
        isPaused = false;
        pausedTime = 0;
        
        if (playBtn) playBtn.style.display = 'inline-block';
        if (pauseBtn) pauseBtn.style.display = 'none';
        
        updateTimeDisplay(0);
    };

    const updateTempo = async (newTempo: number) => {
        currentTempo = newTempo;
        
        // Need to reinitialize synth with new tempo
        if (synth) {
            const wasPlaying = isPlaying;
            if (wasPlaying) {
                stopPlayback();
            }
            
            // Reinitialize with new tempo
            await synth.init({
                visualObj: visualObj[0],
                millisecondsPerMeasure: getMillisecondsPerMeasure(currentTempo),
                options: {
                    soundFontUrl: "https://paulrosen.github.io/midi-js-soundfonts/FluidR3_GM/",
                    program: 0,
                }
            });
            
            await synth.prime();
            
            if (wasPlaying) {
                startPlayback();
            }
        }
    };

    // Event listeners
    if (playBtn) {
        playBtn.addEventListener('click', () => {
            if (isPaused) {
                resumePlayback();
            } else {
                startPlayback();
            }
        });
    }

    if (pauseBtn) {
        pauseBtn.addEventListener('click', pausePlayback);
    }

    if (stopBtn) {
        stopBtn.addEventListener('click', stopPlayback);
    }

    if (tempoSlider && tempoValue) {
        tempoSlider.addEventListener('input', async (e) => {
            const newTempo = parseInt((e.target as HTMLInputElement).value);
            tempoValue.textContent = newTempo.toString();
            await updateTempo(newTempo);
        });
    }

    if (volumeSlider && volumeValue) {
        volumeSlider.addEventListener('input', (e) => {
            currentVolume = parseInt((e.target as HTMLInputElement).value) / 100;
            volumeValue.textContent = Math.round(currentVolume * 100).toString();
            
            // Volume changes would require reinitializing the synth
            // For now, this will only affect new playback sessions
        });
    }

    // Initialize synthesis on first load
    initSynth();
};

// Function to inject ABC-specific styles
const injectAbcStyles = () => {
    const styleId = 'abc-enhanced-styles';
    if (!document.getElementById(styleId)) {
        const style = document.createElement('style');
        style.id = styleId;
        style.textContent = `
        .abc-control-container {
          display: flex;
          flex-direction: column;
          width: 100%;
          gap: 8px;
        }
        .abc-audio-controls {
          display: flex;
          align-items: center;
          gap: 12px;
          padding: 8px;
          background-color: var(--global-secondary-color, #374151);
          border-radius: var(--global-border-radius, 4px);
          flex-wrap: wrap;
        }
        .abc-audio-controls button {
          padding: 6px 12px;
          background-color: var(--global-primary-color, #3b82f6);
          border: none;
          border-radius: var(--global-border-radius, 4px);
          color: white;
          cursor: pointer;
          font-size: 14px;
          min-width: 40px;
        }
        .abc-audio-controls button:hover {
          background-color: var(--global-accent-color, #2563eb);
        }
        .abc-audio-controls button:disabled {
          background-color: var(--global-border-color, #6b7280);
          cursor: not-allowed;
        }
        .abc-progress-container {
          display: flex;
          align-items: center;
          gap: 8px;
          flex: 1;
          min-width: 200px;
        }
        .abc-progress-bar {
          flex: 1;
          height: 6px;
          background-color: var(--global-border-color, #374151);
          border-radius: 3px;
          overflow: hidden;
        }
        .abc-progress-fill {
          height: 100%;
          background-color: var(--global-accent-color, #3b82f6);
          width: 0%;
          transition: width 0.1s ease;
        }
        .abc-time-display {
          font-size: 12px;
          color: var(--global-text-color, #e0e0e0);
          min-width: 80px;
        }
        .abc-tempo-control, .abc-volume-control {
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 4px;
        }
        .abc-tempo-control label, .abc-volume-control label {
          font-size: 12px;
          color: var(--global-text-color, #e0e0e0);
        }
        .abc-tempo-slider, .abc-volume-slider {
          width: 80px;
          height: 4px;
          background-color: var(--global-border-color, #374151);
          border-radius: 2px;
          outline: none;
        }
        .abc-tempo-slider::-webkit-slider-thumb, .abc-volume-slider::-webkit-slider-thumb {
          appearance: none;
          width: 16px;
          height: 16px;
          background-color: var(--global-accent-color, #3b82f6);
          border-radius: 50%;
          cursor: pointer;
        }
        .abc-zoom-controls {
          display: flex;
          justify-content: center;
          gap: 8px;
        }
        .abc-zoom-controls button {
          padding: 4px 8px;
          background-color: var(--global-secondary-color, #374151);
          border: none;
          border-radius: var(--global-border-radius, 4px);
          color: var(--global-text-color, white);
          cursor: pointer;
        }
        .abc-zoom-controls button:hover {
          background-color: var(--global-primary-color, #4b5563);
        }
        .abc-diagram-container {
          overflow: hidden;
          border: 1px solid var(--global-border-color, #374151);
          border-radius: var(--global-border-radius, 4px);
          padding: 16px;
          min-height: 200px;
          position: relative;
          cursor: move;
          background-color: var(--global-background-color, #111827);
        }
        .abc-hint {
          font-size: 0.75rem;
          color: var(--global-secondary-color, #6b7280);
          text-align: center;
        }
        /* Style the ABC notation elements with theme colors */
        .abc-diagram-container svg {
          background-color: transparent;
        }
        .abc-diagram-container .abcjs-note {
          fill: var(--global-text-color, #e0e0e0);
        }
        .abc-diagram-container .abcjs-staff {
          stroke: var(--global-text-color, #e0e0e0);
        }
        .abc-diagram-container .abcjs-bar {
          stroke: var(--global-text-color, #e0e0e0);
        }
        .abc-diagram-container .abcjs-text {
          fill: var(--global-text-color, #e0e0e0);
        }
        .abc-diagram-container .abcjs-title {
          fill: var(--global-text-color, #e0e0e0);
        }
        .abc-diagram-container .abcjs-highlight {
          fill: var(--global-accent-color, #3b82f6) !important;
          stroke: var(--global-accent-color, #3b82f6) !important;
        }
        @media (max-width: 768px) {
          .abc-audio-controls {
            flex-direction: column;
            align-items: stretch;
          }
          .abc-progress-container {
            min-width: unset;
          }
          .abc-tempo-control, .abc-volume-control {
            flex-direction: row;
            justify-content: space-between;
          }
        }
      `;
        document.head.appendChild(style);
    }
};

export const AbcRenderer: CodeBlockRenderer = {
    type: ['abc'],
    initialize: () => {
        injectAbcStyles(); // Initial style injection
    },
    render: async () => {
        // No async rendering needed - handled by Component
    },
    Component: ({ content, className }) => {
        const containerRef = useRef<HTMLDivElement>(null);
        const [error, setError] = useState<string | null>(null);
        const [notationId] = useState('abc-notation-' + Math.random().toString(36).substring(2, 9));
        const [audioSupported, setAudioSupported] = useState(false);

        const renderNotation = useCallback(async () => {
            if (!containerRef.current) return;
            
            // Clear previous content and errors
            containerRef.current.innerHTML = '';
            setError(null);

            try {
                // Validate ABC content - basic check for ABC notation structure
                if (!content.trim()) {
                    setError('Empty ABC notation content');
                    return;
                }

                // Basic ABC validation - should contain at least one note or header
                const hasHeader = /^[A-Z]:\s*/.test(content);
                const hasNotes = /[A-Ga-g]/.test(content);
                
                if (!hasHeader && !hasNotes) {
                    setError('Invalid ABC notation: content should contain ABC headers (X:, T:, etc.) or musical notes');
                    return;
                }

                // Create a temporary container for rendering
                const tempContainer = document.createElement('div');
                tempContainer.id = notationId;
                containerRef.current.appendChild(tempContainer);

                // Render the ABC notation
                const renderingOptions = getAbcjsRenderingOptions();
                
                try {
                    const visualObj = ABCJS.renderAbc(tempContainer, content, renderingOptions);
                    
                    // Check if rendering was successful by looking for SVG content
                    const svg = tempContainer.querySelector('svg');
                    if (!svg || !visualObj || visualObj.length === 0) {
                        setError('ABC rendering failed: No musical notation was generated');
                        return;
                    }

                    // Check audio support
                    const audioSupport = ABCJS.synth.supportsAudio();
                    setAudioSupported(audioSupport);

                    // Wrap with controls and initialize functionality
                    wrapSvgWithControls(containerRef.current);
                    initializeZoomPan(containerRef.current);
                    
                    if (audioSupport) {
                        initializeAudioPlayback(containerRef.current, visualObj, content);
                    } else {
                        // Hide audio controls if not supported
                        const audioControls = containerRef.current.querySelector('.abc-audio-controls');
                        if (audioControls) {
                            (audioControls as HTMLElement).style.display = 'none';
                        }
                    }
                    
                } catch (renderError) {
                    const errorMessage = renderError instanceof Error ? renderError.message : String(renderError);
                    setError(`ABC rendering error: ${errorMessage}`);
                }

            } catch (err) {
                const errorMessage = err instanceof Error ? err.message : String(err);
                console.error('ABC notation rendering failed:', errorMessage);
                setError(errorMessage);
            }
        }, [content, notationId]);

        useEffect(() => {
            renderNotation(); // Initial render
            
            const handleThemeChange = () => {
                renderNotation(); // Re-render on theme change
            };

            window.addEventListener('themechange', handleThemeChange as EventListener);
            return () => {
                window.removeEventListener('themechange', handleThemeChange as EventListener);
            };
        }, [renderNotation]); // renderNotation dependency includes content and notationId

        // Enhanced JSX for displaying the error or the notation
        return (
            <div className={`${className || ''}`}>
                {error ? (
                    <div className="p-3 border border-red-500/50 rounded-md bg-red-900/20 text-red-300 text-xs">
                        <p className="font-bold mb-2">ABC Notation Error</p>
                        <pre className="whitespace-pre-wrap font-mono text-sm">
                            {error}
                        </pre>
                    </div>
                ) : (
                    <div>
                        <div ref={containerRef} className="abc-container w-full" />
                        {!audioSupported && (
                            <div className="mt-2 p-2 bg-yellow-900/20 border border-yellow-500/50 rounded text-yellow-300 text-xs">
                                Audio playback not supported in this browser. Visual notation only.
                            </div>
                        )}
                    </div>
                )}
            </div>
        );
    },
};