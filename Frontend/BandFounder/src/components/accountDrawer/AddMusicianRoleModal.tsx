import React, {FC, useEffect, useState} from 'react';
import {API_URL} from '../../config';
import {authorizedHeaders} from "../../hooks/utils";
import {useDisclosure} from "@mantine/hooks";
import {getAuthToken, getUserId} from "../../hooks/authentication";
import {getMusicianRoles} from "./api";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {Autocomplete, Box, Button, Modal, Stack, TextField, Typography } from '@mui/material';
import {muiDarkTheme} from "../../assets/muiDarkTheme";

export const AddMusicianRoleModal: FC = () => {
    const [opened, {close, open}] = useDisclosure(false);
    const [roles, setRoles] = useState<string[]>([]);
    const [myRoles, setMyRoles] = useState<string[]>([]);
    const [selectedRole, setSelectedRole] = useState<string | null>(null);

    useEffect(() => {
        fetchMusicianRoles();
        fetchMyMusicianRoles();
    }, []);

    const fetchMusicianRoles = async (): Promise<void> => {
        try {
            const response = await getMusicianRoles();
            setRoles(response);
        } catch (error) {
            console.error('Error fetching roles:', error);
        }
    }

    const fetchMyMusicianRoles = async (): Promise<void> => {
        try {
            const response = await fetch(`${API_URL}/accounts/${getUserId()}/roles`, {
                method: 'GET',
                headers: authorizedHeaders()
            });

            if (!response.ok) {
                mantineErrorNotification('Failed to fetch own roles');
                throw new Error('Failed to fetch own roles');
            }

            const myRoles: string[] = await response.json();
            setMyRoles(myRoles);
        } catch (error) {
            console.error('Error fetching own roles:', error);
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

            mantineSuccessNotification(`Role ${selectedRole} was added to your account`);
        } catch (error) {
            mantineErrorNotification(`Failed to add ${selectedRole} role to your account`);
        }

        close();
    };

    const filteredRoles: string[] = roles.filter(musicianRole => !myRoles.includes(musicianRole));

    return (
        <>
            <Button variant="contained" color="success" size="large" onClick={open}>
                Add an musician role
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
                    <Typography variant="h6" align="center" sx={{ mb: 3}}>
                        Add a new role
                    </Typography>

                    <Stack spacing={3} alignItems="center">
                        <Autocomplete
                            options={filteredRoles}
                            freeSolo
                            onInputChange={(event, value) => setSelectedRole(value)}
                            renderInput={(params) => (
                                <TextField {...params} label="Musician Role" variant="outlined" fullWidth />
                            )}
                            sx={{ width: '90%' }}
                        />

                        <Button color="success" variant="contained" onClick={handleAddMusicianRole}>
                            Add role
                        </Button>
                    </Stack>
                </Box>
            </Modal>
        </>

    );
};