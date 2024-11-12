import React, { useEffect, useState } from 'react';
import { getListings } from './api';
import {ListingsFeedDto, ListingWithScore} from "../../../types/ListingFeed";
import getUser from "../../common/frequentlyUsed";
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import {RingLoader} from "../../common/RingLoader";
import ListingPrivate from "./listingPrivate";
import {Listing} from "../../../types/Listing";

const ListingsListPrivate: React.FC = () => {
    const [listings, setListings] = useState<Listing[]>([]);
    const [loading, setLoading] = useState<boolean>(true);

    useEffect(() => {
        const fetchListings = async () => {
            try {
                const data = await getListings();
                setListings(data);
                console.log(data);
            } catch (error) {
                console.error('Error getting listings:', error);
            }
        };

        fetchListings();
        setLoading(false);
    }, []);

    if (loading) {
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
                    <ListingPrivate key={listing.id} listingId={listing.id} />
                ))
            ) : (
                <p>No listings available</p>
            )}
        </div>
    );

};

export default ListingsListPrivate;