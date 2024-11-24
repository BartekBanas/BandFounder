import {API_URL} from "../config";
import {authorizedHeaders} from "../hooks/authentication";
import {mantineErrorNotification} from "../components/common/mantineNotification";
import {commonTaste} from "../types/CommonTaste";
import {ChatroomDto} from "../types/chatroomDto";
import {ListingCreateDto} from "../types/ListingCreateDto";
import {Listing, ListingFeedFilters, ListingsFeedDto} from "../types/Listing";

export async function getCommonTaste(listingId: string): Promise<commonTaste> {
    const response = await fetch(`${API_URL}/listings/${listingId}/commonTaste`, {
        method: 'GET',
        headers: authorizedHeaders()
    });

    if (!response.ok) {
        mantineErrorNotification('Failed to fetch common taste');
        throw new Error('Failed to fetch common taste');
    }

    const artistsAndGenres: Promise<commonTaste> = response.json();
    return artistsAndGenres;
}

export async function contactListingOwner(listingId: string): Promise<ChatroomDto | undefined> {
    try {
        const response = await fetch(`${API_URL}/listings/${listingId}/contact`, {
            method: 'POST',
            headers: authorizedHeaders()
        });

        if (!response.ok) {
            throw new Error('Failed to contact the listing owner');
        }

        const chatroom: ChatroomDto = await response.json();
        return chatroom;
    } catch (error) {
        console.error(error);
    }
}

export async function getListing(listingId: string): Promise<Listing> {
    try {
        const response = await fetch(`${API_URL}/listings/${listingId}`, {
            method: 'GET',
            headers: authorizedHeaders()
        });

        return await response.json();
    } catch (e) {
        mantineErrorNotification('Failed to fetch listing');
        throw new Error('Failed to fetch listing');
    }
}

export async function getUsersListings(accountId: string): Promise<Listing[]> {
    try {
        const response = await fetch(`${API_URL}/accounts/${accountId}/listings`, {
            method: 'GET',
            headers: authorizedHeaders()
        });
        return await response.json();
    } catch (e) {
        mantineErrorNotification('Failed to fetch listings');
        throw new Error('Failed to fetch listings');
    }
}

export async function getListingFeed(ListingFeedFilters: ListingFeedFilters): Promise<ListingsFeedDto> {
    try {
        const params = new URLSearchParams();

        if (ListingFeedFilters.excludeOwn !== undefined) {
            params.append('ExcludeOwn', ListingFeedFilters.excludeOwn.toString());
        }
        if (ListingFeedFilters.matchMusicRole !== undefined) {
            params.append('MatchRole', ListingFeedFilters.matchMusicRole.toString());
        }
        if (ListingFeedFilters.fromLatest !== undefined) {
            params.append('FromLatest', ListingFeedFilters.fromLatest.toString());
        }
        if (ListingFeedFilters.listingType !== undefined) {
            params.append('ListingType', ListingFeedFilters.listingType.toString());
        }
        if (ListingFeedFilters.genre !== undefined) {
            params.append('Genre', ListingFeedFilters.genre.toString());
        }

        const url = `${API_URL}/listings?${params.toString()}`;
        const response = await fetch(url, {
            method: 'GET',
            headers: authorizedHeaders()
        });

        return await response.json();
    } catch (e) {
        mantineErrorNotification('Failed to fetch listings feed');
        throw new Error('Failed to fetch listings');
    }
}

export async function postListing(listing: ListingCreateDto): Promise<void> {
    try {
        const response = await fetch(`${API_URL}/listings`, {
            method: 'POST',
            headers: authorizedHeaders(),
            body: JSON.stringify(listing)
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

    } catch (e: any) {
        mantineErrorNotification('Failed to create listing');
        throw new Error(e.message);
    }
}

export async function updateListing(listing: ListingCreateDto, listingId: string): Promise<void> {
    try {
        const response = await fetch(`${API_URL}/listings/${listingId}`, {
            method: 'PUT',
            headers: authorizedHeaders(),
            body: JSON.stringify(listing)
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const responseText = await response.text();
        return responseText ? JSON.parse(responseText) : {};
    } catch (e) {
        mantineErrorNotification('Failed to update listing');
        console.error('Error updating listing:', e);
    }
}

export async function deleteListing(listingId: string) {
    try {
        const response = await fetch(`${API_URL}/listings/${listingId}`, {
            method: 'DELETE',
            headers: authorizedHeaders()
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return await response.text();
    } catch (e) {
        mantineErrorNotification('Failed to delete listing');
        console.error('Error deleting listing:', e);
    }
}