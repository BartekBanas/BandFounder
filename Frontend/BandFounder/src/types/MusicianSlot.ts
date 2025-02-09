export interface MusicianSlot {
    id: string;
    role: string;
    status: SlotType;
}

export enum SlotType {
    Available = 'Available',
    Filled = 'Filled'
}

export function castToSlotType(value: string): SlotType | null {
    return isSlotType(value) ? (value as SlotType) : null;
}

function isSlotType(value: string): value is SlotType {
    return Object.values(SlotType).includes(value as SlotType);
}