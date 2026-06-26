import React from 'react';
import ListingCard from '../shared/ListingCard';
import ListingCardHeader from '../shared/ListingCardHeader';
import ListingCardBody from '../shared/ListingCardBody';
import AvailableRolesSection from '../shared/AvailableRolesSection';
import OwnerListingElement from './OwnerListingElement';
import {Listing} from '../../../types/Listing';

interface ListingPublicProps {
    listing: Listing;
}

const ListingPublic: React.FC<ListingPublicProps> = ({listing}) => {
    if (!listing) {
        return null;
    }

    return (
        <ListingCard>
            <ListingCardHeader
                ownerElement={<OwnerListingElement listing={listing}/>}
                title={listing.name}
                type={listing.type}
                genre={listing.genre}
            />
            <ListingCardBody description={listing.description}/>
            <AvailableRolesSection slots={listing.musicianSlots}/>
        </ListingCard>
    );
};

export default ListingPublic;
