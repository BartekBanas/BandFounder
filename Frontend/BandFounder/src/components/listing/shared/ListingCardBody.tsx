import React from 'react';
import {formatMessageWithLinks} from '../../common/utils';
import './listingShared.css';

interface ListingCardBodyProps {
    description: string;
}

const ListingCardBody: React.FC<ListingCardBodyProps> = ({description}) => {
    return (
        <div className="listing-card-body">
            <div className="listing-card-body__description custom-scrollbar">
                {formatMessageWithLinks(description)}
            </div>
        </div>
    );
};

export default ListingCardBody;
