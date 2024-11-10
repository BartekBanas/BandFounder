import {Listing} from "./Listing";

export interface ListingWithScore {
    listing: Listing;
    similarityScore: number;
}

export interface ListingsFeedDto {
    listings: ListingWithScore[];
}