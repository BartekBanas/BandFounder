import {MusicianSlotDto} from "./MusicianSlotsDto";
import {Account} from "./Account";

export interface Listing {
    id: string;
    ownerId: string;
    name: string;
    genre?: string;
    type: string;
    description?: string;
    musicianSlots: MusicianSlotDto[];
    owner: Account;
}