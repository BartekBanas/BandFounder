import React, {useEffect, useState} from "react";
import {useNavigate} from "react-router-dom";
import './styles.css';
import {getMyAccount, login} from "../../api/account";
import {setAuthToken, setUserId} from "../../hooks/authentication";
import {mantineErrorNotification, mantineInformationNotification} from "../common/mantineNotification";
import {Box, Button, TextField, ThemeProvider, Typography} from "@mui/material";
import {muiDarkTheme} from "../../styles/muiDarkTheme";

export async function performLogin(usernameOrEmail: string, password: string) {
    const authorizationToken = await login(usernameOrEmail, password);
    setAuthToken(authorizationToken);

    const account = await getMyAccount();
    setUserId(account.id);

    mantineInformationNotification(`Welcome ${account.name}`);
}

export function LoginForm() {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');

    const navigate = useNavigate();

    useEffect(() => {
        if (window.location.pathname === '/login/expiredSession') {
            mantineErrorNotification('Your session has expired. Please log in again.');
        }
    }, []);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            await performLogin(email, password);
            navigate('/home');
        } catch (err: any) {
            console.log(JSON.stringify(err));

            const status = err?.response?.status ?? err?.status;
            const message = err?.responseText ?? err?.message ?? '';

            console.error('Login error details', {status, message, name: err?.name, stack: err?.stack, rawError: err});

            if (status === 429) {
                const retryAfter = err?.retryAfter;
                let retryMsg = '';
                if (retryAfter) {
                    const seconds = parseInt(String(retryAfter), 10);
                    if (!isNaN(seconds)) {
                        retryMsg = ` Try again in ${seconds} seconds.`;
                    } else {
                        retryMsg = ` Retry after: ${retryAfter}.`;
                    }
                }

                mantineErrorNotification("Too many login attempts. Please wait a moment and try again." + retryMsg);
                return;
            }

            // Fall back to server-provided message if available, otherwise a generic message
            mantineErrorNotification(message || "Login failed");
            console.error(err);
        }
    };

    const handleRegisterClick = () => {
        navigate('/register');
    };

    return (
        <ThemeProvider theme={muiDarkTheme}>
            <Box
                sx={{
                    minHeight: '100vh',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    background: 'linear-gradient(135deg, #1c1c1c, #2e2e2e)',
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
                        Login
                    </Typography>

                    <TextField
                        fullWidth
                        label="Email or username"
                        type="username"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                        margin="normal"
                    />
                    <TextField
                        fullWidth
                        label="Password"
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                        margin="normal"
                    />

                    <Box sx={{display: 'flex', justifyContent: 'space-between', marginTop: '24px'}}>
                        <Button
                            type="submit"
                            variant="contained"
                            color="primary"
                            fullWidth
                            sx={{marginRight: '8px', padding: '10px 0'}}
                        >
                            Login
                        </Button>
                        <Button
                            onClick={handleRegisterClick}
                            variant="outlined"
                            color="info"
                            fullWidth
                            sx={{padding: '10px 0'}}
                        >
                            Register
                        </Button>
                    </Box>
                </Box>
            </Box>
        </ThemeProvider>
    );
}