import React, {useEffect, useState} from 'react';
import defaultProfileImage from '../../../assets/defaultProfileImage.jpg';
import './style.css';
import './listingCreator.css'
import {
    Modal,
    Box,
    Button,
    TextField,
    InputLabel,
    Select,
    MenuItem,
    IconButton,
    Autocomplete
} from '@mui/material';
import Cookies from "universal-cookie";
import CloseIcon from "@mui/icons-material/Close";
import {ListingCreateDto} from "../../../types/ListingCreateDto";
import {getUser} from "../../../api/account";
import {postListing} from "../../../api/listing";
import {getGenres, getMusicianRoles} from "../../../api/metadata";

interface ListingTemplateProps {
}

const ListingTemplate: React.FC<ListingTemplateProps> = () => {
    const [user, setUser] = useState<any>(null);
    const [modalOpen, setModalOpen] = useState<boolean>(false);
    const [listingType, setListingType] = useState<string>('CollaborativeSong');
    const [listingGenre, setListingGenre] = useState<string>('');
    const [listingDescription, setListingDescription] = useState<string>('');
    const [listingMusicianSlots, setListingMusicianSlots] = useState<any>([]);
    const [listingName, setListingName] = useState<string>('');
    const [genres, setGenres] = useState<string[]>([]);
    const [roles, setRoles] = useState<string[]>([]);

    useEffect(() => {
        const fetchData = async () => {
            const userId = new Cookies().get('user_id');
            const userData = await getUser(userId);
            setUser(userData);
        };

        fetchData();
    }, []);

    useEffect(() => {
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

        fetchGenres();
        fetchRoles();
    }, []);

    if (!user) {
        return <div>Loading...</div>;
    }

    const handleListingClick = () => {
        setModalOpen(true);
    };

    const handleNameChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setListingName(event.target.value);
    }

    const handleEditType = () => {
        setListingType(prevType => prevType === 'CollaborativeSong' ? 'Band' : 'CollaborativeSong');
    };

    const handleEditSlot = (slotId: string) => {
        const newSlots = listingMusicianSlots.map((slot: any) => {
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
                console.log('role', role);
                return {...slot, role};
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

    const handleDeleteRole = (slotId: string) => {
        setListingMusicianSlots((prevSlots: any[]) => prevSlots.filter((slot: any) => slot.id !== slotId));
    };

    const handlePostListing = async () => {
        try {
            const updatedListing: ListingCreateDto = {
                name: listingName,
                type: listingType,
                genre: listingGenre,
                description: listingDescription,
                musicianSlots: listingMusicianSlots,
            }
            await postListing(updatedListing);
            window.location.reload();
        } catch (e) {
            console.log(e);
        }
    }

    return (
        <div className="listingContainer">
            <div className="listingOverlay">Create your own listing!</div>
            <div className="listingTemplate" onClick={handleListingClick}>
                <div className="listingHeader">
                    <div className="ownerListingElements">
                        <img src={defaultProfileImage} alt="Default Profile"/>
                        <p>{user?.name}</p>
                    </div>
                    <div className="listingTitle">
                        <p>Title</p>
                    </div>
                    <div className="listingType">
                        <div className="listingType-Band">
                            <p>Band</p>
                        </div>
                        <p>Genre</p>
                    </div>
                </div>
                <div className="listingBody">
                    <p>Description</p>
                </div>
            </div>
            <Modal open={modalOpen}
                   onClose={() => setModalOpen(false)}>
                <Box sx={{...modalStyle}} onClick={(e) => e.stopPropagation()} className={'wholeEditBody'}>
                    <div id={'saveButtonEditListing'}>
                        <Button variant={'contained'} color={'success'} onClick={handlePostListing}>Post</Button>
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
                                    minWidth: `${lengthOfGenre(listingGenre.length) + 10}%`,
                                    maxWidth: '40%',
                                    marginTop: '5px',
                                    fontSize: '12px !important',
                                    transition: 'width 1s ease-in-out',
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
                                rows={2}
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
}

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
    // overflow:'scroll',
    // overflowX:'hidden',
};

export default ListingTemplate;

export const lengthOfGenre = (number: number) => {
    if (number < 5) {
        return 15;
    } else if (number < 10) {
        return 15;
    } else if (number < 15) {
        return 20;
    } else if (number < 20) {
        return 25;
    } else {
        return 28;
    }
}