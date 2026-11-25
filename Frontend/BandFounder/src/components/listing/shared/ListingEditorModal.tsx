import React from 'react';
import {Dialog, MenuItem, Select, ThemeProvider} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import PersonOutlineIcon from '@mui/icons-material/PersonOutline';
import {designTokens, muiDarkTheme} from '../../../styles/muiDarkTheme';
import {useTriggerMatchedMenu} from '../../common/useTriggerMatchedMenu';
import './listingModal.css';

export interface EditorMusicianSlot {
    id?: string;
    role: string;
    status: string;
}

export interface ListingEditorModalProps {
    open: boolean;
    title: string;
    submitLabel: string;
    onClose: () => void;
    onSubmit: () => void;
    listingName: string;
    onListingNameChange: (value: string) => void;
    listingType: string;
    onListingTypeChange: (value: string) => void;
    listingGenre: string;
    onListingGenreChange: (value: string) => void;
    listingDescription: string;
    onListingDescriptionChange: (value: string) => void;
    musicianSlots: EditorMusicianSlot[];
    onAddRole: () => void;
    onDeleteRole: (slotId: string) => void;
    onEditMusicianRole: (slotId: string, role: string) => void;
    onEditSlotStatus: (slotId: string, status: string) => void;
    genres: string[];
    roles: string[];
}

const SlotStatusSelect: React.FC<{value: string; onChange: (status: string) => void}> = ({value, onChange}) => {
    const menu = useTriggerMatchedMenu();

    return (
        <Select
            className="listing-editor-modal__select"
            size="small"
            value={value}
            inputProps={{'aria-label': 'Slot status'}}
            onChange={(e) => onChange(e.target.value)}
            {...menu}
        >
            <MenuItem value="Available">Available</MenuItem>
            <MenuItem value="Filled">Filled</MenuItem>
        </Select>
    );
};

