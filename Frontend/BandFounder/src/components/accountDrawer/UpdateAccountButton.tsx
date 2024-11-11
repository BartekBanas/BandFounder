import {useDisclosure} from '@mantine/hooks';
import {Modal, Box, TextField, Button, Stack, Typography, Divider} from '@mui/material';
import React from "react";
import {useForm} from "@mantine/form";
import {updateAccountRequest} from "./api";
import {RegisterFormType} from "../register/api";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {darkTheme} from "./darkTheme";

export function UpdateAccountButton() {
    const [opened, {close, open}] = useDisclosure(false);
    const form = useForm<RegisterFormType>({
        initialValues: {
            Name: '',
            Password: '',
            Email: '',
        },
    });

    const handleUpdateAccount = async () => {
        try {
            await updateAccountRequest(
                form.values.Name || null,
                form.values.Password || null,
                form.values.Email || null
            );
            mantineSuccessNotification("Account updated successfully");
        } catch (error) {
            mantineErrorNotification("Failed to update account");
        }

        close();
    };

    return (
        <>
            <Button variant="text" color="primary" size="large" onClick={open}>
                Update Account
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
                        bgcolor: darkTheme.palette.background.default,
                        borderRadius: 2,
                        boxShadow: 24,
                        p: 4,
                    }}
                >
                    <Typography variant="h5" align="center" sx={{mb: 3}}>
                        Update your account
                    </Typography>
                    <form onSubmit={handleUpdateAccount}>
                        <Stack spacing={3}>
                            <TextField label="Username" variant="outlined"/>
                            <TextField label="Password" variant="outlined" type="password"/>
                            <TextField label="Email" variant="outlined" type="email"/>

                            <Divider sx={{my: 3}}/>

                            <Button type="submit" variant="contained" fullWidth color="success">
                                Update Account
                            </Button>
                        </Stack>
                    </form>
                </Box>
            </Modal>
        </>
    );
}