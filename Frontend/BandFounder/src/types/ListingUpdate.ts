import {MusicianSlot} from "./MusicianSlot";

export interface ListingUpdate {
    name: string;
    genre?: string;
    type: string;
    description?: string;
    musicianSlots: MusicianSlot[];
}