import {act, fireEvent, render, screen, waitFor} from '@testing-library/react';
import {ReactNode} from 'react';
import {beforeEach, describe, expect, it, vi} from 'vitest';
import {Listing, ListingType} from '../../../types/Listing';
import ListingsListPrivate from './listingsListPrivate';

const {getListingMock, getUsersListingsMock, getUserIdMock} = vi.hoisted(() => ({
    getListingMock: vi.fn(),
    getUsersListingsMock: vi.fn(),
    getUserIdMock: vi.fn(),
}));

vi.mock('../../../api/listing', () => ({
    getListing: getListingMock,
    getUsersListings: getUsersListingsMock,
}));

vi.mock('../../../hooks/authentication', () => ({
    getUserId: getUserIdMock,
}));

vi.mock('@mantine/core', () => {
    const Loader = Object.assign(() => <div role="progressbar"/>, {
        defaultLoaders: {},
        extend: vi.fn(),
    });

    return {
        createTheme: vi.fn(() => ({})),
        Loader,
        MantineThemeProvider: ({children}: {children: ReactNode}) => <>{children}</>,
    };
});

vi.mock('./listingPrivate', () => ({
    default: ({listing, onListingChanged}: {listing: Listing; onListingChanged: () => void | Promise<void>}) => (
        <button data-testid="private-listing" onClick={onListingChanged}>{listing.name}</button>
    ),
}));

const listing: Listing = {
    id: 'listing-1',
    ownerId: 'owner-1',
    name: 'Synth Collective',
    genre: 'Electronic',
    type: ListingType.Band,
    description: 'Looking for a drummer',
    dateCreated: '2026-07-11T00:00:00.000Z',
    musicianSlots: [],
    owner: {id: 'owner-1', name: 'Owner', email: 'owner@example.com'},
};

describe('ListingsListPrivate', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        getUserIdMock.mockReturnValue('owner-1');
    });

    it('keeps the loading indicator visible until the listings request resolves', async () => {
        let resolveListings!: (listings: Listing[]) => void;
        getUsersListingsMock.mockReturnValue(new Promise<Listing[]>((resolve) => {
            resolveListings = resolve;
        }));

        render(<ListingsListPrivate/>);

        expect(screen.getByTestId('listings-loading')).toBeInTheDocument();
        expect(getUsersListingsMock).toHaveBeenCalledWith('owner-1');

        await act(async () => {
            resolveListings([listing]);
        });

        expect(screen.queryByTestId('listings-loading')).not.toBeInTheDocument();
        expect(screen.getByTestId('private-listing')).toHaveTextContent('Synth Collective');
        expect(getListingMock).not.toHaveBeenCalled();
    });

    it('refetches listings when a child reports a successful mutation', async () => {
        getUsersListingsMock.mockResolvedValue([listing]);

        render(<ListingsListPrivate/>);

        fireEvent.click(await screen.findByTestId('private-listing'));

        await waitFor(() => {
            expect(getUsersListingsMock).toHaveBeenCalledTimes(2);
        });
    });
});
