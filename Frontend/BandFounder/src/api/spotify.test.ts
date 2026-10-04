import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {parsePlaylistId, SpotifyUtilsApiError, streamPlaylistCopy} from './spotify';

const ID = '37i9dQZF1DXcBWIGoYBM5M';

describe('parsePlaylistId', () => {
    it.each([
        ID,
        `  ${ID}  `,
        `spotify:playlist:${ID}`,
        `https://open.spotify.com/playlist/${ID}?si=abc&pt=1`,
        `https://open.spotify.com/intl-de/playlist/${ID}`,
    ])('accepts %s', (input) => {
        expect(parsePlaylistId(input)).toBe(ID);
    });

    it.each([
        '',
        'road trip',
        `https://open.spotify.com/album/${ID}`,
        `https://open.spotify.com/playlist/${ID}extra`,
    ])('rejects %s', (input) => {
        expect(parsePlaylistId(input)).toBeNull();
    });
});

describe('streamPlaylistCopy', () => {
    beforeEach(() => {
        document.cookie = 'auth_token=test-token';
    });

    afterEach(() => {
        vi.restoreAllMocks();
        document.cookie = 'auth_token=; Max-Age=0';
    });

    const ndjson = (...messages: object[]) =>
        new Response(messages.map((message) => JSON.stringify(message)).join('\n') + '\n', {status: 200});

    it('reports progress and returns the final result', async () => {
        const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(ndjson(
            {type: 'progress', phase: 'reading-source', processed: 50, total: 100, selectedTrackCount: 10},
            {type: 'progress', phase: 'adding', processed: 8, total: 8, addedCount: 8, skippedDuplicateCount: 2},
            {
                type: 'complete',
                selectedTrackCount: 10,
                addedCount: 8,
                skippedDuplicateCount: 2,
                createdTarget: true,
                targetPlaylist: {id: 'new', name: 'Copy', url: 'https://open.spotify.com/playlist/new'},
            },
        ));
        const onProgress = vi.fn();

        const result = await streamPlaylistCopy(
            {sourcePlaylistId: ID, contributorIds: ['alice', ''], target: {kind: 'new', name: 'Copy'}},
            onProgress
        );

        const body = JSON.parse(fetchMock.mock.calls[0][1]!.body as string);
        expect(body).toEqual({
            sourcePlaylistId: ID,
            contributorIds: ['alice', ''],
            targetPlaylistId: null,
            newPlaylistName: 'Copy',
        });
        expect(onProgress).toHaveBeenCalledTimes(2);
        expect(onProgress.mock.calls[0][0]).toMatchObject({phase: 'reading-source', processed: 50, total: 100});
        expect(result).toMatchObject({addedCount: 8, skippedDuplicateCount: 2, createdTarget: true});
        expect(result.targetPlaylist?.id).toBe('new');
    });

    it('turns a streamed error into a SpotifyUtilsApiError with its status', async () => {
        vi.spyOn(globalThis, 'fetch').mockResolvedValue(ndjson(
            {type: 'progress', phase: 'reading-source', processed: 50, total: 100},
            {type: 'error', status: 410, message: 'Reconnect please'},
        ));

        const error = await streamPlaylistCopy(
            {sourcePlaylistId: ID, contributorIds: ['alice'], target: {kind: 'existing', playlistId: 'target'}},
            () => undefined
        ).catch((caught) => caught);

        expect(error).toBeInstanceOf(SpotifyUtilsApiError);
        expect(error.status).toBe(410);
        expect(error.message).toBe('Reconnect please');
    });
});
