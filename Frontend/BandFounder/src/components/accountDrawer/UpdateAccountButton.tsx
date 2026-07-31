import React, {useEffect, useState} from "react";
import {
    Modal,
    Box,
    TextField,
    Button,
    Stack,
    Typography,
    Divider,
    FormControl,
    InputLabel,
    MenuItem,
    Select,
    SelectChangeEvent
} from "@mui/material";
import {muiDarkTheme} from "../../styles/muiDarkTheme";
import {getMyAccount, updateMyAccount} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";

export function UpdateAccountButton() {
    const [opened, setOpened] = useState(false);
    const [loadingPreferences, setLoadingPreferences] = useState(false);
    const [preferencesReady, setPreferencesReady] = useState(false);
    const [formValues, setFormValues] = useState({
        Name: "",
        Password: "",
        Email: "",
    });
    const [emailUnreadDelayMinutes, setEmailUnreadDelayMinutes] = useState(1440);

    useEffect(() => {
        if (!opened) {
            return;
        }

        setLoadingPreferences(true);
        setPreferencesReady(false);
        getMyAccount()
            .then((account) => {
                setEmailUnreadDelayMinutes(account.emailUnreadDelayMinutes ?? 1440);
                setPreferencesReady(true);
            })
            .catch(() => {
                mantineErrorNotification("Failed to fetch email preferences");
            })
            .finally(() => setLoadingPreferences(false));
    }, [opened]);

    const handleClose = () => {
        setOpened(false);
        setPreferencesReady(false);
        setEmailUnreadDelayMinutes(1440);
        setFormValues({Name: "", Password: "", Email: ""});
    };

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const {name, value} = e.target;
        setFormValues((prev) => ({...prev, [name]: value}));
    };

    const handleDelayChange = (event: SelectChangeEvent<number>) => {
        setEmailUnreadDelayMinutes(Number(event.target.value));
    };

    const handleUpdateAccount = async (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!preferencesReady) {
            return;
        }

        try {
            await updateMyAccount(
                formValues.Name || null,
                formValues.Password || null,
                formValues.Email || null,
                undefined,
                emailUnreadDelayMinutes
            );
            mantineSuccessNotification("Account updated successfully");
            window.location.href = '/profile';
        } catch (error) {
            mantineErrorNotification("Failed to update account");
        }
    };

    return (
        <>
            <Button variant="contained" color="info" size="large" onClick={() => setOpened(true)}>
                Update Account
            </Button>

            <Modal open={opened} onClose={handleClose}>
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
                    <form onSubmit={handleUpdateAccount}>
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

                            <Typography variant="subtitle1">
                                Email notifications
                            </Typography>
                            <FormControl fullWidth disabled={loadingPreferences || !preferencesReady}>
                                <InputLabel id="email-notification-delay-label">Notify after</InputLabel>
                                <Select<number>
                                    labelId="email-notification-delay-label"
                                    value={emailUnreadDelayMinutes}
                                    label="Notify after"
                                    onChange={handleDelayChange}
                                >
                                    <MenuItem value={5}>5 minutes</MenuItem>
                                    <MenuItem value={60}>1 hour</MenuItem>
                                    <MenuItem value={1440}>1 day</MenuItem>
                                </Select>
                            </FormControl>

                            <Button
                                variant="contained"
                                fullWidth
                                color="success"
                                type="submit"
                                disabled={loadingPreferences || !preferencesReady}
                            >
                                Update Account
                            </Button>
                        </Stack>
                    </form>
                </Box>
            </Modal>
        </>
    );
}
