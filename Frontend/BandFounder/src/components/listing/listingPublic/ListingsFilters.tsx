import React, {useState, useEffect} from 'react';
import {Autocomplete, TextField, MenuItem} from "@mui/material";
import {ListingType} from "../../../types/Listing";
import {getGenres, getMusicianRoles} from "../../../api/metadata";
import {ANY_ROLE_OPTION} from './roleFilter';
import './style.css';

const MAX_GENRE_FILTER_LENGTH = 50;

export interface ListingsFiltersState {
    fromLatest?: boolean;
    listingType?: ListingType;
    genreFilter?: string;
    availableRole?: string;
}

interface ListingsFiltersProps {
    filters: ListingsFiltersState;
    onApply: (filters: ListingsFiltersState) => void;
    onReset: () => void;
}

const ListingsFilters: React.FC<ListingsFiltersProps> = ({filters, onApply, onReset}: ListingsFiltersProps) => {
    const [tempFromLatest, setTempFromLatest] = useState<boolean | undefined>(filters.fromLatest);
    const [tempListingType, setTempListingType] = useState<ListingType | undefined>(filters.listingType);
    const [tempGenreFilter, setTempGenreFilter] = useState<string | undefined>(filters.genreFilter);
    const [tempAvailableRole, setTempAvailableRole] = useState<string | undefined>(filters.availableRole);

    useEffect(() => {
        setTempFromLatest(filters.fromLatest);
        setTempListingType(filters.listingType);
        setTempGenreFilter(filters.genreFilter);
        setTempAvailableRole(filters.availableRole);
    }, [filters]);

    const [genreOptions, setGenreOptions] = useState<string[]>([]);
    const [roleOptions, setRoleOptions] = useState<string[]>([ANY_ROLE_OPTION]);
    useEffect(() => {
        const fetchGenres = async () => {
            try {
                setGenreOptions(await getGenres());
            } catch (error) {
                console.error('Error fetching genres:', error);
            }
        };
        const fetchRoles = async () => {
            try {
                const roles = await getMusicianRoles();
                const specificRoles = roles.filter((role) => role !== ANY_ROLE_OPTION);
                setRoleOptions([ANY_ROLE_OPTION, ...specificRoles]);
            } catch (error) {
                console.error('Error fetching roles:', error);
            }
        };
        fetchGenres();
        fetchRoles();
    }, []);

    const handleApply = () => {
        const genreFilter = tempGenreFilter?.trim() || undefined;
        onApply({
            fromLatest: tempFromLatest,
            listingType: tempListingType,
            genreFilter,
            availableRole: tempAvailableRole || undefined,
        });
    };

    const handleReset = () => {
        setTempFromLatest(undefined);
        setTempListingType(undefined);
        setTempGenreFilter(undefined);
        setTempAvailableRole(undefined);
        onReset();
    };

    return (
        <div className="listings-filters">
            <h2 className="listings-filters__title">Filter Listings</h2>
            <div className="listings-filters__checkboxes">
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
            <div className="listings-filters__field">
                <Autocomplete
                    fullWidth
                    options={roleOptions}
                    value={tempAvailableRole ?? ''}
                    onChange={(_, value) => setTempAvailableRole(value || undefined)}
                    getOptionLabel={(option) => option === ANY_ROLE_OPTION ? 'All roles' : option}
                    renderInput={(params) => (
                        <TextField
                            {...params}
                            label="Role"
                            size="small"
                            placeholder="e.g. Drummer"
                            helperText="Leave empty to match your profile roles. Select All roles to disable role filtering."
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
