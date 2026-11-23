import {useState} from "react";
import {useNavigate} from "react-router-dom";
import './styles.css';
import {getMyAccount, registerAccount} from "../../api/account";
import {mantineErrorNotification, mantineSuccessNotification} from "../common/mantineNotification";
import {setAuthToken, setUserId} from "../../hooks/authentication";
import {Box, Button, TextField, ThemeProvider, Typography} from "@mui/material";
import {muiDarkTheme} from "../../styles/muiDarkTheme";
import {authCardSx, authPageSx} from "../login/authStyles";
import {AuthBrand} from "../login/AuthBrand";
import {useSharedProfilePath} from "../login/useSharedProfilePath";

export function LoginForm() {
    const [name, setName] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');

    const navigate = useNavigate();
    const sharedProfilePath = useSharedProfilePath();

    async function handleSubmit(e: any) {
        e.preventDefault();
        try {
            const authorizationToken = await registerAccount(name, email, password);
            setAuthToken(authorizationToken);

            const account = await getMyAccount();
            setUserId(account.id);

            mantineSuccessNotification('Account created successfully');

            navigate(sharedProfilePath ?? '/home', {replace: Boolean(sharedProfilePath)});
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
        navigate('/login', {state: {sharedProfilePath}});
    }

    return (
        <ThemeProvider theme={muiDarkTheme}>
            <Box sx={authPageSx}>
                <AuthBrand/>

                <Box component="form" onSubmit={handleSubmit} sx={authCardSx}>
                    <Typography variant="h5" fontWeight={600} sx={{marginBottom: '16px'}}>
                        Create an account
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

                    <Button
                        type="submit"
                        variant="contained"
                        color="primary"
                        fullWidth
                        sx={{marginTop: '24px', padding: '10px 0'}}
                    >
                        Create account
                    </Button>

                    <Box sx={{display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '4px', marginTop: '20px'}}>
                        <Typography variant="body2" color="text.secondary">
                            Already have an account?
                        </Typography>
                        <Button
                            onClick={handleLoginClick}
                            variant="text"
                            color="info"
                            size="small"
                            sx={{textTransform: 'none', minWidth: 0}}
                        >
                            Log in
                        </Button>
                    </Box>
                </Box>
            </Box>
        </ThemeProvider>
    );
};

export default LoginForm;
