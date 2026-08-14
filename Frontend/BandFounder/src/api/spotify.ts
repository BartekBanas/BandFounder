import {API_URL} from "../config";
import {authorizedHeaders, getUserId} from "../hooks/authentication";
import {mantineErrorNotification, mantineInformationNotification} from "../components/common/mantineNotification";

const SPOTIFY_REAUTH_NOTIFICATION =
    'Your Spotify connection has expired. Please link your Spotify account again.';

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
    }
    if (response.status === 422) {
        return null;
    }

    mantineErrorNotification('Failed to verify Spotify account connection');
    throw new Error('Failed to verify Spotify account connection');
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

export interface TopArtist {
    id: string;
    name: string;
    imageUrl: string | null;
    genres?: string[];
}

export type SpotifyTimeRange = 'short_term' | 'medium_term' | 'long_term';

export async function getTopArtists(
    guid: string,
    timeRange: SpotifyTimeRange = 'medium_term',
    limit = 10
): Promise<TopArtist[] | null> {
    try {
        const params = new URLSearchParams({
            timeRange,
            limit: String(limit),
        });
        const response = await fetch(
            `${API_URL}/accounts/${guid}/artists/spotify/top?${params.toString()}`,
            {
                method: 'GET',
                headers: authorizedHeaders()
            }
        );
        if (response.ok) {
            return await response.json();
        }
        if (response.status === 410) {
            if (guid === getUserId()) {
                mantineInformationNotification(SPOTIFY_REAUTH_NOTIFICATION);
            }
            return null;
        }
        if (response.status === 422) {
            return null;
        }
        console.error('Error getting top artists:', await response.text());
        mantineErrorNotification('Failed to load Spotify top artists');
        return null;
    } catch (error) {
        console.error('Error getting top artists:', error);
        mantineErrorNotification('Failed to load Spotify top artists');
        return null;
    }
}

export interface TopTrack {
    id: string;
    name: string;
    imageUrl: string | null;
    artistNames: string[];
}

export interface LikedTrackArtist {
    id: string | null;
    name: string;
    genres: string[];
}

export interface LikedTrack {
    id: string;
    name: string;
    albumImageUrl: string | null;
    artists: LikedTrackArtist[];
}

export interface LikedTracksByGenreResult {
    tracks: LikedTrack[];
    scannedTrackCount: number;
    remainingTrackCount: number;
    totalAvailable: number;
    foundMatchCount: number;
    truncated: boolean;
}

export interface LikedTracksByGenreProgress {
    scannedTrackCount: number;
    remainingTrackCount: number;
    totalAvailable: number;
    foundMatchCount: number;
    truncated: boolean;
}

interface LikedTracksByGenreStreamMessage extends LikedTracksByGenreProgress {
    type: 'progress' | 'complete' | 'error';
    tracks?: LikedTrack[];
    status?: number;
    message?: string;
}

export class SpotifyUtilsApiError extends Error {
    status: number;

    constructor(status: number, message: string) {
        super(message);
        this.status = status;
        this.name = 'SpotifyUtilsApiError';
    }
}

