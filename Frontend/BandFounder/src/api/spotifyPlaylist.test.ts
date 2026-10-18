import {afterEach, describe, expect, it, vi} from "vitest";
import {
    linkPublicPlaylist,
    parsePlaylistId,
    parsePlaylistInput,
    readPublicPlaylist,
    unlinkPublicPlaylist,
    SpotifyPlaylistNotReadableError,
} from "./spotifyPlaylist";

const PLAYLIST_ID = "37i9dQZF1DXcBWIGoYBM5M";

describe("parsePlaylistId", () => {
    it("accepts a bare playlist id", () => {
        expect(parsePlaylistId(PLAYLIST_ID)).toBe(PLAYLIST_ID);
    });

    it("accepts an open.spotify.com URL with a share query", () => {
        expect(parsePlaylistId(`https://open.spotify.com/playlist/${PLAYLIST_ID}?si=abc123`)).toBe(PLAYLIST_ID);
    });

    it("accepts an intl playlist URL", () => {
        expect(parsePlaylistId(`https://open.spotify.com/intl-de/playlist/${PLAYLIST_ID}`)).toBe(PLAYLIST_ID);
    });

    it("accepts a spotify:playlist URI", () => {
        expect(parsePlaylistId(`spotify:playlist:${PLAYLIST_ID}`)).toBe(PLAYLIST_ID);
    });

    it("leaves spotify.link short URLs for the backend", () => {
        expect(parsePlaylistId("https://spotify.link/abc123")).toBeNull();
    });

    it("rejects unrelated input", () => {
        expect(parsePlaylistId("https://open.spotify.com/track/4cOdK2wGLETKBe3p0IXgQd")).toBeNull();
        expect(parsePlaylistId("not-a-playlist")).toBeNull();
        expect(parsePlaylistId("")).toBeNull();
    });
});

describe("parsePlaylistInput", () => {
    it("passes a short link through instead of failing locally", () => {
        expect(parsePlaylistInput("https://spotify.link/abc123")).toEqual({
            kind: "shortLink",
            url: "https://spotify.link/abc123",
        });
    });

    it("normalizes a scheme-less spotify.link URL", () => {
        expect(parsePlaylistInput("spotify.link/abc123")).toEqual({
            kind: "shortLink",
            url: "https://spotify.link/abc123",
        });
    });

    it("returns a playlist id for URLs the frontend can parse", () => {
        expect(parsePlaylistInput(`https://open.spotify.com/playlist/${PLAYLIST_ID}`)).toEqual({
            kind: "playlistId",
            id: PLAYLIST_ID,
        });
    });

    it("returns null for invalid input", () => {
        expect(parsePlaylistInput("https://example.com")).toBeNull();
    });
});

describe("readPublicPlaylist", () => {
    afterEach(() => {
        vi.restoreAllMocks();
        document.cookie = "auth_token=; Max-Age=0";
    });

    it("posts the raw input to the playlist read API", async () => {
        document.cookie = "auth_token=test-token";
        const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(
            new Response(JSON.stringify({
                spotifyId: PLAYLIST_ID,
                name: "Road trip",
                artists: [],
                trackCount: 0,
                truncatedAtTrackCap: false,
            }), {status: 200})
        );

        const result = await readPublicPlaylist("https://spotify.link/abc123");

        expect(result.name).toBe("Road trip");
        const request = fetchMock.mock.calls[0];
        expect(String(request[0])).toContain("/spotify/playlists/read");
        expect(request[1]?.body).toBe(JSON.stringify({input: "https://spotify.link/abc123"}));
    });

    it("maps a 404 to SpotifyPlaylistNotReadableError", async () => {
        document.cookie = "auth_token=test-token";
        vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response("Can't read this playlist. Make sure it's public.", {status: 404}));

        await expect(readPublicPlaylist(PLAYLIST_ID)).rejects.toBeInstanceOf(SpotifyPlaylistNotReadableError);
    });
});

describe("linkPublicPlaylist", () => {
    afterEach(() => {
        vi.restoreAllMocks();
        document.cookie = "auth_token=; Max-Age=0";
    });

    it("posts the raw input to the playlist link API", async () => {
        document.cookie = "auth_token=test-token";
        const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(
            new Response(JSON.stringify({
                message: "Linked Road trip: 2 new artists added",
                playlistName: "Road trip",
                newArtistCount: 2,
                truncatedAtTrackCap: false,
            }), {status: 200})
        );

        const result = await linkPublicPlaylist("https://spotify.link/abc123");

        expect(result.message).toBe("Linked Road trip: 2 new artists added");
        expect(result.newArtistCount).toBe(2);
        const request = fetchMock.mock.calls[0];
        expect(String(request[0])).toContain("/spotify/playlists/link");
        expect(request[1]?.body).toBe(JSON.stringify({input: "https://spotify.link/abc123"}));
    });

    it("maps a 404 to SpotifyPlaylistNotReadableError", async () => {
        document.cookie = "auth_token=test-token";
        vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response("Can't read this playlist. Make sure it's public.", {status: 404}));

        await expect(linkPublicPlaylist(PLAYLIST_ID)).rejects.toBeInstanceOf(SpotifyPlaylistNotReadableError);
    });
});

describe("unlinkPublicPlaylist", () => {
    afterEach(() => {
        vi.restoreAllMocks();
        document.cookie = "auth_token=; Max-Age=0";
    });

    it("deletes the linked playlist by Spotify id", async () => {
        document.cookie = "auth_token=test-token";
        const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(
            new Response(null, {status: 204})
        );

        await unlinkPublicPlaylist(PLAYLIST_ID);

        const request = fetchMock.mock.calls[0];
        expect(String(request[0])).toContain(`/spotify/playlists/${PLAYLIST_ID}`);
        expect(request[1]?.method).toBe("DELETE");
    });

    it("throws when the playlist is not linked", async () => {
        document.cookie = "auth_token=test-token";
        vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response("Not Found", {status: 404}));

        await expect(unlinkPublicPlaylist(PLAYLIST_ID)).rejects.toThrow();
    });
});