const ListingEditorModal: React.FC<ListingEditorModalProps> = ({
    open,
    title,
    submitLabel,
    onClose,
    onSubmit,
    listingName,
    onListingNameChange,
    listingType,
    onListingTypeChange,
    listingGenre,
    onListingGenreChange,
    listingDescription,
    onListingDescriptionChange,
    musicianSlots,
    onAddRole,
    onDeleteRole,
    onEditMusicianRole,
    onEditSlotStatus,
    genres,
    roles,
}) => {
    const typeMenu = useTriggerMatchedMenu();
    const getSlotKey = (slot: EditorMusicianSlot, index: number) => slot.id ?? `slot-${index}`;

    return (
        <ThemeProvider theme={muiDarkTheme}>
            <Dialog
                open={open}
                onClose={onClose}
                className="listing-editor-modal"
                maxWidth={false}
                slotProps={{
                    paper: {
                        sx: {
                            bgcolor: designTokens.cardSurface,
                            backgroundImage: 'none',
                        },
                    },
                    backdrop: {
                        sx: {
                            backgroundColor: designTokens.overlayBackdrop,
                            backdropFilter: 'blur(6px)',
                        },
                    },
                }}
            >
            <header className="listing-editor-modal__header">
                <h2 className="listing-editor-modal__title">{title}</h2>
                <button type="button" className="listing-editor-modal__close" onClick={onClose}
                        aria-label="Close">
                    <CloseIcon fontSize="small"/>
                </button>
            </header>

            <div className="listing-editor-modal__body custom-scrollbar">
                <div className="listing-editor-modal__row listing-editor-modal__row--two-col">
                    <div className="listing-editor-modal__field listing-editor-modal__field--full">
                        <label className="listing-editor-modal__label" htmlFor="listing-title">Title</label>
                        <input
                            id="listing-title"
                            className="listing-editor-modal__input"
                            type="text"
                            value={listingName}
                            onChange={(e) => onListingNameChange(e.target.value)}
                            maxLength={35}
                            placeholder="Give your project a name"
                        />
                        <span className="listing-editor-modal__hint">{listingName.length}/35</span>
                    </div>

                    <div className="listing-editor-modal__field">
                        <label className="listing-editor-modal__label" id="listing-type-label">Type</label>
                        <Select
                            id="listing-type"
                            labelId="listing-type-label"
                            className="listing-editor-modal__select"
                            size="small"
                            value={listingType}
                            onChange={(e) => onListingTypeChange(e.target.value)}
                            {...typeMenu}
                        >
                            <MenuItem value="CollaborativeSong">Song</MenuItem>
                            <MenuItem value="Band">Band</MenuItem>
                        </Select>
                    </div>

                    <div className="listing-editor-modal__field">
                        <label className="listing-editor-modal__label" htmlFor="listing-genre">Genre</label>
                        <input
                            id="listing-genre"
                            className="listing-editor-modal__input"
                            type="text"
                            list="listing-genre-options"
                            value={listingGenre}
                            onChange={(e) => onListingGenreChange(e.target.value)}
                            placeholder="e.g. Dream Pop"
                        />
                        <datalist id="listing-genre-options">
                            {genres.map((genre) => (
                                <option key={genre} value={genre}/>
                            ))}
                        </datalist>
                    </div>

                    <div className="listing-editor-modal__field listing-editor-modal__field--full">
                        <label className="listing-editor-modal__label" htmlFor="listing-description">Description</label>
                        <textarea
                            id="listing-description"
                            className="listing-editor-modal__textarea"
                            value={listingDescription}
                            onChange={(e) => onListingDescriptionChange(e.target.value)}
                            maxLength={220}
                            placeholder="Describe your project, goals, and what you're looking for..."
                            rows={4}
                        />
                        <span className="listing-editor-modal__hint">{listingDescription.length}/220</span>
                    </div>
                </div>

                <section className="listing-editor-modal__roles-section">
                    <div className="listing-editor-modal__roles-header">
                        <h3 className="listing-editor-modal__roles-label">Musician Roles</h3>
                        <button type="button" className="listing-editor-modal__add-role" onClick={onAddRole}>
                            + Add role
                        </button>
                    </div>

                    {musicianSlots.length === 0 ? (
                        <p className="listing-editor-modal__empty-roles">
                            No roles added yet. Add roles for the musicians you need.
                        </p>
                    ) : (
                        <div className="listing-editor-modal__roles-list">
                            {musicianSlots.map((slot, index) => {
                                const slotKey = getSlotKey(slot, index);
                                const isFilled = slot.status === 'Filled';

                                return (
                                    <div
                                        key={slotKey}
                                        className={`listing-editor-modal__role-card ${isFilled ? 'listing-editor-modal__role-card--filled' : ''}`}
                                    >
                                        <div className="listing-editor-modal__role-card-header">
                                            <PersonOutlineIcon sx={{fontSize: 20, color: 'var(--text-muted)'}}/>
                                            <button
                                                type="button"
                                                className="listing-editor-modal__role-delete"
                                                onClick={() => slot.id && onDeleteRole(slot.id)}
                                                aria-label="Remove role"
                                            >
                                                <CloseIcon sx={{fontSize: 16}}/>
                                            </button>
                                        </div>
                                        <input
                                            className="listing-editor-modal__input"
                                            type="text"
                                            list={`role-options-${slotKey}`}
                                            value={slot.role}
                                            onChange={(e) => slot.id && onEditMusicianRole(slot.id, e.target.value)}
                                            placeholder="Role name"
                                        />
                                        <datalist id={`role-options-${slotKey}`}>
                                            {roles.map((role) => (
                                                <option key={role} value={role}/>
                                            ))}
                                        </datalist>
                                        <SlotStatusSelect
                                            value={slot.status}
                                            onChange={(status) => slot.id && onEditSlotStatus(slot.id, status)}
                                        />
                                    </div>
                                );
                            })}
                        </div>
                    )}
                </section>
            </div>

            <footer className="listing-editor-modal__footer">
                <button type="button" className="listing-editor-modal__cancel" onClick={onClose}>
                    Cancel
                </button>
                <button type="button" className="listing-editor-modal__submit" onClick={onSubmit}>
                    {submitLabel}
                </button>
            </footer>
            </Dialog>
        </ThemeProvider>
    );
};

export default ListingEditorModal;
