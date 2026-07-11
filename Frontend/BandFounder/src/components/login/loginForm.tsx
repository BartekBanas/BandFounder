import React, {useEffect, useState} from "react";
import {useNavigate} from "react-router-dom";
import './styles.css';
import '../../styles/auth.css';
import {getMyAccount, login} from "../../api/account";
import {setAuthToken, setUserId} from "../../hooks/authentication";
import {mantineErrorNotification, mantineInformationNotification} from "../common/mantineNotification";
import {Box, Button, TextField, Typography} from "@mui/material";

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
        } catch (error: any) {
            const status = error?.response?.status ?? error?.status;
            const message = error?.responseText ?? error?.message ?? '';

            if (status === 403) {
                mantineErrorNotification("Incorrect login details");
                return;
            }

            if (status === 429) {
                const retryAfter = error?.retryAfter;
                let retryMsg = '';
                if (retryAfter) {
                    const seconds = parseInt(String(retryAfter), 10);
                    if (!isNaN(seconds)) {
                        retryMsg = ` Try again in ${seconds} seconds.`;
                    } else {
                        retryMsg = ` Retry after: ${retryAfter}.`;
                    }
                }

                mantineErrorNotification("Too many login attempts. Please try again later");
                return;
            }

            // Unexpected error
            mantineErrorNotification("Login failed");
            console.error(error);
        }
    };

    const handleRegisterClick = () => {
        navigate('/register');
    };

    return (
        <>
            <Box className="auth-page">
                <Box
                    component="form"
                    onSubmit={handleSubmit}
                    className="auth-panel"
                >
                    <Typography className="auth-panel__title" variant="h4" align="center" gutterBottom>
                        Login
                    </Typography>

                    <TextField
                        fullWidth
                        label="Email or username"
                        type="text"
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

                    <Box className="auth-panel__actions">
                        <Button
                            type="submit"
                            variant="contained"
                            color="primary"
                            fullWidth
                            sx={{padding: '10px 0'}}
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
        </>
    );
}