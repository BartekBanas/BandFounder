import React, { useState, useEffect } from "react";
import { Box, Avatar, Typography, styled } from "@mui/material";
import { Dropzone, MIME_TYPES } from "@mantine/dropzone";
import { getProfilePicture, uploadProfilePicture } from "../../api/account";
import { mantineErrorNotification, mantineSuccessNotification } from "../common/mantineNotification";
import { Account } from "../../types/Account";
import { createDirectChatroom, getDirectChatroomWithUser } from "../../api/chatroom";

interface ProfilePictureProps {
    account: Account;
    isMyProfile: boolean;
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
    top: "50%",
    left: "50%",
    transform: "translate(-50%, -50%)",
    backgroundColor: "rgba(0, 0, 0, 0.5)",
    color: "white",
    padding: "5px 10px",
    borderRadius: "4px",
    opacity: 0,
    transition: "opacity 0.3s ease",
    pointerEvents: "none",
});

const ProfilePicture: React.FC<ProfilePictureProps> = ({ account, isMyProfile }) => {
    const [preview, setPreview] = useState<string>(require("../../assets/defaultProfileImage.jpg"));
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        const loadProfilePicture = async () => {
            try {
                const imageUrl = await getProfilePicture(account.id);
                setPreview(imageUrl || require("../../assets/defaultProfileImage.jpg"));
            } catch (error) {
                console.error("Error fetching profile picture:", error);
                setPreview(require("../../assets/defaultProfileImage.jpg"));
            }
        };
        loadProfilePicture();
    }, [account.id]);

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

    const handleMessage = async () => {
        try {
            const targetId = account.id;

            try {
                const response = await createDirectChatroom(targetId);
                window.location.href = "/messages/" + response.id;
            } catch (e) {
                const chatRoomId = await getDirectChatroomWithUser(targetId);
                if (chatRoomId) {
                    window.location.href = "/messages/" + chatRoomId;
                } else {
                    mantineErrorNotification("An error occurred when trying to message " + account.name);
                    throw new Error("Failed to find chatroom with user " + targetId);
                }
            }
        } catch (e) {
            console.error("Error contacting profile owner:", e);
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
                        <Avatar
                            src={preview}
                            sx={{ width: 100, height: 100, cursor: "pointer" }}
                            alt="Profile"
                        />
                        <HoverText className="hover-text">Update your profile picture</HoverText>
                    </HoverBox>
                </Dropzone>
            ) : (
                <Avatar src={preview} sx={{ width: 100, height: 100 }} alt="Profile" />
            )}

            <div style={{ display: "flex", alignItems: "center", margin: "5px" }}>
                <Typography variant="body1">Username:</Typography>
                <Typography variant="body1" sx={{ marginLeft: 1 }}>
                    {account?.name}
                </Typography>
            </div>

            {!isMyProfile && (
                <div style={{ display: "flex", alignItems: "center" }}>
                    <Typography
                        color="info"
                        onClick={handleMessage}
                        sx={{ cursor: "pointer", textDecoration: "underline" }}
                    >
                        Message
                    </Typography>
                </div>
            )}
        </Box>
    );
};

export default ProfilePicture;