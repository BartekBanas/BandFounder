import {MusicianSlotDto} from "./MusicianSlotsDto";
import {Account} from "./Account";

export interface Listing {
    id: string;
    ownerId: string;
    name: string;
    genre: string;
    type: ListingType;
    description: string;
    musicianSlots: MusicianSlotDto[];
    owner: Account;
}

export enum ListingType {
    Band = 'Band',
    CollaborativeSong = 'CollaborativeSong'
}

export interface ListingWithScore {
    listing: Listing;
    similarityScore: number;
}

export interface ListingsFeedDto {
    listings: ListingWithScore[];
}

export interface ListingFeedFilters {
    excludeOwn: boolean | undefined,
    matchMusicRole: boolean | undefined,
    fromLatest: boolean | undefined,
    listingType: ListingType | undefined,
    genre: string | undefined
}