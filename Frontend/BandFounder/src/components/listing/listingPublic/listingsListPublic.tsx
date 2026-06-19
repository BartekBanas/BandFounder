import React from 'react';
import ListingPublic from './listingPublic';
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import {RingLoader} from "../../common/RingLoader";
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
    const theme = createTheme({
        components: {
            Loader: Loader.extend({
                defaultProps: {
                    loaders: {...Loader.defaultLoaders, ring: RingLoader},
                    type: 'ring',
                },
            }),
        },
    });

    return (
        <div className="listingsList">
            {listings.map((listing, index) => (
                <div ref={listings.length === index + 1 ? lastListingElementRef : null} key={listing.listing.id}>
                    <ListingPublic listing={listing.listing}/>
                </div>
            ))}
            {loading && (
                <div className="listings-list__loader">
                    <MantineThemeProvider theme={theme}>
                        <Loader size={50}/>
                    </MantineThemeProvider>
                </div>
            )}
        </div>
    );
};

export default ListingsListPublic;
