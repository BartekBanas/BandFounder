import {render, screen} from '@testing-library/react';
import {describe, expect, it, vi} from 'vitest';
import {getListing} from '../../../api/listing';
import {Listing, ListingType} from '../../../types/Listing';
import ListingCardView from '../shared/ListingCardView';

vi.mock('../../../api/listing', () => ({
    getListing: vi.fn(),
}));

vi.mock('../shared/OwnerListingElement', () => ({
    default: () => <div data-testid="listing-owner"/>,
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

describe('ListingCardView', () => {
    it('renders the supplied listing without requesting it again', () => {
        render(<ListingCardView listing={listing}/>);

        expect(screen.getByRole('heading', {name: 'Synth Collective'})).toBeInTheDocument();
        expect(screen.getByText('Looking for a drummer')).toBeInTheDocument();
        expect(screen.getByTestId('listing-owner')).toBeInTheDocument();
        expect(getListing).not.toHaveBeenCalled();
    });
});
