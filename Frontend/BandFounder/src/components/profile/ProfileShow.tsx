import React, { useEffect, useState } from 'react';
import {getAccount, getGUID, getTopArtists, getTopGenres} from './api';
import { Account } from "../../types/Account";
import './profile.css';
import {getMyMusicianRoles, deleteMyMusicianRole, getMusicianRoles} from '../accountDrawer/api';
import MusicianRolesList from "../accountDrawer/MusicianRolesList";
import {AddMusicianRoleModal} from "../accountDrawer/AddMusicianRoleModal";
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
import {
    mantineErrorNotification,
    mantineInformationNotification,
    mantineSuccessNotification
} from "../common/mantineNotification";
import {API_URL} from "../../config";
import {getAuthToken} from "../../hooks/authentication";
import DeleteIcon from "@mui/icons-material/Delete";

interface ProfileShowProps {
    username: string;
    isMyProfile: boolean;
}

const ProfileShow: React.FC<ProfileShowProps> = ({ username,isMyProfile}) => {
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
        const fetchGUID = async () => {
            const result = await getGUID(username);
            setGuid(result);
        };

        fetchGUID();
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

        try {
            const response = await fetch(`${API_URL}/accounts/roles`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${getAuthToken()}`,
                },
                body: JSON.stringify(selectedRole)
            });

            if (!response.ok) {
                throw new Error(`Failed to add ${selectedRole} role to your account`);
            }

            if (response.status === 204) {
                mantineInformationNotification(`Your account already has role ${selectedRole} assigned`);
            } else {
                mantineSuccessNotification(`Role ${selectedRole} was added to your account`);
            }

            fetchMyMusicianRoles();

        } catch (error) {
            mantineErrorNotification(`Failed to add ${selectedRole} role to your account`);
        }

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
            <div className={'profileLeftPart'}>
                <img id='profileImage' src={require('../../assets/defaultProfileImage.jpg')} alt="Default Profile"/>

                <div style={{display: 'flex', alignItems: 'center'}}>
                    <p>Username: </p>
                    <p style={{marginLeft: '8px'}}>{account?.name}</p>
                </div>
                <div style={{display: 'flex', alignItems: 'center'}}>
                    <p>Guid: </p>
                    <p style={{marginLeft: '8px'}}><i>{guid}</i></p>
                </div>
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
                <div className={'musicRoles'}>
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