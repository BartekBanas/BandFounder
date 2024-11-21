import {MusicianSlotsDtoUpdatedListing} from "./MusicianSlotsDtoUpdatedListing";

export interface ListingCreateDto {
    name: string;
    genre?: string;
    type: string;
    description?: string;
    musicianSlots: MusicianSlotsDtoUpdatedListing[];
}