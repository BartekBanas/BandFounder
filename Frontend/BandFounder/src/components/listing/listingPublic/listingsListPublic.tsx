import React, {useEffect, useState, useRef, useCallback} from 'react';
import {useNavigate, useLocation} from 'react-router-dom';
import ListingPublic from './listingPublic';
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import {RingLoader} from "../../common/RingLoader";
import {getListingFeed} from "../../../api/listing";
import {ListingFeedFilters, ListingType, ListingWithScore} from "../../../types/Listing";
import ListingsFilters, { ListingsFiltersState } from './ListingsFilters';

const ListingsListPublic: React.FC = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const [listings, setListings] = useState<ListingWithScore[]>([]);
    const [loading, setLoading] = useState<boolean>(true);
    const [pageNumber, setPageNumber] = useState<number>(1);
    const [pageSize] = useState<number>(5);
    const [hasMore, setHasMore] = useState<boolean>(true);

    // Main filters
    const [filters, setFilters] = useState<ListingsFiltersState>({
        matchMusicRole: undefined,
        fromLatest: undefined,
        listingType: undefined,
        genreFilter: undefined,
    });
    const [filtersLoaded, setFiltersLoaded] = useState(false);

    const observer = useRef<IntersectionObserver | null>(null);
    const lastListingElementRef = useCallback((node: any) => {
        if (loading) return;
        if (observer.current) observer.current.disconnect();
        observer.current = new IntersectionObserver((entries) => {
            if (entries[0].isIntersecting && hasMore) {
                setPageNumber((prevPageNumber) => prevPageNumber + 1);
            }
        });
        if (node) observer.current.observe(node);
    }, [loading, hasMore]);

    useEffect(() => {
        // Parse URL parameters and apply filters
        const params = new URLSearchParams(location.search);

        const matchMusic = params.get('matchAnyRole') === 'true';
        const latest = params.has('fromLatest') ? params.get('fromLatest') === 'true' : undefined;
        const type = params.has('listingType') ? (params.get('listingType') as ListingType) : undefined;
        const genre = params.has('genre') ? params.get('genre') ?? undefined : undefined;

        setFilters({
            matchMusicRole: matchMusic,
            fromLatest: latest,
            listingType: type,
            genreFilter: genre,
        });

        setFiltersLoaded(true);
    }, [location.search]);

    useEffect(() => {
        if (!filtersLoaded) return;

        const fetchListings = async () => {
            setLoading(true);
            try {
                const feedFilters: ListingFeedFilters = {
                    matchMusicRole: filters.matchMusicRole,
                    fromLatest: filters.fromLatest,
                    listingType: filters.listingType,
                    genre: filters.genreFilter,
                    pageNumber: pageNumber,
                    pageSize: pageSize
                };
                const listingsFeed = await getListingFeed(feedFilters);
                setListings((prevListings) => [...prevListings, ...listingsFeed.listings]);
                setHasMore(listingsFeed.listings.length > 0);
            } catch (error) {
                console.error('Error getting listings:', error);
            } finally {
                setLoading(false);
            }
        };

        fetchListings();
    }, [filtersLoaded, filters, pageNumber, pageSize]);

    const handleApplyFilters = (newFilters: ListingsFiltersState) => {
        setFilters(newFilters);
        setPageNumber(1);
        setListings([]);

        // Update URL parameters
        const params = new URLSearchParams();
        if (newFilters.matchMusicRole !== undefined) {
            params.set('matchAnyRole', newFilters.matchMusicRole.toString());
        } else {
            params.set('matchAnyRole', 'false');
        }
        if (newFilters.fromLatest !== undefined) {
            params.set('fromLatest', newFilters.fromLatest.toString());
        }
        if (newFilters.listingType) {
            params.set('listingType', newFilters.listingType);
        }
        if (newFilters.genreFilter) {
            params.set('genre', newFilters.genreFilter);
        }

        navigate({search: params.toString()}, {replace: true});
    };

    const handleResetFilters = () => {
        setFilters({
            matchMusicRole: undefined,
            fromLatest: undefined,
            listingType: undefined,
            genreFilter: undefined,
        });
        setPageNumber(1);
        setListings([]);
        const params = new URLSearchParams();
        navigate({ search: params.toString() }, { replace: true });
    };

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
        <>
            <ListingsFilters
                filters={filters}
                onApply={handleApplyFilters}
                onReset={handleResetFilters}
            />

            <div className="listingsList">
                {listings.map((listing, index) => (
                    <div ref={listings.length === index + 1 ? lastListingElementRef : null} key={listing.listing.id}>
                        <ListingPublic listing={listing.listing}/>
                    </div>
                ))}
                {loading && (
                    <div className="App-header">
                        <MantineThemeProvider theme={theme}>
                            <Loader size={50}/>
                        </MantineThemeProvider>
                    </div>
                )}
            </div>
        </>
    );
};

export default ListingsListPublic;

