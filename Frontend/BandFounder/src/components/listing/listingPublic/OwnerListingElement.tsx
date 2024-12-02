import React, {useEffect, useState} from 'react';
import {Menu, IconButton, Box, Typography, Button} from '@mui/material';
import {commonTaste} from "../../../types/CommonTaste";
import {Listing} from "../../../types/Listing";
import {contactListingOwner, getCommonTaste} from "../../../api/listing";
import {getDirectChatroomWithUser} from "../../../api/chatroom";
import {mantineErrorNotification, mantineInformationNotification} from "../../common/mantineNotification";
import ProfilePicture from "../../profile/ProfilePicture";
import {getUserId} from "../../../hooks/authentication";
import {Account} from "../../../types/Account";
import {getAccount} from "../../../api/account";

const OwnerListingElement = ({listing}: { listing: Listing }) => {
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
    const [commonTaste, setCommonTaste] = useState<commonTaste | null>(null);
    const [owner, setOwner] = useState<Account | undefined>(undefined);

    useEffect(() => {
        if (listing?.ownerId) {
            getCommonTaste(listing.id).then((data) => {
                setCommonTaste(data);
            }).catch((error) => {
                console.error(error);
            });

            getAccount(listing.ownerId).then((data) => {
                setOwner(data);
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

    const handleMessageListingOwner = async () => {
        try {
            if (listing.ownerId === getUserId()) {
                console.log("worked");
                mantineInformationNotification("I don't think you need to message yourself");
                return;
            }

            if (listing?.ownerId) {
                const response = await contactListingOwner(listing.id);

                if (response) {
                    window.location.href = '/messages/' + response.id;
                } else {
                    const chatroom = await getDirectChatroomWithUser(listing.ownerId);
                    if (chatroom?.id) {
                        window.location.href = '/messages/' + chatroom.id;
                    } else {
                        throw new Error('Failed to find chatroom with user ' + listing.ownerId);
                    }
                }
            }
        } catch (error) {
            console.error(error);
            mantineErrorNotification("Failed to contact the listing's owner");
        }
    };

    async function handleViewOwnerProfile() {
        window.location.href = '/profile/' + owner?.name;
    }

    return (
        <div className={'ownerListingElements'}>
            <IconButton onClick={handleClick}>
                <ProfilePicture isMyProfile={false} accountId={listing.ownerId} size={40}/>
            </IconButton>
            <p>{owner?.name}</p>

            {listing.ownerId !== getUserId() && (
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
                    <Box sx={{display: 'flex', justifyContent: 'space-between', padding: '5px', gap: '10px'}}>
                        <Button
                            variant="contained"
                            color="primary"
                            onClick={handleViewOwnerProfile}
                            sx={{flex: 1}}
                        >
                            Visit Profile
                        </Button>
                        <Button
                            variant="contained"
                            color="primary"
                            onClick={handleMessageListingOwner}
                            sx={{flex: 1}}
                        >
                            Message Owner
                        </Button>
                    </Box>

                    {commonTaste && (
                        <Box sx={{padding: '10px 0'}}>
                            <Typography
                                variant="h6"
                                gutterBottom
                                sx={{textAlign: 'center', fontWeight: 'bold', marginBottom: '10px'}}
                            >
                                Common Artists and Genres
                            </Typography>

                            <Box sx={{display: 'flex', flexDirection: 'row', gap: '20px'}}>
                                {/* Artists column */}
                                <Box sx={{flex: 1}}>
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
                        </Box>
                    )}
                </Menu>
            )}
        </div>
    );
};

export default OwnerListingElement;