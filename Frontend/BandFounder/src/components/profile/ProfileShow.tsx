import React, {useEffect, useRef, useState} from 'react';
import {createPortal} from 'react-dom';
import {Account} from "../../types/Account";
import './profile.css';
import {
    Autocomplete,
    Box,
    Button,
    Chip,
    CircularProgress,
    List,
    ListItem,
    Modal,
    Stack,
    TextField,
    Typography
} from "@mui/material";
import {muiDarkTheme} from "../../styles/muiDarkTheme";
import {useDisclosure} from "@mantine/hooks";
import {mantineErrorNotification} from "../common/mantineNotification";
import DeleteIcon from "@mui/icons-material/Delete";
import {
    addMyMusicianRole,
    deleteMyMusicianRole,
    getAccount,
    getAccountByUsername,
    getMyMusicianRoles,
    getTopGenres
} from "../../api/account";
import {getMusicianRoles} from "../../api/metadata";
import {getTopArtists, TopArtist} from "../../api/spotify";
import ProfilePicture from "./ProfilePicture";
import {createDirectChatroom, getDirectChatroomWithUser} from "../../api/chatroom";

interface ProfileShowProps {
    username: string;
    isMyProfile: boolean;
}

const ARTIST_POPOVER_HOVER_DELAY_MS = 300;

