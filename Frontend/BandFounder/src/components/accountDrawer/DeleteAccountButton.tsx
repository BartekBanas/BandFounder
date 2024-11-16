import {useDisclosure} from '@mantine/hooks';
import {deleteMyAccount} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {Box, Button, Modal, Stack, Typography} from '@mui/material';
import {muiDarkTheme} from "../../assets/muiDarkTheme";

export function DeleteAccountButton() {
    const [opened, {close, open}] = useDisclosure(false);

    const handleDeleteAccount = async () => {
        try {
            await deleteMyAccount();
            mantineSuccessNotification("Account deleted successfully");
        } catch (error) {
            mantineErrorNotification("Failed to delete account");
        }

        close();
    };

    return (
        <>
            <Button variant="contained" color="error" size="medium" onClick={open}>
                Delete Account
            </Button>

            <Modal open={opened} onClose={close}>
                <Box
                    sx={{
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
                    }}
                >
                    <Typography variant="body1" align="center">
                        Are you sure you want to delete your account?
                    </Typography>
                    <Typography variant="body2" align="center" sx={{color: 'text.secondary', mt: 1}}>
                        This action cannot be reversed
                    </Typography>

                    <Stack direction="row" spacing={2} justifyContent="center" sx={{mt: 3}}>
                        <Button variant="outlined" color="primary" onClick={close}>
                            No, don't delete it
                        </Button>
                        <Button variant="contained" color="error" onClick={handleDeleteAccount}>
                            Delete Account
                        </Button>
                    </Stack>
                </Box>
            </Modal>
        </>
    );
}