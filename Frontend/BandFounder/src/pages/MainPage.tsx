import React, {useEffect, useState} from "react";
import UseSpotifyConnected from "../hooks/useSpotifyAccountLinked";
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import {RingLoader} from "../components/common/RingLoader";
import ListingsListPublic from "../components/listing/listingPublic/listingsListPublic";
import ListingsFilters from "../components/listing/listingPublic/ListingsFilters";
import {useListingsFeed} from "../components/listing/listingPublic/useListingsFeed";
import ListingTemplate from "../components/listing/listingTemplate/listingTemplate";
import './styles/mainContainer.css'
import '../styles/customScrollbar.css'

export function MainPage() {
    const [loading, setLoading] = useState<boolean>(true);
    const {
        listings,
        loading: listingsLoading,
        filters,
        lastListingElementRef,
        handleApplyFilters,
        handleResetFilters,
    } = useListingsFeed();

    useEffect(() => {
        const checkSpotifyConnection = async () => {
            await UseSpotifyConnected();
            setLoading(false);
        };

        checkSpotifyConnection();
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
        <div id="mainContainer" className="custom-scrollbar">
            <div className="feed-layout">
                <aside className="feed-sidebar">
                    <ListingsFilters
                        filters={filters}
                        onApply={handleApplyFilters}
                        onReset={handleResetFilters}
                    />
                </aside>
                <main className="feed-main">
                    <ListingTemplate/>
                    <ListingsListPublic
                        listings={listings}
                        loading={listingsLoading}
                        lastListingElementRef={lastListingElementRef}
                    />
                </main>
            </div>
        </div>
    );
}
