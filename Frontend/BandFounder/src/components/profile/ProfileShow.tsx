import React, {useEffect, useState} from 'react';
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
import {muiDarkTheme} from "../../assets/muiDarkTheme";
import {useDisclosure} from "@mantine/hooks";
import {mantineErrorNotification,} from "../common/mantineNotification";
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
import {getTopArtists} from "../../api/spotify";
import ProfilePicture from "./ProfilePicture";
import {createDirectChatroom, getDirectChatroomWithUser} from "../../api/chatroom";

interface ProfileShowProps {
    username: string;
    isMyProfile: boolean;
}

const ProfileShow: React.FC<ProfileShowProps> = ({username, isMyProfile}) => {
    const [guid, setGuid] = useState<string | undefined>(undefined);
    const [account, setAccount] = useState<Account | undefined>(undefined);
    const [topArtists, setTopArtists] = useState<string[] | undefined>([]);
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
        <div className={'profileMain'}>
            <div className={'profileLeftPart'}
                 style={{display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center'}}>
                {account && <ProfilePicture isMyProfile={isMyProfile} accountId={account.id} size={200}/>}

                <div style={{display: "flex", alignItems: "center", margin: "5px"}}>
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
            <div className={'topArtists'}>
                <p>Top Artists: </p>
                <ul>
                    {topArtists?.map((artist, index) => (
                        <li key={index}>{artist}</li>
                    ))}
                </ul>
            </div>
            <div className={'topGenres'} style={{display: 'flex'}}>
                <p>Genres: </p>
                <ul>
                    {genres?.map((genre, index) => (
                        <li key={index}>{genre}</li>
                    ))}
                </ul>
            </div>
            {isMyProfile ?
                <div className={'musicRoles custom-scrollbar'}>
                    <p>Music Roles: </p>
                    <Box className={'musicRolesList'}>
                        <List>
                            {roles.map((role) => (
                                <ListItem key={role}>
                                    <Chip
                                        label={role}
                                        onDelete={() => handleDeleteRole(role)}
                                        deleteIcon={deleting === role ? <CircularProgress size={24}/> : <DeleteIcon/>}
                                        disabled={deleting === role}
                                        color="primary"
                                        style={{margin: 0, padding: '0 !important'}}
                                    />
                                </ListItem>
                            ))}
                        </List>
                    </Box>
                    <Button variant="contained" color="success" size="large" onClick={open}>
                        Add a musician role
                    </Button>
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
                </div> : <div></div>}
        </div>
    );
};

export default ProfileShow;