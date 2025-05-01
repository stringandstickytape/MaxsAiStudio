import { useEffect, useState } from 'react';

/**
 * Hook to detect if a media query matches the current viewport
 * Uses the MediaQueryList API for better performance and accuracy
 * 
 * @param query CSS media query string (e.g., '(max-width: 768px)')
 * @returns boolean indicating if the query matches
 */
export function useMediaQuery(query: string): boolean {
  
  const [matches, setMatches] = useState<boolean>(() => {
    
    if (typeof window === 'undefined') return false;
    return window.matchMedia(query).matches;
  });

  useEffect(() => {
    if (typeof window === 'undefined') return;
    
    
    const mediaQueryList = window.matchMedia(query);
    
    
    setMatches(mediaQueryList.matches);

    
    const listener = (event: MediaQueryListEvent) => {
      setMatches(event.matches);
    };

    
    if (mediaQueryList.addEventListener) {
      mediaQueryList.addEventListener('change', listener);
      return () => mediaQueryList.removeEventListener('change', listener);
    } else {
      
      mediaQueryList.addListener(listener);
      return () => mediaQueryList.removeListener(listener);
    }
  }, [query]); 

  return matches;
}
