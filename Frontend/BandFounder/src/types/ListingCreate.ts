import {MusicianSlotCreate} from "./MusicianSlotCreate";

export interface ListingCreate {
    name: string;
    genre?: string;
    type: string;
    description?: string;
    musicianSlots: MusicianSlotCreate[];
}