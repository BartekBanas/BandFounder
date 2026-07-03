import React, {useEffect, useState} from 'react';
import {Box, Button, IconButton, Menu, Typography} from '@mui/material';
import UserAvatar from './UserAvatar';
import {getAccount, getCommonTaste} from '../../api/account';
import {getUserId} from '../../hooks/authentication';
import {commonTaste} from '../../types/CommonTaste';

interface InteractiveUserAvatarProps {
    userId: string;
    name?: string;
    size?: number;
    showName?: boolean;
    showCommonTaste?: boolean;
    menuContent?: React.ReactNode;
    extraActions?: React.ReactNode;
    className?: string;
}

const InteractiveUserAvatar: React.FC<InteractiveUserAvatarProps> = ({
    userId,
    name,
    size = 40,
    showName = true,
    showCommonTaste = true,
    menuContent,
    extraActions,
    className,
}) => {
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
    const [resolvedName, setResolvedName] = useState<string | undefined>(name);
    const [taste, setTaste] = useState<commonTaste | null>(null);

    const isSelf = userId === getUserId();

    useEffect(() => {
        setResolvedName(name);

        if (!name && userId) {
            getAccount(userId)
                .then((account) => setResolvedName(account?.name))
                .catch((error) => console.error(error));
        }
    }, [name, userId]);

    useEffect(() => {
        setTaste(null);

        if (showCommonTaste && userId && !isSelf) {
            getCommonTaste(userId)
                .then((data) => setTaste(data))
                .catch((error) => console.error(error));
        }
    }, [showCommonTaste, userId, isSelf]);

    const handleClick = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handleClose = () => {
        setAnchorEl(null);
    };

    const handleVisitProfile = () => {
        if (resolvedName) {
            window.location.href = '/profile/' + resolvedName;
        }
    };

    return (
        <Box
            className={className}
            sx={{
                display: 'inline-flex',
                flexDirection: showName ? 'column' : 'row',
                alignItems: 'center',
                gap: showName ? '4px' : 0,
            }}
        >
            <IconButton
                onClick={handleClick}
                sx={{
                    padding: 0,
                    width: size,
                    height: size,
                }}
            >
                <UserAvatar userId={userId} size={size}/>
            </IconButton>

            {showName && resolvedName && (
                <Box
                    component="p"
                    sx={{
                        margin: 0,
                        fontSize: '0.8rem',
                        color: 'var(--text-secondary)',
                    }}
                >
                    {resolvedName}
                </Box>
            )}

            <Menu
                anchorEl={anchorEl}
                open={Boolean(anchorEl)}
                onClose={handleClose}
                slotProps={{
                    paper: {
                        sx: {
                            width: '320px',
                            padding: '10px',
                        },
                    },
                }}
            >
                <Box sx={{display: 'flex', justifyContent: 'space-between', padding: '5px', gap: '10px'}}>
                    <Button
                        variant="contained"
                        color="primary"
                        onClick={handleVisitProfile}
                        sx={{flex: 1}}
                    >
                        Visit Profile
                    </Button>
                    {extraActions}
                </Box>

                {taste && (
                    <Box sx={{padding: '10px 0'}}>
                        <Typography
                            variant="h6"
                            gutterBottom
                            sx={{textAlign: 'center', fontWeight: 'bold', marginBottom: '10px'}}
                        >
                            Common Artists and Genres
                        </Typography>

                        <Box sx={{display: 'flex', flexDirection: 'row', gap: '20px'}}>
                            <Box sx={{flex: 1}}>
                                <Typography variant="subtitle1" gutterBottom>Artists</Typography>
                                <ul>
                                    {taste.commonArtists.slice(0, 5).map((artist, index) => (
                                        <li key={index}>{artist}</li>
                                    ))}
                                </ul>
                                {taste.commonArtists.length > 5 && (
                                    <Typography variant="body2" color="textSecondary">
                                        and {taste.commonArtists.length - 5} more
                                    </Typography>
                                )}
                            </Box>

                            <Box sx={{flex: 1}}>
                                <Typography variant="subtitle1" gutterBottom>Genres</Typography>
                                <ul>
                                    {taste.commonGenres.slice(0, 5).map((genre, index) => (
                                        <li key={index}>{genre}</li>
                                    ))}
                                </ul>
                                {taste.commonGenres.length > 5 && (
                                    <Typography variant="body2" color="textSecondary">
                                        and {taste.commonGenres.length - 5} more
                                    </Typography>
                                )}
                            </Box>
                        </Box>
                    </Box>
                )}

                {menuContent}
            </Menu>
        </Box>
    );
};

export default InteractiveUserAvatar;
