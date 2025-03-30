import {useState} from "react";
import {useNavigate} from "react-router-dom";
import './styles.css';
import {getMyAccount, registerAccount} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {setAuthToken, setUserId} from "../../hooks/authentication";
import {Box, Button, TextField, ThemeProvider, Typography} from "@mui/material";
import {muiDarkTheme} from "../../styles/muiDarkTheme";

export function LoginForm() {
    const [name, setName] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');

    const navigate = useNavigate();

    async function handleSubmit(e: any) {
        e.preventDefault();
        try {
            const authorizationToken = await registerAccount(name, email, password);
            setAuthToken(authorizationToken);

            const account = await getMyAccount();
            setUserId(account.id);

            mantineSuccessNotification('Account created successfully');

            navigate('/home');
        } catch (e: any) {
            if (e instanceof Error) {
                if (e.message === "Failed to fetch") {
                    mantineErrorNotification('Failed to connect to the server');
                } else {
                    mantineErrorNotification(`${e.message}`);
                }
            } else {
                mantineErrorNotification('Registration failed due to unexpected error');
            }
            console.error(e);
        }
    }

    async function handleLoginClick(e: any) {
        e.preventDefault();
        navigate('/login');
    }

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
                        Register
                    </Typography>

                    <TextField
                        fullWidth
                        label="Username"
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                        required
                        margin="normal"
                    />
                    <TextField
                        fullWidth
                        label="Email"
                        type="email"
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
                            Register
                        </Button>
                        <Button
                            onClick={handleLoginClick}
                            variant="outlined"
                            color="info"
                            fullWidth
                            sx={{padding: '10px 0'}}
                        >
                            Login
                        </Button>
                    </Box>
                </Box>
            </Box>
        </ThemeProvider>
    );
};

export default LoginForm;