import {render, screen} from '@testing-library/react';
import {ReactNode} from 'react';
import {beforeEach, describe, expect, it, vi} from 'vitest';
import {Account} from '../../../types/Account';
import {Listing, ListingType} from '../../../types/Listing';
import ListingsListProfilePublic from './listingsListProfilePublic';

const {getAccountByUsernameMock, getListingMock, getUsersListingsMock} = vi.hoisted(() => ({
    getAccountByUsernameMock: vi.fn(),
    getListingMock: vi.fn(),
    getUsersListingsMock: vi.fn(),
}));

vi.mock('../../../api/account', () => ({
    getAccountByUsername: getAccountByUsernameMock,
}));

vi.mock('../../../api/listing', () => ({
    getListing: getListingMock,
    getUsersListings: getUsersListingsMock,
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

vi.mock('../shared/ListingCardView', () => ({
    default: ({listing, owner}: {listing: Listing; owner: Account}) => (
        <div data-testid="profile-listing">{listing.name} by {owner.name}</div>
    ),
}));

const owner: Account = {
    id: 'owner-1',
    name: 'Owner',
    email: 'owner@example.com',
};

const listings: Listing[] = [
    {
        id: 'listing-1',
        ownerId: owner.id,
        name: 'Synth Collective',
        genre: 'Electronic',
        type: ListingType.Band,
        description: 'Looking for a drummer',
        dateCreated: '2026-07-11T00:00:00.000Z',
        musicianSlots: [],
        owner,
    },
    {
        id: 'listing-2',
        ownerId: owner.id,
        name: 'Ambient Project',
        genre: 'Ambient',
        type: ListingType.CollaborativeSong,
        description: 'Looking for a vocalist',
        dateCreated: '2026-07-11T00:00:00.000Z',
        musicianSlots: [],
        owner,
    },
];

describe('ListingsListProfilePublic', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        getAccountByUsernameMock.mockResolvedValue(owner);
        getUsersListingsMock.mockResolvedValue(listings);
    });

    it('resolves the profile owner once and passes fetched listings to cards without per-card requests', async () => {
        render(<ListingsListProfilePublic profileUsername="owner"/>);

        expect(await screen.findAllByTestId('profile-listing')).toHaveLength(2);
        expect(screen.getByText('Synth Collective by Owner')).toBeInTheDocument();
        expect(screen.getByText('Ambient Project by Owner')).toBeInTheDocument();
        expect(getAccountByUsernameMock).toHaveBeenCalledTimes(1);
        expect(getAccountByUsernameMock).toHaveBeenCalledWith('owner');
        expect(getUsersListingsMock).toHaveBeenCalledTimes(1);
        expect(getUsersListingsMock).toHaveBeenCalledWith(owner.id);
        expect(getListingMock).not.toHaveBeenCalled();
    });
});
