import {MusicianSlot} from "./MusicianSlot";
import {Account} from "./Account";

export interface Listing {
    id: string;
    ownerId: string;
    name: string;
    genre: string;
    type: ListingType;
    description: string;
    musicianSlots: MusicianSlot[];
    owner: Account;
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
    pageNumber: number | undefined;
    pageSize: number | undefined;
}

export enum ListingType {
    Band = 'Band',
    CollaborativeSong = 'CollaborativeSong'
}

export function castToListingType(value: string): ListingType | null {
    return isListingType(value) ? (value as ListingType) : null;
}

function isListingType(value: string): value is ListingType {
    return Object.values(ListingType).includes(value as ListingType);
}