export async function getLikedTracksByGenre(
    genre: string,
    onProgress?: (progress: LikedTracksByGenreProgress) => void
): Promise<LikedTracksByGenreResult> {
    const params = new URLSearchParams({genre});
    const response = await fetch(
        `${API_URL}/spotify/liked-tracks/by-genre/stream?${params.toString()}`,
        {
            method: 'GET',
            headers: authorizedHeaders(),
        }
    );

    if (!response.ok) {
        throwSpotifyUtilsApiError(response.status, await response.text());
    }

    if (!response.body) {
        throw new SpotifyUtilsApiError(500, 'The scan did not return a readable progress stream.');
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    let completedResult: LikedTracksByGenreResult | null = null;

    const processMessage = (line: string) => {
        const message = JSON.parse(line) as LikedTracksByGenreStreamMessage;

        if (message.type === 'progress') {
            onProgress?.({
                scannedTrackCount: message.scannedTrackCount,
                remainingTrackCount: message.remainingTrackCount,
                totalAvailable: message.totalAvailable,
                foundMatchCount: message.foundMatchCount,
                truncated: message.truncated,
            });
            return;
        }

        if (message.type === 'error') {
            throwSpotifyUtilsApiError(
                message.status ?? 500,
                message.message ?? 'Failed to filter liked songs by genre'
            );
        }

        if (message.type === 'complete') {
            completedResult = {
                tracks: message.tracks ?? [],
                scannedTrackCount: message.scannedTrackCount,
                remainingTrackCount: message.remainingTrackCount,
                totalAvailable: message.totalAvailable,
                foundMatchCount: message.foundMatchCount,
                truncated: message.truncated,
            };
        }
    };

    while (true) {
        const {done, value} = await reader.read();
        buffer += decoder.decode(value, {stream: !done});

        const lines = buffer.split('\n');
        buffer = lines.pop() ?? '';
        lines
            .map((line) => line.trim())
            .filter(Boolean)
            .forEach(processMessage);

        if (done) {
            break;
        }
    }

    if (buffer.trim()) {
        processMessage(buffer.trim());
    }

    if (!completedResult) {
        throw new SpotifyUtilsApiError(500, 'The scan ended before results were returned.');
    }

    return completedResult;
}

function throwSpotifyUtilsApiError(status: number, bodyText: string): never {
    if (status === 410) {
        mantineInformationNotification(SPOTIFY_REAUTH_NOTIFICATION);
        throw new SpotifyUtilsApiError(410, bodyText || SPOTIFY_REAUTH_NOTIFICATION);
    }

    if (status === 403) {
        const message =
            bodyText ||
            'Spotify account is missing required permissions. Please reconnect your Spotify account.';
        mantineInformationNotification(message);
        throw new SpotifyUtilsApiError(403, message);
    }

    if (status === 422) {
        throw new SpotifyUtilsApiError(422, bodyText || 'Spotify account is not linked.');
    }

    if (status === 429) {
        mantineErrorNotification('Spotify rate limit hit. Try again in a minute.');
        throw new SpotifyUtilsApiError(429, bodyText || 'Rate limited');
    }

    if (status === 400) {
        mantineErrorNotification(bodyText || 'Invalid genre');
        throw new SpotifyUtilsApiError(400, bodyText || 'Invalid genre');
    }

    if (status === 404) {
        const message =
            'Liked-songs endpoint not found. Restart the backend so it picks up the new API.';
        mantineErrorNotification(message);
        throw new SpotifyUtilsApiError(404, message);
    }

    mantineErrorNotification('Failed to filter liked songs by genre');
    throw new SpotifyUtilsApiError(status, bodyText || `Request failed (${status})`);
}

export async function getTopTracks(
    guid: string,
    timeRange: SpotifyTimeRange = 'medium_term',
    limit = 50
): Promise<TopTrack[] | null> {
    try {
        const params = new URLSearchParams({
            timeRange,
            limit: String(limit),
        });
        const response = await fetch(
            `${API_URL}/accounts/${guid}/tracks/spotify/top?${params.toString()}`,
            {
                method: 'GET',
                headers: authorizedHeaders()
            }
        );
        if (response.ok) {
            return await response.json();
        }
        if (response.status === 410) {
            if (guid === getUserId()) {
                mantineInformationNotification(SPOTIFY_REAUTH_NOTIFICATION);
            }
            return null;
        }
        if (response.status === 422) {
            return null;
        }
        console.error('Error getting top tracks:', await response.text());
        mantineErrorNotification('Failed to load Spotify top tracks');
        return null;
    } catch (error) {
        console.error('Error getting top tracks:', error);
        mantineErrorNotification('Failed to load Spotify top tracks');
        return null;
    }
}
