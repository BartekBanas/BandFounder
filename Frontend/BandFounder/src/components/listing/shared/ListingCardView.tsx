import React, {ReactNode, useState} from 'react';
import {Listing} from '../../../types/Listing';
import {Account} from '../../../types/Account';
import ListingCard from './ListingCard';
import ListingCardHeader from './ListingCardHeader';
import ListingCardBody from './ListingCardBody';
import AvailableRolesSection from './AvailableRolesSection';
import OwnerListingElement from './OwnerListingElement';

interface ListingCardViewProps {
    listing: Listing;
    owner?: Account;
    ownerName?: string;
    ownerElement?: ReactNode;
    actions?: ReactNode;
    className?: string;
}

const ListingCardView: React.FC<ListingCardViewProps> = ({
    listing,
    owner,
    ownerName,
    ownerElement,
    actions,
    className,
}) => {
    if (!listing) {
        return null;
    }

    const [loadedOwnerName, setLoadedOwnerName] = useState<string | undefined>(undefined);
    const displayOwner = owner ?? listing.owner;
    const resolvedOwnerElement = ownerElement ?? (
        <OwnerListingElement
            listing={listing}
            owner={displayOwner}
            onOwnerLoaded={setLoadedOwnerName}
        />
    );

    return (
        <ListingCard actions={actions} className={className}>
            <ListingCardHeader
                ownerElement={resolvedOwnerElement}
                title={listing.name}
                type={listing.type}
                genre={listing.genre}
                authorName={ownerName ?? displayOwner?.name ?? loadedOwnerName}
                dateCreated={listing.dateCreated}
            />
            <ListingCardBody description={listing.description ?? ''}/>
            <AvailableRolesSection slots={listing.musicianSlots}/>
        </ListingCard>
    );
};

export default ListingCardView;
