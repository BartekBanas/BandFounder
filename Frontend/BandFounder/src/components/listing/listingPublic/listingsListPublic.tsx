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
    const [genreOptions, setGenreOptions] = useState<string[]>([]);
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
    const [filtersLoaded, setFiltersLoaded] = useState(false);

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

        setFiltersLoaded(true);
    }, [location.search]);

    useEffect(() => {
        if (!filtersLoaded) return;

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
    }, [filtersLoaded, excludeOwnListings, matchMusicRole, fromLatest, listingType, genreFilter, pageNumber, pageSize]);

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

    const onReset = () => {
        // Reset temporary filters to default values
        setTempExcludeOwnListings(undefined);
        setTempMatchMusicRole(undefined);
        setTempFromLatest(undefined);
        setTempListingType(undefined);
        setTempGenreFilter(undefined);

        // Reset main filters to default values
        setExcludeOwnListings(undefined);
        setMatchMusicRole(undefined);
        setFromLatest(undefined);
        setListingType(undefined);
        setGenreFilter(undefined);

        // Reset pagination
        setPageNumber(1);
        setListings([]);

        // Clear URL parameters
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
                    <TextField
                        select
                        label="Genre"
                        value={tempGenreFilter || ''}
                        onChange={(e) => setTempGenreFilter(e.target.value as string)}
                        sx={{ width: '100%' }}
                        SelectProps={{
                            MenuProps: {
                                PaperProps: {
                                    style: {
                                        maxHeight: '400px', // Set the maximum height here
                                    },
                                },
                            },
                        }}
                    >
                        <MenuItem value="">
                            <em>None</em>
                        </MenuItem>
                        {genreOptions.map((genre) => (
                            <MenuItem key={genre} value={genre}>
                                {genre}
                            </MenuItem>
                        ))}
                    </TextField>
                </div>
                <div style={{display: 'flex', gap: '1rem'}}>
                    <Button variant="contained" color="primary" onClick={applyFilters}>
                        Apply Filters
                    </Button>
                    <Button variant="outlined" color="primary" onClick={onReset}>
                        Reset Filters
                    </Button>
                </div>
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