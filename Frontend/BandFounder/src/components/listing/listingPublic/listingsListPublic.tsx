import React, { useEffect, useState } from 'react';
import { getListings } from './api';
import ListingPublic from './listingPublic';
import { Listing } from '../../../types/Listing';
import {log} from "node:util";
import {ListingsFeedDto, ListingWithScore} from "../../../types/ListingFeed";
import getUser from "../../common/frequentlyUsed";
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import {RingLoader} from "../../common/RingLoader";

const ListingsListPublic: React.FC = () => {
    const [listings, setListings] = useState<ListingWithScore[]>([]);
    const [loading, setLoading] = useState<boolean>(true);

    useEffect(() => {
        const fetchListings = async () => {
            try {
                const data = await getListings();
                setListings(data.listings);
            } catch (error) {
                console.error('Error getting listings:', error);
            }
        };

        fetchListings();
        setLoading(false);
        console.log(listings);
    }, []);

    const theme = createTheme({
        components: {
            Loader: Loader.extend({
                defaultProps: {
                    loaders: { ...Loader.defaultLoaders, ring: RingLoader },
                    type: 'ring',
                },
            }),
        },
    });

    if (loading) {
        return <div className="App-header">
            <MantineThemeProvider theme={theme}>
                <Loader size={200} />
            </MantineThemeProvider>
        </div>;
    }

    return (
        <div className={'listingsList'}>
            {listings && listings.length > 0 ? (
                listings.map((listing) => (
                    <ListingPublic key={listing.listing.id} listingId={listing.listing.id} />
                ))
            ) : (
                <p>No listings available</p>
            )}
        </div>
    );

};

export default ListingsListPublic;