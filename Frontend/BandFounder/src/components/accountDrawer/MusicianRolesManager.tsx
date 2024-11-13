import React, {useState, useEffect} from 'react';
import {getMyMusicianRoles} from './api';
import MusicianRolesList from './MusicianRolesList';
import {AddMusicianRoleModal} from './AddMusicianRoleModal';
import {Typography} from "@mui/material";

const MusicianRolesManager = () => {
    const [roles, setRoles] = useState<string[]>([]);

    const fetchMyMusicianRoles = async () => {
        try {
            const rolesData = await getMyMusicianRoles();
            setRoles(rolesData);
        } catch (error) {
            console.error('Error fetching roles:', error);
        }
    };

    useEffect(() => {
        fetchMyMusicianRoles();
    }, []);

    return (
        <div>
            <Typography variant="h6" gutterBottom>
                My Musician Roles
            </Typography>
            <MusicianRolesList roles={roles} onRoleDeleted={fetchMyMusicianRoles}/>
            <AddMusicianRoleModal onRoleAdded={fetchMyMusicianRoles}/>
        </div>
    );
};

export default MusicianRolesManager;