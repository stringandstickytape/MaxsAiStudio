// src/store/toolSlice.ts
import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { Tool, ToolCategory } from '@/types/toolTypes';
import { ToolService } from '@/services/ToolService';

interface ToolState {
  tools: Tool[];
  categories: ToolCategory[];
  activeTools: string[];
  loading: boolean;
  error: string | null;
}

const initialState: ToolState = {
  tools: [],
  categories: [],
  activeTools: [],
  loading: false,
  error: null,
};

// Async thunks
export const fetchTools = createAsyncThunk('tools/fetchTools', async () => {
  const tools = await ToolService.getTools();
  return tools;
});

export const fetchCategories = createAsyncThunk('tools/fetchCategories', async () => {
  const categories = await ToolService.getToolCategories();
  return categories;
});

export const addTool = createAsyncThunk(
  'tools/addTool',
  async (tool: Omit<Tool, 'guid'>) => {
    const newTool = await ToolService.addTool(tool);
    return newTool;
  }
);

export const updateTool = createAsyncThunk(
  'tools/updateTool',
  async (tool: Tool) => {
    const updatedTool = await ToolService.updateTool(tool);
    return updatedTool;
  }
);

export const deleteTool = createAsyncThunk(
  'tools/deleteTool',
  async (toolId: string) => {
    await ToolService.deleteTool(toolId);
    return toolId;
  }
);

export const addCategory = createAsyncThunk(
  'tools/addCategory',
  async (category: Omit<ToolCategory, 'id'>) => {
    const newCategory = await ToolService.addToolCategory(category);
    return newCategory;
  }
);

export const updateCategory = createAsyncThunk(
  'tools/updateCategory',
  async (category: ToolCategory) => {
    const updatedCategory = await ToolService.updateToolCategory(category);
    return updatedCategory;
  }
);

export const deleteCategory = createAsyncThunk(
  'tools/deleteCategory',
  async (categoryId: string) => {
    await ToolService.deleteToolCategory(categoryId);
    return categoryId;
  }
);

export const importTools = createAsyncThunk(
  'tools/importTools',
  async (json: string) => {
    const tools = await ToolService.importTools(json);
    return tools;
  }
);

const toolSlice = createSlice({
  name: 'tools',
  initialState,
  reducers: {
    setActiveTools: (state, action: PayloadAction<string[]>) => {
      state.activeTools = action.payload;
    },
    addActiveTool: (state, action: PayloadAction<string>) => {
      if (!state.activeTools.includes(action.payload)) {
        state.activeTools.push(action.payload);
      }
    },
    removeActiveTool: (state, action: PayloadAction<string>) => {
      state.activeTools = state.activeTools.filter(id => id !== action.payload);
    },
    clearActiveTools: (state) => {
      state.activeTools = [];
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch tools
      .addCase(fetchTools.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchTools.fulfilled, (state, action) => {
        state.loading = false;
        state.tools = action.payload;
      })
      .addCase(fetchTools.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to fetch tools';
      })
      
      // Fetch categories
      .addCase(fetchCategories.fulfilled, (state, action) => {
        state.categories = action.payload;
      })
      
      // Add tool
      .addCase(addTool.fulfilled, (state, action) => {
        state.tools.push(action.payload);
      })
      
      // Update tool
      .addCase(updateTool.fulfilled, (state, action) => {
        const index = state.tools.findIndex(tool => tool.guid === action.payload.guid);
        if (index !== -1) {
          state.tools[index] = action.payload;
        }
      })
      
      // Delete tool
      .addCase(deleteTool.fulfilled, (state, action) => {
        state.tools = state.tools.filter(tool => tool.guid !== action.payload);
        state.activeTools = state.activeTools.filter(id => id !== action.payload);
      })
      
      // Add category
      .addCase(addCategory.fulfilled, (state, action) => {
        state.categories.push(action.payload);
      })
      
      // Update category
      .addCase(updateCategory.fulfilled, (state, action) => {
        const index = state.categories.findIndex(category => category.id === action.payload.id);
        if (index !== -1) {
          state.categories[index] = action.payload;
        }
      })
      
      // Delete category
      .addCase(deleteCategory.fulfilled, (state, action) => {
        state.categories = state.categories.filter(category => category.id !== action.payload);
      })
      
      // Import tools
      .addCase(importTools.fulfilled, (state, action) => {
        // Merge imported tools with existing tools
        const existingGuids = new Set(state.tools.map(tool => tool.guid));
        const newTools = action.payload.filter(tool => !existingGuids.has(tool.guid));
        state.tools = [...state.tools, ...newTools];
      });
  },
});

export const { 
  setActiveTools, 
  addActiveTool, 
  removeActiveTool,
  clearActiveTools 
} = toolSlice.actions;

export default toolSlice.reducer;
