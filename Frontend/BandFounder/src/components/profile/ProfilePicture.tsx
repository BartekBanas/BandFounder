import React, {useState, useEffect} from "react";
import {Box, Typography, styled} from "@mui/material";
import {Dropzone, MIME_TYPES} from "@mantine/dropzone";
import {getProfilePicture, uploadProfilePicture} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import UserAvatar from "../common/UserAvatar";

interface ProfilePictureProps {
    accountId: string;
    isMyProfile: boolean;
    size: number;
}

const HoverBox = styled(Box)({
    position: "relative",
    display: "inline-block",
    "&:hover .hover-text": {
        opacity: 1,
    },
});

const HoverText = styled(Typography)({
    position: "absolute",
    top: "100%",
    left: "50%",
    transform: "translateX(-50%)",
    marginTop: "5px",
    backgroundColor: "rgba(0, 0, 0, 0.5)",
    color: "white",
    padding: "5px 10px",
    borderRadius: "4px",
    opacity: 0,
    transition: "opacity 0.3s ease",
    pointerEvents: "none",
});

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
                console.error("Error fetching profile picture:", error);
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

    return (
        <Box
            className="profileLeftPart"
            display="flex"
            flexDirection="column"
            justifyContent="center"
            alignItems="center"
            gap={2}
        >
            {isMyProfile ? (
                <Dropzone
                    onDrop={handleDrop}
                    accept={[MIME_TYPES.jpeg, MIME_TYPES.png]}
                    maxSize={3 * 1024 ** 2}
                    styles={{
                        root: {
                            border: "none",
                            textAlign: "center",
                            cursor: "pointer",
                        },
                    }}
                >
                    <HoverBox>
                        <UserAvatar userId={accountId} size={size}/>
                        <HoverText className="hover-text">Update your profile picture</HoverText>
                    </HoverBox>
                </Dropzone>
            ) : (
                <UserAvatar userId={accountId} size={size}/>
            )}
        </Box>
    );
};

export default ProfilePicture;