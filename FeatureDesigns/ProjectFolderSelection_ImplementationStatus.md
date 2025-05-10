# Project Folder Selection Feature - Implementation Status

This document tracks the implementation progress of the Project Folder Selection feature.

## I. Server-Side Changes

1.  **`AiStudio4/Core/Models/ProjectFolderEntry.cs`**: DONE
2.  **`AiStudio4/InjectedDependencies/IProjectHistoryService.cs` & `AiStudio4/InjectedDependencies/ProjectHistoryService.cs`**: DONE
3.  **`AiStudio4/InjectedDependencies/RequestHandlers/ConfigRequestHandler.cs` (API Endpoint)**: DONE
4.  **`AiStudio4/Core/Models/ChatRequest.cs` (Model)**: DONE
5.  **`AiStudio4/Services/ChatProcessingService.cs` (Logic)**: DONE
6.  **`AiStudio4/Services/DefaultChatService.cs` (Logic)**: DONE
7.  **Tool Execution Context**:
    *   `AiStudio4/DataModels/AiRequestOptions.cs` (Model): DONE
    *   `AiStudio4/Core/Tools/BaseToolImplementation.cs` & `AiStudio4/Core/Interfaces/ITool.cs`: DONE
    *   Individual Tool Updates (`DirectoryTreeTool`, `FileSearchTool`, `ReadFilesTool`, `ModifyFilesTool`, `CreateNewFileTool`, `DeleteFileTool`): DONE
    *   **REMAINING FOR TOOLS**:
        *   `AiStudio4/Core/Tools/ReplaceFileTool.cs`: Needs `ProcessAsync` update to use `GetActiveProjectRoot(projectRootPathOverride)` and initialize `PathSecurityManager` with the `activeRoot`.
        *   `AiStudio4/Core/Tools/RenameFileTool.cs`: Needs `ProcessAsync` update similarly.
        *   `AiStudio4/Core/Tools/FileRegExSearch.cs`: Needs `ProcessAsync` update similarly.
        *   `AiStudio4/Core/Tools/FindAndReplaceTool.cs`: Needs `ProcessAsync` update similarly.
    *   **REMAINING FOR TOOL SERVICE CHAIN**:
        *   `AiStudio4/Services/ToolProcessorService.cs`: Modify to pass the `effectiveProjectPath` (obtained in `ChatProcessingService`) when calling `_builtinToolService.ProcessBuiltinToolAsync`.
        *   `AiStudio4/Core/Interfaces/IBuiltinToolService.cs`: Update `ProcessBuiltinToolAsync` signature to include `string projectRootPath = null`.
        *   `AiStudio4/Services/BuiltinToolService.cs`: Update `ProcessBuiltinToolAsync` signature and pass the `projectRootPath` to the individual tool's `ProcessAsync` method. Evaluate the role of `UpdateProjectRoot()` method.
8.  **`AiStudio4/MainWindow.xaml.cs` (Registering Folders)**: PENDING
    *   Inject `IProjectHistoryService`.
    *   Call `await _projectHistoryService.AddOrUpdateProjectFolderAsync(selectedPath);` in `SetProjectPathMenuItem_Click` and `RecentProjectPathMenuItem_Click` after `_generalSettingsService.SaveSettings();`.

## II. Client-Side Changes (All PENDING)

1.  **Type Definition (`AiStudio4/AiStudioClient/src/types/projectFolder.ts` - New File)**:
    *   Create this file with the `ProjectFolder` interface:
        ```typescript
        // AiStudioClient/src/types/projectFolder.ts
        export interface ProjectFolder {
          id: string;
          name: string;
          pathSnippet: string; // e.g., ".../src/components" - for display
        }
        ```
2.  **Zustand Store (`AiStudio4/AiStudioClient/src/stores/useProjectFolderStore.ts` - New File)**:
    *   Create this store for managing project folder state, including `fetchProjectFolders` and `setSelectedProjectFolderId`.
3.  **UI Component (`AiStudio4/AiStudioClient/src/components/common/ProjectFolderSelector.tsx` - New File)**:
    *   Create the React component for the selector dropdown using `@/components/ui/select`.
    *   It should fetch and display `projectFolders` from `useProjectFolderStore`.
    *   Show `folder.name` and `folder.pathSnippet`.
    *   On selection, call `setSelectedProjectFolderId`.
4.  **Integrate Selector (`AiStudio4/AiStudioClient/src/components/InputBar/InputBar.tsx` - Modified)**:
    *   Import `ProjectFolderSelector`.
    *   Add it to the bottom bar of the input section.
5.  **App Initialization (`AiStudio4/AiStudioClient/src/App.tsx` - Modified)**:
    *   Ensure `fetchProjectFolders` is called on application mount (likely handled by the selector component's `useEffect`).
6.  **Chat Request Sending (`AiStudio4/AiStudioClient/src/hooks/useChatManagement.ts` - Modified)**:
    *   Import `useProjectFolderStore`.
    *   In `sendMessage` (or the function that constructs the chat API payload), get `selectedProjectFolderId` from the store.
    *   Add `projectFolderId: selectedProjectFolderId` to the API request payload sent to `/api/chat`.