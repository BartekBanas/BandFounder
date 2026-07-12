import React, {useCallback, useEffect, useState} from 'react';
import {AppLoader} from '../../common/AppLoader';
import ListingPrivate from "./listingPrivate";
import {Listing} from "../../../types/Listing";
import {getUsersListings} from "../../../api/listing";
import {getUserId} from "../../../hooks/authentication";
import '../shared/listingShared.css';

const ListingsListPrivate: React.FC = () => {
    const [listings, setListings] = useState<Listing[]>([]);
    const [loading, setLoading] = useState<boolean>(true);

    const fetchListings = useCallback(async () => {
        setLoading(true);
        try {
            setListings(await getUsersListings(getUserId()));
        } catch (error) {
            console.error('Error getting listings:', error);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchListings();
    }, [fetchListings]);

    if (loading) {
        return <div className="App-header" data-testid="listings-loading">
            <AppLoader size={200}/>
        </div>;
    }

    return (
        <div className={'listingsList'}>
            {listings && listings.length > 0 ? (
                listings.map((listing) => (
                    <ListingPrivate key={listing.id} listing={listing} onListingChanged={fetchListings}/>
                ))
            ) : (
                <p>No listings available</p>
            )}
        </div>
    );

};

export default ListingsListPrivate;