import {useState} from "react";
import {useNavigate} from "react-router-dom";
import './styles.css';
import '../../styles/auth.css';
import {getMyAccount, registerAccount} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {setAuthToken, setUserId} from "../../hooks/authentication";
import {Box, Button, TextField, Typography} from "@mui/material";

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
        <>
            <Box className="auth-page">
                <Box
                    component="form"
                    onSubmit={handleSubmit}
                    className="auth-panel"
                >
                    <Typography className="auth-panel__title" variant="h4" align="center" gutterBottom>
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

                    <Box className="auth-panel__actions">
                        <Button
                            type="submit"
                            variant="contained"
                            color="primary"
                            fullWidth
                            sx={{padding: '10px 0'}}
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
        </>
    );
};

export default LoginForm;