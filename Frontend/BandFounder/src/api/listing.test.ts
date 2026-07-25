import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {getListingFeed} from './listing';

describe('getListingFeed', () => {
    beforeEach(() => {
        document.cookie = 'auth_token=test-token';
    });

    afterEach(() => {
        vi.restoreAllMocks();
        document.cookie = 'auth_token=; Max-Age=0';
    });

    it('disables profile role matching when requested', async () => {
        const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
            new Response(JSON.stringify({listings: []}), {status: 200})
        );

        await getListingFeed({
            disableProfileRoleMatching: true,
            fromLatest: undefined,
            listingType: undefined,
            genre: undefined,
            availableRole: undefined,
            pageNumber: 1,
            pageSize: 5
        });

        const requestUrl = new URL(fetchMock.mock.calls[0][0].toString(), 'http://localhost');
        expect(requestUrl.searchParams.get('MatchRole')).toBe('false');
    });

    it('sends a selected available role', async () => {
        const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
            new Response(JSON.stringify({listings: []}), {status: 200})
        );

        await getListingFeed({
            disableProfileRoleMatching: undefined,
            fromLatest: undefined,
            listingType: undefined,
            genre: undefined,
            availableRole: 'Drummer',
            pageNumber: 1,
            pageSize: 5
        });

        const requestUrl = new URL(fetchMock.mock.calls[0][0].toString(), 'http://localhost');
        expect(requestUrl.searchParams.get('AvailableRole')).toBe('Drummer');
        expect(requestUrl.searchParams.has('MatchRole')).toBe(false);
    });
});
