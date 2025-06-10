// AiStudio4/AiStudioClient/src/stores/useProjectStore.ts

import { create } from 'zustand';
import { Project, ProjectApiResponse } from '../types/project';

interface ProjectStore {
  projects: Project[];
  activeProject: Project | null;
  isLoading: boolean;
  error: string | null;
  
  // Actions
  fetchProjects: () => Promise<void>;
  setProjects: (projects: Project[]) => void;
  setActiveProject: (projectId: string) => Promise<boolean>;
  getActiveProject: () => Promise<void>;
  createProject: (project: Omit<Project, 'guid' | 'createdDate' | 'modifiedDate'>) => Promise<boolean>;
  updateProject: (project: Project) => Promise<boolean>;
  deleteProject: (projectId: string) => Promise<boolean>;
  clearError: () => void;
}

const useProjectStore = create<ProjectStore>((set, get) => ({
  projects: [],
  activeProject: null,
  isLoading: false,
  error: null,

  setProjects: (projects) => {
    set({ projects });
  },

  fetchProjects: async () => {
    set({ isLoading: true, error: null });
    try {
      console.log('🔍 [ProjectStore] Starting fetchProjects...');
      
      const response = await fetch('/api/getProjects', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({}),
      });

      console.log('📥 [ProjectStore] Response status:', response.status);

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result: ProjectApiResponse = await response.json();
      console.log('📥 [ProjectStore] Parsed response:', result);
      
      if (result.success && Array.isArray(result.data)) {
        console.log('✅ [ProjectStore] Successfully fetched projects:', result.data.length, 'projects');
        set({ projects: result.data, isLoading: false });
      } else {
        console.error('❌ [ProjectStore] API returned error or invalid data:', result);
        throw new Error(result.error || 'Failed to fetch projects');
      }
    } catch (error) {
      console.error('💥 [ProjectStore] Error fetching projects:', error);
      set({ 
        error: error instanceof Error ? error.message : 'Unknown error occurred',
        isLoading: false 
      });
    }
  },

  setActiveProject: async (projectId: string) => {
    console.log('🎯 [ProjectStore] setActiveProject called with projectId:', projectId);
    set({ isLoading: true, error: null });
    try {
      console.log('📤 [ProjectStore] Making API call to /api/setActiveProject...');
      
      const response = await fetch('/api/setActiveProject', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ projectId }),
      });

      console.log('📥 [ProjectStore] setActiveProject response status:', response.status);

      if (!response.ok) {
        const errorText = await response.text();
        console.error('❌ [ProjectStore] HTTP error response:', errorText);
        throw new Error(`HTTP error! status: ${response.status}, body: ${errorText}`);
      }

      const result: ProjectApiResponse = await response.json();
      console.log('📥 [ProjectStore] setActiveProject parsed response:', result);
      
      if (result.success) {
        // Update active project in store
        const { projects } = get();
        const activeProject = projects.find(p => p.guid === projectId) || null;
        console.log('✅ [ProjectStore] Successfully set active project:', activeProject?.name);
        set({ activeProject, isLoading: false });
        return true;
      } else {
        console.error('❌ [ProjectStore] API returned error:', result.error);
        throw new Error(result.error || 'Failed to set active project');
      }
    } catch (error) {
      console.error('💥 [ProjectStore] Error setting active project:', error);
      set({ 
        error: error instanceof Error ? error.message : 'Unknown error occurred',
        isLoading: false 
      });
      return false;
    }
  },

  getActiveProject: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch('/api/getActiveProject', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({}),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result: ProjectApiResponse = await response.json();
      
      if (result.success) {
        const activeProject = result.data as Project || null;
        set({ activeProject, isLoading: false });
      } else {
        throw new Error(result.error || 'Failed to get active project');
      }
    } catch (error) {
      console.error('Error getting active project:', error);
      set({ 
        error: error instanceof Error ? error.message : 'Unknown error occurred',
        isLoading: false 
      });
    }
  },

  createProject: async (projectData) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch('/api/createProject', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(projectData),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result: ProjectApiResponse = await response.json();
      
      if (result.success && result.data) {
        const newProject = result.data as Project;
        const { projects } = get();
        set({ 
          projects: [...projects, newProject],
          isLoading: false 
        });
        return true;
      } else {
        throw new Error(result.error || 'Failed to create project');
      }
    } catch (error) {
      console.error('Error creating project:', error);
      set({ 
        error: error instanceof Error ? error.message : 'Unknown error occurred',
        isLoading: false 
      });
      return false;
    }
  },

  updateProject: async (project) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch('/api/updateProject', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(project),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result: ProjectApiResponse = await response.json();
      
      if (result.success && result.data) {
        const updatedProject = result.data as Project;
        const { projects, activeProject } = get();
        const updatedProjects = projects.map(p => 
          p.guid === updatedProject.guid ? updatedProject : p
        );
        
        // Update active project if it was the one being updated
        const newActiveProject = activeProject?.guid === updatedProject.guid 
          ? updatedProject 
          : activeProject;
        
        set({ 
          projects: updatedProjects,
          activeProject: newActiveProject,
          isLoading: false 
        });
        return true;
      } else {
        throw new Error(result.error || 'Failed to update project');
      }
    } catch (error) {
      console.error('Error updating project:', error);
      set({ 
        error: error instanceof Error ? error.message : 'Unknown error occurred',
        isLoading: false 
      });
      return false;
    }
  },

  deleteProject: async (projectId) => {
    set({ isLoading: true, error: null });
    try {
      const response = await fetch('/api/deleteProject', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ projectId }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result: ProjectApiResponse = await response.json();
      
      if (result.success) {
        const { projects, activeProject } = get();
        const updatedProjects = projects.filter(p => p.guid !== projectId);
        
        // Clear active project if it was the one being deleted
        const newActiveProject = activeProject?.guid === projectId ? null : activeProject;
        
        set({ 
          projects: updatedProjects,
          activeProject: newActiveProject,
          isLoading: false 
        });
        return true;
      } else {
        throw new Error(result.error || 'Failed to delete project');
      }
    } catch (error) {
      console.error('Error deleting project:', error);
      set({ 
        error: error instanceof Error ? error.message : 'Unknown error occurred',
        isLoading: false 
      });
      return false;
    }
  },

  clearError: () => set({ error: null }),
}));

export default useProjectStore;