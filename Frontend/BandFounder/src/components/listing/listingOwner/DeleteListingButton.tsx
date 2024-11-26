import React, {useState} from 'react';
import {Button, Modal, Box, Typography, Stack} from "@mui/material";
import DeleteIcon from '@mui/icons-material/Delete';
import {deleteListing} from "../../../api/listing";
import {mantineErrorNotification} from "../../common/mantineNotification";
import {muiDarkTheme} from "../../../assets/muiDarkTheme";

interface DeleteListingButtonProps {
    listingId: string;
}

export const DeleteListingButton: React.FC<DeleteListingButtonProps> = ({listingId}) => {
    const [open, setOpen] = useState(false);

    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);

    const handleConfirmDelete = async () => {
        try {
            await deleteListing(listingId);
            window.location.reload();
        } catch (error) {
            mantineErrorNotification('Failed to delete listing');
        } finally {
            handleClose();
        }
    };

    return (
        <>
            <Button variant="contained" color="warning" onClick={handleOpen}>
                <span>Delete</span>
                <DeleteIcon/>
            </Button>
            <Modal open={open} onClose={handleClose}>
                <Box sx={modalStyle}>
                    <Typography variant="body1" align="center">
                        Are you sure you want to delete this listing?
                    </Typography>
                    <Typography variant="body2" align="center" sx={{color: 'text.secondary', mt: 1}}>
                        This action cannot be reversed
                    </Typography>

                    <Stack direction="row" spacing={2} justifyContent="center" sx={{mt: 3}}>
                        <Button variant="outlined" color="primary" onClick={handleClose}>
                            No, don't delete it
                        </Button>
                        <Button variant="contained" color="error" onClick={handleConfirmDelete}>
                            Delete Listing
                        </Button>
                    </Stack>
                </Box>
            </Modal>
        </>
    );
};

const modalStyle = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 'auto',
    maxWidth: 400,
    bgcolor: muiDarkTheme.palette.background.default,
    borderRadius: 2,
    boxShadow: 24,
    p: 4,
};