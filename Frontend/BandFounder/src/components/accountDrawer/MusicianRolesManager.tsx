import {useEffect, useState} from "react";
import {getMyMusicianRoles} from '../../api/account';
import MusicianRolesList from './MusicianRolesList';
import {Typography} from "@mui/material";
import {getMusicianRoles} from "../../api/metadata";

const MusicianRolesManager = () => {
    const [roles, setRoles] = useState<string[]>([]);
    const [allRoles, setAllRoles] = useState<string[]>([]);

    const fetchMyMusicianRoles = async () => {
        try {
            const rolesData = await getMyMusicianRoles();
            const allRolesData = await getMusicianRoles();
            setRoles(rolesData);
            setAllRoles(allRolesData);
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
            <MusicianRolesList roles={roles} onRoleAdded={fetchMyMusicianRoles} allRoles={allRoles}/>
        </div>
    );
};

export default MusicianRolesManager;