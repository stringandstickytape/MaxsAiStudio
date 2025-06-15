import React, { useState, useMemo, useEffect } from 'react';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
  UnifiedModalFooter,
} from '@/components/ui/unified-modal-dialog';
import { useModalStore } from '@/stores/useModalStore';
import { useOpenRouterModels } from '@/hooks/useOpenRouterModels';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { useModelManagement } from '@/hooks/useResourceManagement';
import { useModelStore } from '@/stores/useModelStore';
import { Model } from '@/types/settings';
import { toast } from '@/hooks/use-toast';
import { OpenRouterModel } from '@/services/api/openRouterApi';
import {
  useReactTable,
  getCoreRowModel,
  getFilteredRowModel,
  getSortedRowModel,
  flexRender,
  ColumnDef,
  SortingState,
} from '@tanstack/react-table';
import { Checkbox } from '@/components/ui/checkbox';
import { ChevronUp, ChevronDown } from 'lucide-react';

function mapOpenRouterToModel(openRouterModel: OpenRouterModel, providerGuid: string, existingModels: Model[]): Model {
  let friendlyName = openRouterModel.name;
  
  const isDuplicate = existingModels.some(m => m.modelName === openRouterModel.id && m.providerGuid === providerGuid);
  if (isDuplicate) {
    const timestamp = new Date().toISOString().slice(0, 16).replace('T', ' ');
    friendlyName = `${friendlyName} (${timestamp})`;
  }

  return {
    guid: '', 
    modelName: openRouterModel.id,
    friendlyName: friendlyName,
    userNotes: openRouterModel.description,
    providerGuid: providerGuid,
    additionalParams: '',
    input1MTokenPrice: parseFloat(openRouterModel.pricing.prompt) * 1_000_000,
    output1MTokenPrice: parseFloat(openRouterModel.pricing.completion) * 1_000_000,
    color: '#ff8800', 
    starred: false,
    supportsPrefill: false,
    requires1fTemp: false,
    allowsTopP: openRouterModel.supported_parameters?.includes('top_p') ?? false,
    reasoningEffort: 'none' as const,
  };
}

