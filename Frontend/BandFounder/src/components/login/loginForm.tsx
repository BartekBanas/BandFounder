import React, {useEffect, useState} from "react";
import {useNavigate} from "react-router-dom";
import './styles.css';
import {getMyAccount, login} from "../../api/account";
import {setAuthToken, setUserId} from "../../hooks/authentication";
import {mantineErrorNotification, mantineInformationNotification} from "../common/mantineNotification";
import {Box, Button, TextField, ThemeProvider, Typography} from "@mui/material";
import {muiDarkTheme} from "../../assets/muiDarkTheme";

export const useLoginApi = () => {
    return async (usernameOrEmail: string, password: string) => {
        const authorizationToken = await login(usernameOrEmail, password);
        setAuthToken(authorizationToken);

        const account = await getMyAccount();
        setUserId(account.id);

        mantineInformationNotification(`Welcome ${account.name}`);
    };
};

export function LoginForm() {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');

    const navigate = useNavigate();
    const login = useLoginApi();

    useEffect(() => {
        if (window.location.pathname === '/login/expiredSession') {
            mantineErrorNotification('Your session has expired. Please log in again.');
        }
    }, []);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            await login(email, password);
            navigate('/home');
        } catch (e: any) {
            mantineErrorNotification("Login failed");
            console.error(e);
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