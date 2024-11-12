import React, { useEffect, useState } from 'react';
import { getListing } from './api';
import defaultProfileImage from '../../../assets/defaultProfileImage.jpg';
import getUser from '../../common/frequentlyUsed';
import './style.css';
import { createTheme, Loader, MantineThemeProvider } from "@mantine/core";
import { RingLoader } from "../../common/RingLoader";
import { Button, Modal, Box, TextField } from "@mui/material";

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
            }
        };

        fetchListing();
    }, [listingId]);

    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);

    const handleEditType = () => {
        setListingType(prevType => prevType === 'CollaborativeSong' ? 'Band' : 'CollaborativeSong');
    };

    const handleGenreChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setListingGenre(event.target.value);
    };

    const calculateWidth = (text: string) => {
        return `${text.length + 1}ch`;
    };

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
                <Box sx={{ ...modalStyle }}>
                    <div className={'listing edited'}>
                        <div className={'listingHeader'}>
                            <div className={'ownerListingElements'}>
                                <img src={defaultProfileImage} alt="Default Profile" />
                                <p>{listing?.owner?.name}</p>
                            </div>
                            <div className={'listingTitle'}>
                                <p>{listing?.name}</p>
                            </div>
                            <div className={'listingType'}>
                                <div className={`listingTypeEdited-${listingType}`} onClick={handleEditType}>
                                    <p>{listingType === 'CollaborativeSong' ? 'Song' : listingType}</p>
                                </div>
                                <TextField
                                    value={listingGenre}
                                    onChange={handleGenreChange}
                                    variant="outlined"
                                    className={'genreTextField'}
                                    style={{ width: `${calculateWidth(listingGenre) + 10000}` }}
                                />
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
                    </div>
                </Box>
            </Modal>
        </div>
    );
};

const modalStyle = {
    position: 'absolute',
    display: 'flex',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 800,
    bgcolor: 'background.paper',
    borderRadius: 8,
    boxShadow: 24,
    padding: 4,
};

export default ListingPrivate;