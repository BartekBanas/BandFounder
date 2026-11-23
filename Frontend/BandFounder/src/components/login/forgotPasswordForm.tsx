import React, {useState} from "react";
import {useNavigate} from "react-router-dom";
import {requestPasswordReset} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {Box, Button, TextField, ThemeProvider, Typography} from "@mui/material";
import {muiDarkTheme} from "../../styles/muiDarkTheme";
import {authCardSx, authPageSx} from "./authStyles";
import {useSharedProfilePath} from "./useSharedProfilePath";

export function ForgotPasswordForm() {
    const [email, setEmail] = useState('');
    const [submitted, setSubmitted] = useState(false);
    const navigate = useNavigate();
    const sharedProfilePath = useSharedProfilePath();

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            await requestPasswordReset(email);
            setSubmitted(true);
            mantineSuccessNotification('If that email exists, a reset link has been sent.');
        } catch (error) {
            mantineErrorNotification('Failed to request password reset');
            console.error(error);
        }
    };

    return (
        <ThemeProvider theme={muiDarkTheme}>
            <Box sx={authPageSx}>
                <Box component="form" onSubmit={handleSubmit} sx={authCardSx}>
                    <Typography variant="h5" fontWeight={600} sx={{marginBottom: '16px'}}>
                        Reset your password
                    </Typography>

                    {submitted ? (
                        <Typography align="center" sx={{marginY: 2}}>
                            If an account with that email exists, a password reset link has been sent.
                        </Typography>
                    ) : (
                        <TextField
                            fullWidth
                            label="Email"
                            type="email"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            required
                            margin="normal"
                        />
                    )}

                    {!submitted && (
                        <Button type="submit" variant="contained" color="primary" fullWidth sx={{marginTop: '24px', padding: '10px 0'}}>
                            Send reset link
                        </Button>
                    )}
                    <Box sx={{display: 'flex', justifyContent: 'center', marginTop: '20px'}}>
                        <Button
                            onClick={() => navigate(sharedProfilePath ?? '/')}
                            variant="text"
                            color="info"
                            size="small"
                            sx={{textTransform: 'none', minWidth: 0}}
                        >
                            Back to log in
                        </Button>
                    </Box>
                </Box>
            </Box>
        </ThemeProvider>
    );
}
