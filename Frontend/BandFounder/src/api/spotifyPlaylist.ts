import {API_URL} from "../config";
import {authorizedHeaders} from "../hooks/authentication";

const PLAYLIST_ID_PATTERN = /^[0-9A-Za-z]{22}$/;
const PLAYLIST_URI_PREFIX = "spotify:playlist:";

export type ParsedSpotifyPlaylistInput =
    | {kind: "playlistId"; id: string}
    | {kind: "shortLink"; url: string};

export interface SpotifyPlaylistArtist {
    id: string;
    name: string;
}

export interface SpotifyPlaylistRead {
    spotifyId: string;
    name: string;
    artists: SpotifyPlaylistArtist[];
    trackCount: number;
    truncatedAtTrackCap: boolean;
}

export class SpotifyPlaylistNotReadableError extends Error {
    readonly code = "PlaylistNotReadable";

    constructor() {
        super("Can't read this playlist. Make sure it's public.");
        this.name = "SpotifyPlaylistNotReadableError";
    }
}

export function parsePlaylistId(raw: string): string | null {
    const trimmed = raw.trim();
    if (trimmed.length === 0) {
        return null;
    }

    if (PLAYLIST_ID_PATTERN.test(trimmed)) {
        return trimmed;
    }

    if (startsWithInsensitive(trimmed, PLAYLIST_URI_PREFIX)) {
        const id = trimmed.slice(PLAYLIST_URI_PREFIX.length).split(/[/?#]/)[0];
        return PLAYLIST_ID_PATTERN.test(id) ? id : null;
    }

    const url = tryParseUrl(trimmed);
    if (url === null || hostnameOf(url) !== "open.spotify.com") {
        return null;
    }

    const match = url.pathname.match(/\/playlist\/([0-9A-Za-z]{22})(?:\/|$)/);
    return match?.[1] ?? null;
}

export function parsePlaylistInput(raw: string): ParsedSpotifyPlaylistInput | null {
    const trimmed = raw.trim();
    const shortLink = trySpotifyShortLink(trimmed);
    if (shortLink !== null) {
        return {kind: "shortLink", url: shortLink};
    }

    const id = parsePlaylistId(trimmed);
    if (id !== null) {
        return {kind: "playlistId", id};
    }

    return null;
}

export async function readPublicPlaylist(input: string): Promise<SpotifyPlaylistRead> {
    const response = await fetch(`${API_URL}/spotify/playlists/read`, {
        method: "POST",
        headers: authorizedHeaders(),
        body: JSON.stringify({input}),
    });

    if (response.status === 404) {
        throw new SpotifyPlaylistNotReadableError();
    }

    if (!response.ok) {
        throw new Error(await response.text());
    }

    return response.json();
}

function trySpotifyShortLink(raw: string): string | null {
    const candidate = raw.includes("://") ? raw : `https://${raw}`;
    const url = tryParseUrl(candidate);
    if (url === null) {
        return null;
    }

    if (url.protocol !== "http:" && url.protocol !== "https:") {
        return null;
    }

    if (hostnameOf(url) !== "spotify.link") {
        return null;
    }

    return url.toString();
}

function tryParseUrl(raw: string): URL | null {
    try {
        return new URL(raw);
    } catch {
        return null;
    }
}

function hostnameOf(url: URL): string {
    return url.hostname.toLowerCase().replace(/^www\./, "");
}

function startsWithInsensitive(value: string, prefix: string): boolean {
    return value.length >= prefix.length && value.slice(0, prefix.length).toLowerCase() === prefix;
}
