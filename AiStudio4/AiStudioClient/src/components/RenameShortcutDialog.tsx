// AiStudioClient\src\components\RenameShortcutDialog.tsx
import React from 'react';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Separator } from '@/components/ui/separator';
import IconSelector from './IconSelector';
import { PinnedCommand } from './pinnedShortcutsUtils';
import { IconSet } from './IconSelector';

// Define themeable properties for the component (currently none)
export const themeableProps = {};

interface RenameShortcutDialogProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    commandToRename: PinnedCommand | null;
    newCommandName: string;
    setNewCommandName: (name: string) => void;
    selectedIconName: string | undefined;
    selectedIconSet: IconSet;
    onIconSelect: (iconName: string, iconSet: IconSet) => void;
    onConfirm: () => void;
    onCancel: () => void;
}

export function RenameShortcutDialog({
    open,
    onOpenChange,
    commandToRename,
    newCommandName,
    setNewCommandName,
    selectedIconName,
    selectedIconSet,
    onIconSelect,
    onConfirm,
    onCancel
}: RenameShortcutDialogProps) {
    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent
                className="sm:max-w-[500px]"
                style={{
                    backgroundColor: 'var(--global-background-color)',
                    color: 'var(--global-text-color)',
                    fontFamily: 'var(--global-font-family)',
                    fontSize: 'var(--global-font-size)'
                }}>
                <DialogHeader>
                    <DialogTitle>Edit Shortcut</DialogTitle>
                    <DialogDescription>
                        Customize the name and icon for your shortcut.
                    </DialogDescription>
                </DialogHeader>
                <div className="grid gap-4 py-4">
                    <div className="space-y-2">
                        <label htmlFor="shortcut-name" className="text-sm font-medium">Name</label>
                        <Input
                            id="shortcut-name"
                            value={newCommandName}
                            onChange={(e) => setNewCommandName(e.target.value)}
                            placeholder="Shortcut name"
                            className="col-span-3"
                            autoFocus
                            style={{
                                backgroundColor: 'var(--global-background-color)',
                                borderColor: 'var(--global-border-color)',
                                color: 'var(--global-text-color)'
                            }}
                            onKeyDown={(e) => {
                                if (e.key === 'Enter') {
                                    onConfirm();
                                } else if (e.key === 'Escape') {
                                    onCancel();
                                }
                            }}
                        />
                    </div>
                    
                    <Separator className="my-2" style={{ backgroundColor: 'var(--global-border-color)' }} />
                    
                    <div className="space-y-2">
                        <label className="text-sm font-medium">Icon</label>
                        <IconSelector 
                            onSelect={onIconSelect}
                            selectedIconName={selectedIconName}
                            selectedIconSet={selectedIconSet}
                        />
                    </div>
                </div>
                <DialogFooter>
                    <Button 
                        variant="outline" 
                        onClick={onCancel}
                        style={{
                            backgroundColor: 'var(--global-background-color)',
                            borderColor: 'var(--global-border-color)',
                            color: 'var(--global-text-color)'
                        }}
                    >
                        Cancel
                    </Button>
                    <Button onClick={onConfirm}>Save</Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}