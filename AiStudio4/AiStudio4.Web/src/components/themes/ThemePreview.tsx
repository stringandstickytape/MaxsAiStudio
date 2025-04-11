// AiStudio4.Web/src/components/themes/ThemePreview.tsx

import { Theme } from '@/types/theme';
import { Card, CardContent } from '@/components/ui/card';

interface ThemePreviewProps {
  theme: Theme;
}

export function ThemePreview({ theme }: ThemePreviewProps) {
  // This component shows a simplified preview of UI elements with the theme applied
  // For now, we'll just show color swatches and some basic information
  
  return (
    <Card className="card-base">
      <CardContent className="p-4">
        <h3 className="text-lg font-medium mb-2">Theme Preview: {theme.name}</h3>
        
        <div className="grid grid-cols-2 gap-4">
          {/* Color swatches */}
          <div>
            <h4 className="text-sm font-medium mb-2">Color Palette</h4>
            <div className="flex flex-wrap gap-2">
              {theme.previewColors.map((color, index) => (
                <div key={index} className="flex flex-col items-center">
                  <div 
                    className="w-10 h-10 rounded-md border border-gray-600"
                    style={{ backgroundColor: color }}
                  />
                  <span className="text-xs mt-1">{color}</span>
                </div>
              ))}
            </div>
          </div>
          
          {/* Theme properties */}
          <div>
            <h4 className="text-sm font-medium mb-2">Components</h4>
            <div className="space-y-2">
              {Object.entries(theme.themeJson).map(([component, props]) => (
                <div key={component} className="text-xs">
                  <span className="font-medium">{component}:</span>
                  <ul className="ml-2">
                    {Object.entries(props).slice(0, 3).map(([prop, value]) => (
                      <li key={prop}>
                        {prop}: <span className="text-gray-400">{value}</span>
                      </li>
                    ))}
                    {Object.keys(props).length > 3 && (
                      <li>+ {Object.keys(props).length - 3} more...</li>
                    )}
                  </ul>
                </div>
              ))}
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}