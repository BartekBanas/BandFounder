import React, {useState} from 'react';
import {List, ListItem, ListItemText, IconButton, Typography, CircularProgress, Box, Chip} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import {deleteMyMusicianRole} from './api';

interface MusicianRolesListProps {
    roles: string[];
    onRoleDeleted: () => void;
}

const MusicianRolesList: React.FC<MusicianRolesListProps> = ({roles, onRoleDeleted}) => {
    const [deleting, setDeleting] = useState<string | null>(null);

    const handleDeleteRole = async (role: string) => {
        setDeleting(role);
        try {
            await deleteMyMusicianRole(role);
            onRoleDeleted();
        } catch (error) {
            console.error(`Error deleting role ${role}:`, error);
        } finally {
            setDeleting(null);
        }
    };

    return (
        <Box>
            <List>
                {roles.map((role) => (
                    <ListItem key={role}>
                        <Chip
                            label={role}
                            onDelete={() => handleDeleteRole(role)}
                            deleteIcon={deleting === role ? <CircularProgress size={24}/> : <DeleteIcon/>}
                            disabled={deleting === role}
                            color="primary"
                        />
                    </ListItem>
                ))}
            </List>
        </Box>
    );
};

export default MusicianRolesList;