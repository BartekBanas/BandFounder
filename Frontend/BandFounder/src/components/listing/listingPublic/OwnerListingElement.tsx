import React, {useEffect, useState} from 'react';
import {Menu, MenuItem, IconButton, Avatar, Box, Typography} from '@mui/material';
import defaultProfileImage from '../../../assets/defaultProfileImage.jpg';
import {mantineInformationNotification} from "../../common/mantineNotification";
import {commonTaste} from "../../../types/CommonTaste";
import {Listing} from "../../../types/Listing";
import {contactListingOwner, getCommonTaste} from "../../../api/listing";
import {getChatroomByOtherUserId} from "../../messeges/AllConversation/api";

const OwnerListingElement = ({listing}: { listing: Listing }) => {
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

    const handleAction = async () => {
        mantineInformationNotification('Action performed');

        if (listing?.ownerId) {
            const response = await contactListingOwner(listing.id);
            console.log(response);
            handleClose();
            if (response) {
                window.location.href = '/messages/' + response.id;
            } else {
                const contactId = listing.ownerId;
                const chatRoomId = await getChatroomByOtherUserId(contactId);
                window.location.href = '/messages/' + chatRoomId;
            }
        }
    };

    return (
        <div className={'ownerListingElements'}>
            <IconButton onClick={handleClick}>
                <Avatar alt="Owner Profile" src={defaultProfileImage}/>
            </IconButton>
            <p>{listing?.owner?.name}</p>

            <Menu
                anchorEl={anchorEl}
                open={Boolean(anchorEl)}
                onClose={handleClose}
                PaperProps={{
                    sx: {
                        width: '320px',
                        padding: '10px',
                    },
                }}
            >
                <MenuItem onClick={handleAction}>Message owner</MenuItem>

                {commonTaste && (
                    <Box sx={{display: 'flex', flexDirection: 'row', padding: '10px'}}>
                        {/* Artists column */}
                        <Box sx={{flex: 1, marginRight: '10px'}}>
                            <Typography variant="subtitle1" gutterBottom>Artists</Typography>
                            <ul>
                                {commonTaste.commonArtists.slice(0, 5).map((artist, index) => (
                                    <li key={index}>{artist}</li>
                                ))}
                            </ul>
                            {commonTaste.commonArtists.length > 5 && (
                                <Typography variant="body2" color="textSecondary">
                                    and {commonTaste.commonArtists.length - 5} more
                                </Typography>
                            )}
                        </Box>

                        {/* Genres column */}
                        <Box sx={{flex: 1}}>
                            <Typography variant="subtitle1" gutterBottom>Genres</Typography>
                            <ul>
                                {commonTaste.commonGenres.slice(0, 5).map((genre, index) => (
                                    <li key={index}>{genre}</li>
                                ))}
                            </ul>
                            {commonTaste.commonGenres.length > 5 && (
                                <Typography variant="body2" color="textSecondary">
                                    and {commonTaste.commonGenres.length - 5} more
                                </Typography>
                            )}
                        </Box>
                    </Box>
                )}
            </Menu>
        </div>
    );
};

export default OwnerListingElement;