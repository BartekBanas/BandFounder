import React from "react";
import '../profile/styles/ProfileDrawer.css';
import {Avatar} from "@mui/material";

interface ImageAvatarProps {
    imageUrl: string;
    size: number;
}

export const ImageAvatar: React.FC<ImageAvatarProps> = ({imageUrl, size = 50}) => {
    return (
        <>
            <div className="user-avatar-container">
                <Avatar
                    src={imageUrl}
                    sx={{
                        width: size,
                        height: size,
                    }}
                />
            </div>
        </>
    );
};