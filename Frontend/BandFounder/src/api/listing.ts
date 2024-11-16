import {API_URL} from "../config";
import {authorizedHeaders} from "../hooks/authentication";
import {mantineErrorNotification} from "../components/common/mantineNotification";
import {commonTaste} from "../types/CommonTaste";
import {ChatroomDto} from "../types/chatroomDto";

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

export async function contactListingOwner(listingId: string): Promise<ChatroomDto> {
    const response = await fetch(`${API_URL}/listings/${listingId}/contact`, {
        method: 'POST',
        headers: authorizedHeaders()
    });

    if (!response.ok) {
        mantineErrorNotification('Failed to contact the listing owner');
        throw new Error('Failed to contact the listing owner');
    }

    const chatroom: Promise<ChatroomDto> = response.json();
    return chatroom;
}