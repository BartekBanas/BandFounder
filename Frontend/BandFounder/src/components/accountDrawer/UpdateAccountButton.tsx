import React, {useState} from "react";
import {Modal, Box, TextField, Button, Stack, Typography, Divider} from "@mui/material";
import {muiDarkTheme} from "../../styles/muiDarkTheme";
import {updateMyAccount} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";

export function UpdateAccountButton() {
    const [opened, setOpened] = useState(false);
    const [formValues, setFormValues] = useState({
        Name: "",
        Password: "",
        Email: "",
    });

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const {name, value} = e.target;
        setFormValues((prev) => ({...prev, [name]: value}));
    };

    const handleUpdateAccount = async () => {
        try {
            await updateMyAccount(
                formValues.Name || null,
                formValues.Password || null,
                formValues.Email || null
            );
            mantineSuccessNotification("Account updated successfully");
        } catch (error) {
            mantineErrorNotification("Failed to update account");
        }

        setOpened(false);
    };

    return (
        <>
            <Button variant="contained" color="info" size="large" onClick={() => setOpened(true)}>
                Update Account
            </Button>

            <Modal open={opened} onClose={() => setOpened(false)}>
                <Box
                    sx={{
                        position: "absolute",
                        top: "50%",
                        left: "50%",
                        transform: "translate(-50%, -50%)",
                        width: "auto",
                        maxWidth: 400,
                        bgcolor: muiDarkTheme.palette.background.default,
                        borderRadius: 2,
                        boxShadow: 24,
                        p: 4,
                    }}
                >
                    <Typography variant="h5" align="center" sx={{mb: 3}}>
                        Update your account
                    </Typography>
                    <form>
                        <Stack spacing={3}>
                            <TextField
                                label="Username"
                                variant="outlined"
                                name="Name"
                                value={formValues.Name}
                                onChange={handleInputChange}
                            />
                            <TextField
                                label="Password"
                                variant="outlined"
                                type="password"
                                name="Password"
                                value={formValues.Password}
                                onChange={handleInputChange}
                            />
                            <TextField
                                label="Email"
                                variant="outlined"
                                type="email"
                                name="Email"
                                value={formValues.Email}
                                onChange={handleInputChange}
                            />

                            <Divider sx={{my: 3}}/>

                            <Button variant="contained" fullWidth color="success" onClick={handleUpdateAccount}>
                                Update Account
                            </Button>
                        </Stack>
                    </form>
                </Box>
            </Modal>
        </>
    );
}