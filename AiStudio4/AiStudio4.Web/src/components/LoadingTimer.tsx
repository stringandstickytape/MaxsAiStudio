// AiStudio4.Web\src\components\LoadingTimer.tsx
import React, { useState, useEffect, useRef, useCallback } from 'react';
import styled, { keyframes } from 'styled-components';

const PacManSize = 20;
const GhostSize = 20;
const DotSize = 5;
const PowerPillSize = 10;
const GridSize = 20;
const GridRows = 15;
const GridCols = 20;
const GameSpeed = 200; // ms between updates
const PowerUpDuration = 5000; // 5 seconds

const PacManAnimation = keyframes`
  0% { clip-path: polygon(0% 0%, 100% 0%, 100% 100%, 0% 100%, 0% 60%, 50% 50%, 0% 40%); }
  50% { clip-path: polygon(0% 0%, 100% 0%, 100% 100%, 0% 100%, 0% 50%, 50% 50%, 0% 50%); }
  100% { clip-path: polygon(0% 0%, 100% 0%, 100% 100%, 0% 100%, 0% 60%, 50% 50%, 0% 40%); }
`;

const PacMan = styled.div`
  width: ${PacManSize}px;
  height: ${PacManSize}px;
  background-color: yellow;
  border-radius: 50%;
  position: absolute;
  // animation: ${PacManAnimation} 0.5s infinite linear;
  /* Using clip-path for better animation shape */
  clip-path: polygon(0% 0%, 100% 0%, 100% 100%, 0% 100%, 0% 60%, 50% 50%, 0% 40%);
`;

const Ghost = styled.div<{ color: string; vulnerable: boolean }>`
  width: ${GhostSize}px;
  height: ${GhostSize}px;
  background-color: ${props => props.vulnerable ? '#add8e6' : props.color}; // Light blue when vulnerable
  position: absolute;
  border-top-left-radius: 50%;
  border-top-right-radius: 50%;
  /* Basic ghost shape */
  &:after {\
    content: '';
    position: absolute;
    bottom: 0;
    left: 0;
    width: 100%;
    height: 30%;
    background: linear-gradient(45deg, transparent 25%, ${props => props.vulnerable ? '#add8e6' : props.color} 25%, ${props => props.vulnerable ? '#add8e6' : props.color} 75%, transparent 75%),
                linear-gradient(-45deg, transparent 25%, ${props => props.vulnerable ? '#add8e6' : props.color} 25%, ${props => props.vulnerable ? '#add8e6' : props.color} 75%, transparent 75%);
    background-size: ${GhostSize / 3}px ${GhostSize * 0.3}px;
  }
`;

const PowerPill = styled.div`
  width: ${PowerPillSize}px;
  height: ${PowerPillSize}px;
  background-color: orange;
  border-radius: 50%;
  position: absolute;
  animation: ${keyframes`
    0% { transform: scale(1); }
    50% { transform: scale(1.2); }
    100% { transform: scale(1); }
  `} 1s infinite;
`;

const Dot = styled.div`
  width: ${DotSize}px;
  height: ${DotSize}px;
  background-color: #555;
  border-radius: 50%;
  position: absolute;
`;

const GameContainer = styled.div`
  position: relative;
  width: ${GridCols * GridSize}px;
  height: ${GridRows * GridSize}px;
  background-color: #222;
  overflow: hidden;
`;

interface LoadingTimerProps {
  // No props needed for this rudimentary Pac-Man
}

interface GhostState {
    id: number;
    x: number;
    y: number;
    color: string;
    vulnerable: boolean;
    vulnerableTimer: number | null;
}

interface PowerPillState {
    x: number;
    y: number;
}

const initialGhosts: GhostState[] = [
    { id: 1, x: GridCols / 2 - 1, y: GridRows / 2, color: 'red', vulnerable: false, vulnerableTimer: null },
    { id: 2, x: GridCols / 2, y: GridRows / 2, color: 'pink', vulnerable: false, vulnerableTimer: null },
    { id: 3, x: GridCols / 2 + 1, y: GridRows / 2, color: 'cyan', vulnerable: false, vulnerableTimer: null },
    { id: 4, x: GridCols / 2, y: GridRows / 2 - 1, color: 'orange', vulnerable: false, vulnerableTimer: null },
];

