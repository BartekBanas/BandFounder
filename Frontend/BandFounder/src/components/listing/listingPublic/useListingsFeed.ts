import {useCallback, useEffect, useRef, useState} from 'react';
import {useNavigate, useLocation} from 'react-router-dom';
import {getListingFeed} from '../../../api/listing';
import {ListingFeedFilters, ListingType, ListingWithScore} from '../../../types/Listing';
import {ListingsFiltersState} from './ListingsFilters';

export function useListingsFeed() {
    const navigate = useNavigate();
    const location = useLocation();
    const [listings, setListings] = useState<ListingWithScore[]>([]);
    const [loading, setLoading] = useState<boolean>(true);
    const [pageNumber, setPageNumber] = useState<number>(1);
    const [pageSize] = useState<number>(5);
    const [hasMore, setHasMore] = useState<boolean>(true);

    const [filters, setFilters] = useState<ListingsFiltersState>({
        matchMusicRole: undefined,
        fromLatest: undefined,
        listingType: undefined,
        genreFilter: undefined,
    });
    const [filtersLoaded, setFiltersLoaded] = useState(false);
    const [refreshKey, setRefreshKey] = useState(0);

    const observer = useRef<IntersectionObserver | null>(null);
    const requestId = useRef(0);
    const lastListingElementRef = useCallback((node: HTMLDivElement | null) => {
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
        const params = new URLSearchParams(location.search);

        const matchMusic = params.get('matchAnyRole') === 'true';
        const latest = params.has('fromLatest') ? params.get('fromLatest') === 'true' : undefined;
        const type = params.has('listingType') ? (params.get('listingType') as ListingType) : undefined;
        const genre = params.has('genre') ? params.get('genre') ?? undefined : undefined;

        const nextFilters = {
            matchMusicRole: matchMusic,
            fromLatest: latest,
            listingType: type,
            genreFilter: genre,
        };

        requestId.current += 1;
        setFilters(nextFilters);
        setListings([]);
        setPageNumber(1);
        setHasMore(true);
        setRefreshKey((currentRefreshKey) => currentRefreshKey + 1);
        setFiltersLoaded(true);
    }, [location.search]);

    useEffect(() => {
        if (!filtersLoaded) return;

        const fetchListings = async () => {
            const currentRequestId = ++requestId.current;
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
                if (currentRequestId !== requestId.current) {
                    return;
                }
                setListings((prevListings) => [...prevListings, ...listingsFeed.listings]);
                setHasMore(listingsFeed.listings.length > 0);
            } catch (error) {
                if (currentRequestId === requestId.current) {
                    console.error('Error getting listings:', error);
                }
            } finally {
                if (currentRequestId === requestId.current) {
                    setLoading(false);
                }
            }
        };

        fetchListings();
    }, [filtersLoaded, filters, pageNumber, pageSize, refreshKey]);

    useEffect(() => () => {
        requestId.current += 1;
    }, []);

    const refetchListings = useCallback(() => {
        requestId.current += 1;
        setListings([]);
        setPageNumber(1);
        setHasMore(true);
        setRefreshKey((currentRefreshKey) => currentRefreshKey + 1);
    }, []);

    const handleApplyFilters = (newFilters: ListingsFiltersState) => {
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
        const params = new URLSearchParams();
        navigate({search: params.toString()}, {replace: true});
    };

    return {
        listings,
        loading,
        filters,
        lastListingElementRef,
        handleApplyFilters,
        handleResetFilters,
        refetchListings,
    };
}
