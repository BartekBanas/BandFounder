import React, {ReactNode} from 'react';
import ListingTypeBadge from './ListingTypeBadge';
import './listingShared.css';

interface ListingCardHeaderProps {
    ownerElement: ReactNode;
    title: string;
    type: string;
    genre: string;
    authorName?: string;
    dateCreated?: string;
}

const formatListingDate = (dateStr: string): string => {
    const date = new Date(dateStr);
    if (Number.isNaN(date.getTime())) {
        return '';
    }

    const day = date.getDate().toString().padStart(2, '0');
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
};

const ListingCardHeader: React.FC<ListingCardHeaderProps> = ({
    ownerElement,
    title,
    type,
    genre,
    authorName,
    dateCreated,
}) => {
    const formattedDate = dateCreated ? formatListingDate(dateCreated) : '';

    return (
        <div className="listing-card-header">
            <div className="listing-card-header__top">
                <div className="listing-card-header__owner">{ownerElement}</div>
                <div className="listing-card-header__content">
                    <div className="listing-card-header__primary-row">
                        <h2 className="listing-card-header__title">{title}</h2>
                        <div className="listing-card-header__tags">
                            <ListingTypeBadge type={type}/>
                            {genre && <span className="listing-genre-tag">{genre}</span>}
                        </div>
                    </div>
                    {(authorName || formattedDate) && (
                        <div className="listing-card-header__meta-row">
                            {authorName && (
                                <span className="listing-card-header__author">{authorName}</span>
                            )}
                            {authorName && formattedDate && (
                                <span className="listing-card-header__separator">·</span>
                            )}
                            {formattedDate && (
                                <span className="listing-card-header__date">{formattedDate}</span>
                            )}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default ListingCardHeader;
