import React, {useState} from "react";
import {useNavigate} from "react-router-dom";
import {requestPasswordReset} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {Box, Button, TextField, ThemeProvider, Typography} from "@mui/material";
import {designTokens, muiDarkTheme} from "../../styles/muiDarkTheme";

export function ForgotPasswordForm() {
    const [email, setEmail] = useState('');
    const [submitted, setSubmitted] = useState(false);
    const navigate = useNavigate();

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
                        Forgot password
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

                    <Box sx={{display: 'flex', justifyContent: 'space-between', marginTop: '24px', gap: 1}}>
                        {!submitted && (
                            <Button type="submit" variant="contained" color="primary" fullWidth>
                                Send reset link
                            </Button>
                        )}
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