export function OpenRouterModelModal() {
  const { currentModal, closeModal } = useModalStore();
  const isOpen = currentModal?.id === 'openRouterImporter';
  const { models, isLoading, error, fetchModels } = useOpenRouterModels();
  const [searchTerm, setSearchTerm] = useState('');
  const [sorting, setSorting] = useState<SortingState>([]);
  const [rowSelection, setRowSelection] = useState({});
  const { addModel } = useModelManagement();
  const existingModels = useModelStore(state => state.models);
  const openRouterProvider = useModelStore(state => 
    state.providers.find(p => p.url.startsWith('https://openrouter.ai'))
  );

  useEffect(() => {
    if (isOpen) {
      fetchModels();
      setRowSelection({});
      setSearchTerm('');
    }
  }, [isOpen, fetchModels]);

  const columns = useMemo<ColumnDef<OpenRouterModel>[]>(() => [
    {
      id: 'select',
      header: ({ table }) => (
        <Checkbox
          checked={table.getIsAllPageRowsSelected()}
          onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
          aria-label="Select all"
        />
      ),
      cell: ({ row }) => (
        <Checkbox
          checked={row.getIsSelected()}
          onCheckedChange={(value) => row.toggleSelected(!!value)}
          aria-label="Select row"
        />
      ),
      enableSorting: false,
      size: 40,
    },
    { 
      accessorKey: 'name', 
      header: 'Name',
      cell: ({ row }) => (
        <div className="max-w-xs">
          <div className="font-medium truncate">{row.original.name}</div>
          <div className="text-xs text-muted-foreground truncate">{row.original.id}</div>
          {row.original.description && (
            <div className="text-xs text-muted-foreground mt-1 break-words whitespace-normal leading-tight">
              {row.original.description}
            </div>
          )}
        </div>
      ),
    },
    { 
      accessorKey: 'context_length', 
      header: 'Context',
      cell: ({ getValue }) => {
        const value = getValue<number>();
        return value >= 1000000 ? `${(value / 1000000).toFixed(1)}M` : 
               value >= 1000 ? `${(value / 1000).toFixed(0)}K` : 
               value.toLocaleString();
      },
    },
    { 
      id: 'promptPrice', 
      header: 'Input /1M', 
      accessorFn: row => parseFloat(row.pricing.prompt) * 1000000, 
      cell: ({ getValue }) => `$${getValue<number>().toFixed(2)}`,
    },
    { 
      id: 'completionPrice', 
      header: 'Output /1M', 
      accessorFn: row => parseFloat(row.pricing.completion) * 1000000, 
      cell: ({ getValue }) => `$${getValue<number>().toFixed(2)}`,
    },
    { 
      id: 'modalities', 
      header: 'Modalities', 
      accessorFn: row => row.architecture?.input_modalities?.join(', ') || row.architecture?.modality || 'text',
      cell: ({ getValue }) => {
        const modalities = getValue<string>();
        return <span className="text-xs">{modalities}</span>;
      },
    },
    { 
      id: 'supportedParams', 
      header: 'Supported Parameters', 
      accessorFn: row => row.supported_parameters?.join(', ') || 'None specified',
      cell: ({ getValue }) => {
        const params = getValue<string>();
        return (
          <div className="text-xs max-w-40 break-words whitespace-normal leading-tight" title={params}>
            {params}
          </div>
        );
      },
      size: 160,
    },
  ], []);

  const filteredData = useMemo(() => {
    if (!searchTerm) return models;
    const term = searchTerm.toLowerCase();
    return models.filter(model => 
      model.name.toLowerCase().includes(term) ||
      model.id.toLowerCase().includes(term) ||
      model.description.toLowerCase().includes(term) ||
      (model.supported_parameters?.join(', ') || '').toLowerCase().includes(term)
    );
  }, [models, searchTerm]);

  const table = useReactTable({
    data: filteredData,
    columns,
    state: { 
      sorting, 
      rowSelection 
    },
    onSortingChange: setSorting,
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    enableRowSelection: true,
  });

  const selectedCount = Object.keys(rowSelection).length;

  const handleAddSelected = async () => {
    const selectedRows = table.getRowModel().rows.filter(row => row.getIsSelected());
    const modelsToAdd = selectedRows.map(row => row.original);
    
    if (modelsToAdd.length === 0 || !openRouterProvider) return;

    try {
      for (const model of modelsToAdd) {
        const newModel = mapOpenRouterToModel(model, openRouterProvider.guid, existingModels);
        await addModel(newModel);
      }
      
      toast({ 
        title: "Success", 
        description: `${modelsToAdd.length} model(s) added successfully.` 
      });
      closeModal();
    } catch (err) {
      console.error('Failed to add models:', err);
      toast({ 
        title: "Error", 
        description: "Failed to add models. Please try again.", 
        variant: "destructive" 
      });
    }
  };

  if (!isOpen) return null;

  return (
    <UnifiedModalDialog 
      open={isOpen} 
      onOpenChange={(open) => !open && closeModal()} 
      size="4xl" 
      height="lg"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">OpenRouter Model Importer</h2>
        <div className="mt-3">
          <Input
            placeholder="Search models by name, ID, or description..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full"
          />
        </div>
      </UnifiedModalHeader>
      
      <UnifiedModalContent>
        {isLoading && (
          <div className="flex items-center justify-center h-64">
            <div className="text-center">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-2"></div>
              <p>Loading models from OpenRouter...</p>
            </div>
          </div>
        )}
        
        {error && (
          <div className="flex items-center justify-center h-64">
            <div className="text-center text-red-500">
              <p className="font-medium">Error loading models</p>
              <p className="text-sm mt-1">{error}</p>
              <Button 
                variant="outline" 
                size="sm" 
                className="mt-3" 
                onClick={fetchModels}
              >
                Retry
              </Button>
            </div>
          </div>
        )}
        
        {!isLoading && !error && filteredData.length === 0 && (
          <div className="flex items-center justify-center h-64">
            <p className="text-muted-foreground">
              {searchTerm ? 'No models match your search criteria.' : 'No models available.'}
            </p>
          </div>
        )}
        
        {!isLoading && !error && filteredData.length > 0 && (
          <div className="space-y-4">
            <div className="text-sm text-muted-foreground">
              Showing {filteredData.length} model{filteredData.length !== 1 ? 's' : ''} 
              {selectedCount > 0 && ` • ${selectedCount} selected`}
            </div>
            
            <div className="border rounded-lg overflow-hidden">
              <div className="overflow-auto max-h-96">
                <table className="w-full text-sm">
                  <thead className="bg-muted/50 sticky top-0">
                    {table.getHeaderGroups().map(headerGroup => (
                      <tr key={headerGroup.id}>
                        {headerGroup.headers.map(header => (
                          <th 
                            key={header.id} 
                            className="p-3 text-left font-medium cursor-pointer hover:bg-muted/75 transition-colors"
                            onClick={header.column.getCanSort() ? header.column.getToggleSortingHandler() : undefined}
                          >
                            <div className="flex items-center gap-1">
                              {flexRender(header.column.columnDef.header, header.getContext())}
                              {header.column.getCanSort() && (
                                <div className="flex flex-col">
                                  {header.column.getIsSorted() === 'asc' ? (
                                    <ChevronUp className="h-3 w-3" />
                                  ) : header.column.getIsSorted() === 'desc' ? (
                                    <ChevronDown className="h-3 w-3" />
                                  ) : (
                                    <div className="h-3 w-3 opacity-30">
                                      <ChevronUp className="h-2 w-2" />
                                      <ChevronDown className="h-2 w-2 -mt-1" />
                                    </div>
                                  )}
                                </div>
                              )}
                            </div>
                          </th>
                        ))}
                      </tr>
                    ))}
                  </thead>
                  <tbody>
                    {table.getRowModel().rows.map(row => (
                      <tr 
                        key={row.id} 
                        className="border-b hover:bg-muted/25 transition-colors"
                      >
                        {row.getVisibleCells().map(cell => (
                          <td key={cell.id} className="p-3">
                            {flexRender(cell.column.columnDef.cell, cell.getContext())}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        )}
      </UnifiedModalContent>
      
      <UnifiedModalFooter>
        <Button variant="outline" onClick={closeModal}>
          Cancel
        </Button>
        <Button 
          onClick={handleAddSelected} 
          disabled={selectedCount === 0}
        >
          Add {selectedCount > 0 ? `${selectedCount} Selected` : ''} Model{selectedCount !== 1 ? 's' : ''}
        </Button>
      </UnifiedModalFooter>
    </UnifiedModalDialog>
  );
}