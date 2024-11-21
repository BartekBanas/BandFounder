import React from 'react';
import { Box, Stack, Autocomplete, TextField, Checkbox } from '@mui/material';
import {addMyMusicianRole} from '../../api/account';

interface MusicianRolesListProps {
    roles: string[];
    allRoles: string[];
    onRoleAdded: () => void;
}

const MusicianRolesList: React.FC<MusicianRolesListProps> = ({ roles, allRoles, onRoleAdded }) => {
    const handleAddMusicianRole = async (role: string) => {
        await addMyMusicianRole(role);
        onRoleAdded();
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