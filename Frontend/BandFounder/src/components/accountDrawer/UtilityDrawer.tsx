import React, {FC, useEffect, useState} from 'react';
import {useDisclosure} from "@mantine/hooks";
import {Drawer, IconButton, Button, Menu, MenuItem} from "@mui/material";
import {DeleteAccountButton} from "./DeleteAccountButton";
import {UpdateAccountButton} from "./UpdateAccountButton";
import {SpotifyConnectionButton} from "./spotifyConnection/SpotifyConnectionButton";
import {AddArtistModal} from "./AddArtistModal";
import {getUserId, removeAuthToken, removeUserId} from "../../hooks/authentication";
import './UtilityDrawer.css';
import {Account} from "../../types/Account";
import {getTopArtists} from "../../api/spotify";
import {getUsersGenres} from "../../api/metadata";
import {getAccount} from "../../api/account";
import UserAvatar from '../common/UserAvatar';

interface UtilityDrawerProps {
}

export const UtilityDrawer: FC<UtilityDrawerProps> = () => {
    const [opened, {open, close}] = useDisclosure(false);
    const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);
    const [user, setUser] = useState<Account>();
    const [topArtists, setTopArtists] = useState<string[]>([]);
    const [topGenres, setTopGenres] = useState<string[]>([]);

    const handleLogout = () => {
        removeAuthToken();
        removeUserId();
        window.location.reload();
    };

    const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
        setMenuAnchor(event.currentTarget);
    };

    const handleMenuClose = () => {
        setMenuAnchor(null);
    };

    useEffect(() => {
        const getUser = async () => {
            const user = await getAccount(getUserId());
            setUser(user);
        }

        const fetchTopArtists = async () => {
            const artists = await getTopArtists(getUserId());
            setTopArtists(artists.splice(0, 5));
        }

        const fetchTopGenres = async () => {
            const genres = await getUsersGenres(getUserId());
            setTopGenres(genres.splice(0, 5));
        }

        getUser();
        fetchTopArtists();
        fetchTopGenres();
    }, []);

    return (
        <>
            <Drawer
                open={opened}
                onClose={close}
                anchor="left"
                sx={{
                    width: 300,
                    '& .MuiDrawer-paper': {
                        width: 300,
                        backgroundColor: 'background.default',
                        color: 'text.primary',
                    },
                }}
                id={'mainDrawer'}
            >
                <div className={'drawerHeader'}>
                    <h1 id={'mainDrawerTitle'}>Account Utilities</h1>
                </div>
                <div className={'profileShowDrawer'}>
                    <UserAvatar userId={user?.id!} size={120}/>
                    <p>{user?.name}</p>
                </div>
                <div className={'musicTasteDrawer'}>
                    <div id={'topArtistsDrawer'}>
                        <h2>Top Artists</h2>
                        <ul>
                            {topArtists.map((artist, index) => (
                                <li key={index}>{index + 1}. {artist}</li>
                            ))}
                        </ul>
                    </div>
                    <div id={'topGenresDrawer'}>
                        <h2>Top Genres</h2>
                        <ul>
                            {topGenres.map((genre, index) => (
                                <li key={index}>{index + 1}. {genre}</li>
                            ))}
                        </ul>
                    </div>
                </div>
                <div className={'drawerBody'}>
                    <div className={'accountButtonsDrawer'}>
                        {/* Dropdown Menu */}
                        <Button
                            variant="outlined"
                            onClick={handleMenuOpen}
                            color="info"
                        >
                            Account Actions
                        </Button>
                        <Menu
                            anchorEl={menuAnchor}
                            open={Boolean(menuAnchor)}
                            onClose={handleMenuClose}
                            id={'accountActionsMenu'}
                        >
                            <MenuItem>
                                <DeleteAccountButton/>
                            </MenuItem>
                            <MenuItem onClick={handleMenuClose}>
                                <SpotifyConnectionButton/>
                            </MenuItem>
                        </Menu>
                        <div className={'smallerButtonsDrawer'}>
                            <UpdateAccountButton/>
                            <AddArtistModal/>
                        </div>
                    </div>
                </div>
                <div className={'drawerFooter'}>
                    <Button variant={"outlined"} onClick={handleLogout} color={"warning"}>
                        Logout
                    </Button>
                </div>
            </Drawer>

            <div>
                <IconButton onClick={open} size="large" color="primary">
                    <svg xmlns="http://www.w3.org/2000/svg" width={36} height={36} viewBox="0 0 24 24" fill="none"
                         stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round"
                         className="icon icon-tabler icons-tabler-outline icon-tabler-adjustments">
                        <path stroke="none" d="M0 0h24v24H0z" fill="none"/>
                        <path d="M4 10a2 2 0 1 0 4 0a2 2 0 0 0 -4 0"/>
                        <path d="M6 4v4"/>
                        <path d="M6 12v8"/>
                        <path d="M10 16a2 2 0 1 0 4 0a2 2 0 0 0 -4 0"/>
                        <path d="M12 4v10"/>
                        <path d="M12 18v2"/>
                        <path d="M16 7a2 2 0 1 0 4 0a2 2 0 0 0 -4 0"/>
                        <path d="M18 4v1"/>
                        <path d="M18 9v11"/>
                    </svg>
                </IconButton>
            </div>
        </>
    );
};