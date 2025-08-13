import React, {useState, useEffect} from 'react';
import {MenuItem, TextField, Button} from "@mui/material";
import {ListingType} from "../../../types/Listing";
import {getGenres} from "../../../api/metadata";

export interface ListingsFiltersState {
    excludeOwnListings?: boolean;
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

const ListingsFilters: React.FC<ListingsFiltersProps> = ({filters, onApply, onReset}) => {
    const [tempFilters, setTempFilters] = useState<ListingsFiltersState>(filters);
    const [genreOptions, setGenreOptions] = useState<string[]>([]);

    useEffect(() => {
        setTempFilters(filters);
    }, [filters]);

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
    return (
        <div className="listingsFilters">
            <div className="filtersCheckboxes">
                <div>
                    <input
                        type="checkbox"
                        id="matchMusicRole"
                        checked={tempFilters.matchMusicRole || false}
                        onChange={() =>
                            setTempFilters(f => ({
                                ...f,
                                matchMusicRole: !f.matchMusicRole
                            }))
                        }
                    />
                    <label htmlFor="matchMusicRole">Match any role</label>
                </div>
                <div>
                    <input
                        type="checkbox"
                        id="fromLatest"
                        checked={tempFilters.fromLatest || false}
                        onChange={() =>
                            setTempFilters(f => ({
                                ...f,
                                fromLatest: !f.fromLatest
                            }))
                        }
                    />
                    <label htmlFor="fromLatest">From latest</label>
                </div>
            </div>
            <div>
                <TextField
                    select
                    label="Listing type"
                    value={tempFilters.listingType || ''}
                    onChange={(e) =>
                        setTempFilters(f => ({
                            ...f,
                            listingType: e.target.value as ListingType
                        }))
                    }
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
                    value={tempFilters.genreFilter || ''}
                    onChange={(e) =>
                        setTempFilters(f => ({
                            ...f,
                            genreFilter: e.target.value as string
                        }))
                    }
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
                <Button
                    variant="contained"
                    color="primary"
                    onClick={() => onApply(tempFilters)}
                >
                    Apply Filters
                </Button>
                <Button
                    variant="outlined"
                    color="primary"
                    onClick={onReset}
                >
                    Reset Filters
                </Button>
            </div>
        </div>
    );
};

export default ListingsFilters;

