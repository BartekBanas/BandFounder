import React from 'react';
import ListingCardView from '../shared/ListingCardView';
import {AppLoader} from '../../common/AppLoader';
import {ListingWithScore} from "../../../types/Listing";
import '../shared/listingShared.css';
import './style.css';

interface ListingsListPublicProps {
    listings: ListingWithScore[];
    loading: boolean;
    lastListingElementRef: (node: HTMLDivElement | null) => void;
}

const ListingsListPublic: React.FC<ListingsListPublicProps> = ({
    listings,
    loading,
    lastListingElementRef,
}) => {
    return (
        <div className="listingsList">
            {listings.map((listing, index) => (
                <div ref={listings.length === index + 1 ? lastListingElementRef : null} key={listing.listing.id}>
                    <ListingCardView listing={listing.listing}/>
                </div>
            ))}
            {loading && (
                <div className="listings-list__loader">
                    <AppLoader size={50}/>
                </div>
            )}
        </div>
    );
};

export default ListingsListPublic;
