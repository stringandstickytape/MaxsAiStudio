// AiStudio4/AiStudioClient/src/types/project.ts

export interface Project {
  guid: string;
  name: string;
  path: string;
  description: string;
  createdDate: string;
  modifiedDate: string;
}

export interface ProjectFormValues {
  name: string;
  path: string;
  description: string;
}

export interface ProjectApiResponse {
  success: boolean;
  data?: Project | Project[];
  error?: string;
}