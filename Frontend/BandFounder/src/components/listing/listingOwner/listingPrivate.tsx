import React, { useEffect, useState } from 'react';
import {getGenres, getListing, getMusicianRoles, updateListing} from './api';
import defaultProfileImage from '../../../assets/defaultProfileImage.jpg';
import getUser from '../../common/frequentlyUsed';
import './style.css';
import './listingCreator.css';
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import { RingLoader } from "../../common/RingLoader";
import {
    Button,
    Modal,
    Box,
    TextField,
    Select,
    InputLabel,
    MenuItem,
    SelectChangeEvent,
    IconButton
} from "@mui/material";
import {API_URL} from "../../../config";
import {ListingUpdated} from "../../../types/ListingUpdated";
import CloseIcon from "@mui/icons-material/Close";

interface ListingPrivateProps {
    listingId: string;
}

const ListingPrivate: React.FC<ListingPrivateProps> = ({ listingId }) => {
    const [listing, setListing] = useState<any>(null);
    const [open, setOpen] = useState(false);
    const [listingType, setListingType] = useState<string>('');
    const [listingGenre, setListingGenre] = useState<string>('');
    const [listingDescription, setListingDescription] = useState<string>('');
    const [listingMusicianSlots, setListingMusicianSlots] = useState<any>([]);
    const [listingName, setListingName] = useState<string>('');
    const [genres, setGenres] = useState<string[]>([]);
    const [roles, setRoles] = useState<string[]>([]);

    useEffect(() => {
        const fetchListing = async () => {
            const data = await getListing(listingId);
            if (data) {
                const owner = await getUser(data.ownerId);
                setListing({ ...data, owner });
                setListingType(data.type);
                setListingGenre(data.genre);
                setListingDescription(data.description);
                setListingMusicianSlots(data.musicianSlots);
                setListingName(data.name);
            }
        };

        const fetchGenres = async () => {
            const genres = await getGenres();
            if (genres) {
                setGenres(genres);
            }
        }

        const fetchRoles = async () => {
            const roles = await getMusicianRoles();
            if (roles) {
                setRoles(roles);
            }
        }

        fetchListing();
        fetchGenres();
        fetchRoles();
    }, [listingId]);

    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);

    const handleNameChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setListingName(event.target.value);
    }

    const handleEditType = () => {
        setListingType(prevType => prevType === 'CollaborativeSong' ? 'Band' : 'CollaborativeSong');
    };

    const handleGenreSelectChange = (event: SelectChangeEvent<string>) => {
        setListingGenre(event.target.value);
    };

    const handleEditSlot = (slotId: string) => {
        const newSlots = listingMusicianSlots.map((slot: any) => {
            if (slot.id === slotId) {
                return { ...slot, status: slot.status === 'Available' ? 'Filled' : 'Available' };
            }
            return slot;
        });
        setListingMusicianSlots(newSlots);
    }

    const handleEditMusicianRole = (slotId: string, role: string) => {
        const newSlots = listingMusicianSlots.map((slot: any) => {
            if (slot.id === slotId) {
                // console.log('role', role);
                if(role) {
                    return {...slot, role};
                }
                else{
                    return {...slot, role: 'Any'};
                }
            }
            return slot;
        });
        setListingMusicianSlots(newSlots);
    }

    const handleAddNewRole = () => {
        const newSlot = {
            id: Math.random().toString(36).substr(2, 9),
            role: 'Any',
            status: 'Available',
        };
        setListingMusicianSlots([...listingMusicianSlots, newSlot]);
    }

    const handleUpdateListing = async () => {
        try{
            const updatedListing:ListingUpdated = {
                name: listingName,
                type: listingType,
                genre: listingGenre,
                description: listingDescription,
                musicianSlots: listingMusicianSlots,
            }
            await updateListing(updatedListing,listingId);
            window.location.reload();
        }
        catch (e) {
            console.log(e);
        }
    }

    const handleDeleteRole = (slotId: string) => {
        const newSlots = listingMusicianSlots.filter((slot: any) => slot.id !== slotId);
        setListingMusicianSlots(newSlots);
    }

    if (!listing) {
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
        return (
            <div className="App-header">
                <MantineThemeProvider theme={theme}>
                    <Loader size={200} />
                </MantineThemeProvider>
            </div>
        );
    }

    return (
        <div className={'listing'}>
            <div className={'editButton'}>
                <Button variant={'contained'} color={'error'} onClick={handleOpen}>Edit</Button>
            </div>
            <div className={'listingHeader'}>
                <div className={'ownerListingElements'}>
                    <img src={defaultProfileImage} alt="Default Profile" />
                    <p>{listing?.owner?.name}</p>
                </div>
                <div className={'listingTitle'}>
                    <p>{listing?.name}</p>
                </div>
                <div className={'listingType'}>
                    <div className={`listingType-${listing.type}`} onClick={handleEditType}>
                        <p>{listing.type === 'CollaborativeSong' ? 'Song' : listing.type}</p>
                    </div>
                    <p>{listing?.genre}</p>
                </div>
            </div>
            <div className={'listingBody'}>
                <p>{listing?.description}</p>
            </div>
            <div className={'listingFooter'}>
                {listing?.musicianSlots.map((slot: any) => (
                    <div key={slot.id} className={`listingRole ${slot.status === 'Available' ? 'status-available' : 'status-filled'}`}>
                        <img src={defaultProfileImage} alt="Default Profile" />
                        <p>Role: {slot.role}</p>
                        <p>Status: {slot.status}</p>
                    </div>
                ))}
            </div>

            <Modal open={open} onClose={handleClose}>
                <Box sx={{ ...modalStyle }} onClick={(e) => e.stopPropagation()}>
                    <div id={'saveButtonEditListing'}>
                        <Button variant={'contained'} color={'success'} onClick={handleUpdateListing}>Post</Button>
                    </div>
                    <div className={'listing-editor'}>
                        <div className={'editorHeader'}>
                            <TextField
                                label={'Title'}
                                value={listingName}
                                onChange={handleNameChange}
                                variant="filled"
                                color={'success'}
                                style={{ minWidth: '50%' }}
                            />
                            <div>
                                <InputLabel id="typeSelectLabel">Type</InputLabel>
                                <Select
                                    labelId="typeSelectLabel"
                                    id="typeSelectLabel"
                                    value={listingType}
                                    label="Type"
                                    onChange={handleEditType}
                                >
                                    <MenuItem value={'CollaborativeSong'}>Song</MenuItem>
                                    <MenuItem value={'Band'}>Band</MenuItem>
                                </Select>
                            </div>
                        </div>
                        <div className={'editorUnderHeader'}>
                            <div>
                                <InputLabel id="genreSelectLabel">Genre</InputLabel>
                                <Select
                                    labelId="genreSelectLabel"
                                    id="genreSelectLabel"
                                    value={listingGenre}
                                    label="Genre"
                                    onChange={handleGenreSelectChange}
                                >
                                    {genres.map((genre, index) => (
                                        <MenuItem key={index} value={genre}>{genre}</MenuItem>
                                    ))}
                                </Select>
                            </div>
                        </div>
                        <div className={'editorBody'}>
                            <TextField
                                label="Description"
                                value={listingDescription}
                                onChange={(event) => setListingDescription(event.target.value)}
                                variant="filled"
                                color="success"
                                style={{ minWidth: '70%' }}
                                multiline
                                rows={2}  // Adjust the number of rows as needed
                            />
                        </div>
                        <div className={'editorFooter'}>
                            {listingMusicianSlots.map((slot: any) => (
                                <div key={slot.id}
                                     className={`listingRoleEdited ${slot.status === 'Available' ? 'status-available' : 'status-filled'}`}>
                                    <div className={'kindaHeader'}>
                                        <div>
                                            <img src={defaultProfileImage} alt="Default Profile" />
                                        </div>
                                        <IconButton
                                            aria-label="delete"
                                            size="small"
                                            onClick={() => handleDeleteRole(slot.id)}
                                            style={{}}
                                        >
                                            <CloseIcon fontSize="small" />
                                        </IconButton>
                                    </div>
                                    <div className={'underEditorFooter'}>
                                        <InputLabel id="roleSelectLabel"
                                                    style={{ fontSize: '12px', maxHeight: '35px' }}>Role</InputLabel>
                                        <Select
                                            labelId="roleSelectLabel"
                                            id="roleSelectLabel"
                                            value={slot.role ? slot.role : 'Any'}
                                            label="Role"
                                            onChange={(event) => handleEditMusicianRole(slot.id, event.target.value)}
                                            style={{ fontSize: '12px', maxHeight: '35px' }}
                                        >
                                            {roles.map((role, index) => (
                                                <MenuItem key={index} value={role}>{role}</MenuItem>
                                            ))}
                                        </Select>
                                    </div>
                                    <div className={'underEditorFooter'}>
                                        <InputLabel id="statusSelectLabel"
                                                    style={{ fontSize: '12px', maxHeight: '35px' }}>Status</InputLabel>
                                        <Select
                                            labelId="statusSelectLabel"
                                            id="statusSelectLabel"
                                            value={slot.status}
                                            label="Status"
                                            onChange={() => handleEditSlot(slot.id)}
                                            style={{ fontSize: '12px', maxHeight: '35px' }}
                                        >
                                            <MenuItem value={'Available'}>Available</MenuItem>
                                            <MenuItem value={'Filled'}>Filled</MenuItem>
                                        </Select>
                                    </div>
                                </div>
                            ))}

                            <div id={'addRolesButton'}>
                                <Button variant={'contained'} color={'success'} onClick={handleAddNewRole}>Add new
                                    role</Button>
                            </div>
                        </div>
                    </div>
                </Box>
            </Modal>
        </div>
    );
};

const modalStyle = {
    position: 'absolute',
    display: 'block',
    top: '50%',
    left: '50%',
    maxHeight: '80%',
    transform: 'translate(-50%, -50%)',
    width: 800,
    bgcolor: 'background.paper',
    borderRadius: 8,
    boxShadow: 24,
    padding: 4,
};

export default ListingPrivate;