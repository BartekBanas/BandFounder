import React, {useEffect, useState, useRef, useCallback} from 'react';
import {useNavigate, useLocation} from 'react-router-dom';
import ListingPublic from './listingPublic';
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import {RingLoader} from "../../common/RingLoader";
import {Autocomplete, MenuItem, TextField, Button} from "@mui/material";
import {getGenres} from "../../../api/metadata";
import {getListingFeed} from "../../../api/listing";
import {ListingFeedFilters, ListingType, ListingWithScore} from "../../../types/Listing";

const ListingsListPublic: React.FC = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const [listings, setListings] = useState<ListingWithScore[]>([]);
    const [loading, setLoading] = useState<boolean>(true);
    const [pageNumber, setPageNumber] = useState<number>(1);
    const [pageSize] = useState<number>(5);
    const [hasMore, setHasMore] = useState<boolean>(true);

    // Main filters
    const [excludeOwnListings, setExcludeOwnListings] = useState<boolean | undefined>(undefined);
    const [matchMusicRole, setMatchMusicRole] = useState<boolean | undefined>(undefined);
    const [fromLatest, setFromLatest] = useState<boolean | undefined>(undefined);
    const [listingType, setListingType] = useState<ListingType | undefined>(undefined);
    const [genreFilter, setGenreFilter] = useState<string | undefined>(undefined);
    const [genreOptions, setGenreOptions] = useState<string[]>([]);

    // Temporary filters for UI
    const [tempExcludeOwnListings, setTempExcludeOwnListings] = useState<boolean | undefined>(undefined);
    const [tempMatchMusicRole, setTempMatchMusicRole] = useState<boolean | undefined>(undefined);
    const [tempFromLatest, setTempFromLatest] = useState<boolean | undefined>(undefined);
    const [tempListingType, setTempListingType] = useState<ListingType | undefined>(undefined);
    const [tempGenreFilter, setTempGenreFilter] = useState<string | undefined>(undefined);

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
        const fetchGenres = async () => {
            try {
                setGenreOptions(await getGenres());
            } catch (error) {
                console.error('Error fetching genres:', error);
            }
        };

        fetchGenres();
    }, []);

    useEffect(() => {
        // Parse URL parameters and apply filters
        const params = new URLSearchParams(location.search);

        const excludeOwn = params.has('excludeOwn') ? params.get('excludeOwn') === 'true' : undefined;
        const matchMusic = params.has('matchMusicRole') ? params.get('matchMusicRole') === 'true' : true;
        const latest = params.has('fromLatest') ? params.get('fromLatest') === 'true' : undefined;
        const type = params.has('listingType') ? (params.get('listingType') as ListingType) : undefined;
        const genre = params.has('genre') ? params.get('genre') ?? undefined : undefined;

        // Update main filters
        setExcludeOwnListings(excludeOwn);
        setMatchMusicRole(matchMusic);
        setFromLatest(latest);
        setListingType(type);
        setGenreFilter(genre);

        // Update temporary filters for UI
        setTempExcludeOwnListings(excludeOwn);
        setTempMatchMusicRole(matchMusic);
        setTempFromLatest(latest);
        setTempListingType(type);
        setTempGenreFilter(genre);
    }, [location.search]);

    useEffect(() => {
        // Fetch listings based on filters and pagination
        const fetchListings = async () => {
            setLoading(true);
            try {
                const filters: ListingFeedFilters = {
                    excludeOwn: excludeOwnListings,
                    matchMusicRole: matchMusicRole,
                    fromLatest: fromLatest,
                    listingType: listingType,
                    genre: genreFilter,
                    pageNumber: pageNumber,
                    pageSize: pageSize
                };
                const listingsFeed = await getListingFeed(filters);
                setListings((prevListings) => [...prevListings, ...listingsFeed.listings]);
                setHasMore(listingsFeed.listings.length > 0);
            } catch (error) {
                console.error('Error getting listings:', error);
            } finally {
                setLoading(false);
            }
        };

        fetchListings();
    }, [excludeOwnListings, matchMusicRole, fromLatest, listingType, genreFilter, pageNumber]);

    const applyFilters = () => {
        // Sync temporary filters with main filters
        setMatchMusicRole(tempMatchMusicRole);
        setFromLatest(tempFromLatest);
        setListingType(tempListingType);
        setGenreFilter(tempGenreFilter);

        // Reset pagination
        setPageNumber(1);
        setListings([]);

        // Update URL parameters
        const params = new URLSearchParams();
        if (tempExcludeOwnListings !== undefined) {
            params.set('excludeOwn', tempExcludeOwnListings.toString());
        }
        if (tempMatchMusicRole !== undefined) {
            params.set('matchMusicRole', tempMatchMusicRole.toString());
        }
        if (tempFromLatest !== undefined) {
            params.set('fromLatest', tempFromLatest.toString());
        }
        if (tempListingType) {
            params.set('listingType', tempListingType);
        }
        if (tempGenreFilter) {
            params.set('genre', tempGenreFilter);
        }

        navigate({search: params.toString()}, {replace: true});
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
            <div className="listingsFilters">
                <div className="filtersCheckboxes">
                    <div>
                        <input
                            type="checkbox"
                            id="matchMusicRole"
                            checked={tempMatchMusicRole || false}
                            onChange={() => setTempMatchMusicRole(!tempMatchMusicRole)}
                        />
                        <label htmlFor="matchMusicRole">Match music role</label>
                    </div>
                    <div>
                        <input
                            type="checkbox"
                            id="fromLatest"
                            checked={tempFromLatest || false}
                            onChange={() => setTempFromLatest(!tempFromLatest)}
                        />
                        <label htmlFor="fromLatest">From latest</label>
                    </div>
                </div>
                <div>
                    <TextField
                        select
                        label="Listing type"
                        value={tempListingType || ''}
                        onChange={(e) => setTempListingType(e.target.value as ListingType)}
                        sx={{width: '100%'}}
                        id="listingType"
                    >
                        <MenuItem value="">All</MenuItem>
                        <MenuItem value="CollaborativeSong">CollaborativeSong</MenuItem>
                        <MenuItem value="Band">Band</MenuItem>
                    </TextField>
                </div>
                <div>
                    <Autocomplete
                        options={genreOptions}
                        value={tempGenreFilter || ''}
                        renderInput={(params) => <TextField {...params} label="Genre" variant="outlined"/>}
                        onChange={(e, value) => setTempGenreFilter(value || undefined)}
                        sx={{width: '100%'}}
                    />
                </div>
                <Button variant="contained" color="primary" onClick={applyFilters}>
                    Apply Filters
                </Button>
            </div>

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