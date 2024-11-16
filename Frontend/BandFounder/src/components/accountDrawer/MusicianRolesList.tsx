import React, {useState} from 'react';
import {List, ListItem, Typography, CircularProgress, Box, Chip} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import {deleteMyMusicianRole} from '../../api/account';

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
            <Typography variant="h6" gutterBottom>
                My Musician Roles
            </Typography>
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