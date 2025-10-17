import React, {useState, useEffect} from 'react';
import {TextField, MenuItem, Button} from "@mui/material";
import {ListingType} from "../../../types/Listing";
import {getGenres} from "../../../api/metadata";

export interface ListingsFiltersState {
    matchMusicRole?: boolean;
    fromLatest?: boolean;
    listingType?: ListingType;
    genreFilter?: string;
}

interface ListingsFiltersProps {
    filters: ListingsFiltersState;
    onApply: (filters: ListingsFiltersState) => void;
    onReset: () => void;
}

const ListingsFilters: React.FC<ListingsFiltersProps> = ({filters, onApply, onReset}: ListingsFiltersProps) => {
    // Local temp state for editing filters before applying
    const [tempMatchMusicRole, setTempMatchMusicRole] = useState<boolean | undefined>(filters.matchMusicRole);
    const [tempFromLatest, setTempFromLatest] = useState<boolean | undefined>(filters.fromLatest);
    const [tempListingType, setTempListingType] = useState<ListingType | undefined>(filters.listingType);
    const [tempGenreFilter, setTempGenreFilter] = useState<string | undefined>(filters.genreFilter);

    useEffect(() => {
        setTempMatchMusicRole(filters.matchMusicRole);
        setTempFromLatest(filters.fromLatest);
        setTempListingType(filters.listingType);
        setTempGenreFilter(filters.genreFilter);
    }, [filters]);

    const [genreOptions, setGenreOptions] = useState<string[]>([]);
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

    const handleApply = () => {
        onApply({
            matchMusicRole: tempMatchMusicRole,
            fromLatest: tempFromLatest,
            listingType: tempListingType,
            genreFilter: tempGenreFilter,
        });
    };

    const handleReset = () => {
        setTempMatchMusicRole(undefined);
        setTempFromLatest(undefined);
        setTempListingType(undefined);
        setTempGenreFilter(undefined);
        onReset();
    };

    return (
        <div className="listingsFilters">
            <div className="filtersCheckboxes">
                <div>
                    <input
                        type="checkbox"
                        id="matchMusicRole"
                        checked={tempMatchMusicRole || false}
                        onChange={() => setTempMatchMusicRole(!tempMatchMusicRole)}
                    />
                    <label htmlFor="matchMusicRole">Match any role</label>
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
                    sx={{width: '100%'}}
                    SelectProps={{
                        MenuProps: {
                            PaperProps: {
                                style: {
                                    maxHeight: '400px',
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
            <div style={{display: 'flex', gap: '1rem'}} id={'filtersButtons'}>
                <Button variant="contained" color="primary" onClick={handleApply}>
                    Apply Filters
                </Button>
                <Button variant="outlined" color="primary" onClick={handleReset}>
                    Reset Filters
                </Button>
            </div>
        </div>
    );
};

export default ListingsFilters;