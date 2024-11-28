import React, {useState, useEffect} from "react";
import {getProfilePicture, uploadProfilePicture} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import UserAvatar from "../common/UserAvatar";
import UploadFileIcon from '@mui/icons-material/UploadFile';
import './styles/ProfileDrawer.css';

interface ProfilePictureProps {
    accountId: string;
    isMyProfile: boolean;
    size: number;
}

const ProfilePicture: React.FC<ProfilePictureProps> = ({accountId, isMyProfile, size = 50}) => {
    const [preview, setPreview] = useState<string>("");
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        const loadProfilePicture = async () => {
            try {
                const imageUrl = await getProfilePicture(accountId);
                if (imageUrl) {
                    setPreview(imageUrl);
                }
            } catch (error) {
                console.info("Error fetching profile picture:", error);
            }
        };
        loadProfilePicture();
    }, [accountId]);

    const handleDrop = async (files: File[]) => {
        const uploadedFile = files[0];
        const imagePreview = URL.createObjectURL(uploadedFile);
        setPreview(imagePreview);

        setLoading(true);
        try {
            await uploadProfilePicture(uploadedFile);
            mantineSuccessNotification("Profile picture updated successfully!");
            window.location.reload();
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
                        />
                        <label htmlFor="profile-picture-upload" style={{cursor: 'pointer', position: 'relative'}}>
                            <UserAvatar userId={accountId} size={size} className={'avatar-hover-effect'}/>
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