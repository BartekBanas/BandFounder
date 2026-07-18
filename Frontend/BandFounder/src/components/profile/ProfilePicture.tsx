import React, {useState} from "react";
import {uploadProfilePicture} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import UserAvatar from "../common/UserAvatar";
import UploadFileIcon from '@mui/icons-material/UploadFile';
import './styles/ProfileDrawer.css';

interface ProfilePictureProps {
    accountId?: string;
    isMyProfile?: boolean;
    size: number;
}

const ProfilePicture: React.FC<ProfilePictureProps> = ({accountId, isMyProfile = false, size = 50}) => {
    const [loading, setLoading] = useState(false);
    const [avatarVersion, setAvatarVersion] = useState(0);

    const handleDrop = async (files: File[]) => {
        const uploadedFile = files[0];

        setLoading(true);
        try {
            await uploadProfilePicture(uploadedFile);
            mantineSuccessNotification("Profile picture updated successfully!");
            setAvatarVersion((version) => version + 1);
        } catch (error) {
            console.error(error);
            mantineErrorNotification("Failed to upload the profile picture.");
        } finally {
            setLoading(false);
        }
    };

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        if (event.target.files) {
            handleDrop(Array.from(event.target.files));
        }
    };

    return (
        <>
            {isMyProfile ? (
                <div className="user-avatar-container">
                    <div className={'hover-avatar'}>
                        <p className="hover-text">Update your profile picture</p>
                        <input
                            type="file"
                            accept="image/*"
                            style={{display: 'none'}}
                            id="profile-picture-upload"
                            onChange={handleFileChange}
                            disabled={loading}
                        />
                        <label htmlFor="profile-picture-upload" style={{cursor: 'pointer', position: 'relative'}}>
                            <UserAvatar
                                key={`${accountId}-${avatarVersion}`}
                                userId={accountId}
                                size={size}
                                className={'avatar-hover-effect'}
                            />
                            <UploadFileIcon className="upload-icon"/>
                        </label>
                    </div>
                </div>
            ) : (
                <div className="user-avatar-container">
                    <UserAvatar userId={accountId} size={size}/>
                </div>
            )}
        </>
    );
};

export default ProfilePicture;
