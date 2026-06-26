import React, {ReactNode} from 'react';
import ListingTypeBadge from './ListingTypeBadge';
import './listingShared.css';

interface ListingCardHeaderProps {
    ownerElement: ReactNode;
    title: string;
    type: string;
    genre: string;
}

const ListingCardHeader: React.FC<ListingCardHeaderProps> = ({ownerElement, title, type, genre}) => {
    return (
        <div className="listing-card-header">
            <div className="listing-card-header__top">
                <div className="listing-card-header__owner">{ownerElement}</div>
                <div className="listing-card-header__title-row">
                    <h2 className="listing-card-header__title">{title}</h2>
                    <div className="listing-card-header__tags">
                        <ListingTypeBadge type={type}/>
                        {genre && <span className="listing-genre-tag">{genre}</span>}
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ListingCardHeader;
