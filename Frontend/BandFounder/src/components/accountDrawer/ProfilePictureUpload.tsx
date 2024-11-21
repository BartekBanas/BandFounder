import React, {useState} from "react";
import {Box, Button, Typography, CircularProgress, Avatar} from "@mui/material";
import {Dropzone, MIME_TYPES} from "@mantine/dropzone";
import {uploadProfilePicture} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";

const ProfilePictureUpload: React.FC = () => {
    const [file, setFile] = useState<File | null>(null);
    const [preview, setPreview] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    const handleDrop = (files: File[]) => {
        const uploadedFile = files[0];
        setFile(uploadedFile);
        setPreview(URL.createObjectURL(uploadedFile));
    };

    const handleUpload = async () => {
        if (!file) return;

        setLoading(true);
        try {
            await uploadProfilePicture(file);
            mantineSuccessNotification("Profile picture updated successfully!");
        } catch (error) {
            console.error(error);
            mantineErrorNotification("Failed to upload the profile picture.");
        } finally {
            setLoading(false);
        }
    };

    return (
        <Box display="flex" flexDirection="column" alignItems="center" gap={2}>
            <Typography variant="h6">Upload Profile Picture</Typography>
            <Dropzone
                onDrop={handleDrop}
                accept={[MIME_TYPES.jpeg, MIME_TYPES.png]}
                maxSize={3 * 1024 ** 2} // Maksymalny rozmiar: 3MB
                styles={{
                    root: {
                        border: "2px dashed #ddd",
                        padding: "20px",
                        textAlign: "center",
                        borderRadius: "8px",
                        cursor: "pointer",
                        width: "300px",
                    },
                }}
            >
                <Typography variant="body2">
                    Select profile picture
                </Typography>
            </Dropzone>
            {preview && <Avatar src={preview} sx={{width: 100, height: 100}}/>}
            <Button
                variant="contained"
                color="primary"
                onClick={handleUpload}
                disabled={!file || loading}
                startIcon={loading && <CircularProgress size={20}/>}
            >
                Upload
            </Button>
        </Box>
    );
};

export default ProfilePictureUpload;
