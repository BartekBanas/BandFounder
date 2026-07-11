import React, {useState} from 'react';
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
    const [authorName, setAuthorName] = useState<string>();

    if (!listing) {
        return null;
    }

    return (
        <ListingCard>
            <ListingCardHeader
                ownerElement={
                    <OwnerListingElement listing={listing} onOwnerLoaded={setAuthorName}/>
                }
                title={listing.name}
                type={listing.type}
                genre={listing.genre}
                authorName={authorName}
                dateCreated={listing.dateCreated}
            />
            <ListingCardBody description={listing.description}/>
            <AvailableRolesSection slots={listing.musicianSlots}/>
        </ListingCard>
    );
};

export default ListingPublic;
