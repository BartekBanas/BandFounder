import React, {useEffect, useState} from 'react';
import { Menu, MenuItem, IconButton, Avatar, Box, Typography } from '@mui/material';
import defaultProfileImage from '../../../assets/defaultProfileImage.jpg';
import {mantineInformationNotification, mantineSuccessNotification} from "../../common/mantineNotification";
import {commonTaste} from "../../../types/CommonTaste";
import {Listing} from "../../../types/Listing";
import {getCommonTaste} from "../../../api/listing";

const OwnerListingElement = ({ listing }: { listing: Listing }) => {
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
    const [commonTaste, setCommonTaste] = useState<commonTaste | null>(null);

    useEffect(() => {
        if (listing?.ownerId) {
            getCommonTaste(listing.id).then((data) => {
                setCommonTaste(data);
            }).catch((error) => {
                console.error(error);
            });
        }
    }, [listing]);

    const handleClick = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handleClose = () => {
        setAnchorEl(null);
    };

    const handleAction = () => {
        mantineInformationNotification('Action performed');
        if (listing?.ownerId) {
            mantineSuccessNotification(`Action performed on ${listing.owner.id}`);
        }
        handleClose();
    };

    return (
        <div className={'ownerListingElements'}>
            <IconButton onClick={handleClick}>
                <Avatar alt="Owner Profile" src={defaultProfileImage} />
            </IconButton>
            <p>{listing?.owner?.name}</p>

            <Menu
                anchorEl={anchorEl}
                open={Boolean(anchorEl)}
                onClose={handleClose}
            >
                <MenuItem onClick={handleAction}>Perform Action</MenuItem>

                {commonTaste && (
                    <Box sx={{ display: 'flex', flexDirection: 'row', padding: '10px' }}>
                        <Box sx={{ flex: 1, marginRight: '10px' }}>
                            <Typography variant="subtitle1" gutterBottom>Artists</Typography>
                            <ul>
                                {commonTaste.commonArtists.slice(0, 5).map((artist, index) => (
                                    <li key={index}>{artist}</li>
                                ))}
                            </ul>
                        </Box>
                        <Box sx={{ flex: 1 }}>
                            <Typography variant="subtitle1" gutterBottom>Genres</Typography>
                            <ul>
                                {commonTaste.commonGenres.slice(0, 5).map((genre, index) => (
                                    <li key={index}>{genre}</li>
                                ))}
                            </ul>
                        </Box>
                    </Box>
                )}
            </Menu>
        </div>
    );
};

export default OwnerListingElement;
