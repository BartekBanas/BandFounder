import React, { useEffect, useState } from 'react';
import { getListings } from './api';
import ListingPublic from './listingPublic';
import {ListingWithScore} from "../../../types/ListingFeed";
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import {RingLoader} from "../../common/RingLoader";
import {Autocomplete, InputLabel, MenuItem, Select, TextField} from "@mui/material";
import {getGenres} from "../../../api/metadata";

const ListingsListPublic: React.FC = () => {
    const [listings, setListings] = useState<ListingWithScore[]>([]);
    const [loading, setLoading] = useState<boolean>(true);
    // filters
    const [excludeOwnListings, setExcludeOwnListings] = useState<boolean | undefined>(true);
    const [matchMusicRole, setMatchMusicRole] = useState<boolean | undefined>(undefined);
    const [fromLatest, setFromLatest] = useState<boolean | undefined>(undefined);
    const [listingType, setListingType] = useState< 'Band' | 'CollaborativeSong' | undefined>(undefined);
    const [genreFilter, setGenreFilter] = useState<string | undefined>(undefined);
    const [genreOptions, setGenreOptions] = useState<string[]>([]);

    useEffect(() => {
        const fetchListings = async () => {
            try {
                const data = await getListings(excludeOwnListings, matchMusicRole, fromLatest, listingType, genreFilter);
                setListings(data.listings);
            } catch (error) {
                console.error('Error getting listings:', error);
            } finally {
                setLoading(false);
            }
        };

        const fetchGenres = async () => {
            const genres = await getGenres();
            setGenreOptions(genres);
        }

        fetchListings();
        fetchGenres();
    }, [excludeOwnListings, matchMusicRole, fromLatest, listingType, genreFilter]);

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
        <>
            <div className={'listingsFilters'}>
                <div className={'filtersCheckboxes'}>
                    <div>
                        <input
                            type="checkbox"
                            id="excludeOwnListings"
                            checked={excludeOwnListings}
                            onChange={() => setExcludeOwnListings(!excludeOwnListings)}
                        />
                        <label htmlFor="excludeOwnListings">Exclude own listings</label>
                    </div>
                    <div>
                        <input
                            type="checkbox"
                            id="matchMusicRole"
                            checked={matchMusicRole}
                            onChange={() => setMatchMusicRole(!matchMusicRole)}
                        />
                        <label htmlFor="matchMusicRole">Match music role</label>
                    </div>
                    <div>
                        <input
                            type="checkbox"
                            id="fromLatest"
                            checked={fromLatest}
                            onChange={() => setFromLatest(!fromLatest)}
                        />
                        <label htmlFor="fromLatest">From latest</label>
                    </div>
                </div>
                <div>
                    <TextField
                        select
                        label="Listing type"
                        value={listingType}
                        onChange={(e) => setListingType(e.target.value as 'CollaborativeSong' | 'Band')}
                        sx={{ width: '100%' }}
                        id="listingType"
                    >
                        <MenuItem value={undefined}>All</MenuItem>
                        <MenuItem value={'CollaborativeSong'}>CollaborativeSong</MenuItem>
                        <MenuItem value={'Band'}>Band</MenuItem>
                    </TextField>
                </div>
                <div>
                    <Autocomplete
                        options={genreOptions}
                        renderInput={(params) => <TextField {...params} label="Genre" variant="outlined"/>}
                        onChange={(e, value) => setGenreFilter(value ? value : undefined)}
                        sx={{width: '100%'}}
                    />
                </div>
            </div>

            <div className={'listingsList'}>
                {listings && listings.length > 0 ? (
                    listings.map((listing) => (
                        <ListingPublic key={listing.listing.id} listingId={listing.listing.id}/>
                    ))
                ) : (
                    <p>No listings available</p>
                )}
            </div>
        </>
    );

};

export default ListingsListPublic;