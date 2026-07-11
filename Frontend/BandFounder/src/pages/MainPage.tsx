import React, {useEffect, useState} from "react";
import UseSpotifyConnected from "../hooks/useSpotifyAccountLinked";
import {AppLoader} from "../components/common/AppLoader";
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
        refetchListings,
    } = useListingsFeed();

    useEffect(() => {
        const checkSpotifyConnection = async () => {
            await UseSpotifyConnected();
            setLoading(false);
        };

        checkSpotifyConnection();
    }, []);

    if (loading) {
        return <div className="App-header">
            <AppLoader size={200}/>
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
                    <ListingTemplate onListingCreated={refetchListings}/>
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
