import React from 'react';
import {ListingType} from '../../../types/Listing';
import './listingShared.css';

interface ListingTypeBadgeProps {
    type: ListingType | string;
}

const ListingTypeBadge: React.FC<ListingTypeBadgeProps> = ({type}) => {
    const isSong = type === 'CollaborativeSong' || type === ListingType.CollaborativeSong;
    const label = isSong ? 'Song Collaboration' : 'Band';

    return (
        <span className={`listing-type-badge ${isSong ? 'listing-type-badge--song' : 'listing-type-badge--band'}`}>
            {label}
        </span>
    );
};

export default ListingTypeBadge;