const LoadingTimer: React.FC<LoadingTimerProps> = () => {
    const [pacManPosition, setPacManPosition] = useState({ x: 1, y: 1 }); // Start at a valid position
    const [direction, setDirection] = useState<'up' | 'down' | 'left' | 'right' | 'stop'>('right'); // Start moving right
    const [dots, setDots] = useState<Array<{ x: number; y: number }>>([]);
    const [powerPills, setPowerPills] = useState<PowerPillState[]>([]);
    const [ghosts, setGhosts] = useState<GhostState[]>(initialGhosts);
    const [isPowerUpActive, setIsPowerUpActive] = useState(false);
    const [score, setScore] = useState(0); // Basic score tracking
    const gameContainerRef = useRef<HTMLDivElement>(null);
    const powerUpTimeoutRef = useRef<NodeJS.Timeout | null>(null);

    // Focus the container to capture key events
    useEffect(() => {
        gameContainerRef.current?.focus();
    }, []);

    // Initialize dots and power pills
    useEffect(() => {
        const newDots: { x: number; y: number }[] = [];
        const newPowerPills: PowerPillState[] = [];
        for (let row = 0; row < GridRows; row++) {
            for (let col = 0; col < GridCols; col++) {
                // Avoid placing dots/pills on initial ghost/pacman positions
                const isInitialPacManPos = col === 1 && row === 1;
                const isInitialGhostPos = initialGhosts.some(g => g.x === col && g.y === row);

                if (!isInitialPacManPos && !isInitialGhostPos) {
                    if (Math.random() < 0.05 && (col % 5 === 0 && row % 5 === 0)) { // Less frequent power pills
                       newPowerPills.push({ x: col, y: row });
                    } else if (Math.random() < 0.3) { // Dot density
                        newDots.push({ x: col, y: row });
                    }
                }
            }
        }
        setDots(newDots);
        setPowerPills(newPowerPills);
        setGhosts(initialGhosts.map(g => ({...g}))); // Reset ghosts
        setPacManPosition({ x: 1, y: 1 }); // Reset Pac-Man
        setScore(0); // Reset score
    }, []);

    const resetGame = useCallback(() => {
         // Simple reset: Pac-Man and Ghosts to start, maybe keep score?
         setPacManPosition({ x: 1, y: 1 });
         setGhosts(initialGhosts.map(g => ({...g, vulnerable: false, vulnerableTimer: null })));
         setIsPowerUpActive(false);
         if (powerUpTimeoutRef.current) {
            clearTimeout(powerUpTimeoutRef.current);
            powerUpTimeoutRef.current = null;
         }
    }, []);

    // Keyboard Control
    useEffect(() => {
        const handleKeyDown = (event: KeyboardEvent) => {
            switch (event.key) {
                case 'ArrowUp':
                    setDirection('up');
                    break;
                case 'ArrowDown':
                    setDirection('down');
                    break;
                case 'ArrowLeft':
                    setDirection('left');
                    break;
                case 'ArrowRight':
                    setDirection('right');
                    break;
                default:
                    // Optional: Handle other keys or ignore
                    break;
            }
        };

        const currentRef = gameContainerRef.current;
        currentRef?.addEventListener('keydown', handleKeyDown);

        // Cleanup function
        return () => {
            currentRef?.removeEventListener('keydown', handleKeyDown);
        };
    }, []); // Empty dependency array ensures this runs only once

    useEffect(() => {
        const gameInterval = setInterval(() => {
            // --- Pac-Man Movement ---\
            setPacManPosition((prevPosition) => {
                let newX = prevPosition.x;
                let newY = prevPosition.y;

                switch (direction) {
                    case 'up':
                        newY = prevPosition.y > 0 ? prevPosition.y - 1 : GridRows - 1;
                        break;
                    case 'down':
                        newY = (prevPosition.y + 1) % GridRows;
                        break;
                    case 'left':
                        newX = prevPosition.x > 0 ? prevPosition.x - 1 : GridCols - 1;
                        break;
                    case 'right':
                        newX = (prevPosition.x + 1) % GridCols;
                        break;
                    case 'stop':
                    default:
                        return prevPosition;
                }
                return { x: newX, y: newY };
            });

            // --- Ghost Movement (Simple Random) ---\
            setGhosts(currentGhosts => currentGhosts.map(ghost => {
                // Simple random movement
                const moves = ['up', 'down', 'left', 'right'];
                const randomMove = moves[Math.floor(Math.random() * moves.length)];
                let newX = ghost.x;
                let newY = ghost.y;

                switch (randomMove) {
                    case 'up': newY = ghost.y > 0 ? ghost.y - 1 : GridRows - 1; break;
                    case 'down': newY = (ghost.y + 1) % GridRows; break;
                    case 'left': newX = ghost.x > 0 ? ghost.x - 1 : GridCols - 1; break;
                    case 'right': newX = (ghost.x + 1) % GridCols; break;
                }
                // Basic check to prevent ghosts overlapping badly - could be improved
                const occupied = currentGhosts.some(g => g.id !== ghost.id && g.x === newX && g.y === newY);
                if (!occupied) {
                   return { ...ghost, x: newX, y: newY };
                }
                return ghost; // Don't move if target is occupied by another ghost
            }));

            // --- Collision Detection & Game Logic ---\
            const currentPacManPos = pacManPositionRef.current; // Use ref for current pos inside interval

            // Dot Collision
            setDots(currentDots => {
                const dotEaten = currentDots.some(dot => dot.x === currentPacManPos.x && dot.y === currentPacManPos.y);
                if (dotEaten) {
                    setScore(s => s + 10);
                    return currentDots.filter(dot => dot.x !== currentPacManPos.x || dot.y !== currentPacManPos.y);
                }
                return currentDots;
            });

            // Power Pill Collision
            setPowerPills(currentPills => {
                const pillEaten = currentPills.some(pill => pill.x === currentPacManPos.x && pill.y === currentPacManPos.y);
                if (pillEaten) {
                    setScore(s => s + 50);
                    setIsPowerUpActive(true);
                    setGhosts(g => g.map(ghost => ({ ...ghost, vulnerable: true })));

                    // Clear previous timer if exists
                    if (powerUpTimeoutRef.current) clearTimeout(powerUpTimeoutRef.current);

                    powerUpTimeoutRef.current = setTimeout(() => {
                        setIsPowerUpActive(false);
                        setGhosts(g => g.map(ghost => ({ ...ghost, vulnerable: false })));
                        powerUpTimeoutRef.current = null;
                    }, PowerUpDuration);

                    return currentPills.filter(pill => pill.x !== currentPacManPos.x || pill.y !== currentPacManPos.y);
                }
                return currentPills;
            });

            // Ghost Collision
            ghosts.forEach(ghost => {
                if (ghost.x === currentPacManPos.x && ghost.y === currentPacManPos.y) {
                    if (ghost.vulnerable) {
                        // Eat ghost
                        setScore(s => s + 200);
                        // Reset eaten ghost to starting position (or a 'jail')
                        const originalGhost = initialGhosts.find(g => g.id === ghost.id);
                        if (originalGhost) {
                           setGhosts(current => current.map(g =>
                                g.id === ghost.id ? { ...originalGhost, vulnerable: false } : g
                            ));
                        }
                    } else {
                        // Pac-Man caught - reset game state
                        resetGame();
                    }
                }
            });

        }, GameSpeed); // Use GameSpeed constant

        return () => clearInterval(gameInterval);
    }, [direction, resetGame]); // Add resetGame dependency

    // Ref to track latest pacman position for interval logic
    const pacManPositionRef = useRef(pacManPosition);
    useEffect(() => {
        pacManPositionRef.current = pacManPosition;
    }, [pacManPosition]);


    return (
        <GameContainer ref={gameContainerRef} tabIndex={0}> {/* Make container focusable */}
            {/* Render Score */}
            <div style={{ position: 'absolute', top: '5px', left: '5px', color: 'white', zIndex: 10 }}>Score: {score}</div>

            {/* Render Dots */}
            {dots.map((dot, index) => (
                <Dot
                    key={`dot-${index}`}
                    style={{
                        left: dot.x * GridSize + (GridSize - DotSize) / 2,
                        top: dot.y * GridSize + (GridSize - DotSize) / 2,
                    }}
                />
            ))}

            {/* Render Power Pills */}
            {powerPills.map((pill, index) => (
                <PowerPill
                    key={`pill-${index}`}
                    style={{
                        left: pill.x * GridSize + (GridSize - PowerPillSize) / 2,
                        top: pill.y * GridSize + (GridSize - PowerPillSize) / 2,
                    }}
                />
            ))}

            {/* Render Ghosts */}
            {ghosts.map((ghost) => (
                <Ghost
                    key={`ghost-${ghost.id}`}
                    color={ghost.color}
                    vulnerable={ghost.vulnerable}
                    style={{
                        left: ghost.x * GridSize,
                        top: ghost.y * GridSize,
                        transition: 'background-color 0.3s ease', // Smooth color change when vulnerable\
                    }}
                 />
            ))}

            {/* Render Pac-Man */}
            <PacMan
                style={{
                    left: pacManPosition.x * GridSize,
                    top: pacManPosition.y * GridSize,
                    /* Rotate Pac-Man based on direction */
                    transform: direction === 'left' ? 'scaleX(-1)' :
                               direction === 'up' ? 'rotate(-90deg)' :
                               direction === 'down' ? 'rotate(90deg)' : 'none',
                }}
            />
        </GameContainer>
    );
};

export { LoadingTimer }; // Ensure named export matches import in ConvView.tsx