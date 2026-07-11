import React, {useState, useEffect} from 'react';
import {Autocomplete, TextField, MenuItem} from "@mui/material";
import {ListingType} from "../../../types/Listing";
import {getGenres} from "../../../api/metadata";
import './style.css';

const MAX_GENRE_FILTER_LENGTH = 50;

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
        const genreFilter = tempGenreFilter?.trim() || undefined;
        onApply({
            matchMusicRole: tempMatchMusicRole,
            fromLatest: tempFromLatest,
            listingType: tempListingType,
            genreFilter,
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
        <div className="listings-filters">
            <h2 className="listings-filters__title">Filter Listings</h2>
            <div className="listings-filters__checkboxes">
                <div className="listings-filters__checkbox-row">
                    <input
                        type="checkbox"
                        id="matchMusicRole"
                        checked={tempMatchMusicRole || false}
                        onChange={() => setTempMatchMusicRole(!tempMatchMusicRole)}
                    />
                    <label htmlFor="matchMusicRole">Match any role</label>
                </div>
                <div className="listings-filters__checkbox-row">
                    <input
                        type="checkbox"
                        id="fromLatest"
                        checked={tempFromLatest || false}
                        onChange={() => setTempFromLatest(!tempFromLatest)}
                    />
                    <label htmlFor="fromLatest">From latest</label>
                </div>
            </div>
            <div className="listings-filters__field">
                <TextField
                    select
                    label="Listing type"
                    value={tempListingType || ''}
                    onChange={(e) => setTempListingType(e.target.value as ListingType)}
                    fullWidth
                    size="small"
                    id="listingType"
                >
                    <MenuItem value="">All</MenuItem>
                    <MenuItem value="CollaborativeSong">Song</MenuItem>
                    <MenuItem value="Band">Band</MenuItem>
                </TextField>
            </div>
            <div className="listings-filters__field">
                <Autocomplete
                    freeSolo
                    fullWidth
                    options={genreOptions}
                    value={tempGenreFilter ?? ''}
                    onInputChange={(_, value) => setTempGenreFilter(value || undefined)}
                    renderInput={(params) => (
                        <TextField
                            {...params}
                            label="Genre"
                            size="small"
                            placeholder="e.g. Dream Pop"
                            slotProps={{
                                htmlInput: {
                                    ...params.inputProps,
                                    maxLength: MAX_GENRE_FILTER_LENGTH,
                                },
                            }}
                        />
                    )}
                />
            </div>
            <div className="listings-filters__actions">
                <button type="button" className="listings-filters__apply" onClick={handleApply}>
                    Apply Filters
                </button>
                <button type="button" className="listings-filters__reset" onClick={handleReset}>
                    Reset
                </button>
            </div>
        </div>
    );
};

export default ListingsFilters;
