import React, {useMemo, useState} from "react";
import {useNavigate, useSearchParams} from "react-router-dom";
import {completePasswordReset} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {Box, Button, TextField, ThemeProvider, Typography} from "@mui/material";
import {designTokens, muiDarkTheme} from "../../styles/muiDarkTheme";

export function ResetPasswordForm() {
    const [searchParams] = useSearchParams();
    const token = useMemo(() => searchParams.get('token') ?? '', [searchParams]);
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const navigate = useNavigate();

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!token) {
            mantineErrorNotification('Reset token is missing');
            return;
        }

        if (password.length < 8) {
            mantineErrorNotification('Password must be at least 8 characters');
            return;
        }

        if (password !== confirmPassword) {
            mantineErrorNotification('Passwords do not match');
            return;
        }

        try {
            await completePasswordReset(token, password);
            mantineSuccessNotification('Password reset successfully');
            navigate('/');
        } catch (error: any) {
            mantineErrorNotification(error?.message || 'Failed to reset password');
            console.error(error);
        }
    };

    return (
        <ThemeProvider theme={muiDarkTheme}>
            <Box
                sx={{
                    minHeight: '100vh',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    background: `linear-gradient(135deg, ${designTokens.authGradientStart}, ${designTokens.authGradientEnd})`,
                }}
            >
                <Box
                    component="form"
                    onSubmit={handleSubmit}
                    sx={{
                        width: '100%',
                        maxWidth: '450px',
                        padding: '40px',
                        borderRadius: '12px',
                        boxShadow: '0 4px 20px rgba(0, 0, 0, 0.7)',
                        backgroundColor: 'background.paper',
                        color: 'text.primary',
                    }}
                >
                    <Typography variant="h4" align="center" gutterBottom>
                        Reset password
                    </Typography>

                    {!token && (
                        <Typography align="center" color="error" sx={{marginBottom: 2}}>
                            This reset link is invalid or incomplete.
                        </Typography>
                    )}

                    <TextField
                        fullWidth
                        label="New password"
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                        margin="normal"
                        disabled={!token}
                    />
                    <TextField
                        fullWidth
                        label="Confirm password"
                        type="password"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        required
                        margin="normal"
                        disabled={!token}
                    />

                    <Box sx={{display: 'flex', justifyContent: 'space-between', marginTop: '24px', gap: 1}}>
                        <Button
                            type="submit"
                            variant="contained"
                            color="primary"
                            fullWidth
                            disabled={!token}
                        >
                            Reset password
                        </Button>
                        <Button
                            onClick={() => navigate('/')}
                            variant="outlined"
                            color="info"
                            fullWidth
                        >
                            Back to login
                        </Button>
                    </Box>
                </Box>
            </Box>
        </ThemeProvider>
    );
}
