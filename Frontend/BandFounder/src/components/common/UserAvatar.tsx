import React, {useState, useEffect} from "react";
import {Avatar, CircularProgress} from "@mui/material";
import {getProfilePicture} from "../../api/account";
import defaultProfileImage from '../../assets/defaultProfileImage.jpg';

interface UserAvatarProps {
    userId: string;
    size?: number;
}

const UserAvatar: React.FC<UserAvatarProps> = ({userId, size = 50}) => {
    const [avatarUrl, setAvatarUrl] = useState<string>(defaultProfileImage);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        const fetchAvatar = async () => {
            setLoading(true);
            try {
                const imageUrl = await getProfilePicture(userId);
                setAvatarUrl(imageUrl || defaultProfileImage);
            } catch (error) {
                console.error("Error fetching user avatar:", error);
                setAvatarUrl(defaultProfileImage);
            } finally {
                setLoading(false);
            }
        };

        fetchAvatar();
    }, [userId]);

    return (
        <>
            {loading ? (
                <CircularProgress size={size / 2}/>
            ) : (
                <Avatar
                    src={avatarUrl}
                    alt={`User ${userId}`}
                    sx={{
                        width: size,
                        height: size,
                    }}
                />
            )}
        </>
    );
};

export default UserAvatar;