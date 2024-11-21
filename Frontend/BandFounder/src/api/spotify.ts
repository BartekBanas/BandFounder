import {API_URL} from "../config";
import {authorizedHeaders} from "../hooks/authentication";
import {mantineErrorNotification, mantineInformationNotification} from "../components/common/mantineNotification";

export async function requestSpotifyAccountLinkFromCode(spotifyConnectionPageUrl: string, code: string): Promise<void> {
    const response = await fetch(`${API_URL}/spotify/tokens`, {
        method: 'POST',
        headers: authorizedHeaders(),
        body: JSON.stringify({
            code: code,
            base_app_url: encodeURI(spotifyConnectionPageUrl)
        }),
    });

    if (response.status === 409) {
        mantineInformationNotification('Your account is already connected to a Spotify account');
        throw new Error(await response.text());
    } else if (!response.ok) {
        mantineErrorNotification('An error occurred while linking your Spotify account');
        throw new Error(await response.text());
    }
}

export async function fetchSpotifyTokens(): Promise<string | null> {
    const response = await fetch(`${API_URL}/spotify/tokens`, {
        method: 'GET',
        headers: authorizedHeaders()
    });

    if (response.ok) {
        return response.json();
    } else if (response.status === 404) {
        return null;
    } else {
        mantineErrorNotification('Failed to verify Spotify account connection');
        throw new Error('Failed to verify Spotify account connection');
    }
}

export async function fetchSpotifyAppClientId(): Promise<string> {
    const response = await fetch(`${API_URL}/spotify/app/clientId`, {
        method: 'GET',
        headers: authorizedHeaders()
    });

    const responseText = await response.text();

    if (!response.ok) {
        mantineErrorNotification('Failed to fetch Spotify app client ID');
        throw new Error(responseText);
    }

    return responseText;
}

export async function getTopArtists(guid: string): Promise<string[]> {
    try {
        const response = await fetch(`${API_URL}/accounts/${guid}/artists/spotify/top`, {
            method: 'GET',
            headers: authorizedHeaders()
        });
        if (!response.ok) {
            throw new Error(await response.text());
        }
        return await response.json();
    } catch (error) {
        console.error('Error getting top artists:', error);
        return [];
    }
}