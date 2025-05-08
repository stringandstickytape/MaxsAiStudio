export interface Attachment {
  id: string;               
  type: string;             
  name: string;             
  size: number;             
  content: ArrayBuffer;     
  textContent?: string;     
  previewUrl?: string;      
  thumbnailUrl?: string;    
  metadata?: {
    width?: number;         
    height?: number;        
    lastModified?: number;  
  };
}
