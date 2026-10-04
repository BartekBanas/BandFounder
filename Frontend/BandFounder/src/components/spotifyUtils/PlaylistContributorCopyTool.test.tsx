import {fireEvent, render, screen, waitFor} from '@testing-library/react';
import {beforeEach, describe, expect, it, vi} from 'vitest';
import type {PlaylistContributorsPreview, SpotifyPlaylistSummary} from '../../api/spotify';
import {PlaylistContributorCopyTool} from './PlaylistContributorCopyTool';

const {fetchMinePlaylistsMock, previewMock, streamCopyMock, linkedMock} = vi.hoisted(() => ({
    fetchMinePlaylistsMock: vi.fn(),
    previewMock: vi.fn(),
    streamCopyMock: vi.fn(),
    linkedMock: vi.fn(),
}));

vi.mock('../../api/spotify', async (importOriginal) => ({
    ...(await importOriginal<typeof import('../../api/spotify')>()),
    fetchMyEditablePlaylists: fetchMinePlaylistsMock,
    previewPlaylistContributors: previewMock,
    streamPlaylistCopy: streamCopyMock,
}));

vi.mock('../../hooks/useSpotifyAccountLinked', () => ({default: linkedMock}));

vi.mock('../accountDrawer/spotifyConnection/spotifyConnection', () => ({
    redirectToSpotifyAuthorizationPage: vi.fn(),
}));

vi.mock('../common/AppLoader', () => ({
    AppLoader: () => <div data-testid="app-loader"/>,
}));

const SOURCE_ID = '37i9dQZF1DXcBWIGoYBM5M';

const playlist = (overrides: Partial<SpotifyPlaylistSummary>): SpotifyPlaylistSummary => ({
    id: SOURCE_ID,
    name: 'Road Trip',
    imageUrl: null,
    ownerName: 'Olivia',
    ownedByMe: false,
    collaborative: true,
    trackCount: 5,
    url: `https://open.spotify.com/playlist/${SOURCE_ID}`,
    ...overrides,
});

const preview: PlaylistContributorsPreview = {
    playlist: playlist({}),
    trackCount: 5,
    skippedItemCount: 1,
    contributors: [
        {id: 'alice', displayName: 'Alice', isCurrentUser: false, trackCount: 3},
        {id: 'me', displayName: 'Me Myself', isCurrentUser: true, trackCount: 2},
    ],
};

describe('PlaylistContributorCopyTool', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        linkedMock.mockResolvedValue(true);
        fetchMinePlaylistsMock.mockResolvedValue([
            playlist({}),
            playlist({id: 'mine', name: 'My Mix', ownedByMe: true, collaborative: false}),
        ]);
        previewMock.mockResolvedValue(preview);
    });

    it('walks from a pasted link to a finished copy into a new playlist', async () => {
        streamCopyMock.mockImplementation(async (_request, onProgress) => {
            onProgress({
                phase: 'adding', processed: 2, total: 2, selectedTrackCount: 3,
                addedCount: 2, skippedDuplicateCount: 1, targetPlaylist: null,
            });
            return {
                selectedTrackCount: 3,
                addedCount: 2,
                skippedDuplicateCount: 1,
                createdTarget: true,
                failureMessage: null,
                targetPlaylist: playlist({id: 'new', name: 'Road Trip', ownedByMe: true, url: 'https://open.spotify.com/playlist/new'}),
            };
        });

        render(<PlaylistContributorCopyTool/>);

        const input = await screen.findByLabelText('Playlist link or name');
        fireEvent.change(input, {target: {value: `https://open.spotify.com/playlist/${SOURCE_ID}?si=x`}});
        fireEvent.click(screen.getByRole('button', {name: 'Continue'}));

        expect(await screen.findByText('Whose songs should be copied?')).toBeTruthy();
        expect(previewMock).toHaveBeenCalledWith(SOURCE_ID, expect.any(AbortSignal));
        expect(screen.getByRole('button', {name: 'Pick at least one person'})).toHaveProperty('disabled', true);

        fireEvent.click(screen.getByRole('checkbox', {name: /Alice/}));
        fireEvent.click(screen.getByRole('button', {name: 'Continue with 3 songs'}));

        expect(await screen.findByText('Where should the songs go?')).toBeTruthy();
        expect(screen.getByLabelText('Playlist name')).toHaveProperty('value', 'Road Trip');

        fireEvent.click(screen.getByRole('button', {name: 'Copy 3 songs'}));

        expect(await screen.findByText('Added 2 songs to “Road Trip”')).toBeTruthy();
        expect(streamCopyMock.mock.calls[0][0]).toEqual({
            sourcePlaylistId: SOURCE_ID,
            contributorIds: ['alice'],
            target: {kind: 'new', name: 'Road Trip'},
        });
        expect(screen.getByRole('link', {name: /Open in Spotify/})).toHaveProperty(
            'href',
            'https://open.spotify.com/playlist/new'
        );
    });

    it('stops a running copy and keeps a link to the playlist being filled', async () => {
        streamCopyMock.mockImplementation((_request, onProgress, signal: AbortSignal) => {
            onProgress({
                phase: 'adding', processed: 100, total: 300, selectedTrackCount: 300,
                addedCount: 100, skippedDuplicateCount: 0,
                targetPlaylist: playlist({id: 'new', name: 'Road Trip', url: 'https://open.spotify.com/playlist/new'}),
            });
            return new Promise((_, reject) => {
                signal.addEventListener('abort', () => reject(new DOMException('Aborted', 'AbortError')));
            });
        });

        render(<PlaylistContributorCopyTool/>);

        fireEvent.change(await screen.findByLabelText('Playlist link or name'), {target: {value: SOURCE_ID}});
        fireEvent.click(screen.getByRole('button', {name: 'Continue'}));
        fireEvent.click(await screen.findByRole('checkbox', {name: /Alice/}));
        fireEvent.click(screen.getByRole('button', {name: 'Continue with 3 songs'}));
        fireEvent.click(await screen.findByRole('button', {name: 'Copy 3 songs'}));

        fireEvent.click(await screen.findByRole('button', {name: 'Stop'}));

        expect(await screen.findByText('Copy stopped')).toBeTruthy();
        expect(screen.getByText(/100 songs were added before you stopped/)).toBeTruthy();
        expect(screen.getByRole('link', {name: /Open in Spotify/})).toHaveProperty(
            'href',
            'https://open.spotify.com/playlist/new'
        );
    });

    it('rejects text that is not a playlist link without calling the API', async () => {
        render(<PlaylistContributorCopyTool/>);

        const input = await screen.findByLabelText('Playlist link or name');
        fireEvent.change(input, {target: {value: 'not a link'}});
        fireEvent.click(screen.getByRole('button', {name: 'Continue'}));

        expect(await screen.findByText(/doesn't look like a Spotify playlist link/)).toBeTruthy();
        expect(previewMock).not.toHaveBeenCalled();
    });

    it('asks to reconnect when Spotify is missing playlist permissions', async () => {
        const {SpotifyUtilsApiError} = await import('../../api/spotify');
        previewMock.mockRejectedValue(new SpotifyUtilsApiError(403, 'Missing permissions'));

        render(<PlaylistContributorCopyTool/>);

        const input = await screen.findByLabelText('Playlist link or name');
        fireEvent.change(input, {target: {value: SOURCE_ID}});
        fireEvent.click(screen.getByRole('button', {name: 'Continue'}));

        await waitFor(() => expect(screen.getByRole('button', {name: 'Reconnect Spotify'})).toBeTruthy());
    });
});
