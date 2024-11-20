import React, { useState } from 'react';
import { List, ListItem, CircularProgress, Box, Chip, Stack, Autocomplete, TextField, Checkbox } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import { deleteMyMusicianRole } from '../../api/account';
import { API_URL } from '../../config';
import Cookies from 'universal-cookie';
import {
    mantineErrorNotification,
    mantineInformationNotification,
    mantineSuccessNotification,
} from '../common/mantineNotification';
import { getAuthToken } from '../../hooks/authentication';

interface MusicianRolesListProps {
    roles: string[];
    onRoleDeleted: () => void;
    allRoles: string[];
    onRoleAdded: () => void;
}

const MusicianRolesList: React.FC<MusicianRolesListProps> = ({ roles, onRoleDeleted, allRoles, onRoleAdded }) => {
    const [deleting, setDeleting] = useState<string | null>(null);
    const [adding, setAdding] = useState<string | null>(null);

    const handleDeleteRole = async (role: string) => {
        setDeleting(role);
        const payload = { role: role };
        try {
            const response = await fetch(`${API_URL}/accounts/roles`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${getAuthToken()}`,
                },
                body: JSON.stringify({ role: role }),
            });

            if (!response.ok) {
                throw new Error(`Failed to delete the role ${role}`);
            }

            mantineSuccessNotification(`Role ${role} was successfully removed from your account`);
            onRoleDeleted();
        } catch (error) {
            mantineErrorNotification(`Failed to delete the role ${role}`);
        } finally {
            setDeleting(null);
        }
    };

    const handleAddMusicianRole = async (role: string) => {
        try {
            const response = await fetch(`${API_URL}/accounts/roles`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${getAuthToken()}`,
                },
                body: JSON.stringify(role), // Ensure the role field is correctly formatted
            });

            if (!response.ok) {
                throw new Error(`Failed to add ${role} role to your account`);
            }

            if (response.status === 204) {
                mantineInformationNotification(`Your account already has role ${role} assigned`);
            } else {
                mantineSuccessNotification(`Role ${role} was added to your account`);
            }

            onRoleAdded();
        } catch (error) {
            mantineErrorNotification(`Failed to add ${role} role to your account`);
        }
    };

    return (
        <Box>
            <Stack spacing={1} sx={{ width: 200 }}>
                <Autocomplete
                    multiple
                    id="checkboxes-tags-demo"
                    options={allRoles}
                    disableCloseOnSelect
                    getOptionLabel={(option) => option}
                    value={roles} // Set the starting value to roles
                    renderOption={(props, option, { selected }) => {
                        const { key, ...optionProps } = props;
                        return (
                            <li key={key} {...optionProps}>
                                <Checkbox
                                    style={{ marginRight: 8 }}
                                    checked={selected}
                                />
                                {option}
                            </li>
                        );
                    }}
                    renderInput={(params) => (
                        <TextField {...params} label="Musician roles" placeholder="Roles" />
                    )}
                    onChange={(event, newValue) => {
                        // Detect added roles
                        const addedRoles = newValue.filter((role) => !roles.includes(role));
                        addedRoles.forEach((role) => handleAddMusicianRole(role));
                    }}
                />
            </Stack>
        </Box>
    );
};

export default MusicianRolesList;