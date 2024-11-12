import {MusicianSlotDto} from "./MusicianSlotsDto";
import {Account} from "./Account";
import {MusicianSlotsDtoUpdatedListing} from "./MusicianSlotsDtoUpdatedListing";

export interface ListingUpdated {
    name: string;
    genre?: string;
    type: string;
    description?: string;
    musicianSlots: MusicianSlotsDtoUpdatedListing[];
}