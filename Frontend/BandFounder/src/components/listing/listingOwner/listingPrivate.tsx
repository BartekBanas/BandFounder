import React, {useEffect, useState} from 'react';
import defaultProfileImage from '../../../assets/defaultProfileImage.jpg';
import './style.css';
import './listingCreator.css';
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import {RingLoader} from "../../common/RingLoader";
import {
    Button,
    Modal,
    Box,
    TextField,
    Select,
    InputLabel,
    MenuItem,
    IconButton,
    Autocomplete
} from "@mui/material";
import {ListingCreate} from "../../../types/ListingCreate";
import CloseIcon from "@mui/icons-material/Close";
import {lengthOfGenre} from "../listingTemplate/listingTemplate";
import EditIcon from '@mui/icons-material/Edit';
import {getListing, updateListing} from "../../../api/listing";
import {getGenres, getMusicianRoles} from "../../../api/metadata";
import ProfilePicture from "../../profile/ProfilePicture";
import {DeleteListingButton} from "./DeleteListingButton";
import {formatMessageWithLinks} from "../../common/utils";
import {MusicianSlot} from "../../../types/MusicianSlot";
import {Listing} from "../../../types/Listing";

interface ListingPrivateProps {
    listingId: string;
}

const ListingPrivate: React.FC<ListingPrivateProps> = ({listingId}) => {
    const [listing, setListing] = useState<Listing>();
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
                setListing(data);
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
        if (listing) {
            listing.name = (event.target.value);
        }
    }

    const handleEditType = () => {
        setListingType(prevType => prevType === 'CollaborativeSong' ? 'Band' : 'CollaborativeSong');
    };

    const handleEditSlot = (slotId: string) => {
        const newSlots = listingMusicianSlots.map((slot: MusicianSlot) => {
            if (slot.id === slotId) {
                return {...slot, status: slot.status === 'Available' ? 'Filled' : 'Available'};
            }
            return slot;
        });
        setListingMusicianSlots(newSlots);
    }

    const handleEditMusicianRole = (slotId: string, role: string) => {
        const newSlots = listingMusicianSlots.map((slot: any) => {
            if (slot.id === slotId) {
                if (role) {
                    return {...slot, role};
                } else {
                    return {...slot, role: ''};
                }
            }
            return slot;
        });
        setListingMusicianSlots(newSlots);
    }

    const handleAddNewRole = () => {
        const newSlot = {
            id: Math.random().toString(36).substr(2, 9),
            role: '',
            status: 'Available',
        };
        setListingMusicianSlots([...listingMusicianSlots, newSlot]);
    }

    const handleUpdateListing = async () => {
        try {
            const updatedListing: ListingCreate = {
                name: listingName,
                type: listingType,
                genre: listing?.genre,
                description: listingDescription,
                musicianSlots: listingMusicianSlots,
            }
            await updateListing(updatedListing, listingId);
            window.location.reload();
        } catch (e) {
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
                        loaders: {...Loader.defaultLoaders, ring: RingLoader},
                        type: 'ring',
                    },
                }),
            },
        });
        return (
            <div className="App-header">
                <MantineThemeProvider theme={theme}>
                    <Loader size={200}/>
                </MantineThemeProvider>
            </div>
        );
    }

    return (
        <div className={'listing custom-scrollbar'}>
            <div className={'editButton'}>
                <Button variant={'contained'} color={'info'} onClick={handleOpen}>
                    <span>Edit</span> <EditIcon/>
                </Button>
            </div>
            <div className={'deleteButton'}>
                <DeleteListingButton listingId={listingId}/>
            </div>
            <div className={'listingHeader'}>
                <div className={'ownerListingElements'}>
                    <ProfilePicture isMyProfile={false} accountId={listing.ownerId} size={40}/>
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
                <p>{formatMessageWithLinks(listing?.description)}</p>
            </div>
            <div className={'listingFooter'}>
                {listing?.musicianSlots.map((slot: any) => (
                    <div key={slot.id}
                         className={`listingRole ${slot.status === 'Available' ? 'status-available' : 'status-filled'}`}>
                        <img src={defaultProfileImage} alt="Default Profile"/>
                        <p>Role: {slot.role}</p>
                        <p>Status: {slot.status}</p>
                    </div>
                ))}
            </div>

            <Modal open={open} onClose={handleClose}>
                <Box sx={{...modalStyle}} onClick={(e) => e.stopPropagation()} className={'wholeEditBody'}>
                    <div id={'saveButtonEditListing'}>
                        <Button variant={'contained'} color={'success'} onClick={handleUpdateListing}>Post</Button>
                        <div>
                            <InputLabel id="typeSelectLabel" sx={{fontSize: '12px'}}>Type</InputLabel>
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
                    <div className={'listing-editor'}>
                        <div className={'editorHeader'}>
                            <TextField
                                label={'Title'}
                                value={listingName}
                                onChange={handleNameChange}
                                variant="filled"
                                color={'info'}
                                style={{minWidth: '50%'}}
                                helperText={`${listingName.length}/35`}
                                inputProps={{maxLength: 35}}
                            />
                            <Autocomplete
                                options={genres}
                                freeSolo
                                id={'genreSelectLabel'}
                                onInputChange={(event, value) => setListingGenre(value)}
                                renderInput={(params) => (
                                    <TextField {...params} label="Genre" variant="outlined" fullWidth
                                               sx={{fontSize: '20px !important'}}/>
                                )}
                                sx={{
                                    minWidth: `${lengthOfGenre(listingGenre?.length || 0) + 10}%`,
                                    maxWidth: '40%',
                                    marginTop: '5px',
                                    fontSize: '12px !important',
                                    transition: 'width 1s ease-in-out'
                                }}
                                value={listingGenre}
                            />
                        </div>
                        <div className={'editorBody'}>
                            <TextField
                                label="Description"
                                value={listingDescription}
                                onChange={(event) => setListingDescription(event.target.value)}
                                variant="filled"
                                color="info"
                                style={{minWidth: '60%'}}
                                multiline
                                rows={3}  // Adjust the number of rows as needed
                                helperText={`${listingDescription.length}/220`}
                                inputProps={{maxLength: 220}}
                            />
                        </div>
                        <div className={'editorFooter'}>
                            {listingMusicianSlots.map((slot: any) => (
                                <div key={slot.id}
                                     className={`listingRoleEdited ${slot.status === 'Available' ? 'status-available' : 'status-filled'}`}>
                                    <div className={'kindaHeader'}>
                                        <div>
                                            <img src={defaultProfileImage} alt="Default Profile"/>
                                        </div>
                                        <IconButton
                                            aria-label="delete"
                                            size="small"
                                            onClick={() => handleDeleteRole(slot.id)}
                                            style={{}}
                                        >
                                            <CloseIcon fontSize="small"/>
                                        </IconButton>
                                    </div>
                                    <div className={'underEditorFooter'}>
                                        <Autocomplete
                                            options={roles}
                                            freeSolo
                                            onInputChange={(event, value) => handleEditMusicianRole(slot.id, value)}
                                            renderInput={(params) => (
                                                <TextField {...params} label="Role" variant="outlined" fullWidth/>
                                            )}
                                            value={slot.role}
                                            sx={{width: '200%'}}
                                        />
                                    </div>
                                    <div className={'underEditorFooter'}>
                                        <InputLabel id="statusSelectLabel"
                                                    style={{fontSize: '12px', maxHeight: '35px'}}>Status</InputLabel>
                                        <Select
                                            labelId="statusSelectLabel"
                                            id="statusSelectLabel"
                                            value={slot.status}
                                            label="Status"
                                            onChange={() => handleEditSlot(slot.id)}
                                            style={{fontSize: '12px', maxHeight: '35px'}}
                                        >
                                            <MenuItem value={'Available'}>Available</MenuItem>
                                            <MenuItem value={'Filled'}>Filled</MenuItem>
                                        </Select>
                                    </div>
                                </div>
                            ))}

                            <div id={'addRolesButton'}>
                                <Button variant={'contained'} color={'info'} onClick={handleAddNewRole}>Add new
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