import React, {FC, useEffect, useRef, useState} from 'react';
import {useDisclosure} from "@mantine/hooks";
import {Button, Drawer, IconButton, Typography} from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import CloseIcon from "@mui/icons-material/Close";
import {DeleteAccountButton} from "./DeleteAccountButton";
import {UpdateAccountButton} from "./UpdateAccountButton";
import {SpotifyConnectionButton} from "./spotifyConnection/SpotifyConnectionButton";
import {AddArtistModal} from "./AddArtistModal";
import {getUserId, removeAuthToken, removeUserId} from "../../hooks/authentication";
import './UtilityDrawer.css';
import '../../styles/customScrollbar.css';
import type {AccountSettings} from "../../types/Account";
import {getTopArtists, TopArtist} from "../../api/spotify";
import {getUsersGenres} from "../../api/metadata";
import {getMyAccount, updateMyAccount} from "../../api/account";
import ProfilePicture from "../profile/ProfilePicture";
import {EmailNotificationCard} from "./EmailNotificationCard";
import {resolveEmailUnreadDelayMinutes} from "../../constants/emailNotifications";

interface UtilityDrawerProps {
}

export const UtilityDrawer: FC<UtilityDrawerProps> = () => {
    const [opened, {open, close}] = useDisclosure(false);
    const [user, setUser] = useState<AccountSettings>();
    const [topArtists, setTopArtists] = useState<TopArtist[]>([]);
    const [topGenres, setTopGenres] = useState<string[]>([]);
    const emailNotificationRequestId = useRef(0);
    const emailDelayRequestId = useRef(0);

    const handleLogout = () => {
        removeAuthToken();
        removeUserId();
        window.location.reload();
    };

    const handleEmailNotificationChange = async (enabled: boolean) => {
        if (!user || user.emailOnNewMessage === enabled) {
            return;
        }

        const previousEnabled = user.emailOnNewMessage;
        const requestId = ++emailNotificationRequestId.current;
        setUser({...user, emailOnNewMessage: enabled});

        try {
            await updateMyAccount(null, null, null, enabled);
        } catch {
            if (requestId !== emailNotificationRequestId.current) {
                return;
            }

            setUser((currentUser) =>
                currentUser
                    ? {...currentUser, emailOnNewMessage: previousEnabled}
                    : currentUser
            );
        }
    };

    const handleEmailDelayChange = async (minutes: number) => {
        if (!user || user.emailUnreadDelayMinutes === minutes) {
            return;
        }

        const previousDelay = user.emailUnreadDelayMinutes;
        const requestId = ++emailDelayRequestId.current;
        setUser({...user, emailUnreadDelayMinutes: minutes});

        try {
            await updateMyAccount(null, null, null, undefined, minutes);
        } catch {
            if (requestId !== emailDelayRequestId.current) {
                return;
            }

            setUser((currentUser) =>
                currentUser
                    ? {...currentUser, emailUnreadDelayMinutes: previousDelay}
                    : currentUser
            );
        }
    };

    useEffect(() => {
        const getUser = async () => {
            const user = await getMyAccount();
            setUser(user);
        };

        const fetchTopArtists = async () => {
            const artists = await getTopArtists(getUserId());
            if (artists) {
                setTopArtists(artists.slice(0, 5));
            }
        };

        const fetchTopGenres = async () => {
            const genres = await getUsersGenres(getUserId());
            setTopGenres(genres.slice(0, 5));
        };

        getUser();
        fetchTopArtists();
        fetchTopGenres();
    }, []);

    return (
        <>
            <Drawer
                className="utility-drawer"
                open={opened}
                onClose={close}
                anchor="left"
                id="mainDrawer"
            >
                <header className="utility-drawer__header">
                    <h1 className="utility-drawer__title" id="mainDrawerTitle">Account</h1>
                    <IconButton
                        className="utility-drawer__close"
                        onClick={close}
                        aria-label="Close account menu"
                        size="small"
                    >
                        <CloseIcon/>
                    </IconButton>
                </header>

                <div className="utility-drawer__body custom-scrollbar">
                    <div className="utility-drawer__profile">
                        {user?.id && (
                            <ProfilePicture accountId={user.id} isMyProfile={true} size={120}/>
                        )}
                        <Typography className="utility-drawer__username" component="p">
                            {user?.name ?? ''}
                        </Typography>
                    </div>

                    <div className="utility-drawer__taste-grid">
                        <section className="utility-drawer__taste-card custom-scrollbar">
                            <h3 className="utility-drawer__taste-title">Top Artists</h3>
                            {topArtists.length > 0 ? (
                                <ol className="utility-drawer__taste-list">
                                    {topArtists.map((artist) => (
                                        <li key={artist.id}>{artist.name}</li>
                                    ))}
                                </ol>
                            ) : (
                                <span className="utility-drawer__taste-empty">No artists yet</span>
                            )}
                        </section>

                        <section className="utility-drawer__taste-card custom-scrollbar">
                            <h3 className="utility-drawer__taste-title">Top Genres</h3>
                            {topGenres.length > 0 ? (
                                <ol className="utility-drawer__taste-list">
                                    {topGenres.map((genre, index) => (
                                        <li key={index}>{genre}</li>
                                    ))}
                                </ol>
                            ) : (
                                <span className="utility-drawer__taste-empty">No genres yet</span>
                            )}
                        </section>
                    </div>

                    <section className="utility-drawer__section">
                        <h3 className="utility-drawer__section-label">Account</h3>
                        <div className="utility-drawer__section-actions">
                            <UpdateAccountButton/>
                            <AddArtistModal/>
                        </div>
                    </section>

                    <section className="utility-drawer__section">
                        <h3 className="utility-drawer__section-label">Spotify</h3>
                        <div className="utility-drawer__section-actions">
                            <SpotifyConnectionButton/>
                        </div>
                    </section>

                    <section className="utility-drawer__section">
                        <h3 className="utility-drawer__section-label">Notifications</h3>
                        <EmailNotificationCard
                            enabled={user?.emailOnNewMessage ?? true}
                            delayMinutes={resolveEmailUnreadDelayMinutes(user?.emailUnreadDelayMinutes)}
                            disabled={!user}
                            onEnabledChange={handleEmailNotificationChange}
                            onDelayChange={handleEmailDelayChange}
                        />
                    </section>

                    <section className="utility-drawer__section">
                        <h3 className="utility-drawer__section-label">Danger zone</h3>
                        <div className="utility-drawer__section-actions">
                            <DeleteAccountButton/>
                        </div>
                    </section>
                </div>

                <footer className="utility-drawer__footer">
                    <Button
                        className="utility-drawer__logout"
                        variant="outlined"
                        onClick={handleLogout}
                    >
                        Logout
                    </Button>
                </footer>
            </Drawer>

            <IconButton onClick={open} color="inherit" aria-label="Open account menu">
                <MenuIcon/>
            </IconButton>
        </>
    );
};
