import React, {useEffect, useState} from 'react';
import {AppLoader} from '../../common/AppLoader';
import ListingCardView from '../shared/ListingCardView';
import {Listing} from "../../../types/Listing";
import {getUsersListings} from "../../../api/listing";
import {getAccountByUsername} from "../../../api/account";
import {Account} from "../../../types/Account";
import '../shared/listingShared.css';

interface ListingsListProfilePublicProps {
    profileUsername: string;
}

const ListingsListProfilePublic: React.FC<ListingsListProfilePublicProps> = ({profileUsername}) => {
    const [listings, setListings] = useState<Listing[]>([]);
    const [owner, setOwner] = useState<Account>();
    const [loading, setLoading] = useState<boolean>(true);

    useEffect(() => {
        const fetchListings = async () => {
            try {
                const user = await getAccountByUsername(profileUsername);
                const data = await getUsersListings(user.id);
                setOwner(user);
                setListings(data);
            } catch (error) {
                console.error('Error getting listings:', error);
            }
            setLoading(false);
        };

        fetchListings();
    }, [profileUsername]);

    if (loading) {
        return <div className="App-header">
            <AppLoader size={200}/>
        </div>;
    }

    return (
        <div className={'listingsList'}>
            {listings && listings.length > 0 ? (
                listings.map((listing) => (
                    <ListingCardView key={listing.id} listing={listing} owner={owner}/>
                ))
            ) : (
                <p>No listings available</p>
            )}
        </div>
    );
};

export default ListingsListProfilePublic;