const ProfileShow: React.FC<ProfileShowProps> = ({username, isMyProfile}) => {
    const [guid, setGuid] = useState<string | undefined>(undefined);
    const [account, setAccount] = useState<Account | undefined>(undefined);
    const [topArtists, setTopArtists] = useState<TopArtist[] | undefined>([]);
    const [artistPopover, setArtistPopover] = useState<{ url: string; name: string; top: number; left: number } | null>(null);
    const artistPopoverTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

    const clearArtistPopoverTimeout = () => {
        if (artistPopoverTimeoutRef.current !== null) {
            clearTimeout(artistPopoverTimeoutRef.current);
            artistPopoverTimeoutRef.current = null;
        }
    };

    const showArtistPopover = (artist: TopArtist, target: HTMLElement) => {
        const imageUrl = artist.imageUrl;
        if (!imageUrl) {
            return;
        }
        clearArtistPopoverTimeout();
        artistPopoverTimeoutRef.current = setTimeout(() => {
            const rect = target.getBoundingClientRect();
            setArtistPopover({
                url: imageUrl,
                name: artist.name,
                top: rect.top,
                left: rect.left + rect.width / 2,
            });
        }, ARTIST_POPOVER_HOVER_DELAY_MS);
    };

    const hideArtistPopover = () => {
        clearArtistPopoverTimeout();
        setArtistPopover(null);
    };
    const [genres, setGenres] = useState<string[] | undefined>([]);
    const [roles, setRoles] = useState<string[]>([]);
    const [myRoles, setMyRoles] = useState<string[]>([]);
    const [selectedRole, setSelectedRole] = useState<string | null>(null);
    const [opened, {close, open}] = useDisclosure(false);
    const [allRoles, setAllRoles] = useState<string[]>([]);
    const [deleting, setDeleting] = useState<string | null>(null);

    useEffect(() => {
        const fetchAccountId = async () => {
            setGuid((await getAccountByUsername(username)).id);
        };

        fetchAccountId();
    }, [username]);

    useEffect(() => () => clearArtistPopoverTimeout(), []);

    useEffect(() => {
        const fetchAccount = async () => {
            if (guid) {
                const result = await getAccount(guid);
                setAccount(result);
            }
        };

        const fetchTopArtists = async () => {
            if (guid) {
                const result = await getTopArtists(guid);
                setTopArtists(result);
            }
        }

        const fetchGenres = async () => {
            if (guid) {
                const result = await getTopGenres(guid);
                setGenres(result);
            }
        }

        fetchGenres();
        fetchTopArtists();
        fetchAccount();
        fetchMyMusicianRoles();
        fetchMusicianRoles();
    }, [guid]);

    const handleMessage = async () => {
        try {
            if (!account) {
                setAccount(await getAccountByUsername(username));
            }

            const targetId = account!.id;

            try {
                const response = await createDirectChatroom(targetId);
                window.location.href = "/messages/" + response.id;
            } catch (e) {
                const chatRoom = await getDirectChatroomWithUser(targetId);
                if (chatRoom?.id) {
                    window.location.href = "/messages/" + chatRoom.id;
                } else {
                    mantineErrorNotification("An error occurred when trying to message " + account!.name);
                    throw new Error("Failed to find chatroom with user " + targetId);
                }
            }
        } catch (e) {
            console.error("Error contacting profile owner:", e);
        }
    };

    const fetchMyMusicianRoles = async () => {
        try {
            const rolesData = await getMyMusicianRoles();
            setRoles(rolesData);
        } catch (error) {
            console.error('Error fetching roles:', error);
        }
    };

    const fetchMusicianRoles = async (): Promise<void> => {
        try {
            const response = await getMusicianRoles();
            setAllRoles(response);
        } catch (error) {
            console.error('Error fetching roles:', error);
        }
    }

    const handleAddMusicianRole = async () => {
        if (!selectedRole || selectedRole.trim() === '') {
            mantineErrorNotification("Please enter a role before adding.");
            return;
        }

        await addMyMusicianRole(selectedRole);

        fetchMyMusicianRoles();

        close();
    };
    const filteredRoles: string[] = allRoles.filter(musicianRole => !myRoles.includes(musicianRole));

    const handleDeleteRole = async (role: string) => {
        setDeleting(role);
        try {
            await deleteMyMusicianRole(role);
            fetchMyMusicianRoles();
        } catch (error) {
            console.error(`Error deleting role ${role}:`, error);
        } finally {
            setDeleting(null);
        }
    };

    return (
        <div className="profileMain">
            <div className="profileLeftPart">
                {account && <ProfilePicture isMyProfile={isMyProfile} accountId={account.id} size={200}/>}

                <div style={{display: "flex", alignItems: "center"}}>
                    <Typography variant="body1">Username:</Typography>
                    <Typography variant="body1" sx={{marginLeft: 1}}>
                        {account?.name}
                    </Typography>
                </div>

                {!isMyProfile && (
                    <div style={{display: "flex", alignItems: "center"}}>
                        <Typography
                            color="info"
                            onClick={handleMessage}
                            sx={{cursor: "pointer", textDecoration: "underline"}}
                        >
                            Message
                        </Typography>
                    </div>
                )}
            </div>

            <div className="profileTasteGrid">
                <section className="profile-taste-card custom-scrollbar">
                    <h3 className="profile-taste-card__title">Top Artists</h3>
                    <div className="profile-taste-card__tags">
                        {topArtists && topArtists.length > 0 ? (
                            topArtists.map((artist) => (
                                <span
                                    key={artist.id}
                                    className="profile-taste-tag profile-taste-tag--artist profile-artist-tag"
                                    onMouseEnter={(e) => showArtistPopover(artist, e.currentTarget)}
                                    onMouseLeave={hideArtistPopover}
                                >
                                    {artist.name}
                                </span>
                            ))
                        ) : (
                            <span className="profile-taste-card__empty">No artists yet</span>
                        )}
                    </div>
                </section>

                <section className="profile-taste-card custom-scrollbar">
                    <h3 className="profile-taste-card__title">Top Genres</h3>
                    <div className="profile-taste-card__tags">
                        {genres && genres.length > 0 ? (
                            genres.map((genre, index) => (
                                <span key={index} className="profile-taste-tag profile-taste-tag--genre">
                                    {genre}
                                </span>
                            ))
                        ) : (
                            <span className="profile-taste-card__empty">No genres yet</span>
                        )}
                    </div>
                </section>

                {isMyProfile && (
                    <section className="profile-taste-card profile-taste-card--roles custom-scrollbar">
                        <h3 className="profile-taste-card__title">Music Roles</h3>
                        <Box className="musicRolesList">
                            {roles.length > 0 ? (
                                <List>
                                    {roles.map((role) => (
                                        <ListItem key={role}>
                                            <Chip
                                                label={role}
                                                onDelete={() => handleDeleteRole(role)}
                                                deleteIcon={deleting === role ? <CircularProgress size={24}/> : <DeleteIcon/>}
                                                disabled={deleting === role}
                                                style={{margin: 0}}
                                            />
                                        </ListItem>
                                    ))}
                                </List>
                            ) : (
                                <span className="profile-taste-card__empty">No roles added yet</span>
                            )}
                        </Box>
                        <Button
                            className="profile-taste-card__add-btn"
                            variant="contained"
                            color="success"
                            size="medium"
                            onClick={open}
                        >
                            Add a musician role
                        </Button>
                    </section>
                )}
            </div>

            {isMyProfile && (
                <Modal open={opened} onClose={close}>
                        <Box
                            sx={{
                                position: 'absolute',
                                top: '50%',
                                left: '50%',
                                transform: 'translate(-50%, -50%)',
                                width: 400,
                                bgcolor: muiDarkTheme.palette.background.default,
                                borderRadius: 2,
                                boxShadow: 24,
                                p: 4,
                                outline: 'none',
                            }}
                        >
                            <Typography variant="h6" align="center" sx={{mb: 3}}>
                                Add a new role
                            </Typography>

                            <Stack spacing={3} alignItems="center">
                                <Autocomplete
                                    options={filteredRoles}
                                    freeSolo
                                    onInputChange={(event, value) => setSelectedRole(value)}
                                    renderInput={(params) => (
                                        <TextField {...params} label="Musician Role" variant="outlined" fullWidth/>
                                    )}
                                    sx={{width: '90%'}}
                                />

                                <Button color="success" variant="contained" onClick={handleAddMusicianRole}>
                                    Add role
                                </Button>
                            </Stack>
                        </Box>
                </Modal>
            )}

            {artistPopover && createPortal(
                <div
                    className="profile-artist-popover"
                    style={{top: artistPopover.top, left: artistPopover.left}}
                >
                    <img
                        className="profile-artist-popover__img"
                        src={artistPopover.url}
                        alt={artistPopover.name}
                        loading="lazy"
                        onError={hideArtistPopover}
                    />
                </div>,
                document.body
            )}
        </div>
    );
};

export default ProfileShow;